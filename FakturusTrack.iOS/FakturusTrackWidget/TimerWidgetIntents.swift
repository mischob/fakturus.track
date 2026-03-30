import AppIntents
import WidgetKit

struct StartTimerIntent: AppIntent {
    static let title: LocalizedStringResource = "Timer starten"
    static let description: IntentDescription = IntentDescription("Startet den Arbeitstimer")

    static let openAppWhenRun: Bool = false

    func perform() async throws -> some IntentResult {
        // Write state for immediate widget update
        SharedDefaults.writeTimerState(
            isRunning: true, startDate: Date(),
            isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
        )

        return .result()
    }
}

struct StopTimerIntent: AppIntent {
    static let title: LocalizedStringResource = "Timer stoppen"
    static let description: IntentDescription = IntentDescription("Stoppt den Arbeitstimer")

    static let openAppWhenRun: Bool = false

    func perform() async throws -> some IntentResult {
        SharedDefaults.writeTimerState(
            isRunning: false, startDate: nil,
            isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
        )

        return .result()
    }
}

struct PauseResumeTimerIntent: AppIntent {
    static let title: LocalizedStringResource = "Pause / Weiter"

    static let openAppWhenRun: Bool = false

    func perform() async throws -> some IntentResult {
        let state = SharedDefaults.readTimerState()

        SharedDefaults.writeTimerState(
            isRunning: state.isRunning, startDate: state.startDate,
            isPaused: !state.isPaused, pauseMinutes: state.pauseMinutes,
            todayTotalSeconds: state.todayTotalSeconds
        )

        return .result()
    }
}
