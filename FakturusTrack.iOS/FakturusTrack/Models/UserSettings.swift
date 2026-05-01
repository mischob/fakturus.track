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

    /// Date on which the next pending change to `workDays` / `workHoursPerWeek`
    /// should take effect on the server. Cleared once the change has synced.
    /// nil means "today" on the server side.
    var pendingEffectiveDate: Date?

    init(
        userId: String = "",
        calendarUrl: String? = nil,
        vacationDaysPerYear: Int = 30,
        workHoursPerWeek: Double = 40.0,
        workDays: Int = 31,
        bundesland: String = "NW",
        isSynced: Bool = false,
        isPendingSync: Bool = true,
        personalNumber: String? = nil,
        pendingEffectiveDate: Date? = nil
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
        self.pendingEffectiveDate = pendingEffectiveDate
    }

    // MARK: - DTO Conversion

    func toDTO() -> UserSettingsDTO {
        UserSettingsDTO(
            calendarUrl: calendarUrl,
            vacationDaysPerYear: vacationDaysPerYear,
            workHoursPerWeek: workHoursPerWeek,
            workDays: workDays,
            bundesland: bundesland,
            updatedAt: updatedAt?.ISO8601Format(),
            effectiveDate: pendingEffectiveDate.map { ISO8601DateFormatter.dateOnly.string(from: $0) }
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
