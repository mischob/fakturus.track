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
    @State private var showSettingsHistory = false
    @AppStorage("appearance") private var appearance = "system"

    private enum NumericField: Hashable { case workHours, vacationDays, personalNumber }
    @FocusState private var focusedField: NumericField?

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
                    authManager: authManager,
                    apiClient: services.apiClient
                )
                viewModel = vm
            }
        }
        .sheet(isPresented: $showSettingsHistory) {
            if let vm = viewModel {
                WorkSettingsHistorySheet(viewModel: vm)
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
                        .focused($focusedField, equals: .workHours)
                }
                .onChange(of: vm.workHoursPerWeek) { _, _ in vm.onSettingsChanged() }

                VStack(alignment: .leading, spacing: 8) {
                    Text(String(localized: "settings_work_days"))
                    WorkdaySelector(workDays: $vm.workDays)
                }
                .onChange(of: vm.workDays) { _, _ in vm.onSettingsChanged() }

                // Stage 2: stays visible while a historized change is staged
                // but not yet acknowledged by the server (latched in the VM
                // so it survives the debounced save).
                if vm.hasUnsyncedHistorizedChange {
                    DatePicker(
                        "Gültig ab",
                        selection: $vm.effectiveDate,
                        in: ...Calendar.current.startOfDay(for: Date()),
                        displayedComponents: .date
                    )
                    .environment(\.locale, Locale(identifier: "de_DE"))
                    .onChange(of: vm.effectiveDate) { _, _ in vm.onSettingsChanged() }

                    Text("Standard: heute. Für Korrekturen vergangener Wochen kann ein früheres Datum gewählt werden — Soll-Stunden werden ab dann mit den neuen Werten berechnet.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Button {
                    showSettingsHistory = true
                    Task { await vm.loadSettingsHistory() }
                } label: {
                    Label("Verlauf der Arbeitstage anzeigen", systemImage: "clock.arrow.circlepath")
                }
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
                        .focused($focusedField, equals: .vacationDays)
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
                        .focused($focusedField, equals: .personalNumber)
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
                                try await services.apiClient?.delete("/api/account")
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
        // .immediately so any drag dismisses, even small ones — useful when the
        // numeric keyboard hides scroll content.
        .scrollDismissesKeyboard(.immediately)
        .toolbar {
            ToolbarItemGroup(placement: .keyboard) {
                Spacer()
                Button(String(localized: "settings_keyboard_done")) {
                    // Drop both the SwiftUI focus state and any UIKit
                    // first responder so we cover decimal/number-pad keyboards
                    // that don't get a return key by default.
                    focusedField = nil
                    UIApplication.shared.sendAction(
                        #selector(UIResponder.resignFirstResponder),
                        to: nil, from: nil, for: nil
                    )
                }
                .bold()
            }
        }
        .sheet(isPresented: $showPaywall) {
            PaywallView()
        }
    }
}

// MARK: - WorkSettingsHistorySheet

struct WorkSettingsHistorySheet: View {
    @Bindable var viewModel: SettingsViewModel
    @Environment(\.dismiss) private var dismiss

    private static let isoDate: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withFullDate]
        return f
    }()

    private static let displayDate: DateFormatter = {
        let f = DateFormatter()
        f.locale = Locale(identifier: "de_DE")
        f.dateStyle = .medium
        return f
    }()

    private static let dayLabels = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"]

    private func formattedDate(_ iso: String) -> String {
        guard let d = Self.isoDate.date(from: iso) else { return iso }
        return Self.displayDate.string(from: d)
    }

    private func workDayList(_ bitmask: Int) -> String {
        var days: [String] = []
        for i in 0..<7 where (bitmask & (1 << i)) != 0 {
            days.append(Self.dayLabels[i])
        }
        return days.isEmpty ? "—" : days.joined(separator: ", ")
    }

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoadingHistory {
                    ProgressView()
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                } else if let err = viewModel.historyError {
                    VStack(spacing: 12) {
                        Image(systemName: "exclamationmark.triangle")
                            .font(.largeTitle)
                            .foregroundStyle(.orange)
                        Text(err).foregroundStyle(.secondary)
                        Button("Erneut versuchen") {
                            Task { await viewModel.loadSettingsHistory() }
                        }
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                } else if viewModel.settingsHistory.isEmpty {
                    VStack(spacing: 12) {
                        Image(systemName: "clock.arrow.circlepath")
                            .font(.largeTitle)
                            .foregroundStyle(.secondary)
                        Text("Noch keine Änderungen erfasst.")
                            .foregroundStyle(.secondary)
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                } else {
                    List {
                        Section {
                            Text("Hier siehst du, welche Wochentage und Wochenstunden in welchem Zeitraum für die Soll-Berechnung galten. Neueste Änderung zuerst.")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                        ForEach(viewModel.settingsHistory, id: \.id) { entry in
                            VStack(alignment: .leading, spacing: 6) {
                                HStack {
                                    Text(formattedDate(entry.validFrom))
                                        .font(.subheadline.bold())
                                    Text("–")
                                        .foregroundStyle(.secondary)
                                    if let to = entry.validTo {
                                        Text(formattedDate(to))
                                            .font(.subheadline.bold())
                                    } else {
                                        Text("aktuell")
                                            .font(.subheadline.bold())
                                            .foregroundStyle(.green)
                                    }
                                }
                                HStack {
                                    Image(systemName: "calendar")
                                        .foregroundStyle(.secondary)
                                    Text(workDayList(entry.workDays))
                                        .font(.callout)
                                }
                                HStack {
                                    Image(systemName: "clock")
                                        .foregroundStyle(.secondary)
                                    Text("\(String(format: "%.1f", entry.workHoursPerWeek)) h / Woche")
                                        .font(.callout)
                                }
                            }
                            .padding(.vertical, 4)
                        }
                    }
                }
            }
            .navigationTitle("Verlauf der Arbeitstage")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Fertig") { dismiss() }
                }
            }
            .task { await viewModel.loadSettingsHistory() }
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
