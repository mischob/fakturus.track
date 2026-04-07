import SwiftData
import Foundation

@Model
final class UserSettings {
    @Attribute(.unique) var userId: String
    var calendarUrl: String?
    var vacationDaysPerYear: Int
    var workHoursPerWeek: Double
    var workDays: Int
    var bundesland: String
    var updatedAt: Date?
    var isSynced: Bool
    var isPendingSync: Bool
    var personalNumber: String?

    init(
        userId: String = "",
        calendarUrl: String? = nil,
        vacationDaysPerYear: Int = 30,
        workHoursPerWeek: Double = 40.0,
        workDays: Int = 31,
        bundesland: String = "NW",
        isSynced: Bool = false,
        isPendingSync: Bool = true,
        personalNumber: String? = nil
    ) {
        self.userId = userId
        self.calendarUrl = calendarUrl
        self.vacationDaysPerYear = vacationDaysPerYear
        self.workHoursPerWeek = workHoursPerWeek
        self.workDays = workDays
        self.bundesland = bundesland
        self.isSynced = isSynced
        self.isPendingSync = isPendingSync
        self.personalNumber = personalNumber
    }

    // MARK: - DTO Conversion

    func toDTO() -> UserSettingsDTO {
        UserSettingsDTO(
            calendarUrl: calendarUrl,
            vacationDaysPerYear: vacationDaysPerYear,
            workHoursPerWeek: workHoursPerWeek,
            workDays: workDays,
            bundesland: bundesland,
            updatedAt: updatedAt?.ISO8601Format()
        )
    }

    func update(from dto: UserSettingsDTO) {
        calendarUrl = dto.calendarUrl
        vacationDaysPerYear = dto.vacationDaysPerYear
        workHoursPerWeek = dto.workHoursPerWeek
        workDays = dto.workDays
        bundesland = dto.bundesland
        isSynced = true
        isPendingSync = false
    }
}
