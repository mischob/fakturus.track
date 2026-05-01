import SwiftData
import Foundation

actor SyncEngine {
    private let modelContainer: ModelContainer
    nonisolated(unsafe) private var apiClient: APIClient!
    nonisolated(unsafe) private var networkMonitor: NetworkMonitor!

    private(set) var isSyncing = false
    private(set) var lastSyncDate: Date?
    private(set) var lastError: String?

    init(modelContainer: ModelContainer) {
        self.modelContainer = modelContainer
    }

    func configure(apiClient: APIClient, networkMonitor: NetworkMonitor) {
        self.apiClient = apiClient
        self.networkMonitor = networkMonitor
    }

    // MARK: - Public

    func syncAll() async {
        guard !isSyncing else {
            print("[SyncEngine] Already syncing, skipping")
            return
        }
        guard apiClient != nil else {
            print("[SyncEngine] No apiClient configured, skipping")
            return
        }
        let isConnected = await MainActor.run { networkMonitor?.isConnected ?? false }
        guard isConnected else {
            print("[SyncEngine] Offline, skipping sync")
            return
        }

        print("[SyncEngine] Starting sync...")
        isSyncing = true
        lastError = nil
        defer { isSyncing = false }

        do {
            print("[SyncEngine] Syncing work sessions...")
            try await syncWorkSessions()
            print("[SyncEngine] Syncing vacation days...")
            try await syncVacationDays()
            print("[SyncEngine] Syncing sick days...")
            do {
                try await syncSickDays()
            } catch APIError.notFound {
                print("[SyncEngine] Sick days endpoint not available (404), skipping")
            } catch APIError.serverError(let code) {
                print("[SyncEngine] Sick days server error (\(code)), skipping")
            }
            print("[SyncEngine] Syncing user settings...")
            try await syncUserSettings()
            lastSyncDate = Date()
            print("[SyncEngine] Sync completed successfully")
        } catch {
            lastError = error.localizedDescription
            print("[SyncEngine] Sync failed: \(error)")
        }
    }

    // MARK: - WorkSessions

    private func syncWorkSessions() async throws {
        // Fresh context each sync to avoid stale cached objects
        let ctx = ModelContext(modelContainer)

        // Step 1: Process pending deletes first
        let pendingDeletes = try ctx.fetch(
            FetchDescriptor<PendingDelete>(predicate: #Predicate {
                $0.entityType == "WorkSession"
            })
        )

        for pendingDelete in pendingDeletes {
            do {
                try await apiClient.deleteWorkSession(id: pendingDelete.entityId.uuidString)
                ctx.delete(pendingDelete)
            } catch APIError.notFound {
                // Already deleted on server, remove local tracking
                ctx.delete(pendingDelete)
            }
        }

        let pendingDeleteIds = Set(pendingDeletes.map(\.entityId))

        // Step 2: Collect pending sessions
        let pending = try ctx.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate {
                $0.isPendingSync && $0.isFinished
            })
        )

        // Step 3: Upload or fetch
        let serverSessions: [WorkSessionDTO]
        if !pending.isEmpty {
            let request = SyncWorkSessionsRequest(
                workSessions: pending.map { $0.toDTO() }
            )
            serverSessions = try await apiClient.syncWorkSessions(request)
        } else {
            serverSessions = try await apiClient.getWorkSessions()
        }

        // Step 4: Load synced sessions
        let synced = try ctx.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate { $0.isSynced })
        )

        // Step 5: Server ID set
        let serverIds = Set(serverSessions.map(\.id))

        // Step 6: Set difference -- delete local synced that are no longer on server
        for local in synced where !serverIds.contains(local.id.uuidString) {
            ctx.delete(local)
        }

        // Step 7: Upsert server sessions (skip those with pending deletes)
        let pendingIds = Set(pending.map(\.id))
        for dto in serverSessions {
            let uuid = UUID(uuidString: dto.id) ?? UUID()

            // Skip if we just deleted this locally
            if pendingDeleteIds.contains(uuid) { continue }

            let descriptor = FetchDescriptor<WorkSession>(
                predicate: #Predicate { session in session.id == uuid }
            )
            if let existing = try ctx.fetch(descriptor).first {
                // Don't overwrite sessions with pending local changes
                if !existing.isPendingSync && !pendingIds.contains(existing.id) {
                    let beforeStop = existing.stopTime
                    existing.update(from: dto)
                    if beforeStop != nil && existing.stopTime == nil {
                        print("[SyncEngine] WARNING: stopTime cleared by server for session \(dto.id), dto.stopTime=\(String(describing: dto.stopTime))")
                    }
                }
            } else {
                ctx.insert(WorkSession(from: dto))
            }
        }

        // Step 8: Mark pending as synced
        for session in pending {
            session.isPendingSync = false
            session.isSynced = true
            session.syncedAt = Date()
        }

        if !pending.isEmpty {
            print("[SyncEngine] Uploaded \(pending.count) work session(s); first stopTime sent=\(String(describing: pending.first?.stopTime))")
        }

        try ctx.save()
    }

    // MARK: - VacationDays

    private func syncVacationDays() async throws {
        let ctx = ModelContext(modelContainer)

        let allLocal = try ctx.fetch(FetchDescriptor<VacationDay>())
        let pending = allLocal.filter { $0.isPendingSync }

        // Optimization: only POST when local changes exist, otherwise GET suffices
        let serverVacationDays: [VacationDayDTO]
        var deletedIds: [String] = []

        if !pending.isEmpty {
            // Pending exists -> send ALL local days (not just pending!)
            let request = SyncVacationDaysRequest(
                vacationDays: allLocal.map { $0.toDTO() }
            )
            let response = try await apiClient.syncVacationDays(request)
            serverVacationDays = response.serverVacationDays
            deletedIds = response.deletedIds
        } else {
            // No pending -> simple GET suffices
            serverVacationDays = try await apiClient.getVacationDays()
        }

        // Process deleted IDs
        for deletedId in deletedIds {
            let uuid = UUID(uuidString: deletedId) ?? UUID()
            let descriptor = FetchDescriptor<VacationDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let toDelete = try ctx.fetch(descriptor).first {
                ctx.delete(toDelete)
            }
        }

        // Upsert server vacation days
        for dto in serverVacationDays {
            let uuid = UUID(uuidString: dto.id) ?? UUID()
            let descriptor = FetchDescriptor<VacationDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let existing = try ctx.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                ctx.insert(VacationDay(from: dto))
            }
        }

        // Mark all as synced
        for day in allLocal {
            day.isPendingSync = false
            day.isSynced = true
            day.syncedAt = Date()
        }

        try ctx.save()
    }

    // MARK: - SickDays

    private func syncSickDays() async throws {
        let ctx = ModelContext(modelContainer)

        let allLocal = try ctx.fetch(FetchDescriptor<SickDay>())
        let pending = allLocal.filter { $0.isPendingSync }

        let serverSickDays: [SickDayDTO]
        var deletedIds: [String] = []

        if !pending.isEmpty {
            // Pending exists -> send ALL local days (not just pending!)
            let request = SyncSickDaysRequest(
                sickDays: allLocal.map { $0.toDTO() }
            )
            let response = try await apiClient.syncSickDays(request)
            serverSickDays = response.serverSickDays
            deletedIds = response.deletedIds
        } else {
            // No pending -> simple GET suffices
            serverSickDays = try await apiClient.getSickDays()
        }

        // Process deleted IDs
        for deletedId in deletedIds {
            let uuid = UUID(uuidString: deletedId) ?? UUID()
            let descriptor = FetchDescriptor<SickDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let toDelete = try ctx.fetch(descriptor).first {
                ctx.delete(toDelete)
            }
        }

        // Set-difference: delete local synced that are no longer on server
        let serverIds = Set(serverSickDays.map(\.id))
        let localSynced = allLocal.filter { $0.isSynced }
        for local in localSynced where !serverIds.contains(local.id.uuidString) {
            ctx.delete(local)
        }

        // Upsert server sick days
        for dto in serverSickDays {
            let uuid = UUID(uuidString: dto.id) ?? UUID()
            let descriptor = FetchDescriptor<SickDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let existing = try ctx.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                ctx.insert(SickDay(from: dto))
            }
        }

        // Mark all as synced
        for day in allLocal {
            day.isPendingSync = false
            day.isSynced = true
            day.syncedAt = Date()
        }

        try ctx.save()
    }

    // MARK: - UserSettings (Last-Write-Wins)

    private func syncUserSettings() async throws {
        let ctx = ModelContext(modelContainer)

        let serverSettings = try await apiClient.getUserSettings()
        let localSettings = try ctx.fetch(FetchDescriptor<UserSettings>()).first

        if let local = localSettings {
            let localUpdatedAt = local.updatedAt
            let serverUpdatedAt: Date? = serverSettings.updatedAt.flatMap { ISO8601DateFormatter().date(from: $0) }

            if let localDate = localUpdatedAt,
               (serverUpdatedAt == nil || localDate > serverUpdatedAt!) {
                // Local is newer -> upload
                try await apiClient.updateUserSettings(local.toDTO())
                local.isSynced = true
                local.isPendingSync = false
            } else if let serverDate = serverUpdatedAt,
                      localUpdatedAt == nil || serverDate > localUpdatedAt! {
                // Server is newer -> overwrite local
                local.vacationDaysPerYear = serverSettings.vacationDaysPerYear
                local.workHoursPerWeek = serverSettings.workHoursPerWeek
                local.workDays = serverSettings.workDays
                local.bundesland = serverSettings.bundesland
                local.calendarUrl = serverSettings.calendarUrl
                local.updatedAt = serverUpdatedAt
                local.isSynced = true
                local.isPendingSync = false
            } else if serverUpdatedAt == nil && localUpdatedAt == nil {
                // No updatedAt on either side -> server-wins (fallback)
                local.vacationDaysPerYear = serverSettings.vacationDaysPerYear
                local.workHoursPerWeek = serverSettings.workHoursPerWeek
                local.workDays = serverSettings.workDays
                local.bundesland = serverSettings.bundesland
                local.calendarUrl = serverSettings.calendarUrl
                local.isSynced = true
                local.isPendingSync = false
            }
            // Equal -> do nothing
        } else {
            let settings = UserSettings(userId: "")
            settings.vacationDaysPerYear = serverSettings.vacationDaysPerYear
            settings.workHoursPerWeek = serverSettings.workHoursPerWeek
            settings.workDays = serverSettings.workDays
            settings.bundesland = serverSettings.bundesland
            settings.calendarUrl = serverSettings.calendarUrl
            settings.isSynced = true
            settings.isPendingSync = false
            ctx.insert(settings)
        }

        try ctx.save()
    }
}
