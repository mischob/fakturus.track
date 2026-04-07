import Foundation

// MARK: - Sick Day DTOs

struct SyncSickDaysRequest: Encodable {
    let sickDays: [SickDaySyncItem]
}

struct SickDaySyncItem: Codable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct SickDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct SyncSickDaysResponse: Decodable {
    let serverSickDays: [SickDayDTO]
    let deletedIds: [String]
}

// MARK: - Request Types

struct SyncWorkSessionsRequest: Encodable {
    let workSessions: [WorkSessionSyncItem]
}

struct WorkSessionSyncItem: Encodable {
    let id: String
    let date: String
    let startTime: String
    let stopTime: String?
    let pauseMinutes: Int
}

struct SyncVacationDaysRequest: Encodable {
    let vacationDays: [VacationDaySyncItem]
}

struct VacationDaySyncItem: Encodable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
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
}

struct VacationDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct SyncVacationDaysResponse: Decodable {
    let serverVacationDays: [VacationDayDTO]
    let deletedIds: [String]
}

struct UserSettingsDTO: Codable {
    let calendarUrl: String?
    let vacationDaysPerYear: Int
    let workHoursPerWeek: Double
    let workDays: Int
    let bundesland: String
    let updatedAt: String?
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
}

struct MonthlyOvertimeDTO: Codable {
    let year: Int
    let month: Int
    let monthName: String
    let overtimeHours: Double
    let workedHours: Double
    let expectedHours: Double
    let sickDays: Int?
}
