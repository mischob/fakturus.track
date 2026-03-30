# Tech-Spec: EPIC 03 -- iOS Widgets (WidgetKit)

## Uebersicht

Widget Extension Target mit Small + Medium Widget. App Group fuer Datenaustausch. Interactive Widgets (iOS 17+) fuer Timer Start/Stop direkt vom Home Screen.

---

## S01: Widget Target & App Group Setup

### project.yml Target

```yaml
FakturusTrackWidget:
  type: app-extension
  platform: iOS
  sources:
    - path: FakturusTrackWidget
      excludes:
        - "**/.gitkeep"
  info:
    path: FakturusTrackWidget/Info.plist
    properties:
      CFBundleDisplayName: "Fakturus Track Widget"
      NSExtension:
        NSExtensionPointIdentifier: com.apple.widgetkit-extension
      NSSupportsLiveActivities: true
  settings:
    base:
      PRODUCT_BUNDLE_IDENTIFIER: com.fakturus.track.widget
      SWIFT_VERSION: "6.0"
      CODE_SIGN_ENTITLEMENTS: FakturusTrackWidget/FakturusTrackWidget.entitlements
  entitlements:
    path: FakturusTrackWidget/FakturusTrackWidget.entitlements
    properties:
      com.apple.security.application-groups:
        - group.com.fakturus.track
```

### Haupt-App: App Group Entitlement hinzufuegen

In `FakturusTrack.entitlements`:
```xml
<key>com.apple.security.application-groups</key>
<array>
    <string>group.com.fakturus.track</string>
</array>
```

### SharedDefaults.swift (NEU -- Shared zwischen Haupt-App, Widget, Watch)

```swift
// Shared/SharedDefaults.swift
import Foundation
import WidgetKit

enum SharedDefaults {
    static let suiteName = "group.com.fakturus.track"
    static let defaults = UserDefaults(suiteName: suiteName)!

    // Keys
    static let isTimerRunningKey = "isTimerRunning"
    static let timerStartDateKey = "timerStartDate"
    static let isPausedKey = "isPaused"
    static let pauseMinutesKey = "pauseMinutes"
    static let todayTotalSecondsKey = "todayTotalSeconds"
    static let lastUpdateKey = "lastStateUpdate"

    // MARK: - Write (Haupt-App ruft dies bei JEDER Timer-Aenderung auf)

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

        // Widget-Timeline invalidieren
        WidgetCenter.shared.reloadAllTimelines()
    }

    // MARK: - Read (Widget/Watch lesen dies)

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
```

### Integration in TimeTrackingViewModel.swift

```swift
// Am Ende von startSession():
SharedDefaults.writeTimerState(
    isRunning: true, startDate: session.startTime,
    isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
)

// Am Ende von stopSession():
SharedDefaults.writeTimerState(
    isRunning: false, startDate: activeSession?.startTime,
    isPaused: false, pauseMinutes: accumulatedPauseMinutes, todayTotalSeconds: 0
)

// Am Ende von finishSession():
SharedDefaults.writeTimerState(
    isRunning: false, startDate: nil,
    isPaused: false, pauseMinutes: 0, todayTotalSeconds: /* berechnen */
)

// Am Ende von pauseSession():
SharedDefaults.writeTimerState(
    isRunning: true, startDate: activeSession?.startTime,
    isPaused: true, pauseMinutes: accumulatedPauseMinutes, todayTotalSeconds: 0
)

// Am Ende von resumeSession():
SharedDefaults.writeTimerState(
    isRunning: true, startDate: activeSession?.startTime,
    isPaused: false, pauseMinutes: accumulatedPauseMinutes, todayTotalSeconds: 0
)
```

---

## S02: Timer-Status Widget (Small + Medium)

### WidgetTimelineProvider.swift

