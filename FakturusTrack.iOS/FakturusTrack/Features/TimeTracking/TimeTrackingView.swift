import SwiftUI
import SwiftData

struct TimeTrackingView: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(ServiceContainer.self) private var services: ServiceContainer?
    @Query(sort: \WorkSession.date, order: .reverse) private var sessions: [WorkSession]
    @State private var viewModel: TimeTrackingViewModel?
    @State private var selectedSession: WorkSession?
    @State private var manualSession: WorkSession?
    @State private var syncStatus: SyncStatus = .idle

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 16) {
                    // ArbZG Banner (E08-S05)
                    if let vm = viewModel, vm.activeSession != nil {
                        ArbZGBannerContainer(
                            netWorkMinutes: vm.netWorkMinutes,
                            pauseMinutes: vm.accumulatedPauseMinutes,
                            isActive: vm.activeSession?.isRunning == true || vm.isPaused
                        )
                    }

                    // Active Session Card
                    if let vm = viewModel {
                        ActiveSessionCard(
                            session: vm.activeSession,
                            isPaused: vm.isPaused,
                            currentPauseStart: vm.currentPauseStart,
                            accumulatedPauseMinutes: vm.accumulatedPauseMinutes,
                            onStart: { vm.startSession() },
                            onStop: { vm.stopSession() },
                            onFinish: { vm.finishSession() },
                            onPause: { vm.pauseSession() },
                            onResume: { vm.resumeSession() },
                            onSave: { d, s, e, p in vm.updateSession(date: d, startTime: s, stopTime: e, pauseMinutes: p) },
                            onDelete: {
                                if let s = vm.activeSession { vm.deleteSession(s) }
                            }
                        )
                    }

                    // History
                    if finishedSessions.isEmpty {
                        emptyState
                    } else {
                        ForEach(groupedByMonth, id: \.key) { month, monthSessions in
                            MonthGroup(
                                monthName: month,
                                sessions: monthSessions,
                                isCurrentMonth: month == Date().monthYearString,
                                onDeleteSession: { viewModel?.deleteSession($0) },
                                onSelectSession: { selectedSession = $0 }
                            )
                        }
                    }
                }
                .padding()
            }
            .navigationTitle("Zeiten")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    SyncStatusView(status: syncStatus) {
                        Task {
                            syncStatus = .syncing
                            await services?.syncEngine?.syncAll()
                            syncStatus = .synced

                            // Return to idle after 3 seconds
                            try? await Task.sleep(for: .seconds(3))
                            syncStatus = .idle
                        }
                    }
                }
                ToolbarItem(placement: .primaryAction) {
                    Button {
                        guard let vm = viewModel else { return }
                        let session = vm.createManualSession()
                        manualSession = session
                    } label: {
                        Image(systemName: "plus")
                    }
                }
            }
            .refreshable {
                await services?.syncEngine?.syncAll()
            }
            .sheet(item: $selectedSession) { session in
                SessionDetailSheet(
                    session: session,
                    onSave: { d, s, e, p in
                        viewModel?.updateFinishedSession(session, date: d, startTime: s, stopTime: e, pauseMinutes: p)
                    },
                    onDelete: { viewModel?.deleteSession(session) }
                )
            }
            .sheet(item: $manualSession) { session in
                SessionDetailSheet(
                    session: session,
                    isCreateMode: true,
                    onSave: { d, s, e, p in
                        session.date = d
                        session.startTime = s
                        session.stopTime = e
                        session.pauseMinutes = p
                        session.isFinished = true
                        session.isPendingSync = true
                        session.updatedAt = Date()
                        try? modelContext.save()
                    },
                    onDelete: {
                        viewModel?.deleteSession(session)
                    }
                )
            }
        }
        .onAppear {
            if viewModel == nil {
                viewModel = TimeTrackingViewModel(
                    modelContext: modelContext,
                    syncEngine: services?.syncEngine,
                    networkMonitor: services?.networkMonitor
                )
            }
            updateSyncStatus()
        }
    }

    // MARK: - Sync Status

    private func updateSyncStatus() {
        let pendingCount = sessions.filter { $0.isPendingSync }.count
        if pendingCount > 0 {
            syncStatus = .pending(count: pendingCount)
        } else {
            syncStatus = .idle
        }
    }

    // MARK: - Computed

    private var finishedSessions: [WorkSession] {
        sessions.filter { $0.isFinished }
    }

    private var groupedByMonth: [(key: String, value: [WorkSession])] {
        Dictionary(grouping: finishedSessions) { $0.monthKey }
            .sorted { $0.key > $1.key }
    }

    // MARK: - Empty State

    @ViewBuilder
    private var emptyState: some View {
        VStack(spacing: 12) {
            Image(systemName: "clock")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)
            Text("Noch keine Eintraege")
                .font(.headline)
            Text("Starten Sie Ihre erste Arbeitssitzung!")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .padding(.vertical, 48)
    }
}

// MARK: - Preview

#Preview {
    TimeTrackingView()
        .modelContainer(for: WorkSession.self, inMemory: true)
}
