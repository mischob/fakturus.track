# Tech-Spec: EPIC 04 -- iOS Live Activity & Dynamic Island

## Uebersicht

ActivityKit-basierte Live Activity fuer laufende Timer-Sessions. Zeigt Timer auf dem Sperrbildschirm und in der Dynamic Island. Teilt sich die App Group und das Widget Extension Target mit EPIC 03.

---

## S01: ActivityAttributes & Setup

### WorkSessionAttributes.swift (NEU in Services/LiveActivity/)

```swift
import ActivityKit
import Foundation

struct WorkSessionAttributes: ActivityAttributes {
    // Statische Attribute (aendern sich nie waehrend der Activity)
    let startTime: Date

    // Dynamischer State (wird bei Updates geaendert)
    struct ContentState: Codable, Hashable {
        let isRunning: Bool
        let isPaused: Bool
        let pauseMinutes: Int
    }
}
```

### Info.plist Ergaenzung (Haupt-App)

```xml
<key>NSSupportsLiveActivities</key>
<true/>
```

---

## S02/S03: Lock Screen + Dynamic Island UI

### Live Activity Configuration (im Widget Extension Target)

Die Live Activity View wird im **Widget Extension Target** definiert (nicht in der Haupt-App), da sie Teil des WidgetBundle ist.

```swift
// FakturusTrackWidget/WorkSessionLiveActivity.swift
import ActivityKit
import WidgetKit
import SwiftUI

struct WorkSessionLiveActivity: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: WorkSessionAttributes.self) { context in
            // Lock Screen Banner
            lockScreenView(context: context)
        } dynamicIsland: { context in
            DynamicIsland {
                // Expanded View
                DynamicIslandExpandedRegion(.leading) {
                    Image(systemName: "timer")
                        .foregroundStyle(.green)
                }
                DynamicIslandExpandedRegion(.trailing) {
                    if context.state.isPaused {
                        Image(systemName: "pause.circle.fill")
                            .foregroundStyle(.orange)
                    }
                }
                DynamicIslandExpandedRegion(.center) {
                    Text(timerInterval: context.attributes.startTime...Date.distantFuture,
                         countsDown: false)
                        .font(.system(.title, design: .monospaced))
                        .monospacedDigit()
                }
                DynamicIslandExpandedRegion(.bottom) {
                    HStack {
                        Text("Seit \(context.attributes.startTime.formatted(date: .omitted, time: .shortened))")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                        Spacer()
                        if context.state.pauseMinutes > 0 {
                            Text("Pause: \(context.state.pauseMinutes) Min")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }
                }
            } compactLeading: {
                // Compact: Linke Seite der Dynamic Island
                Image(systemName: context.state.isPaused ? "pause.circle.fill" : "timer")
                    .foregroundStyle(context.state.isPaused ? .orange : .green)
            } compactTrailing: {
                // Compact: Rechte Seite der Dynamic Island
                Text(timerInterval: context.attributes.startTime...Date.distantFuture,
                     countsDown: false)
                    .monospacedDigit()
                    .frame(width: 50)
            } minimal: {
                // Minimal: Wenn andere Live Activities aktiv sind
                Image(systemName: "timer")
                    .foregroundStyle(context.state.isPaused ? .orange : .green)
            }
        }
    }

    // MARK: - Lock Screen View

    @ViewBuilder
    private func lockScreenView(context: ActivityViewContext<WorkSessionAttributes>) -> some View {
        HStack(spacing: 12) {
            // App-Icon
            Image(systemName: "clock.fill")
                .font(.title2)
                .foregroundStyle(.blue)

            VStack(alignment: .leading, spacing: 2) {
                Text("Arbeitszeit")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if context.state.isPaused {
                    HStack {
                        Text("Pausiert")
                            .font(.headline)
                            .foregroundStyle(.orange)
                        Text("(\(context.state.pauseMinutes) Min)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                } else {
                    Text(timerInterval: context.attributes.startTime...Date.distantFuture,
                         countsDown: false)
                        .font(.system(.title2, design: .monospaced))
                        .monospacedDigit()
                }
            }

            Spacer()

            VStack(alignment: .trailing, spacing: 2) {
                Text("Seit")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
                Text(context.attributes.startTime.formatted(date: .omitted, time: .shortened))
                    .font(.subheadline)
            }
        }
        .padding()
    }
}
```

### WidgetBundle erweitern (EPIC 03 + 04 vereint)

```swift
// WidgetBundle.swift -- aktualisiert
@main
struct FakturusTrackWidgetBundle: WidgetBundle {
    var body: some Widget {
        TimerWidget()                  // EPIC 03
        WorkSessionLiveActivity()      // EPIC 04
    }
}
```

---

## S04: Live Activity Lifecycle-Management

### LiveActivityManager.swift (NEU in Services/LiveActivity/)