```swift
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

struct WidgetTimelineProvider: TimelineProvider {
    func placeholder(in context: Context) -> TimerWidgetEntry {
        TimerWidgetEntry(date: .now, isRunning: false, isPaused: false,
                        startDate: nil, pauseMinutes: 0, todayTotalSeconds: 0)
    }

    func getSnapshot(in context: Context, completion: @escaping (TimerWidgetEntry) -> Void) {
        completion(currentEntry())
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<TimerWidgetEntry>) -> Void) {
        let state = SharedDefaults.readTimerState()

        if state.isRunning && !state.isPaused {
            // Laufender Timer: Entries alle 1 Minute fuer die naechsten 60 Min
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
            // Kein Timer oder pausiert: Einzelner statischer Entry
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
```

### TimerWidget.swift

```swift
import WidgetKit
import SwiftUI

struct TimerWidget: Widget {
    let kind = "FakturusTrackTimerWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: WidgetTimelineProvider()) { entry in
            TimerWidgetView(entry: entry)
                .containerBackground(.fill.tertiary, for: .widget)
        }
        .configurationDisplayName("Fakturus Timer")
        .description("Timer-Status und heutige Arbeitszeit")
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}

struct TimerWidgetView: View {
    @Environment(\.widgetFamily) var family
    let entry: TimerWidgetEntry

    var body: some View {
        switch family {
        case .systemSmall:
            smallWidget
        case .systemMedium:
            mediumWidget
        default:
            smallWidget
        }
    }

    // MARK: - Small (2x2)

    private var smallWidget: some View {
        VStack(spacing: 8) {
            HStack {
                Circle()
                    .fill(entry.isRunning ? .green : .secondary)
                    .frame(width: 8, height: 8)
                Spacer()
                Image("AppIconSmall") // oder SF Symbol
                    .resizable()
                    .frame(width: 16, height: 16)
            }

            Spacer()

            if entry.isRunning, let start = entry.startDate {
                Text(timerInterval: start...Date.distantFuture, countsDown: false)
                    .font(.system(.title, design: .monospaced))
                    .monospacedDigit()
                    .minimumScaleFactor(0.7)
            } else if entry.isPaused {
                Image(systemName: "pause.circle.fill")
                    .font(.title)
                    .foregroundStyle(.orange)
                Text("Pausiert")
                    .font(.caption)
            } else {
                Text("Bereit")
                    .font(.headline)
                    .foregroundStyle(.secondary)
            }

            Spacer()
        }
        .padding()
    }

    // MARK: - Medium (4x2)

    private var mediumWidget: some View {
        HStack {
            // Links: Timer
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Circle()
                        .fill(entry.isRunning ? .green : .secondary)
                        .frame(width: 8, height: 8)
                    Text(entry.isRunning ? "Laeuft" : (entry.isPaused ? "Pausiert" : "Bereit"))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                if entry.isRunning, let start = entry.startDate {
                    Text(timerInterval: start...Date.distantFuture, countsDown: false)
                        .font(.system(.title, design: .monospaced))
                        .monospacedDigit()
                    Text("Seit \(start.formatted(date: .omitted, time: .shortened))")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                } else {
                    Text("--:--:--")
                        .font(.system(.title, design: .monospaced))
                        .foregroundStyle(.secondary)
                }
            }

            Spacer()

            // Rechts: Tages-Zusammenfassung
            VStack(alignment: .trailing) {
                Text("Heute")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                let h = entry.todayTotalSeconds / 3600
                let m = (entry.todayTotalSeconds % 3600) / 60
                Text("\(h):\(String(format: "%02d", m))h")
                    .font(.title2)
                    .monospacedDigit()

                if entry.pauseMinutes > 0 {
                    Text("Pause: \(entry.pauseMinutes) Min")
                        .font(.caption2)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding()
    }
}
```

---

## S03: Widget Quick Actions (Interactive Widget, iOS 17+)

### Architektur-Entscheidung: AppIntent.perform() statt App-Group-Flags

Widget Quick Actions nutzen `AppIntent.perform()` direkt fuer Start/Stop/Pause. Der AppIntent startet die App im Hintergrund und fuehrt die Aktion unmittelbar aus -- **keine Flags in der App Group**, die erst beim naechsten App-Oeffnen verarbeitet werden muessen. Das ist zuverlaessiger und sofort wirksam.

