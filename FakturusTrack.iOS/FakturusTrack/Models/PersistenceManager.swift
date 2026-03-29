import SwiftData

enum PersistenceManager {
    static let schema = Schema([
        WorkSession.self,
        VacationDay.self,
        SickDay.self,
        UserSettings.self,
        PendingDelete.self,
        SchoolHolidayPeriod.self
    ])

    static let container: ModelContainer = {
        let config = ModelConfiguration(schema: schema, isStoredInMemoryOnly: false)
        return try! ModelContainer(for: schema, configurations: [config])
    }()

    /// For tests: in-memory container
    static func testContainer() -> ModelContainer {
        let config = ModelConfiguration(schema: schema, isStoredInMemoryOnly: true)
        return try! ModelContainer(for: schema, configurations: [config])
    }
}
