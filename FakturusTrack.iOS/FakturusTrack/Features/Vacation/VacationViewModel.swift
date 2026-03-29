import SwiftData
import Foundation

@Observable @MainActor
final class VacationViewModel {
    // MARK: - State

    var displayedMonth: Int
    var displayedYear: Int
    var vacationDates: Set<DateComponents> = []
    var sickDayDates: Set<DateComponents> = []
    var holidayDates: Set<DateComponents> = []
    var holidaysList: [Holiday] = []
    var workDays: Int = 31
    var vacationDaysPerYear: Int = 30
    var bundesland: String = "NW"
    var error: String?
    var editMode: AbsenceEditMode = .vacation

    enum AbsenceEditMode: String, CaseIterable {
        case vacation = "Urlaub"
        case sickDay = "Krankheit"
    }

    // MARK: - Dependencies

    private let modelContext: ModelContext
    var syncEngine: SyncEngine?

    init(modelContext: ModelContext, syncEngine: SyncEngine? = nil) {
        self.modelContext = modelContext
        self.syncEngine = syncEngine
        let now = Date()
        let cal = Calendar.current
        self.displayedMonth = cal.component(.month, from: now)
        self.displayedYear = cal.component(.year, from: now)
        loadSettings()
        loadVacationDays()
        updateHolidays()
    }

    // MARK: - Load Data

    func loadSettings() {
        let descriptor = FetchDescriptor<UserSettings>()
        guard let settings = try? modelContext.fetch(descriptor).first else { return }
        workDays = settings.workDays
        vacationDaysPerYear = settings.vacationDaysPerYear
        bundesland = settings.bundesland
    }

    func loadVacationDays() {
        let cal = Calendar.current
        let currentYear = cal.component(.year, from: Date())
        let descriptor = FetchDescriptor<VacationDay>()
        let allDays = (try? modelContext.fetch(descriptor)) ?? []

        vacationDates = Set(allDays.compactMap { day -> DateComponents? in
            let comps = cal.dateComponents([.year, .month, .day], from: day.date)
            return comps
        })

        // Filter to current year only for count
        _ = allDays.filter { cal.component(.year, from: $0.date) == currentYear }

        // Load sick days
        let sickDescriptor = FetchDescriptor<SickDay>()
        let allSickDays = (try? modelContext.fetch(sickDescriptor)) ?? []
        sickDayDates = Set(allSickDays.compactMap { day -> DateComponents? in
            cal.dateComponents([.year, .month, .day], from: day.date)
        })
    }

    func updateHolidays() {
        let holidays = HolidayCalculator.holidays(bundesland: bundesland, year: displayedYear)
        let cal = Calendar.current
        holidayDates = Set(holidays.map { cal.dateComponents([.year, .month, .day], from: $0.date) })
        holidaysList = holidays
    }

    // MARK: - Computed

    var vacationDaysTakenThisYear: Int {
        let cal = Calendar.current
        let currentYear = cal.component(.year, from: Date())
        return vacationDates.filter { $0.year == currentYear }.count
    }

    var vacationDaysRemaining: Int {
        vacationDaysPerYear - vacationDaysTakenThisYear
    }

    // MARK: - Toggle Vacation Day

    func toggleVacationDay(date: Date) {
        let cal = Calendar.current
        let startOfDay = cal.startOfDay(for: date)
        let endOfDay = cal.date(byAdding: .day, value: 1, to: startOfDay)!

        let descriptor = FetchDescriptor<VacationDay>(
            predicate: #Predicate { day in
                day.date >= startOfDay && day.date < endOfDay
            }
        )

        if let existing = try? modelContext.fetch(descriptor).first {
            // Remove
            modelContext.delete(existing)
        } else {
            // Add
            let vacationDay = VacationDay(date: startOfDay)
            modelContext.insert(vacationDay)
        }

        try? modelContext.save()
        loadVacationDays()

        // Trigger sync
        Task { await syncEngine?.syncAll() }
    }

    // MARK: - Toggle Sick Day

    func toggleSickDay(date: Date) {
        let cal = Calendar.current
        let startOfDay = cal.startOfDay(for: date)
        let endOfDay = cal.date(byAdding: .day, value: 1, to: startOfDay)!

        let descriptor = FetchDescriptor<SickDay>(
            predicate: #Predicate { day in
                day.date >= startOfDay && day.date < endOfDay
            }
        )

        if let existing = try? modelContext.fetch(descriptor).first {
            // Remove
            modelContext.delete(existing)
        } else {
            // Add (also remove vacation day for this date if exists)
            let vacDescriptor = FetchDescriptor<VacationDay>(
                predicate: #Predicate { day in
                    day.date >= startOfDay && day.date < endOfDay
                }
            )
            if let vacDay = try? modelContext.fetch(vacDescriptor).first {
                modelContext.delete(vacDay)
            }
            let sickDay = SickDay(date: startOfDay)
            modelContext.insert(sickDay)
        }

        try? modelContext.save()
        loadVacationDays()

        // Trigger sync
        Task { await syncEngine?.syncAll() }
    }

    // MARK: - Switch Absence Type

    func switchAbsenceType(date: Date) {
        let cal = Calendar.current
        let startOfDay = cal.startOfDay(for: date)
        let endOfDay = cal.date(byAdding: .day, value: 1, to: startOfDay)!

        // Check if it's a vacation day
        let vacDescriptor = FetchDescriptor<VacationDay>(
            predicate: #Predicate { day in
                day.date >= startOfDay && day.date < endOfDay
            }
        )
        if let vacDay = try? modelContext.fetch(vacDescriptor).first {
            modelContext.delete(vacDay)
            modelContext.insert(SickDay(date: startOfDay))
            try? modelContext.save()
            loadVacationDays()
            Task { await syncEngine?.syncAll() }
            return
        }

        // Check if it's a sick day
        let sickDescriptor = FetchDescriptor<SickDay>(
            predicate: #Predicate { day in
                day.date >= startOfDay && day.date < endOfDay
            }
        )
        if let sickDay = try? modelContext.fetch(sickDescriptor).first {
            modelContext.delete(sickDay)
            modelContext.insert(VacationDay(date: startOfDay))
            try? modelContext.save()
            loadVacationDays()
            Task { await syncEngine?.syncAll() }
            return
        }
    }

    // MARK: - Remove Absence

    func removeAbsence(date: Date) {
        let cal = Calendar.current
        let startOfDay = cal.startOfDay(for: date)
        let endOfDay = cal.date(byAdding: .day, value: 1, to: startOfDay)!

        let vacDescriptor = FetchDescriptor<VacationDay>(
            predicate: #Predicate { day in
                day.date >= startOfDay && day.date < endOfDay
            }
        )
        if let vacDay = try? modelContext.fetch(vacDescriptor).first {
            modelContext.delete(vacDay)
        }

        let sickDescriptor = FetchDescriptor<SickDay>(
            predicate: #Predicate { day in
                day.date >= startOfDay && day.date < endOfDay
            }
        )
        if let sickDay = try? modelContext.fetch(sickDescriptor).first {
            modelContext.delete(sickDay)
        }

        try? modelContext.save()
        loadVacationDays()

        Task { await syncEngine?.syncAll() }
    }

    // MARK: - Refresh

    func refresh() {
        loadSettings()
        loadVacationDays()
        updateHolidays()
    }
}
