import WidgetKit
import SwiftUI

struct TimerWidgetEntry: TimelineEntry {
    let date: Date
    let isRunning: Bool
    let isPaused: Bool
    let startDate: Date?
    let pauseMinutes: Int
    let todayTotalSeconds: Int
}

struct TimerWidgetProvider: TimelineProvider {
    func placeholder(in context: Context) -> TimerWidgetEntry {
        TimerWidgetEntry(
            date: .now, isRunning: false, isPaused: false,
            startDate: nil, pauseMinutes: 0, todayTotalSeconds: 0
        )
    }

    func getSnapshot(in context: Context, completion: @escaping (TimerWidgetEntry) -> Void) {
        completion(currentEntry())
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<TimerWidgetEntry>) -> Void) {
        let state = SharedDefaults.readTimerState()

        if state.isRunning && !state.isPaused {
            // Running timer: entries every 1 minute for the next 60 minutes
            var entries: [TimerWidgetEntry] = []
            let now = Date()
            for minuteOffset in 0..<60 {
                let entryDate = Calendar.current.date(byAdding: .minute, value: minuteOffset, to: now)!
                entries.append(TimerWidgetEntry(
                    date: entryDate,
                    isRunning: true, isPaused: false,
                    startDate: state.startDate,
                    pauseMinutes: state.pauseMinutes,
                    todayTotalSeconds: state.todayTotalSeconds
                ))
            }
            completion(Timeline(entries: entries, policy: .after(now.addingTimeInterval(3600))))
        } else {
            // No timer or paused: single static entry
            let entry = currentEntry()
            completion(Timeline(entries: [entry], policy: .never))
        }
    }

    private func currentEntry() -> TimerWidgetEntry {
        let state = SharedDefaults.readTimerState()
        return TimerWidgetEntry(
            date: .now,
            isRunning: state.isRunning, isPaused: state.isPaused,
            startDate: state.startDate,
            pauseMinutes: state.pauseMinutes,
            todayTotalSeconds: state.todayTotalSeconds
        )
    }
}
