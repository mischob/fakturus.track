import Foundation

// MARK: - Sick Day DTOs (Placeholder for E04)

struct SyncSickDaysRequest: Encodable {
    let sickDays: [SickDaySyncItem]

    enum CodingKeys: String, CodingKey {
        case sickDays = "SickDays"
    }
}

struct SickDaySyncItem: Codable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case date = "Date"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

struct SickDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case userId = "UserId"
        case date = "Date"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

struct SyncSickDaysResponse: Decodable {
    let serverSickDays: [SickDayDTO]
    let deletedIds: [String]

    enum CodingKeys: String, CodingKey {
        case serverSickDays = "ServerSickDays"
        case deletedIds = "DeletedIds"
    }
}

// MARK: - Request Types

struct SyncWorkSessionsRequest: Encodable {
    let workSessions: [WorkSessionSyncItem]

    enum CodingKeys: String, CodingKey {
        case workSessions = "WorkSessions"
    }
}

struct WorkSessionSyncItem: Encodable {
    let id: String
    let date: String
    let startTime: String
    let stopTime: String?
    let pauseMinutes: Int

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case date = "Date"
        case startTime = "StartTime"
        case stopTime = "StopTime"
        case pauseMinutes = "PauseMinutes"
    }
}

struct SyncVacationDaysRequest: Encodable {
    let vacationDays: [VacationDaySyncItem]

    enum CodingKeys: String, CodingKey {
        case vacationDays = "VacationDays"
    }
}

struct VacationDaySyncItem: Encodable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case date = "Date"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

// MARK: - Response Types

struct WorkSessionDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let startTime: String
    let stopTime: String?
    let pauseMinutes: Int?
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case userId = "UserId"
        case date = "Date"
        case startTime = "StartTime"
        case stopTime = "StopTime"
        case pauseMinutes = "PauseMinutes"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

struct VacationDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case userId = "UserId"
        case date = "Date"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

struct SyncVacationDaysResponse: Decodable {
    let serverVacationDays: [VacationDayDTO]
    let deletedIds: [String]

    enum CodingKeys: String, CodingKey {
        case serverVacationDays = "ServerVacationDays"
        case deletedIds = "DeletedIds"
    }
}

struct UserSettingsDTO: Codable {
    let calendarUrl: String?
    let vacationDaysPerYear: Int
    let workHoursPerWeek: Double
    let workDays: Int
    let bundesland: String
    let updatedAt: String?

    enum CodingKeys: String, CodingKey {
        case calendarUrl = "CalendarUrl"
        case vacationDaysPerYear = "VacationDaysPerYear"
        case workHoursPerWeek = "WorkHoursPerWeek"
        case workDays = "WorkDays"
        case bundesland = "Bundesland"
        case updatedAt = "UpdatedAt"
    }
}

struct OvertimeSummaryDTO: Codable {
    let totalOvertimeHours: Double
    let monthlyOvertime: [MonthlyOvertimeDTO]
    let vacationDaysTaken: Int
    let vacationDaysRemaining: Int
    let vacationDaysPerYear: Int
    let holidaysTaken: Int
    let schoolHolidayHoursNotWorked: Double
    let sickDaysTaken: Int?

    enum CodingKeys: String, CodingKey {
        case totalOvertimeHours = "TotalOvertimeHours"
        case monthlyOvertime = "MonthlyOvertime"
        case vacationDaysTaken = "VacationDaysTaken"
        case vacationDaysRemaining = "VacationDaysRemaining"
        case vacationDaysPerYear = "VacationDaysPerYear"
        case holidaysTaken = "HolidaysTaken"
        case schoolHolidayHoursNotWorked = "SchoolHolidayHoursNotWorked"
        case sickDaysTaken = "SickDaysTaken"
    }
}

struct MonthlyOvertimeDTO: Codable {
    let year: Int
    let month: Int
    let monthName: String
    let overtimeHours: Double
    let workedHours: Double
    let expectedHours: Double
    let sickDays: Int?

    enum CodingKeys: String, CodingKey {
        case year = "Year"
        case month = "Month"
        case monthName = "MonthName"
        case overtimeHours = "OvertimeHours"
        case workedHours = "WorkedHours"
        case expectedHours = "ExpectedHours"
        case sickDays = "SickDays"
    }
}