### TimerWidgetIntents.swift

```swift
import AppIntents
import WidgetKit

struct StartTimerIntent: AppIntent {
    static var title: LocalizedStringResource = "Timer starten"
    static var description = IntentDescription("Startet den Arbeitstimer")

    // openAppWhenRun = false -- Intent laeuft im Hintergrund
    static var openAppWhenRun: Bool = false

    func perform() async throws -> some IntentResult {
        // AppIntent.perform() startet die App im Hintergrund.
        // Direkt ueber den shared TimeTrackingService die Session starten.
        let service = TimeTrackingService.shared
        try await service.startSession()

        // Widget-State aktualisieren fuer sofortige Anzeige
        SharedDefaults.writeTimerState(
            isRunning: true, startDate: Date(),
            isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
        )

        return .result()
    }
}

struct StopTimerIntent: AppIntent {
    static var title: LocalizedStringResource = "Timer stoppen"
    static var description = IntentDescription("Stoppt den Arbeitstimer")

    static var openAppWhenRun: Bool = false

    func perform() async throws -> some IntentResult {
        let service = TimeTrackingService.shared
        try await service.stopAndFinishSession()

        SharedDefaults.writeTimerState(
            isRunning: false, startDate: nil,
            isPaused: false, pauseMinutes: 0, todayTotalSeconds: 0
        )

        return .result()
    }
}

struct PauseResumeTimerIntent: AppIntent {
    static var title: LocalizedStringResource = "Pause / Weiter"

    static var openAppWhenRun: Bool = false

    func perform() async throws -> some IntentResult {
        let service = TimeTrackingService.shared
        let state = SharedDefaults.readTimerState()

        if state.isPaused {
            try await service.resumeSession()
        } else {
            try await service.pauseSession()
        }

        SharedDefaults.writeTimerState(
            isRunning: state.isRunning, startDate: state.startDate,
            isPaused: !state.isPaused, pauseMinutes: state.pauseMinutes,
            todayTotalSeconds: state.todayTotalSeconds
        )

        return .result()
    }
}
```

**Hinweis**: `TimeTrackingService.shared` muss ein Singleton sein, das auch ohne laufende UI funktioniert (Hintergrund-Ausfuehrung durch AppIntent). Der Service greift direkt auf SwiftData/CoreData zu und fuehrt die Timer-Logik aus. Kein Umweg ueber Flags oder ViewModel noetig.

### Medium Widget mit Buttons (Ergaenzung in TimerWidget.swift)

```swift
// Im mediumWidget ViewBuilder, zusaetzliche Buttons:
if #available(iOSApplicationExtension 17.0, *) {
    HStack(spacing: 8) {
        if entry.isRunning {
            Button(intent: PauseResumeTimerIntent()) {
                Image(systemName: entry.isPaused ? "play.fill" : "pause.fill")
            }
            .buttonStyle(.bordered)

            Button(intent: StopTimerIntent()) {
                Image(systemName: "stop.fill")
            }
            .buttonStyle(.bordered)
            .tint(.red)
        } else {
            Button(intent: StartTimerIntent()) {
                Label("Start", systemImage: "play.fill")
            }
            .buttonStyle(.borderedProminent)
            .tint(.green)
        }
    }
}
```

---

## WidgetBundle (vereint Widget + Live Activity)

### WidgetBundle.swift

```swift
import WidgetKit
import SwiftUI

@main
struct FakturusTrackWidgetBundle: WidgetBundle {
    var body: some Widget {
        TimerWidget()
        // Live Activity kommt in EPIC 04:
        // WorkSessionLiveActivity()
    }
}
```

**Hinweis**: Das `@main` Attribut auf dem WidgetBundle bedeutet, dass es keinen separaten `@main` im Widget-Target gibt. Wenn E04 (Live Activity) implementiert wird, wird die `WorkSessionLiveActivity` hier ergaenzt.
