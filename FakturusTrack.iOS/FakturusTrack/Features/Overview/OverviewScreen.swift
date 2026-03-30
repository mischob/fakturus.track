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
            .navigationTitle(String(localized: "overview_tab_title"))
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
                // Summary Cards (with shimmer loading)
                if vm.isLoading && vm.summary == nil {
                    ScrollView(.horizontal, showsIndicators: false) {
                        HStack(spacing: 12) {
                            OvertimeCard(
                                title: "---",
                                value: "---",
                                icon: "clock.badge.checkmark",
                                valueColor: Theme.textSecondary
                            )
                            .shimmer()

                            OvertimeCard(
                                title: "---",
                                value: "---",
                                icon: "sun.max",
                                valueColor: Theme.textSecondary
                            )
                            .shimmer()

                            OvertimeCard(
                                title: "---",
                                value: "---",
                                icon: "cross.circle",
                                valueColor: Theme.textSecondary
                            )
                            .shimmer()
                        }
                        .padding(.horizontal)
                    }
                } else if let summary = vm.summary {
                    ScrollView(.horizontal, showsIndicators: false) {
                        HStack(spacing: 12) {
                            OvertimeCard(
                                title: String(localized: "overview_overtime"),
                                value: OverviewViewModel.formatHours(summary.totalOvertimeHours),
                                icon: "clock.badge.checkmark",
                                valueColor: summary.totalOvertimeHours >= 0 ? Theme.success : Theme.danger
                            )

                            OvertimeCard(
                                title: String(localized: "overview_vacation"),
                                value: "\(summary.vacationDaysTaken) / \(summary.vacationDaysPerYear)",
                                subtitle: "\(summary.vacationDaysRemaining) \(String(localized: "overview_vacation_remaining"))",
                                icon: "sun.max",
                                valueColor: Theme.textPrimary
                            )

                            OvertimeCard(
                                title: String(localized: "overview_sick_days"),
                                value: "\(summary.sickDaysTaken ?? 0)",
                                icon: "cross.circle",
                                valueColor: summary.sickDaysTaken ?? 0 > 0 ? Theme.danger : Theme.textPrimary
                            )

                            OvertimeCard(
                                title: String(localized: "overview_holidays"),
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
                    ErrorBanner(
                        error,
                        action: { Task { await vm.loadSummary() } },
                        actionLabel: String(localized: "error_retry")
                    )
                    .padding(.horizontal)
                }

                // Cache hint
                if vm.isShowingCachedData, let lastUpdated = vm.lastUpdated {
                    Text("\(String(localized: "overview_last_updated")): \(lastUpdated.formatted(.relative(presentation: .named)))")
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
                Text(String(localized: "overview_export"))
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
                    Text(String(localized: "overview_export_pdf"))
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
            .featureLocked(.pdfExport)

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
                        Text(String(localized: "overview_export_csv_month"))
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
                        Text("\(String(localized: "overview_export_csv_quarter")) (Q\(currentQuarter))")
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
                        Text("\(String(localized: "overview_export_csv_year")) (\(String(currentYear)))")
                        Spacer()
                        Image(systemName: "square.and.arrow.up")
                    }
                    .padding(12)
                    .background(Theme.gray200.opacity(0.5))
                    .cornerRadius(8)
                }
            }
            .disabled(vm.isGeneratingCSV)
            .featureLocked(.csvExport)

            // DATEV Export
            Button {
                vm.generateDATEVExport(month: currentMonth, year: currentYear)
                if vm.exportedFileURL != nil {
                    showShareSheet = true
                }
            } label: {
                HStack {
                    Image(systemName: "doc.text")
                    Text(String(localized: "overview_export_datev_beta"))
                    Spacer()
                    Text("BETA")
                        .font(.caption2.bold())
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(Theme.primary.opacity(0.2))
                        .clipShape(RoundedRectangle(cornerRadius: 4))
                    if vm.isGeneratingDATEV {
                        ProgressView()
                    } else {
                        Image(systemName: "square.and.arrow.up")
                    }
                }
                .padding(12)
                .background(Theme.gray200.opacity(0.5))
                .cornerRadius(8)
            }
            .disabled(vm.isGeneratingDATEV)
            .featureLocked(.datevExport)

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
