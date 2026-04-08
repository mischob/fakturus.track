import SwiftData
import Foundation
import WidgetKit

@Observable @MainActor
final class TimeTrackingViewModel {
    // MARK: - State

    var activeSession: WorkSession?
    var isLoading = false
    var error: String?

    // Pause state (E08)
    var isPaused = false
    var currentPauseStart: Date?
    var accumulatedPauseMinutes: Int = 0

    // MARK: - Dependencies

    private let modelContext: ModelContext
    var syncEngine: SyncEngine?
    var networkMonitor: NetworkMonitor?

    // MARK: - UserDefaults Keys (Crash-Recovery)

    private static let pauseStartKey = "fakturus.currentPauseStart"
    private static let pauseAccumulatedKey = "fakturus.accumulatedPauseMinutes"
    private static let pauseActiveKey = "fakturus.isPaused"

    init(modelContext: ModelContext, syncEngine: SyncEngine? = nil, networkMonitor: NetworkMonitor? = nil) {
        self.modelContext = modelContext
        self.syncEngine = syncEngine
        self.networkMonitor = networkMonitor
        loadActiveSession()
        restorePauseState()
    }

    private func loadActiveSession() {
        let descriptor = FetchDescriptor<WorkSession>(
            predicate: #Predicate { !$0.isFinished }
        )
        activeSession = try? modelContext.fetch(descriptor).first
    }

    /// Restore pause state from UserDefaults after crash/restart
    private func restorePauseState() {
        guard activeSession != nil else {
            clearPauseDefaults()
            return
        }

        let defaults = UserDefaults.standard
        let wasPaused = defaults.bool(forKey: Self.pauseActiveKey)

        if wasPaused {
            isPaused = true
            accumulatedPauseMinutes = defaults.integer(forKey: Self.pauseAccumulatedKey)
            if let savedStart = defaults.object(forKey: Self.pauseStartKey) as? Date {
                currentPauseStart = savedStart
            }
        }
    }

    private func persistPauseState() {
        let defaults = UserDefaults.standard
        defaults.set(isPaused, forKey: Self.pauseActiveKey)
        defaults.set(accumulatedPauseMinutes, forKey: Self.pauseAccumulatedKey)
        if let start = currentPauseStart {
            defaults.set(start, forKey: Self.pauseStartKey)
        } else {
            defaults.removeObject(forKey: Self.pauseStartKey)
        }
    }

    private func clearPauseDefaults() {
        let defaults = UserDefaults.standard
        defaults.removeObject(forKey: Self.pauseActiveKey)
        defaults.removeObject(forKey: Self.pauseAccumulatedKey)
        defaults.removeObject(forKey: Self.pauseStartKey)
    }

    // MARK: - Actions

    func startSession() {
        let session = WorkSession(
            date: Date(),
            startTime: Date(),
            isPendingSync: true,
            isSynced: false,
            isFinished: false
        )
        modelContext.insert(session)
        try? modelContext.save()
        activeSession = session
        accumulatedPauseMinutes = 0
        isPaused = false
        currentPauseStart = nil
        clearPauseDefaults()

        SharedDefaults.writeTimerState(
            isRunning: true, startDate: session.startTime,
            isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
        )
    }

    func stopSession() {
        guard let session = activeSession, session.isRunning else { return }
        if isPaused { resumeSession() }
        session.stopTime = Date()
        session.updatedAt = Date()
        try? modelContext.save()

        SharedDefaults.writeTimerState(
            isRunning: false, startDate: session.startTime,
            isPaused: false, pauseMinutes: accumulatedPauseMinutes, todayTotalSeconds: 0
        )
    }

    func finishSession() {
        guard let session = activeSession else { return }
        if isPaused { resumeSession() }
        if session.stopTime == nil { session.stopTime = Date() }
        session.isFinished = true
        session.isPendingSync = true
        session.updatedAt = Date()
        try? modelContext.save()
        activeSession = nil
        isPaused = false
        accumulatedPauseMinutes = 0
        currentPauseStart = nil
        clearPauseDefaults()

        SharedDefaults.writeTimerState(
            isRunning: false, startDate: nil,
            isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
        )

        // Trigger sync after finishing a session
        Task { await syncEngine?.syncAll() }
    }

