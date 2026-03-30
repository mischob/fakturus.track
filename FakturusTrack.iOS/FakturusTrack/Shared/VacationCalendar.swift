import SwiftUI

// MARK: - Data Types

enum DayType: Equatable {
    case empty
    case workday
    case weekend
    case holiday(String)
    case vacation
    case sickDay
}

struct DayCell: Identifiable {
    let id: Int
    let date: Date?
    let dayNumber: Int?
    let type: DayType
    let isToday: Bool

    var isTappable: Bool {
        switch type {
        case .workday, .vacation, .sickDay: return true
        default: return false
        }
    }
}

// MARK: - VacationCalendar

struct VacationCalendar: View {
    let vacationDates: Set<DateComponents>
    let sickDayDates: Set<DateComponents>
    let holidays: Set<DateComponents>
    let holidaysList: [Holiday]
    let workDays: Int
    let onDayTap: (Date) -> Void
    let onDayLongPress: (Date) -> Void
    let onSwitchAbsenceType: (Date) -> Void
    let onRemoveAbsence: (Date) -> Void

    @State private var displayedMonth: Int
    @State private var displayedYear: Int

    init(
        year: Int,
        month: Int,
        vacationDates: Set<DateComponents>,
        sickDayDates: Set<DateComponents> = [],
        holidays: Set<DateComponents>,
        holidaysList: [Holiday],
        workDays: Int,
        onDayTap: @escaping (Date) -> Void,
        onDayLongPress: @escaping (Date) -> Void = { _ in },
        onSwitchAbsenceType: @escaping (Date) -> Void = { _ in },
        onRemoveAbsence: @escaping (Date) -> Void = { _ in }
    ) {
        self.vacationDates = vacationDates
        self.sickDayDates = sickDayDates
        self.holidays = holidays
        self.holidaysList = holidaysList
        self.workDays = workDays
        self.onDayTap = onDayTap
        self.onDayLongPress = onDayLongPress
        self.onSwitchAbsenceType = onSwitchAbsenceType
        self.onRemoveAbsence = onRemoveAbsence
        _displayedMonth = State(initialValue: month)
        _displayedYear = State(initialValue: year)
    }

    var body: some View {
        VStack(spacing: 8) {
            // Month header with navigation
            HStack {
                Button(action: previousMonth) {
                    Image(systemName: "chevron.left")
                        .font(.title3)
                        .foregroundStyle(Theme.primary)
                }
                .accessibilityLabel("Vorheriger Monat")
                Spacer()
                Text(monthYearString)
                    .font(.headline)
                Spacer()
                Button(action: nextMonth) {
                    Image(systemName: "chevron.right")
                        .font(.title3)
                        .foregroundStyle(Theme.primary)
                }
                .accessibilityLabel("Naechster Monat")
            }
            .padding(.horizontal)

            // Weekday header
            HStack(spacing: 0) {
                ForEach(["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"], id: \.self) { day in
                    Text(day)
                        .font(.caption2)
                        .frame(maxWidth: .infinity)
                        .foregroundStyle(.secondary)
                }
            }

            // Day grid (6 x 7)
            LazyVGrid(columns: Array(repeating: GridItem(.flexible(minimum: 36)), count: 7),
                      spacing: 4) {
                ForEach(cells, id: \.id) { cell in
                    DayCellView(
                        cell: cell,
                        onTap: { if let d = cell.date { onDayTap(d) } },
                        onLongPress: { if let d = cell.date { onDayLongPress(d) } },
                        onSwitchAbsenceType: { if let d = cell.date { onSwitchAbsenceType(d) } },
                        onRemoveAbsence: { if let d = cell.date { onRemoveAbsence(d) } }
                    )
                }
            }
        }
    }

    // MARK: - Month Navigation

    private var monthYearString: String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "de_DE")
        formatter.dateFormat = "MMMM yyyy"
        var comps = DateComponents()
        comps.year = displayedYear
        comps.month = displayedMonth
        comps.day = 1
        let date = Calendar.current.date(from: comps) ?? Date()
        return formatter.string(from: date)
    }

    private func previousMonth() {
        if displayedMonth == 1 {
            displayedMonth = 12
            displayedYear -= 1
        } else {
            displayedMonth -= 1
        }
    }

    private func nextMonth() {
        if displayedMonth == 12 {
            displayedMonth = 1
            displayedYear += 1
        } else {
            displayedMonth += 1
        }
    }

    // MARK: - Cell Building

    private var cells: [DayCell] {
        buildCells(year: displayedYear, month: displayedMonth)
    }

    private func buildCells(year: Int, month: Int) -> [DayCell] {
        var cal = Calendar.current
        cal.firstWeekday = 2 // Monday

        guard let firstOfMonth = cal.date(from: DateComponents(year: year, month: month, day: 1)),
              let range = cal.range(of: .day, in: .month, for: firstOfMonth)
        else { return [] }

        let weekday = cal.component(.weekday, from: firstOfMonth)
        let offset = (weekday - 2 + 7) % 7
        let today = Date()
        let todayComps = cal.dateComponents([.year, .month, .day], from: today)

        let yearHolidays = holidays

        var result: [DayCell] = []

        // Empty cells before first day
        for i in 0..<offset {
            result.append(DayCell(id: i, date: nil, dayNumber: nil, type: .empty, isToday: false))
        }

        // Days of month
        for day in range {
            let cellId = offset + day - 1
            let date = cal.date(from: DateComponents(year: year, month: month, day: day))!
            let comps = DateComponents(year: year, month: month, day: day)
            let isToday = (comps.year == todayComps.year &&
                           comps.month == todayComps.month &&
                           comps.day == todayComps.day)

            let type: DayType
            if vacationDates.contains(where: { $0.year == comps.year && $0.month == comps.month && $0.day == comps.day }) {
                type = .vacation
            } else if sickDayDates.contains(where: { $0.year == comps.year && $0.month == comps.month && $0.day == comps.day }) {
                type = .sickDay
            } else if yearHolidays.contains(where: { $0.year == comps.year && $0.month == comps.month && $0.day == comps.day }) {
                let name = holidaysList.first { cal.isDate($0.date, inSameDayAs: date) }?.name ?? ""
                type = .holiday(name)
            } else if !isWorkday(date: date, calendar: cal) {
                type = .weekend
            } else {
                type = .workday
            }

            result.append(DayCell(id: cellId, date: date, dayNumber: day, type: type, isToday: isToday))
        }

        // Fill remaining cells to reach 42
        let remaining = 42 - result.count
        for i in 0..<remaining {
            result.append(DayCell(id: offset + range.count + i, date: nil, dayNumber: nil, type: .empty, isToday: false))
        }

        return result
    }

    private func isWorkday(date: Date, calendar: Calendar) -> Bool {
        let weekday = calendar.component(.weekday, from: date)
        let mondayBased = weekday == 1 ? 7 : weekday - 1
        return (workDays & (1 << (mondayBased - 1))) != 0
    }
}

