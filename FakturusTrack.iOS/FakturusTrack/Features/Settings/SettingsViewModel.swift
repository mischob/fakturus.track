import SwiftData
import SwiftUI
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

    /// Effective date for changes to `workDays` / `workHoursPerWeek`. Default
    /// is today. Picker lets the user backdate when correcting earlier entries.
    var effectiveDate: Date = Calendar.current.startOfDay(for: Date())

    /// True while a historized change is staged but not yet acknowledged by
    /// the server. Drives whether the "Gültig ab"-picker is visible. Stays on
    /// across the debounced save so the picker doesn't flicker; only flips
    /// back to false once the local model has its `pendingEffectiveDate`
    /// cleared (i.e. the server confirmed the change).
    var hasUnsyncedHistorizedChange: Bool = false

    /// Snapshot of the values at load time so we know whether the historized
    /// fields actually changed (and only then send `effectiveDate`).
    private var initialWorkDays: Int = 31
    private var initialWorkHoursPerWeek: Double = 40.0

    var workDaysOrHoursChanged: Bool {
        workDays != initialWorkDays || workHoursPerWeek != initialWorkHoursPerWeek
    }

    // MARK: - History (Stage 2)

    var settingsHistory: [UserSettingsHistoryEntryDTO] = []
    var isLoadingHistory: Bool = false
    var historyError: String?
    var apiClient: APIClient?

    // E10-S01: App settings
    @ObservationIgnored @AppStorage("notificationsEnabled") var notificationsEnabled: Bool = true

    var personalNumber: String = ""

    var appVersion: String {
        let version = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "?"
        let build = Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "?"
        return "\(version) (\(build))"
    }

    // MARK: - Dependencies

    private let modelContext: ModelContext
    var syncEngine: SyncEngine?
    var authManager: AuthManager?

    // MARK: - Debounce

    private var debounceTask: Task<Void, Never>?
    private var personalNumberDebounceTask: Task<Void, Never>?

    init(modelContext: ModelContext, syncEngine: SyncEngine? = nil, authManager: AuthManager? = nil, apiClient: APIClient? = nil) {
        self.modelContext = modelContext
        self.syncEngine = syncEngine
        self.authManager = authManager
        self.apiClient = apiClient
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
        personalNumber = settings.personalNumber ?? ""
        initialWorkDays = settings.workDays
        initialWorkHoursPerWeek = settings.workHoursPerWeek
        // If we're entering the screen with a still-pending historized change
        // (e.g. last sync failed offline), keep the picker visible.
        hasUnsyncedHistorizedChange = settings.pendingEffectiveDate != nil
        effectiveDate = settings.pendingEffectiveDate ?? Calendar.current.startOfDay(for: Date())
    }

    func loadSchoolHolidays() {
        let descriptor = FetchDescriptor<SchoolHolidayPeriod>(
            sortBy: [SortDescriptor(\.startDate)]
        )
        schoolHolidays = (try? modelContext.fetch(descriptor)) ?? []
    }

    // MARK: - Save with Debounce

    func onSettingsChanged() {
        // Latch the picker visibility now (synchronously) so it doesn't
        // disappear during the debounce/save round-trip.
        if workDaysOrHoursChanged {
            hasUnsyncedHistorizedChange = true
        }
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

        // Stage 2: only attach an effectiveDate when historized fields changed.
        // Otherwise leave nil so the server doesn't write a noise history row.
        let historizedFieldsChanged = workDaysOrHoursChanged
        let effectiveDateForUpload: Date? = historizedFieldsChanged ? effectiveDate : settings.pendingEffectiveDate

        settings.workHoursPerWeek = workHoursPerWeek
        settings.vacationDaysPerYear = vacationDaysPerYear
        settings.workDays = workDays
        settings.bundesland = bundesland
        settings.personalNumber = personalNumber
        settings.updatedAt = Date()
        settings.isPendingSync = true
        settings.isSynced = false
        settings.pendingEffectiveDate = effectiveDateForUpload

        try? modelContext.save()

        // Refresh the "initial" snapshot so subsequent edits compare against
        // the just-saved values.
        initialWorkDays = workDays
        initialWorkHoursPerWeek = workHoursPerWeek

        // Trigger sync
        await syncEngine?.syncAll()

        // After sync completes, re-read the local model. If sync succeeded the
        // pendingEffectiveDate is cleared and we can hide the picker. If sync
        // failed (offline) the marker is still there and the picker remains
        // visible so the user knows the change hasn't shipped yet.
        if let saved = try? modelContext.fetch(FetchDescriptor<UserSettings>()).first {
            hasUnsyncedHistorizedChange = saved.pendingEffectiveDate != nil
        }
    }

    // MARK: - History (Stage 2)

    /// Refreshes the WorkDays / WorkHoursPerWeek timeline from the server.
    func loadSettingsHistory() async {
        guard let client = apiClient else { return }
        isLoadingHistory = true
        historyError = nil
        defer { isLoadingHistory = false }
        do {
            let entries: [UserSettingsHistoryEntryDTO] = try await client.get("/v1/settings/history")
            settingsHistory = entries
        } catch {
            historyError = "Verlauf konnte nicht geladen werden"
        }
    }

    // MARK: - Personal Number (debounced save)

    func onPersonalNumberChanged() {
        personalNumberDebounceTask?.cancel()
        personalNumberDebounceTask = Task { [weak self] in
            try? await Task.sleep(for: .seconds(1))
            guard !Task.isCancelled else { return }
            await self?.savePersonalNumber()
        }
    }

    private func savePersonalNumber() async {
        let descriptor = FetchDescriptor<UserSettings>()
        guard let settings = try? modelContext.fetch(descriptor).first else { return }
        settings.personalNumber = personalNumber
        settings.updatedAt = Date()
        try? modelContext.save()
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

    // MARK: - Import School Holidays from API

    func importSchoolHolidays() async throws {
        let year = Calendar.current.component(.year, from: Date())
        let stateCode = bundesland.uppercased()
        let url = URL(string: "https://ferien-api.de/api/v1/holidays/\(stateCode)/\(year)")!
        let (data, _) = try await URLSession.shared.data(from: url)

        struct FerienApiHoliday: Decodable {
            let start: String
            let end: String
            let name: String
        }

        let holidays = try JSONDecoder().decode([FerienApiHoliday].self, from: data)

        // Delete all existing school holidays
        let descriptor = FetchDescriptor<SchoolHolidayPeriod>()
        let existing = (try? modelContext.fetch(descriptor)) ?? []
        for period in existing {
            modelContext.delete(period)
        }

        // Insert new ones
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd"

        for h in holidays {
            guard let startDate = dateFormatter.date(from: h.start),
                  let endDate = dateFormatter.date(from: h.end) else { continue }

            let displayName = h.name.split(separator: " ").first.map { String($0).capitalized } ?? h.name

            let period = SchoolHolidayPeriod(
                name: displayName,
                startDate: startDate,
                endDate: endDate
            )
            modelContext.insert(period)
        }

        try? modelContext.save()
        loadSchoolHolidays()
    }

    // MARK: - Logout

    func logout() {
        authManager?.logout()
    }
}
