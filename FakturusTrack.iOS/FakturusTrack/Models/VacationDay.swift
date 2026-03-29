import SwiftData
import Foundation

@Model
final class VacationDay {
    @Attribute(.unique) var id: UUID
    var userId: String
    var date: Date
    var createdAt: Date
    var updatedAt: Date
    var syncedAt: Date?
    var isPendingSync: Bool
    var isSynced: Bool

    init(
        id: UUID = UUID(),
        userId: String = "",
        date: Date = Date(),
        isPendingSync: Bool = true,
        isSynced: Bool = false
    ) {
        self.id = id
        self.userId = userId
        self.date = date
        self.createdAt = Date()
        self.updatedAt = Date()
        self.isPendingSync = isPendingSync
        self.isSynced = isSynced
    }

    // MARK: - DTO Conversion

    func toDTO() -> VacationDaySyncItem {
        VacationDaySyncItem(
            id: id.uuidString,
            date: ISO8601DateFormatter.dateOnly.string(from: date),
            createdAt: createdAt.ISO8601Format(),
            updatedAt: updatedAt.ISO8601Format(),
            syncedAt: syncedAt?.ISO8601Format()
        )
    }

    func update(from dto: VacationDayDTO) {
        if let d = ISO8601DateFormatter.dateOnly.date(from: dto.date) { date = d }
        updatedAt = Date()
        syncedAt = Date()
        isPendingSync = false
        isSynced = true
    }

    convenience init(from dto: VacationDayDTO) {
        self.init(
            id: UUID(uuidString: dto.id) ?? UUID(),
            userId: dto.userId ?? "",
            date: ISO8601DateFormatter.dateOnly.date(from: dto.date) ?? Date(),
            isPendingSync: false,
            isSynced: true
        )
        self.syncedAt = Date()
    }
}
