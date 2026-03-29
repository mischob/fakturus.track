import SwiftUI
import UIKit

struct OverviewScreen: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(ServiceContainer.self) private var services
    @State private var viewModel: OverviewViewModel?
    @State private var showShareSheet = false

    var body: some View {
        NavigationStack {
            Group {
                if let vm = viewModel {
                    overviewContent(vm: vm)
                } else {
                    ProgressView()
                }
            }
            .navigationTitle("Gesamt")
        }
        .onAppear {
            if viewModel == nil {
                let vm = OverviewViewModel(apiClient: services.apiClient, modelContext: modelContext)
                viewModel = vm
                Task { await vm.loadSummary() }
            }
        }
    }

    @ViewBuilder
    private func overviewContent(vm: OverviewViewModel) -> some View {
        ScrollView {
            VStack(spacing: 16) {
                // Summary Cards
                if let summary = vm.summary {
                    ScrollView(.horizontal, showsIndicators: false) {
                        HStack(spacing: 12) {
                            OvertimeCard(
                                title: "Ueberstunden",
                                value: OverviewViewModel.formatHours(summary.totalOvertimeHours),
                                icon: "clock.badge.checkmark",
                                valueColor: summary.totalOvertimeHours >= 0 ? Theme.success : Theme.danger
                            )

                            OvertimeCard(
                                title: "Urlaub",
                                value: "\(summary.vacationDaysTaken) / \(summary.vacationDaysPerYear)",
                                subtitle: "\(summary.vacationDaysRemaining) verbleibend",
                                icon: "sun.max",
                                valueColor: Theme.textPrimary
                            )

                            OvertimeCard(
                                title: "Krankheitstage",
                                value: "\(summary.sickDaysTaken ?? 0)",
                                icon: "cross.circle",
                                valueColor: summary.sickDaysTaken ?? 0 > 0 ? Theme.danger : Theme.textPrimary
                            )

                            OvertimeCard(
                                title: "Feiertage",
                                value: "\(summary.holidaysTaken)",
                                icon: "calendar",
                                valueColor: Theme.textPrimary
                            )
                        }
                        .padding(.horizontal)
                    }
                }

                // Year Navigation
                HStack {
                    Button {
                        vm.previousYear()
                    } label: {
                        HStack(spacing: 4) {
                            Image(systemName: "chevron.left")
                            Text("\(String(vm.selectedYear - 1))")
                        }
                        .font(.subheadline)
                        .foregroundStyle(Theme.primary)
                    }

                    Spacer()

                    Text("\(String(vm.selectedYear))")
                        .font(.title3.bold())

                    Spacer()

                    Button {
                        vm.nextYear()
                    } label: {
                        HStack(spacing: 4) {
                            Text("\(String(vm.selectedYear + 1))")
                            Image(systemName: "chevron.right")
                        }
                        .font(.subheadline)
                        .foregroundStyle(Theme.primary)
                    }
                }
                .padding(.horizontal)

                // Monthly Table
                if let summary = vm.summary, !summary.monthlyOvertime.isEmpty {
                    MonthlyOvertimeTable(months: summary.monthlyOvertime)
                        .padding(.horizontal)
                }

                // Loading
                if vm.isLoading {
                    ProgressView()
                        .padding()
                }

                // Error
                if let error = vm.error {
                    VStack(spacing: 8) {
                        Image(systemName: "exclamationmark.triangle")
                            .font(.largeTitle)
                            .foregroundStyle(.secondary)
                        Text(error)
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                        Button("Erneut versuchen") {
                            Task { await vm.loadSummary() }
                        }
                        .font(.subheadline)
                        .foregroundStyle(Theme.primary)
                    }
                    .padding()
                }

                // Cache hint
                if vm.isShowingCachedData, let lastUpdated = vm.lastUpdated {
                    Text("Zuletzt aktualisiert: \(lastUpdated.formatted(.relative(presentation: .named)))")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.horizontal)
                }

                // Export
                exportSection(vm: vm)

                Spacer(minLength: 24)
            }
            .padding(.top)
        }
        .refreshable {
            await vm.loadSummary()
        }
        .sheet(isPresented: $showShareSheet) {
            if let url = vm.exportedFileURL {
                ShareSheet(activityItems: [url])
            }
        }
    }

    // MARK: - Export Section

    @ViewBuilder
    private func exportSection(vm: OverviewViewModel) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("Export")
                    .font(.headline)
                Spacer()
            }

            // PDF Export (current month)
            let currentMonth = Calendar.current.component(.month, from: Date())
            let currentYear = vm.selectedYear

            Button {
                Task {
                    await vm.generatePDF(month: currentMonth, year: currentYear)
                    if vm.exportedFileURL != nil {
                        showShareSheet = true
                    }
                }
            } label: {
                HStack {
                    Image(systemName: "doc.richtext")
                    Text("PDF Monatsnachweis")
                    Spacer()
                    if vm.isGeneratingPDF {
                        ProgressView()
                    } else {
                        Image(systemName: "square.and.arrow.up")
                    }
                }
                .padding(12)
                .background(Theme.gray200.opacity(0.5))
                .cornerRadius(8)
            }
            .disabled(vm.isGeneratingPDF)

            // CSV Export
            VStack(spacing: 8) {
                Button {
                    vm.generateCSV(timeRange: .month(month: currentMonth, year: currentYear))
                    if vm.exportedFileURL != nil {
                        showShareSheet = true
                    }
                } label: {
                    HStack {
                        Image(systemName: "tablecells")
                        Text("CSV Monat")
                        Spacer()
                        Image(systemName: "square.and.arrow.up")
                    }
                    .padding(12)
                    .background(Theme.gray200.opacity(0.5))
                    .cornerRadius(8)
                }

                let currentQuarter = (currentMonth - 1) / 3 + 1
                Button {
                    vm.generateCSV(timeRange: .quarter(quarter: currentQuarter, year: currentYear))
                    if vm.exportedFileURL != nil {
                        showShareSheet = true
                    }
                } label: {
                    HStack {
                        Image(systemName: "tablecells")
                        Text("CSV Quartal (Q\(currentQuarter))")
                        Spacer()
                        Image(systemName: "square.and.arrow.up")
                    }
                    .padding(12)
                    .background(Theme.gray200.opacity(0.5))
                    .cornerRadius(8)
                }

                Button {
                    vm.generateCSV(timeRange: .year(year: currentYear))
                    if vm.exportedFileURL != nil {
                        showShareSheet = true
                    }
                } label: {
                    HStack {
                        Image(systemName: "tablecells")
                        Text("CSV Jahr (\(String(currentYear)))")
                        Spacer()
                        Image(systemName: "square.and.arrow.up")
                    }
                    .padding(12)
                    .background(Theme.gray200.opacity(0.5))
                    .cornerRadius(8)
                }
            }
            .disabled(vm.isGeneratingCSV)

            if let exportError = vm.exportError {
                Text(exportError)
                    .font(.caption)
                    .foregroundStyle(Theme.danger)
            }
        }
        .padding(.horizontal)
    }
}

// MARK: - ShareSheet

struct ShareSheet: UIViewControllerRepresentable {
    let activityItems: [Any]

    func makeUIViewController(context: Context) -> UIActivityViewController {
        UIActivityViewController(activityItems: activityItems, applicationActivities: nil)
    }

    func updateUIViewController(_ uiViewController: UIActivityViewController, context: Context) {}
}
