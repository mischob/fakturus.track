import SwiftData
import Foundation

@Model
final class SchoolHolidayPeriod {
    @Attribute(.unique) var id: UUID
    var name: String
    var startDate: Date
    var endDate: Date

    init(
        id: UUID = UUID(),
        name: String = "",
        startDate: Date = Date(),
        endDate: Date = Date()
    ) {
        self.id = id
        self.name = name
        self.startDate = startDate
        self.endDate = endDate
    }
}
