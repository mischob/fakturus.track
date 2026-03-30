# Tech-Spec: EPIC 02 -- Apple Watch Companion App

## Uebersicht

Neues watchOS Target im Xcode-Projekt. Die Watch kommuniziert ausschliesslich mit dem iPhone via WatchConnectivity -- kein eigener API-Zugriff. Minimaler Scope: Timer-Steuerung und heutige Arbeitszeit.

---

## S01: watchOS Target & Projekt-Setup

### project.yml Erweiterung

```yaml
FakturusTrackWatch:
  type: application
  platform: watchOS
  deploymentTarget:
    watchOS: "10.0"
  sources:
    - path: FakturusTrackWatch
      excludes:
        - "**/.gitkeep"
  info:
    path: FakturusTrackWatch/Info.plist
    properties:
      CFBundleDisplayName: "Fakturus Track"
      WKApplication:
        WKCompanionAppBundleIdentifier: com.fakturus.track
  settings:
    base:
      PRODUCT_BUNDLE_IDENTIFIER: com.fakturus.track.watchkitapp
      SWIFT_VERSION: "6.0"
      CODE_SIGN_ENTITLEMENTS: FakturusTrackWatch/FakturusTrackWatch.entitlements
  entitlements:
    path: FakturusTrackWatch/FakturusTrackWatch.entitlements
    properties:
      com.apple.security.application-groups:
        - group.com.fakturus.track
```

### Haupt-App Target: WatchConnectivity hinzufuegen

Kein neues SPM-Paket noetig -- `WatchConnectivity` ist ein System-Framework.

### Ordnerstruktur

```
FakturusTrackWatch/
  FakturusTrackWatchApp.swift
  WatchTimerScreen.swift
  WatchTimerViewModel.swift
  WatchComplicationProvider.swift
  WatchComplicationViews.swift
  Info.plist
  FakturusTrackWatch.entitlements
  Assets.xcassets/              -- Watch App Icon (rund)
```

---

## S02: WatchConnectivity Manager

### Shared/WatchConnectivityManager.swift (NEU -- geteilt zwischen iOS und watchOS Target)

Diese Datei muss in **beide Targets** eingebunden werden (iOS App + watchOS App).

```swift
import Foundation
import WatchConnectivity

@Observable
final class WatchConnectivityManager: NSObject, WCSessionDelegate {
    static let shared = WatchConnectivityManager()

    var isReachable = false
    var receivedTimerState: TimerState?
    var receivedAction: TimerAction?

    // Timer-State den die Gegenseite empfaengt
    struct TimerState: Codable {
        let isRunning: Bool
        let isPaused: Bool
        let startTime: Date?
        let elapsedSeconds: Int
        let pauseMinutes: Int
        let todayTotalSeconds: Int
    }

    enum TimerAction: String, Codable {
        case start, stop, pause, resume, finish
    }

    // MARK: - Setup

    func activate() {
        guard WCSession.isSupported() else { return }
        WCSession.default.delegate = self
        WCSession.default.activate()
    }

    // MARK: - Senden

    /// iPhone -> Watch: Aktuellen Timer-State senden
    func sendTimerState(_ state: TimerState) {
        guard WCSession.default.activationState == .activated else { return }

        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        guard let data = try? encoder.encode(state),
              let dict = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
        else { return }

        // updateApplicationContext ist last-write-wins und ueberlebt App-Neustarts
        try? WCSession.default.updateApplicationContext(dict)
    }

    /// Watch -> iPhone: Aktion senden (mit Reply-Handler fuer Bestaetigung)
    func sendAction(_ action: TimerAction) {
        guard WCSession.default.isReachable else { return }

        WCSession.default.sendMessage(
            ["action": action.rawValue],
            replyHandler: { _ in /* Bestaetigung erhalten */ },
            errorHandler: { error in
                print("Watch action send failed: \(error.localizedDescription)")
            }
        )
    }

    // MARK: - WCSessionDelegate

    func session(_ session: WCSession, activationDidCompleteWith state: WCSessionActivationState, error: Error?) {
        DispatchQueue.main.async {
            self.isReachable = session.isReachable
        }
    }

    // Empfang von ApplicationContext (Timer-State)
    func session(_ session: WCSession, didReceiveApplicationContext applicationContext: [String: Any]) {
        guard let data = try? JSONSerialization.data(withJSONObject: applicationContext) else { return }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        if let state = try? decoder.decode(TimerState.self, from: data) {
            DispatchQueue.main.async {
                self.receivedTimerState = state
            }
        }
    }

    // Empfang von Messages (Aktionen)
    func session(_ session: WCSession, didReceiveMessage message: [String: Any],
                 replyHandler: @escaping ([String: Any]) -> Void) {
        if let actionStr = message["action"] as? String,
           let action = TimerAction(rawValue: actionStr) {
            DispatchQueue.main.async {
                self.receivedAction = action
            }
        }
        replyHandler(["status": "ok"])
    }

    func sessionReachabilityDidChange(_ session: WCSession) {
        DispatchQueue.main.async {
            self.isReachable = session.isReachable
        }
    }

    // iOS-only Pflicht-Delegates (auf watchOS nicht noetig, aber muessen deklariert werden)
    #if os(iOS)
    func sessionDidBecomeInactive(_ session: WCSession) {}
    func sessionDidDeactivate(_ session: WCSession) {
        WCSession.default.activate()
    }
    #endif
}
```

