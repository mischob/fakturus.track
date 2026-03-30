import SwiftUI
import SwiftData

struct VacationScreen: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(ServiceContainer.self) private var services
    @Environment(SubscriptionManager.self) private var subscriptionManager
    @State private var viewModel: VacationViewModel?

    var body: some View {
        NavigationStack {
            Group {
                if subscriptionManager.isAvailable(.vacation) {
                    if let vm = viewModel {
                        vacationContent(vm: vm)
                    } else {
                        ProgressView()
                    }
                } else {
                    PaywallTeaserView(
                        feature: .vacation,
                        title: String(localized: "vacation_locked_title"),
                        description: String(localized: "vacation_locked_description")
                    )
                }
            }
            .navigationTitle(String(localized: "vacation_tab_title"))
        }
        .onAppear {
            if viewModel == nil {
                viewModel = VacationViewModel(
                    modelContext: modelContext,
                    syncEngine: services.syncEngine
                )
            } else {
                viewModel?.refresh()
            }
        }
    }

    @ViewBuilder
    private func vacationContent(vm: VacationViewModel) -> some View {
        ScrollView {
            VStack(spacing: 16) {
                // Mode Selector
                VStack(spacing: 8) {
                    Picker(String(localized: "vacation_mode"), selection: Binding(
                        get: { vm.editMode },
                        set: { vm.editMode = $0 }
                    )) {
                        ForEach(VacationViewModel.AbsenceEditMode.allCases, id: \.self) { mode in
                            Text(mode.rawValue).tag(mode)
                        }
                    }
                    .pickerStyle(.segmented)
                    .padding(.horizontal)

                    Text(vm.editMode == .vacation
                         ? String(localized: "vacation_tap_hint")
                         : String(localized: "vacation_sick_tap_hint"))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal)
                }

                // Calendar
                VacationCalendar(
                    year: vm.displayedYear,
                    month: vm.displayedMonth,
                    vacationDates: vm.vacationDates,
                    sickDayDates: vm.sickDayDates,
                    schoolHolidayDates: vm.schoolHolidayDates,
                    holidays: vm.holidayDates,
                    holidaysList: vm.holidaysList,
                    workDays: vm.workDays,
                    onDayTap: { date in
                        switch vm.editMode {
                        case .vacation:
                            vm.toggleVacationDay(date: date)
                        case .sickDay:
                            vm.toggleSickDay(date: date)
                        }
                    },
                    onDayLongPress: { _ in },
                    onSwitchAbsenceType: { date in
                        vm.switchAbsenceType(date: date)
                    },
                    onRemoveAbsence: { date in
                        vm.removeAbsence(date: date)
                    }
                )
                .padding(.horizontal)

                // Remaining vacation display
                VStack(spacing: 8) {
                    HStack {
                        Text(String(localized: "vacation_remaining"))
                            .font(.headline)
                        Spacer()
                    }

                    HStack(spacing: 4) {
                        Text("\(vm.vacationDaysTakenThisYear)")
                            .font(.title2.bold())
                            .foregroundStyle(Theme.vacation)
                        Text("/")
                            .font(.title2)
                            .foregroundStyle(.secondary)
                        Text("\(vm.vacationDaysPerYear)")
                            .font(.title2.bold())
                            .foregroundStyle(Theme.textPrimary)
                        Text(String(localized: "vacation_days_taken"))
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                        Spacer()
                    }
                    .accessibilityElement(children: .combine)
                    .accessibilityLabel("\(vm.vacationDaysTakenThisYear) von \(vm.vacationDaysPerYear) Urlaubstagen genommen")

                    // Progress bar
                    GeometryReader { geo in
                        ZStack(alignment: .leading) {
                            RoundedRectangle(cornerRadius: 4)
                                .fill(Theme.gray200)
                                .frame(height: 8)

                            let progress = vm.vacationDaysPerYear > 0
                                ? min(1.0, Double(vm.vacationDaysTakenThisYear) / Double(vm.vacationDaysPerYear))
                                : 0.0
                            RoundedRectangle(cornerRadius: 4)
                                .fill(Theme.vacation)
                                .frame(width: geo.size.width * progress, height: 8)
                        }
                    }
                    .frame(height: 8)
                    .accessibilityHidden(true)

                    if vm.vacationDaysRemaining <= 0 {
                        Text(String(localized: "vacation_all_used"))
                            .font(.caption)
                            .foregroundStyle(Theme.danger)
                    }
                }
                .padding(.horizontal)

                // Legend
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        Text(String(localized: "vacation_legend"))
                            .font(.headline)
                        Spacer()
                    }

                    legendRow(color: Theme.vacation.opacity(0.3), text: String(localized: "vacation_legend_vacation"))
                    legendRow(color: Theme.sickDay.opacity(0.3), text: String(localized: "vacation_legend_sick"))
                    legendRow(color: .purple, text: String(localized: "vacation_legend_holiday"), isSmallDot: true)
                    legendRow(color: Color.orange.opacity(0.3), text: String(localized: "vacation_legend_school_holiday"))
                    legendRow(color: Theme.gray200, text: String(localized: "vacation_legend_weekend"))
                }
                .padding(.horizontal)

                Spacer(minLength: 24)
            }
            .padding(.top)
        }
    }

    @ViewBuilder
    private func legendRow(color: Color, text: String, isSmallDot: Bool = false) -> some View {
        HStack(spacing: 8) {
            if isSmallDot {
                Circle()
                    .fill(color)
                    .frame(width: 8, height: 8)
                    .padding(.horizontal, 4)
            } else {
                Circle()
                    .fill(color)
                    .frame(width: 16, height: 16)
            }
            Text(text)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }
}
