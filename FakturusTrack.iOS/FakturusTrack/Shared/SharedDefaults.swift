import Foundation
import WidgetKit

enum SharedDefaults {
    static let suiteName = "group.com.fakturus.track"
    nonisolated(unsafe) static let defaults = UserDefaults(suiteName: suiteName)!

    // Notification name for cross-process communication
    static let widgetActionNotification = CFNotificationName("com.fakturus.track.widgetAction" as CFString)

    // Keys
    static let isTimerRunningKey = "isTimerRunning"
    static let timerStartDateKey = "timerStartDate"
    static let isPausedKey = "isPaused"
    static let pauseMinutesKey = "pauseMinutes"
    static let todayTotalSecondsKey = "todayTotalSeconds"
    static let lastUpdateKey = "lastStateUpdate"

    // MARK: - Write (main app calls this on EVERY timer change)

    static func writeTimerState(
        isRunning: Bool,
        startDate: Date?,
        isPaused: Bool,
        pauseMinutes: Int,
        todayTotalSeconds: Int
    ) {
        defaults.set(isRunning, forKey: isTimerRunningKey)
        defaults.set(startDate, forKey: timerStartDateKey)
        defaults.set(isPaused, forKey: isPausedKey)
        defaults.set(pauseMinutes, forKey: pauseMinutesKey)
        defaults.set(todayTotalSeconds, forKey: todayTotalSecondsKey)
        defaults.set(Date(), forKey: lastUpdateKey)

        // Invalidate widget timeline
        WidgetCenter.shared.reloadAllTimelines()
    }

    // MARK: - Post notification (widget -> app)

    static func postWidgetActionNotification() {
        CFNotificationCenterPostNotification(
            CFNotificationCenterGetDarwinNotifyCenter(),
            widgetActionNotification,
            nil, nil, true
        )
    }

    // MARK: - Read (Widget/Watch reads this)

    static func readTimerState() -> (isRunning: Bool, startDate: Date?, isPaused: Bool, pauseMinutes: Int, todayTotalSeconds: Int) {
        return (
            isRunning: defaults.bool(forKey: isTimerRunningKey),
            startDate: defaults.object(forKey: timerStartDateKey) as? Date,
            isPaused: defaults.bool(forKey: isPausedKey),
            pauseMinutes: defaults.integer(forKey: pauseMinutesKey),
            todayTotalSeconds: defaults.integer(forKey: todayTotalSecondsKey)
        )
    }
}