### Integration in FakturusTrackApp.swift (iPhone-Seite)

```swift
// In FakturusTrackApp.swift -- init oder onAppear:
WatchConnectivityManager.shared.activate()

// Reaktion auf Watch-Aktionen:
.onChange(of: WatchConnectivityManager.shared.receivedAction) { _, action in
    guard let action else { return }
    switch action {
    case .start: /* viewModel.startSession() via ServiceContainer */
    case .stop: /* viewModel.stopSession() */
    case .pause: /* viewModel.pauseSession() */
    case .resume: /* viewModel.resumeSession() */
    case .finish: /* viewModel.finishSession() */
    }
    WatchConnectivityManager.shared.receivedAction = nil
}
```

### Integration in TimeTrackingViewModel.swift

Bei jeder Timer-Aenderung den State an die Watch senden:

```swift
private func notifyWatch() {
    let state = WatchConnectivityManager.TimerState(
        isRunning: activeSession?.isRunning ?? false,
        isPaused: isPaused,
        startTime: activeSession?.startTime,
        elapsedSeconds: Int(Date().timeIntervalSince(activeSession?.startTime ?? Date())),
        pauseMinutes: accumulatedPauseMinutes,
        todayTotalSeconds: 0 // TODO: Tages-Summe berechnen
    )
    WatchConnectivityManager.shared.sendTimerState(state)
}
```

---

## S03: Watch Timer-Screen

### FakturusTrackWatchApp.swift

```swift
import SwiftUI
import WatchConnectivity

@main
struct FakturusTrackWatchApp: App {
    @State private var viewModel = WatchTimerViewModel()

    init() {
        WatchConnectivityManager.shared.activate()
    }

    var body: some Scene {
        WindowGroup {
            WatchTimerScreen(viewModel: viewModel)
        }
    }
}
```

### WatchTimerViewModel.swift

