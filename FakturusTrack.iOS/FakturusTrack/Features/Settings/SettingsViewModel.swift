import SwiftData
import Foundation
import Combine

@Observable @MainActor
final class SettingsViewModel {
    // MARK: - State

    var workHoursPerWeek: Double = 40.0
    var vacationDaysPerYear: Int = 30
    var workDays: Int = 31
    var bundesland: String = "NW"
    var schoolHolidays: [SchoolHolidayPeriod] = []
    var userName: String = ""
    var userEmail: String = ""
    var error: String?
    var isSaving = false

    // MARK: - Dependencies

    private let modelContext: ModelContext
    var syncEngine: SyncEngine?
    var authManager: AuthManager?

    // MARK: - Debounce

    private var debounceTask: Task<Void, Never>?

    init(modelContext: ModelContext, syncEngine: SyncEngine? = nil, authManager: AuthManager? = nil) {
        self.modelContext = modelContext
        self.syncEngine = syncEngine
        self.authManager = authManager
        loadSettings()
        loadSchoolHolidays()
    }

    // MARK: - Load

    private func loadSettings() {
        let descriptor = FetchDescriptor<UserSettings>()
        guard let settings = try? modelContext.fetch(descriptor).first else { return }
        workHoursPerWeek = settings.workHoursPerWeek
        vacationDaysPerYear = settings.vacationDaysPerYear
        workDays = settings.workDays
        bundesland = settings.bundesland
    }

    func loadSchoolHolidays() {
        let descriptor = FetchDescriptor<SchoolHolidayPeriod>(
            sortBy: [SortDescriptor(\.startDate)]
        )
        schoolHolidays = (try? modelContext.fetch(descriptor)) ?? []
    }

    // MARK: - Save with Debounce

    func onSettingsChanged() {
        debounceTask?.cancel()
        debounceTask = Task { [weak self] in
            try? await Task.sleep(for: .seconds(1))
            guard !Task.isCancelled else { return }
            await self?.saveSettings()
        }
    }

    private func saveSettings() async {
        let descriptor = FetchDescriptor<UserSettings>()
        guard let settings = try? modelContext.fetch(descriptor).first else { return }

        settings.workHoursPerWeek = workHoursPerWeek
        settings.vacationDaysPerYear = vacationDaysPerYear
        settings.workDays = workDays
        settings.bundesland = bundesland
        settings.updatedAt = Date()
        settings.isPendingSync = true
        settings.isSynced = false

        try? modelContext.save()

        // Trigger sync
        await syncEngine?.syncAll()
    }

    // MARK: - School Holidays CRUD

    func addSchoolHoliday(name: String, startDate: Date, endDate: Date) {
        let period = SchoolHolidayPeriod(name: name, startDate: startDate, endDate: endDate)
        modelContext.insert(period)
        try? modelContext.save()
        loadSchoolHolidays()
    }

    func updateSchoolHoliday(_ period: SchoolHolidayPeriod, name: String, startDate: Date, endDate: Date) {
        period.name = name
        period.startDate = startDate
        period.endDate = endDate
        try? modelContext.save()
        loadSchoolHolidays()
    }

    func deleteSchoolHoliday(_ period: SchoolHolidayPeriod) {
        modelContext.delete(period)
        try? modelContext.save()
        loadSchoolHolidays()
    }

    // MARK: - Logout

    func logout() {
        authManager?.logout()
    }
}
