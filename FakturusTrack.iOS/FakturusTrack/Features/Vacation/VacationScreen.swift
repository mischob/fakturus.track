import SwiftUI
import SwiftData

struct VacationScreen: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(ServiceContainer.self) private var services
    @State private var viewModel: VacationViewModel?

    var body: some View {
        NavigationStack {
            Group {
                if let vm = viewModel {
                    vacationContent(vm: vm)
                } else {
                    ProgressView()
                }
            }
            .navigationTitle("Urlaub")
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
                    Picker("Modus", selection: Binding(
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
                         ? "Tippen Sie auf einen Arbeitstag um Urlaub zu setzen oder zu entfernen."
                         : "Tippen Sie auf einen Arbeitstag um einen Krankheitstag zu setzen oder zu entfernen.")
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

                // Resturlaub-Anzeige
                VStack(spacing: 8) {
                    HStack {
                        Text("Resturlaub")
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
                        Text("Tage genommen")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                        Spacer()
                    }

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

                    if vm.vacationDaysRemaining <= 0 {
                        Text("Alle Urlaubstage aufgebraucht")
                            .font(.caption)
                            .foregroundStyle(Theme.danger)
                    }
                }
                .padding(.horizontal)

                // Legende
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        Text("Legende")
                            .font(.headline)
                        Spacer()
                    }

                    legendRow(color: Theme.vacation.opacity(0.3), text: "Urlaub")
                    legendRow(color: Theme.sickDay.opacity(0.3), text: "Krankheit")
                    legendRow(color: .purple, text: "Feiertag", isSmallDot: true)
                    legendRow(color: Theme.gray200, text: "Wochenende / kein Arbeitstag")
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
