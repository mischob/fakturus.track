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
        guard !isSyncing else { return }
        let isConnected = await MainActor.run { networkMonitor.isConnected }
        guard isConnected else { return }

        isSyncing = true
        lastError = nil
        defer { isSyncing = false }

        do {
            try await syncWorkSessions()
            try await syncVacationDays()
            try await syncUserSettings()
            lastSyncDate = Date()
        } catch {
            lastError = error.localizedDescription
            #if DEBUG
            print("[SyncEngine] Error: \(error)")
            #endif
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

    // MARK: - UserSettings (Server-wins in Phase 1)

    private func syncUserSettings() async throws {
        let serverSettings = try await apiClient.getUserSettings()
        let localSettings = try modelContext.fetch(FetchDescriptor<UserSettings>()).first

        if let local = localSettings {
            local.vacationDaysPerYear = serverSettings.vacationDaysPerYear
            local.workHoursPerWeek = serverSettings.workHoursPerWeek
            local.workDays = serverSettings.workDays
            local.bundesland = serverSettings.bundesland
            local.calendarUrl = serverSettings.calendarUrl
            local.isSynced = true
            local.isPendingSync = false
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