```swift
import Foundation
import Observation

@Observable
final class WatchTimerViewModel {
    var isRunning = false
    var isPaused = false
    var startTime: Date?
    var elapsedSeconds: Int = 0
    var pauseMinutes: Int = 0
    var todayTotalSeconds: Int = 0
    var isConnected = false

    private let connectivity = WatchConnectivityManager.shared

    init() {
        // State von ApplicationContext laden (falls Watch-App vor iPhone geoeffnet)
        updateFromConnectivity()
    }

    func updateFromConnectivity() {
        isConnected = connectivity.isReachable
        if let state = connectivity.receivedTimerState {
            isRunning = state.isRunning
            isPaused = state.isPaused
            startTime = state.startTime
            elapsedSeconds = state.elapsedSeconds
            pauseMinutes = state.pauseMinutes
            todayTotalSeconds = state.todayTotalSeconds
        }
    }

    func start() { connectivity.sendAction(.start) }
    func stop() { connectivity.sendAction(.stop) }
    func pause() { connectivity.sendAction(.pause) }
    func resume() { connectivity.sendAction(.resume) }
    func finish() { connectivity.sendAction(.finish) }
}
```

### WatchTimerScreen.swift

```swift
import SwiftUI

struct WatchTimerScreen: View {
    let viewModel: WatchTimerViewModel

    var body: some View {
        VStack(spacing: 12) {
            if !viewModel.isConnected {
                disconnectedView
            } else if viewModel.isRunning {
                runningView
            } else if viewModel.isPaused {
                pausedView
            } else {
                idleView
            }
        }
        .containerBackground(.fill.tertiary, for: .navigation)
        .onChange(of: WatchConnectivityManager.shared.receivedTimerState) { _, _ in
            viewModel.updateFromConnectivity()
        }
        .onChange(of: WatchConnectivityManager.shared.isReachable) { _, _ in
            viewModel.updateFromConnectivity()
        }
    }

    // MARK: - States

    @ViewBuilder
    private var idleView: some View {
        Text("Bereit")
            .font(.headline)
            .foregroundStyle(.secondary)

        if viewModel.todayTotalSeconds > 0 {
            Text("Heute: \(formattedDuration(viewModel.todayTotalSeconds))")
                .font(.caption)
        }

        Button(action: viewModel.start) {
            Image(systemName: "play.fill")
                .font(.title2)
                .frame(width: 60, height: 60)
        }
        .buttonStyle(.borderedProminent)
        .tint(.green)
    }

    @ViewBuilder
    private var runningView: some View {
        if let start = viewModel.startTime {
            Text(timerInterval: start...Date.distantFuture, countsDown: false)
                .font(.system(.title, design: .monospaced))
                .monospacedDigit()
        }

        Text("Seit \(formattedTime(viewModel.startTime))")
            .font(.caption)
            .foregroundStyle(.secondary)

        HStack(spacing: 8) {
            Button(action: viewModel.pause) {
                Image(systemName: "pause.fill")
            }
            .buttonStyle(.bordered)

            Button(action: viewModel.stop) {
                Image(systemName: "stop.fill")
            }
            .buttonStyle(.bordered)
            .tint(.red)
        }
    }

    @ViewBuilder
    private var pausedView: some View {
        Image(systemName: "pause.circle.fill")
            .font(.title)
            .foregroundStyle(.orange)

        Text("Pausiert")
            .font(.headline)

        Text("\(viewModel.pauseMinutes) Min")
            .font(.caption)
            .foregroundStyle(.secondary)

        HStack(spacing: 8) {
            Button(action: viewModel.resume) {
                Image(systemName: "play.fill")
            }
            .buttonStyle(.borderedProminent)

            Button(action: viewModel.stop) {
                Image(systemName: "stop.fill")
            }
            .buttonStyle(.bordered)
            .tint(.red)
        }
    }

    @ViewBuilder
    private var disconnectedView: some View {
        Image(systemName: "iphone.slash")
            .font(.title)
            .foregroundStyle(.secondary)
        Text("iPhone nicht\nverbunden")
            .font(.headline)
            .multilineTextAlignment(.center)
    }

    // MARK: - Helpers

    private func formattedDuration(_ seconds: Int) -> String {
        let h = seconds / 3600
        let m = (seconds % 3600) / 60
        return "\(h):\(String(format: "%02d", m))h"
    }

    private func formattedTime(_ date: Date?) -> String {
        guard let date else { return "--:--" }
        return date.formatted(date: .omitted, time: .shortened)
    }
}
```