// MARK: - DayCellView

struct DayCellView: View {
    let cell: DayCell
    let onTap: () -> Void
    let onLongPress: () -> Void
    let onSwitchAbsenceType: () -> Void
    let onRemoveAbsence: () -> Void

    var body: some View {
        ZStack {
            // Background
            Group {
                switch cell.type {
                case .vacation:
                    Circle().fill(Theme.vacation.opacity(0.3))
                case .sickDay:
                    Circle().fill(Theme.sickDay.opacity(0.3))
                default:
                    Color.clear
                }
            }

            // Today outline
            if cell.isToday {
                Circle().stroke(Theme.danger, lineWidth: 2)
            }

            // Text + type indicator
            if let dayNumber = cell.dayNumber {
                VStack(spacing: 0) {
                    Text("\(dayNumber)")
                        .font(.body)
                        .foregroundStyle(foregroundColor)

                    // Color + text label for non-color-only information
                    switch cell.type {
                    case .vacation:
                        Text("U")
                            .font(.system(size: 8, weight: .bold))
                            .foregroundStyle(Theme.vacation)
                    case .sickDay:
                        Text("K")
                            .font(.system(size: 8, weight: .bold))
                            .foregroundStyle(Theme.sickDay)
                    default:
                        EmptyView()
                    }
                }
            }

            // Holiday indicator
            if case .holiday = cell.type {
                Circle()
                    .fill(Color.purple)
                    .frame(width: 4, height: 4)
                    .offset(y: 12)
            }
        }
        .frame(height: 40)
        .contentShape(Rectangle())
        .onTapGesture {
            guard cell.isTappable else { return }
            onTap()
        }
        .accessibilityLabel(accessibleDayLabel)
        .accessibilityHint(accessibleDayHint)
        .contextMenu {
            if cell.type == .workday || cell.isToday {
                Button {
                    onTap()
                } label: {
                    Label(String(localized: "vacation_context_vacation"), systemImage: "sun.max.fill")
                }
                Button {
                    onLongPress()
                } label: {
                    Label(String(localized: "vacation_context_sick"), systemImage: "cross.circle.fill")
                }
            }
            if cell.type == .vacation || cell.type == .sickDay {
                Button {
                    onSwitchAbsenceType()
                } label: {
                    Label(String(localized: "vacation_context_switch_type"), systemImage: "arrow.triangle.2.circlepath")
                }
                Button(role: .destructive) {
                    onRemoveAbsence()
                } label: {
                    Label(String(localized: "vacation_context_remove"), systemImage: "trash")
                }
            }
        }
    }

    // MARK: - Accessibility

    private var accessibleDayLabel: String {
        guard let date = cell.date else { return "" }
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "de_DE")
        formatter.dateFormat = "EEEE, d. MMMM"
        let dateStr = formatter.string(from: date)

        switch cell.type {
        case .vacation: return "\(dateStr), Urlaubstag"
        case .sickDay: return "\(dateStr), Krankheitstag"
        case .holiday(let name): return "\(dateStr), Feiertag, \(name)"
        case .workday: return "\(dateStr), Arbeitstag"
        case .weekend: return "\(dateStr), Wochenende"
        case .empty: return ""
        }
    }

    private var accessibleDayHint: String {
        guard cell.isTappable else { return "" }
        switch cell.type {
        case .vacation: return "Doppeltippen um Urlaub zu entfernen"
        case .sickDay: return "Doppeltippen um Krankheitstag zu entfernen"
        case .workday: return "Doppeltippen um Abwesenheit zu setzen"
        default: return ""
        }
    }

    private var foregroundColor: Color {
        switch cell.type {
        case .weekend: return Theme.textSecondary
        case .holiday: return .purple
        default: return Theme.textPrimary
        }
    }
}
