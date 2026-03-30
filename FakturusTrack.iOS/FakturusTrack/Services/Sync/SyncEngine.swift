import SwiftData
import Foundation

@ModelActor
actor SyncEngine {
    nonisolated(unsafe) private var apiClient: APIClient!
    nonisolated(unsafe) private var networkMonitor: NetworkMonitor!

    private(set) var isSyncing = false
    private(set) var lastSyncDate: Date?
    private(set) var lastError: String?

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
        // Step 1: Process pending deletes first
        let pendingDeletes = try modelContext.fetch(
            FetchDescriptor<PendingDelete>(predicate: #Predicate {
                $0.entityType == "WorkSession"
            })
        )

        for pendingDelete in pendingDeletes {
            do {
                try await apiClient.deleteWorkSession(id: pendingDelete.entityId.uuidString)
                modelContext.delete(pendingDelete)
            } catch APIError.notFound {
                // Already deleted on server, remove local tracking
                modelContext.delete(pendingDelete)
            }
        }

        let pendingDeleteIds = Set(pendingDeletes.map(\.entityId))

        // Step 2: Collect pending sessions
        let pending = try modelContext.fetch(
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
        let synced = try modelContext.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate { $0.isSynced })
        )

        // Step 5: Server ID set
        let serverIds = Set(serverSessions.map(\.id))

        // Step 6: Set difference -- delete local synced that are no longer on server
        for local in synced where !serverIds.contains(local.id.uuidString) {
            modelContext.delete(local)
        }

        // Step 7: Upsert server sessions (skip those with pending deletes)
        for dto in serverSessions {
            let uuid = UUID(uuidString: dto.id) ?? UUID()

            // Skip if we just deleted this locally
            if pendingDeleteIds.contains(uuid) { continue }

            let descriptor = FetchDescriptor<WorkSession>(
                predicate: #Predicate { session in session.id == uuid }
            )
            if let existing = try modelContext.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(WorkSession(from: dto))
            }
        }

        // Step 8: Mark pending as synced
        for session in pending {
            session.isPendingSync = false
            session.isSynced = true
            session.syncedAt = Date()
        }

        try modelContext.save()
    }

    // MARK: - VacationDays

    private func syncVacationDays() async throws {
        let allLocal = try modelContext.fetch(FetchDescriptor<VacationDay>())
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
            if let toDelete = try modelContext.fetch(descriptor).first {
                modelContext.delete(toDelete)
            }
        }

        // Upsert server vacation days
        for dto in serverVacationDays {
            let uuid = UUID(uuidString: dto.id) ?? UUID()
            let descriptor = FetchDescriptor<VacationDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let existing = try modelContext.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(VacationDay(from: dto))
            }
        }

        // Mark all as synced
        for day in allLocal {
            day.isPendingSync = false
            day.isSynced = true
            day.syncedAt = Date()
        }

        try modelContext.save()
    }

    // MARK: - SickDays

    private func syncSickDays() async throws {
        let allLocal = try modelContext.fetch(FetchDescriptor<SickDay>())
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
            if let toDelete = try modelContext.fetch(descriptor).first {
                modelContext.delete(toDelete)
            }
        }

        // Set-difference: delete local synced that are no longer on server
        let serverIds = Set(serverSickDays.map(\.id))
        let localSynced = allLocal.filter { $0.isSynced }
        for local in localSynced where !serverIds.contains(local.id.uuidString) {
            modelContext.delete(local)
        }

        // Upsert server sick days
        for dto in serverSickDays {
            let uuid = UUID(uuidString: dto.id) ?? UUID()
            let descriptor = FetchDescriptor<SickDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let existing = try modelContext.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(SickDay(from: dto))
            }
        }

        // Mark all as synced
        for day in allLocal {
            day.isPendingSync = false
            day.isSynced = true
            day.syncedAt = Date()
        }

        try modelContext.save()
    }

    // MARK: - UserSettings (Last-Write-Wins)

    private func syncUserSettings() async throws {
        let serverSettings = try await apiClient.getUserSettings()
        let localSettings = try modelContext.fetch(FetchDescriptor<UserSettings>()).first

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
            modelContext.insert(settings)
        }

        try modelContext.save()
    }
}
