import Foundation

extension APIClient {
    // MARK: - Work Sessions

    func getWorkSessions() async throws -> [WorkSessionDTO] {
        try await get("/v1/work-sessions")
    }

    func syncWorkSessions(_ request: SyncWorkSessionsRequest) async throws -> [WorkSessionDTO] {
        try await post("/v1/work-sessions/sync", body: request)
    }

    func deleteWorkSession(id: String) async throws {
        try await delete("/v1/work-sessions/\(id)")
    }

    // MARK: - Vacation Days

    func syncVacationDays(_ request: SyncVacationDaysRequest) async throws -> SyncVacationDaysResponse {
        try await post("/v1/vacation-days/sync", body: request)
    }

    func getVacationDays() async throws -> [VacationDayDTO] {
        try await get("/v1/vacation-days")
    }

    // MARK: - Settings

    func getUserSettings() async throws -> UserSettingsDTO {
        try await get("/v1/settings")
    }

    func updateUserSettings(_ settings: UserSettingsDTO) async throws {
        try await put("/v1/settings", body: settings)
    }
}
