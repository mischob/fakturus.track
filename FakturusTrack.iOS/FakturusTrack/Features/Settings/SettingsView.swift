import SwiftUI
import SwiftData

struct SettingsView: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(ServiceContainer.self) private var services
    @Environment(AuthManager.self) private var authManager
    @State private var viewModel: SettingsViewModel?
    @State private var showSchoolHolidays = false

    var body: some View {
        NavigationStack {
            Group {
                if let vm = viewModel {
                    settingsContent(vm: vm)
                } else {
                    ProgressView()
                }
            }
            .navigationTitle("Einstellungen")
            .navigationDestination(isPresented: $showSchoolHolidays) {
                if let vm = viewModel {
                    SchoolHolidaysScreen(viewModel: vm)
                }
            }
        }
        .onAppear {
            if viewModel == nil {
                let vm = SettingsViewModel(
                    modelContext: modelContext,
                    syncEngine: services.syncEngine,
                    authManager: authManager
                )
                viewModel = vm
            }
        }
    }

    @ViewBuilder
    private func settingsContent(vm: SettingsViewModel) -> some View {
        @Bindable var vm = vm
        List {
            // MARK: - Arbeitszeit
            Section("Arbeitszeit") {
                HStack {
                    Text("Wochenstunden")
                    Spacer()
                    TextField("", value: $vm.workHoursPerWeek, format: .number)
                        .keyboardType(.decimalPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                }
                .onChange(of: vm.workHoursPerWeek) { _, _ in vm.onSettingsChanged() }

                VStack(alignment: .leading, spacing: 8) {
                    Text("Arbeitstage")
                    WorkdaySelector(workDays: $vm.workDays)
                }
                .onChange(of: vm.workDays) { _, _ in vm.onSettingsChanged() }
            }

            // MARK: - Urlaub
            Section("Urlaub") {
                HStack {
                    Text("Urlaubstage pro Jahr")
                    Spacer()
                    TextField("", value: $vm.vacationDaysPerYear, format: .number)
                        .keyboardType(.numberPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                }
                .onChange(of: vm.vacationDaysPerYear) { _, _ in vm.onSettingsChanged() }
            }

            // MARK: - Region
            Section("Region") {
                BundeslandPicker(selectedBundesland: $vm.bundesland)
                    .onChange(of: vm.bundesland) { _, _ in vm.onSettingsChanged() }
            }

            // MARK: - Schulferien
            Section("Schulferien") {
                Button {
                    showSchoolHolidays = true
                } label: {
                    HStack {
                        Text("Schulferien verwalten")
                            .foregroundStyle(Theme.textPrimary)
                        Spacer()
                        Text("\(vm.schoolHolidays.count)")
                            .foregroundStyle(.secondary)
                        Image(systemName: "chevron.right")
                            .foregroundStyle(.secondary)
                            .font(.caption)
                    }
                }
            }

            // MARK: - Konto
            Section("Konto") {
                Button(role: .destructive) {
                    vm.logout()
                } label: {
                    HStack {
                        Spacer()
                        Text("Abmelden")
                        Spacer()
                    }
                }
            }
        }
        .listStyle(.insetGrouped)
    }
}