```swift
import ActivityKit
import Foundation

@MainActor
enum LiveActivityManager {
    private static var currentActivity: Activity<WorkSessionAttributes>?

    // MARK: - Start

    static func startActivity(startTime: Date) {
        guard ActivityAuthorizationInfo().areActivitiesEnabled else { return }

        // Bestehende Activity beenden
        cleanupStaleActivities()

        let attributes = WorkSessionAttributes(startTime: startTime)
        let state = WorkSessionAttributes.ContentState(
            isRunning: true, isPaused: false, pauseMinutes: 0
        )
        let content = ActivityContent(state: state, staleDate: nil)

        do {
            currentActivity = try Activity.request(
                attributes: attributes,
                content: content,
                pushType: nil // Lokale Updates, kein Push
            )
        } catch {
            print("Failed to start Live Activity: \(error)")
        }
    }

    // MARK: - Update

    static func updateActivity(isRunning: Bool, isPaused: Bool, pauseMinutes: Int) {
        guard let activity = currentActivity else { return }

        let state = WorkSessionAttributes.ContentState(
            isRunning: isRunning,
            isPaused: isPaused,
            pauseMinutes: pauseMinutes
        )
        let content = ActivityContent(state: state, staleDate: nil)

        Task {
            await activity.update(content)
        }
    }

    // MARK: - End

    static func endActivity(pauseMinutes: Int) {
        guard let activity = currentActivity else { return }

        let finalState = WorkSessionAttributes.ContentState(
            isRunning: false, isPaused: false, pauseMinutes: pauseMinutes
        )
        let content = ActivityContent(state: finalState, staleDate: nil)

        Task {
            // Bleibt 60 Sekunden sichtbar nach Beendigung
            await activity.end(content, dismissalPolicy: .after(.now + 60))
        }

        currentActivity = nil
    }

    static func endActivityImmediately() {
        guard let activity = currentActivity else { return }

        let finalState = WorkSessionAttributes.ContentState(
            isRunning: false, isPaused: false, pauseMinutes: 0
        )
        let content = ActivityContent(state: finalState, staleDate: nil)

        Task {
            await activity.end(content, dismissalPolicy: .immediate)
        }

        currentActivity = nil
    }

    // MARK: - Cleanup

    /// Bereinigt verwaiste Activities (z.B. nach App-Crash)
    static func cleanupStaleActivities() {
        for activity in Activity<WorkSessionAttributes>.activities {
            Task {
                let state = WorkSessionAttributes.ContentState(
                    isRunning: false, isPaused: false, pauseMinutes: 0
                )
                await activity.end(
                    ActivityContent(state: state, staleDate: nil),
                    dismissalPolicy: .immediate
                )
            }
        }
        currentActivity = nil
    }
}
```

### Integration in TimeTrackingViewModel.swift

```swift
// startSession() -- am Ende:
LiveActivityManager.startActivity(startTime: session.startTime)

// stopSession() -- am Ende:
LiveActivityManager.updateActivity(isRunning: false, isPaused: false,
                                    pauseMinutes: accumulatedPauseMinutes)

// finishSession() -- am Ende:
LiveActivityManager.endActivity(pauseMinutes: accumulatedPauseMinutes)

// pauseSession() -- am Ende:
LiveActivityManager.updateActivity(isRunning: true, isPaused: true,
                                    pauseMinutes: accumulatedPauseMinutes)

// resumeSession() -- am Ende:
LiveActivityManager.updateActivity(isRunning: true, isPaused: false,
                                    pauseMinutes: accumulatedPauseMinutes)
```

### Integration in FakturusTrackApp.swift

```swift
// Bei App-Start: Verwaiste Activities bereinigen
.task {
    // Pruefen ob es eine aktive Session gibt
    // Falls nicht, aber eine Activity laeuft -> bereinigen
    if /* kein aktiver Timer */ {
        LiveActivityManager.cleanupStaleActivities()
    }
}
```

---

## Kritische Implementierungsdetails

1. **Text(timerInterval:)**: Dieser SwiftUI-Text aktualisiert sich automatisch jede Sekunde -- auch in Widgets und Live Activities. Kein manueller Timer noetig.

2. **Live Activity Limit**: iOS erlaubt max. 1 Live Activity pro App-Instanz zur gleichen Zeit. Da unser Timer-Modell immer nur 1 aktive Session hat, passt das perfekt.

3. **12-Stunden-Limit**: iOS beendet Live Activities automatisch nach 12 Stunden. Bei sehr langen Arbeitstagen (>12h, was sowieso gegen ArbZG verstoesst) verschwindet die Activity vom Lock Screen. Die App-interne Timer-Logik laeuft natuerlich weiter.

4. **Widget Extension Target**: Die Live Activity View (`WorkSessionLiveActivity`) lebt im **Widget Extension Target**, nicht in der Haupt-App. Die `WorkSessionAttributes` und `LiveActivityManager` leben in der Haupt-App und werden von der Widget Extension ueber das Framework/Module referenziert.

5. **Graceful Degradation**: Auf iPhones ohne Dynamic Island (vor iPhone 14 Pro) zeigt iOS nur die Lock Screen Live Activity. Das Layout muss ohne Dynamic Island funktionieren -- was es tut, da Lock Screen und Dynamic Island separate Views sind.