    func updateSession(date: Date, startTime: Date, stopTime: Date?, pauseMinutes: Int) {
        guard let session = activeSession else { return }
        if let stop = stopTime, stop <= startTime { return }

        session.date = date
        session.startTime = startTime
        session.stopTime = stopTime
        session.pauseMinutes = pauseMinutes
        session.updatedAt = Date()
        session.isPendingSync = true
        try? modelContext.save()
    }

    func deleteSession(_ session: WorkSession) {
        let wasSynced = session.isSynced
        let sessionId = session.id

        modelContext.delete(session)

        // Track pending delete for synced sessions so the server is notified
        if wasSynced {
            let pendingDelete = PendingDelete(entityId: sessionId, entityType: "WorkSession")
            modelContext.insert(pendingDelete)
        }

        try? modelContext.save()

        if sessionId == activeSession?.id {
            activeSession = nil
            isPaused = false
            accumulatedPauseMinutes = 0
            currentPauseStart = nil
            clearPauseDefaults()

            SharedDefaults.writeTimerState(
                isRunning: false, startDate: nil,
                isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
            )
        }

        // Trigger sync to process the delete on server
        if wasSynced {
            Task { await syncEngine?.syncAll() }
        }
    }

    /// Update an existing finished session (used from SessionDetailSheet)
    func updateFinishedSession(_ session: WorkSession, date: Date, startTime: Date, stopTime: Date?, pauseMinutes: Int) {
        if let stop = stopTime, stop <= startTime { return }

        session.date = date
        session.startTime = startTime
        session.stopTime = stopTime
        session.pauseMinutes = pauseMinutes
        session.updatedAt = Date()
        session.isPendingSync = true
        try? modelContext.save()

        // Trigger sync to upload the edited session
        Task { await syncEngine?.syncAll() }
    }

    /// Create a manual session (E06-S09)
    func createManualSession() -> WorkSession {
        let now = Date()
        let session = WorkSession(
            date: now,
            startTime: now.addingTimeInterval(-3600),
            stopTime: now,
            pauseMinutes: 0,
            isPendingSync: true,
            isSynced: false,
            isFinished: false
        )
        modelContext.insert(session)
        try? modelContext.save()
        return session
    }

    // MARK: - Pause (E08-S01)

    func pauseSession() {
        guard let session = activeSession, session.isRunning, !isPaused else { return }
        isPaused = true
        currentPauseStart = Date()
        persistPauseState()

        SharedDefaults.writeTimerState(
            isRunning: true, startDate: session.startTime,
            isPaused: true, pauseMinutes: accumulatedPauseMinutes, todayTotalSeconds: 0
        )
    }

    func resumeSession() {
        guard isPaused, let pauseStart = currentPauseStart else { return }
        let pauseDuration = Date().timeIntervalSince(pauseStart)
        let pauseMinutes = Int(ceil(pauseDuration / 60.0))
        accumulatedPauseMinutes += pauseMinutes

        if let session = activeSession {
            session.pauseMinutes = accumulatedPauseMinutes
            session.updatedAt = Date()
            try? modelContext.save()
        }

        isPaused = false
        currentPauseStart = nil
        persistPauseState()

        SharedDefaults.writeTimerState(
            isRunning: true, startDate: activeSession?.startTime,
            isPaused: false, pauseMinutes: accumulatedPauseMinutes, todayTotalSeconds: 0
        )
    }

    /// Computed net work minutes for ArbZG checks (accounts for current ongoing pause)
    var netWorkMinutes: Int {
        guard let session = activeSession else { return 0 }
        let totalElapsed = Date().timeIntervalSince(session.startTime)
        var totalPauseSeconds = Double(accumulatedPauseMinutes * 60)
        if let pauseStart = currentPauseStart {
            totalPauseSeconds += Date().timeIntervalSince(pauseStart)
        }
        let netSeconds = max(0, totalElapsed - totalPauseSeconds)
        return Int(netSeconds / 60)
    }
}