---

## S04: Watch Complication

### WatchComplicationProvider.swift

```swift
import WidgetKit
import SwiftUI

struct WatchComplicationProvider: TimelineProvider {
    func placeholder(in context: Context) -> ComplicationEntry {
        ComplicationEntry(date: Date(), isRunning: false, timerText: "Bereit")
    }

    func getSnapshot(in context: Context, completion: @escaping (ComplicationEntry) -> Void) {
        let entry = readCurrentState()
        completion(entry)
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<ComplicationEntry>) -> Void) {
        let entry = readCurrentState()
        // Aktualisiert sich bei naechstem ApplicationContext-Update
        let timeline = Timeline(entries: [entry], policy: .after(Date().addingTimeInterval(300)))
        completion(timeline)
    }

    private func readCurrentState() -> ComplicationEntry {
        let defaults = UserDefaults(suiteName: "group.com.fakturus.track")
        let isRunning = defaults?.bool(forKey: "isTimerRunning") ?? false
        let text = isRunning ? "Laeuft" : "Bereit"
        return ComplicationEntry(date: Date(), isRunning: isRunning, timerText: text)
    }
}

struct ComplicationEntry: TimelineEntry {
    let date: Date
    let isRunning: Bool
    let timerText: String
}
```

### WatchComplicationViews.swift

```swift
import WidgetKit
import SwiftUI

struct FakturusWatchComplication: Widget {
    let kind = "FakturusTrackComplication"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: WatchComplicationProvider()) { entry in
            ComplicationView(entry: entry)
        }
        .configurationDisplayName("Fakturus Track")
        .description("Timer-Status auf dem Zifferblatt")
        .supportedFamilies([
            .accessoryCircular,
            .accessoryRectangular,
            .accessoryInline
        ])
    }
}

struct ComplicationView: View {
    @Environment(\.widgetFamily) var family
    let entry: ComplicationEntry

    var body: some View {
        switch family {
        case .accessoryCircular:
            circularView
        case .accessoryRectangular:
            rectangularView
        case .accessoryInline:
            inlineView
        default:
            Text(entry.timerText)
        }
    }

    private var circularView: some View {
        ZStack {
            AccessoryWidgetBackground()
            Image(systemName: entry.isRunning ? "timer" : "clock")
                .foregroundStyle(entry.isRunning ? .green : .secondary)
        }
    }

    private var rectangularView: some View {
        HStack {
            Image(systemName: entry.isRunning ? "timer" : "clock")
                .foregroundStyle(entry.isRunning ? .green : .secondary)
            VStack(alignment: .leading) {
                Text("Arbeit")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
                Text(entry.timerText)
                    .font(.headline)
            }
        }
    }

    private var inlineView: some View {
        Text("Arbeit: \(entry.timerText)")
    }
}
```

---

## Kritische Implementierungsdetails

1. **WatchConnectivity Target-Membership**: `WatchConnectivityManager.swift` muss in BEIDE Targets (iOS + watchOS) eingebunden sein. Das `#if os(iOS)` Guard stellt sicher, dass iOS-only Delegates nur auf dem iPhone kompilieren.

2. **App Group**: `group.com.fakturus.track` muss in allen 4 Targets aktiviert sein (Haupt-App, Widget, Watch, Tests). Die Watch nutzt die App Group fuer Complication-Daten.

3. **Background-Verhalten**: Wenn das iPhone im Hintergrund ist und die Watch eine Aktion sendet, weckt `WCSession.sendMessage` die iPhone-App kurz auf. Das funktioniert zuverlaessig.

4. **Simulator-Testing**: Watch-Simulator kommuniziert mit iPhone-Simulator. Fuer echte Latenz-Tests braucht man physische Geraete.
