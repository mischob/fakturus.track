import SwiftUI
import SwiftData

struct SettingsView: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(ServiceContainer.self) private var services
    @Environment(AuthManager.self) private var authManager
    @Environment(SubscriptionManager.self) private var subscriptionManager
    @State private var viewModel: SettingsViewModel?
    @State private var showSchoolHolidays = false
    @State private var showPaywall = false
    @State private var showDeleteAccountConfirmation = false
    @State private var deleteAccountError: String?
    @AppStorage("appearance") private var appearance = "system"

    var body: some View {
        NavigationStack {
            Group {
                if let vm = viewModel {
                    settingsContent(vm: vm)
                } else {
                    ProgressView()
                }
            }
            .navigationTitle(String(localized: "settings_tab_title"))
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
            Section(String(localized: "settings_work_time")) {
                HStack {
                    Text(String(localized: "settings_work_hours"))
                    Spacer()
                    TextField("", value: $vm.workHoursPerWeek, format: .number)
                        .keyboardType(.decimalPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                }
                .onChange(of: vm.workHoursPerWeek) { _, _ in vm.onSettingsChanged() }

                VStack(alignment: .leading, spacing: 8) {
                    Text(String(localized: "settings_work_days"))
                    WorkdaySelector(workDays: $vm.workDays)
                }
                .onChange(of: vm.workDays) { _, _ in vm.onSettingsChanged() }
            }

            // MARK: - Urlaub
            Section(String(localized: "settings_vacation_title")) {
                HStack {
                    Text(String(localized: "settings_vacation_days_per_year"))
                    Spacer()
                    TextField("", value: $vm.vacationDaysPerYear, format: .number)
                        .keyboardType(.numberPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                }
                .onChange(of: vm.vacationDaysPerYear) { _, _ in vm.onSettingsChanged() }
            }

            // MARK: - Region
            Section(String(localized: "settings_region")) {
                BundeslandPicker(selectedBundesland: $vm.bundesland)
                    .onChange(of: vm.bundesland) { _, _ in vm.onSettingsChanged() }
            }

            // MARK: - Schulferien
            Section(String(localized: "settings_school_holidays")) {
                Button {
                    showSchoolHolidays = true
                } label: {
                    HStack {
                        Text(String(localized: "settings_school_holidays_manage"))
                            .foregroundStyle(Theme.textPrimary)
                        Spacer()
                        Text("\(vm.schoolHolidays.count)")
                            .foregroundStyle(.secondary)
                        Image(systemName: "chevron.right")
                            .foregroundStyle(.secondary)
                            .font(.caption)
                    }
                }
                .featureLocked(.schoolHolidays)
            }

            // MARK: - Erscheinungsbild
            Section(String(localized: "settings_appearance")) {
                Picker(String(localized: "settings_appearance_mode"), selection: $appearance) {
                    Text(String(localized: "settings_appearance_system")).tag("system")
                    Text(String(localized: "settings_appearance_light")).tag("light")
                    Text(String(localized: "settings_appearance_dark")).tag("dark")
                }
            }

            // MARK: - App (E10-S01)
            Section(String(localized: "settings_app")) {
                // Notifications (ArbZG hints)
                Toggle(String(localized: "settings_notifications"), isOn: $vm.notificationsEnabled)

                // Personal number (for DATEV export)
                HStack {
                    Text(String(localized: "settings_personal_number"))
                    Spacer()
                    TextField("12345", text: $vm.personalNumber)
                        .keyboardType(.numberPad)
                        .multilineTextAlignment(.trailing)
                        .frame(maxWidth: 120)
                }
            }

            // MARK: - Info (E10-S01)
            Section(String(localized: "settings_info")) {
                // Version
                HStack {
                    Text(String(localized: "settings_version"))
                    Spacer()
                    Text(vm.appVersion)
                        .foregroundStyle(.secondary)
                }

                // Privacy
                Link(String(localized: "settings_privacy"),
                     destination: URL(string: "https://track.fakturus.com/privacy")!)

                // Imprint
                Link(String(localized: "settings_imprint"),
                     destination: URL(string: "https://track.fakturus.com/imprint")!)

                // Licenses
                NavigationLink(String(localized: "settings_licenses")) {
                    LicensesView()
                }
            }

            // MARK: - Abo
            Section(String(localized: "settings_subscription")) {
                HStack {
                    Text(String(localized: "settings_current_tier"))
                    Spacer()
                    Text(subscriptionManager.currentTier.displayName)
                        .foregroundStyle(.secondary)
                }

                Button {
                    showPaywall = true
                } label: {
                    HStack {
                        Text(String(localized: "settings_manage_subscription"))
                            .foregroundStyle(Theme.textPrimary)
                        Spacer()
                        Image(systemName: "chevron.right")
                            .foregroundStyle(.secondary)
                            .font(.caption)
                    }
                }

                Button(String(localized: "settings_restore_purchases")) {
                    Task {
                        try? await services.storeKitManager.restorePurchases()
                    }
                }
            }

            // MARK: - Konto
            Section(String(localized: "settings_account")) {
                Button(role: .destructive) {
                    vm.logout()
                } label: {
                    HStack {
                        Spacer()
                        Text(String(localized: "settings_logout"))
                        Spacer()
                    }
                }

                Button(role: .destructive) {
                    showDeleteAccountConfirmation = true
                } label: {
                    HStack {
                        Spacer()
                        Text(String(localized: "settings_delete_account"))
                        Spacer()
                    }
                }
                .confirmationDialog(
                    String(localized: "delete_account_title"),
                    isPresented: $showDeleteAccountConfirmation,
                    titleVisibility: .visible
                ) {
                    Button(String(localized: "delete_account_confirm"), role: .destructive) {
                        Task {
                            do {
                                try await services.apiClient?.deleteRaw(path: "/api/account")
                                services.consentManager.clearConsent()
                                vm.logout()
                            } catch {
                                deleteAccountError = error.localizedDescription
                            }
                        }
                    }
                } message: {
                    Text(String(localized: "delete_account_message"))
                }
            }
        }
        .listStyle(.insetGrouped)
        .scrollDismissesKeyboard(.interactively)
        .toolbar {
            ToolbarItemGroup(placement: .keyboard) {
                Spacer()
                Button(String(localized: "settings_keyboard_done")) {
                    UIApplication.shared.sendAction(#selector(UIResponder.resignFirstResponder), to: nil, from: nil, for: nil)
                }
            }
        }
        .sheet(isPresented: $showPaywall) {
            PaywallView()
        }
    }
}

// MARK: - LicensesView

struct LicensesView: View {
    var body: some View {
        List {
            Section("Microsoft Authentication Library (MSAL)") {
                Text("MIT License")
                Text("Copyright (c) Microsoft Corporation")
            }
        }
        .navigationTitle(String(localized: "settings_licenses"))
    }
}
