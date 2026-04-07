import SwiftData
import Foundation

@Model
final class PendingDelete {
    @Attribute(.unique) var id: UUID
    var entityId: UUID      // ID of the deleted entry
    var entityType: String  // "WorkSession", "VacationDay", etc.
    var deletedAt: Date

    init(entityId: UUID, entityType: String) {
        self.id = UUID()
        self.entityId = entityId
        self.entityType = entityType
        self.deletedAt = Date()
    }
}
