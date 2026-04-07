# Parallel-Implementation-Guide Phase 3

## Ueberblick

Phase 3 hat die hoechste Parallelitaet aller Phasen: Bis zu 6 unabhaengige Arbeitsstroeme. Dieser Guide definiert die Contracts zwischen den Stroemen, die Mocks fuer isolierte Entwicklung und die Merge-Reihenfolge.

---

## 1. Contracts (Schnittstellen zwischen Features)

### Contract A: Timer-State-Bridge (Haupt-App -> Widget/Watch/LiveActivity)

Alle drei Erweiterungen (Widget, Watch, Live Activity) brauchen den aktuellen Timer-State. Der Contract ist `SharedDefaults` (iOS) bzw. `WidgetStateHelper` (Android).

**iOS Contract**:
```swift
// SharedDefaults.writeTimerState() wird von TimeTrackingViewModel aufgerufen
// SharedDefaults.readTimerState() wird von Widget/Watch/LiveActivity gelesen
struct TimerStateContract {
    let isRunning: Bool
    let startDate: Date?
    let isPaused: Bool
    let pauseMinutes: Int
    let todayTotalSeconds: Int
}
```

**Android Contract**:
```kotlin
// WidgetStateHelper.writeTimerState() wird von TimeTrackingViewModel aufgerufen
// WidgetStateHelper.readTimerState() wird von Widget gelesen
data class TimerWidgetState(
    val isRunning: Boolean,
    val startTimeMillis: Long?,
    val isPaused: Boolean,
    val pauseMinutes: Int,
    val todayTotalSeconds: Long
)
```

**Wer implementiert was**:
- **SharedDefaults / WidgetStateHelper**: Wird als erstes implementiert (Welle 1, Tag 1)
- **TimeTrackingViewModel Erweiterung**: Aufrufe am Ende von start/stop/pause/resume/finish
- **Widget/Watch/LiveActivity**: Lesen den State -- koennen parallel dazu entwickelt werden

### Contract B: Watch-Actions (Watch -> iPhone)

```swift
// Watch sendet eine Action, iPhone fuehrt sie aus
enum TimerAction: String {
    case start, stop, pause, resume, finish
}
```

**Wer implementiert was**:
- **WatchConnectivityManager**: Definiert das Protokoll (beide Seiten)
- **Watch-App**: Sendet Actions
- **FakturusTrackApp / TimeTrackingViewModel**: Empfaengt und fuehrt aus

### Contract C: Personalnummer (Settings -> DATEV-Export)

```swift
// UserSettings hat neues Property:
var personalNumber: String?
```

**Wer implementiert was**:
- **E10 (Settings)**: Fuegt das UI-Feld und die Persistenz hinzu
- **E09 (DATEV)**: Liest den Wert aus UserSettings -- kann mit leerem String arbeiten solange Settings nicht fertig ist

### Contract D: Appearance (Settings -> Theme)

```swift
// iOS: @AppStorage("appearance") -- "system" / "light" / "dark"
// Android: DataStore stringPreferencesKey("appearance")
```

**Wer implementiert was**:
- **E01 (Dark Mode)**: Implementiert die Theme-Logik, die auf den Key reagiert
- **E10 (Settings)**: Implementiert den Picker, der den Key schreibt

---

## 2. Mocks fuer isolierte Entwicklung

### Mock A: SharedDefaults (fuer Widget/Watch/LiveActivity Entwicklung)

Bevor `TimeTrackingViewModel` die `SharedDefaults.writeTimerState()` Aufrufe hat, koennen Widget/Watch/LiveActivity mit Mock-Daten entwickelt werden:

```swift
// Fuer Preview und isolierte Entwicklung:
#if DEBUG
extension SharedDefaults {
    static func writeMockState() {
        writeTimerState(
            isRunning: true,
            startDate: Date().addingTimeInterval(-3600 * 2), // Vor 2h gestartet
            isPaused: false,
            pauseMinutes: 30,
            todayTotalSeconds: 7200
        )
    }
}
#endif
```

```kotlin
// Android: Mock-State fuer Widget-Preview
#if DEBUG
suspend fun writeMockWidgetState(context: Context) {
    WidgetStateHelper.writeTimerState(
        context = context,
        isRunning = true,
        startTimeMillis = System.currentTimeMillis() - 7200_000,
        isPaused = false,
        pauseMinutes = 30,
        todayTotalSeconds = 7200
    )
}
#endif
```

### Mock B: WatchConnectivityManager (fuer Watch-App ohne iPhone)

```swift
// Fuer Watch-Simulator ohne gekoppeltes iPhone:
#if DEBUG
class MockWatchConnectivityManager {
    var mockTimerState = WatchConnectivityManager.TimerState(
        isRunning: true, isPaused: false,
        startTime: Date().addingTimeInterval(-3600),
        elapsedSeconds: 3600, pauseMinutes: 0, todayTotalSeconds: 0
    )

    func sendAction(_ action: WatchConnectivityManager.TimerAction) {
        switch action {
        case .start: mockTimerState = TimerState(isRunning: true, ...)
        case .stop: mockTimerState = TimerState(isRunning: false, ...)
        // ...
        }
    }
}
#endif
```

### Mock C: Personalnummer (fuer DATEV-Export ohne Settings)

```swift
// DATEVExporter akzeptiert personalNumber als Parameter
// Wenn Settings noch nicht fertig: leerer String uebergeben
let datev = DATEVExporter.generateExport(
    ..., personalNumber: ""  // Wird "00000" als Default
)
```

---

## 3. Integrationspunkte

### Integration 1: TimeTrackingViewModel Erweiterung (Welle 1 Ende)

Am Ende von Welle 1 muessen alle Timer-Actions in `TimeTrackingViewModel` folgende Aufrufe haben:

```swift
func startSession() {
    // ... bestehender Code ...

    // Phase 3 Erweiterungen:
    SharedDefaults.writeTimerState(...)          // E03 Widget
    LiveActivityManager.startActivity(...)       // E04 Live Activity
    WatchConnectivityManager.shared.sendTimerState(...) // E02 Watch
    HapticManager.timerStart()                   // E01 Haptics
}
```

**Reihenfolge der Integration**: Die Aufrufe koennen einzeln hinzugefuegt werden, sobald das jeweilige Feature fertig ist. Jeder Aufruf ist unabhaengig -- wenn einer fehlt, funktioniert der Rest trotzdem.

### Integration 2: WidgetBundle (E03 + E04)

Widget und Live Activity teilen sich das Widget Extension Target. Reihenfolge:
1. E03 erstellt das Target mit `TimerWidget`
2. E04 fuegt `WorkSessionLiveActivity` zum Bundle hinzu

```swift
@main
struct FakturusTrackWidgetBundle: WidgetBundle {
    var body: some Widget {
        TimerWidget()                  // E03 -- zuerst
        WorkSessionLiveActivity()      // E04 -- spaeter ergaenzt
    }
}
```

### Integration 3: App Group (E02 + E03 + E04)

Alle drei Features nutzen `group.com.fakturus.track`. Die App Group muss in **allen relevanten Targets** aktiviert sein:
- Haupt-App (FakturusTrack)
- Widget Extension (FakturusTrackWidget)
- Watch App (FakturusTrackWatch)

**Wer erstellt die App Group**: Das erste Feature das sie braucht (E03 Widget-Setup oder E02 Watch-Setup). Die anderen Features fuegen sie dann zu ihrem Target hinzu.

### Integration 4: SettingsView Erweiterung (E10 + E01)

E01 implementiert die Dark-Mode-Logik (`preferredColorScheme`). E10 implementiert den Settings-Picker. Reihenfolge:
1. E01 implementiert `@AppStorage("appearance")` in `FakturusTrackApp` und die Theme-Logik
2. E10 implementiert den Picker in `SettingsView` der den gleichen Key schreibt

Solange E10 noch nicht fertig ist, kann der User ueber System-Settings den Dark Mode steuern. Der App-interne Picker ist nur Komfort.

### Integration 5: OverviewScreen Erweiterung (E09)

E09 fuegt den DATEV-Button zum bestehenden Export-Bereich in `OverviewScreen` hinzu. Reine UI-Ergaenzung, kein Konflikt mit bestehenden PDF/CSV Buttons.

---

## 4. Merge-Reihenfolge

### Prinzip: Feature-Branches, kein Monsterbranch

Jedes EPIC oder jeder Strang bekommt einen eigenen Branch. Merge in `main` nach DoD.

### Empfohlene Merge-Reihenfolge (Welle 1)

```
1. feat/p3-polish-darkmode        -- E01 S01/S02 (Dark Mode)
2. feat/p3-polish-haptics         -- E01 S03/S04 (Haptics)
3. feat/p3-widget-setup           -- E03 S01 (App Group + Widget Target)
4. feat/p3-watch-setup            -- E02 S01 (Watch Target)
5. feat/p3-polish-animations      -- E01 S05-S09 (Rest von E01)
6. feat/p3-widget-ui              -- E03 S02+S03 (Widget UI + Actions)
7. feat/p3-live-activity           -- E04 (Live Activity)
8. feat/p3-watch-app              -- E02 S02-S05 (Watch Connectivity + UI)
9. feat/p3-android-widget         -- E05 (Android Widget)
10. feat/p3-performance           -- E07 (Performance)
```

**Warum diese Reihenfolge**:
- Dark Mode zuerst: Alle nachfolgenden Features nutzen Theme-Farben
- App Group Setup vor Widget/Watch/LiveActivity: Alle brauchen die gleiche Group
- Widget-Target vor Live Activity: Live Activity lebt im gleichen Target

### Empfohlene Merge-Reihenfolge (Welle 2)

```
11. feat/p3-accessibility         -- E06 (A11y Audit)
12. feat/p3-app-settings          -- E10 (Settings-Erweiterung)
13. feat/p3-localization          -- E08 (String-Extraktion)
```

**Warum diese Reihenfolge**:
- A11y zuerst: Aendert Labels in allen Views -- vor Lokalisierung sinnvoll
- Settings vor Lokalisierung: Neue Strings aus E10 werden in E08 mit extrahiert

### Empfohlene Merge-Reihenfolge (Welle 3)

```
14. feat/p3-datev-export          -- E09 (DATEV Generator + UI)
```

---

## 5. Branch-Konflikte minimieren

### Hoch-Risiko-Dateien (mehrere EPICs aendern die gleiche Datei)

| Datei | Geaendert von | Strategie |
|-------|---------------|-----------|
| `TimeTrackingViewModel.swift/kt` | E01 (Haptics), E02 (Watch), E03 (SharedDefaults), E04 (LiveActivity) | Jedes EPIC fuegt **nur Aufrufe am Ende** der bestehenden Methoden hinzu. Kein Refactoring der Methoden selbst. |
| `FakturusTrackApp.swift` | E01 (Dark Mode), E02 (Watch Activation), E04 (LA Cleanup) | Separate Code-Bloecke, keine Ueberlappung. |
| `SettingsView.swift/kt` | E10 (App-Settings) | Nur E10 aendert diese Datei in Phase 3. |
| `OverviewScreen.swift/kt` | E09 (DATEV Button) | Nur E09 aendert diese Datei. |

### Niedrig-Risiko-Dateien (je nur von einem EPIC geaendert)

Fast alle neuen Dateien (Widget, Watch, LiveActivity, DATEVExporter) sind nur von einem EPIC betroffen -- kein Merge-Konflikt moeglich.

### Strategie fuer TimeTrackingViewModel

Da mehrere EPICs `TimeTrackingViewModel` aendern, empfehle ich:

1. **E01 Haptics**: Fuegt `HapticManager.xyz()` Aufrufe in start/stop/pause/finish hinzu
2. **E03 Widget**: Fuegt `SharedDefaults.writeTimerState(...)` Aufrufe hinzu
3. **E04 LiveActivity**: Fuegt `LiveActivityManager.xyz(...)` Aufrufe hinzu
4. **E02 Watch**: Fuegt `WatchConnectivityManager.shared.sendTimerState(...)` hinzu

Alle Aufrufe sind **additiv** (neue Zeile am Ende einer Methode). Keine Methode wird refactored oder umstrukturiert. Merge-Konflikte sind minimal und trivial zu loesen (alle Zeilen beibehalten).

---

## 6. Agent-Zuweisung (bei 2 Agents)

### iOS-Agent Reihenfolge

```
Tag 1-2:  E01 (Dark Mode + Haptics + Animationen)
Tag 3-4:  E03 (Widget Setup + UI + Actions)
Tag 5:    E04 (Live Activity)
Tag 6-8:  E02 (Watch App komplett)
Tag 9:    E07 (Performance Profiling + Optimierung)
Tag 10:   E06 (VoiceOver Audit)
Tag 11:   E08 (String-Extraktion)
Tag 12:   E10 (App-Settings)
Tag 13:   E09 (DATEV iOS)
Tag 14-15: Integration + Testing
```

### Android-Agent Reihenfolge

```
Tag 1-2:  E01 (Dark Mode + Haptics + Animationen)
Tag 3-5:  E05 (Android Widget + Shortcuts)
Tag 6:    E07 (Performance Profiling + Optimierung)
Tag 7:    E06 (TalkBack Audit)
Tag 8:    E08 (String-Extraktion)
Tag 9:    E10 (App-Settings)
Tag 10:   E09 (DATEV Android)
Tag 11-12: Integration + Testing
```

### Synchronisationspunkte

| Zeitpunkt | Sync-Event |
|-----------|-----------|
| Welle 1 Ende (Tag 8) | SharedDefaults/WidgetStateHelper Contract verifizieren: Widget zeigt gleichen State wie App |
| Welle 2 Start (Tag 9) | E01 muss abgeschlossen sein (finale UI fuer A11y-Audit und String-Extraktion) |
| Welle 3 Start (Tag 12) | E10 Personalnummer-Feld muss existieren fuer DATEV UI-Integration |
| Welle 4 (Tag 14) | Alle Features integriert, Cross-Feature-Tests |

---

## 7. Integrations-Test-Matrix

| Test | Beteiligte Features | Erwartetes Verhalten |
|------|--------------------|--------------------|
| Timer via Widget starten -> App oeffnen | E03 + App | Timer laeuft in App |
| Timer in App starten -> Widget zeigt Status | App + E03 | Widget zeigt "Laeuft" |
| Timer starten -> Lock Screen | App + E04 | Live Activity sichtbar |
| Timer stoppen -> Live Activity verschwindet | App + E04 | Activity endet |
| Watch Start -> iPhone Timer laeuft | E02 + App | Bidirektionale Kommunikation |
| Dark Mode Toggle -> alle Screens | E10 + E01 | Sofortiger Wechsel |
| VoiceOver + Dark Mode | E06 + E01 | Labels funktionieren in beiden Modi |
| Englische Sprache + DATEV-Export | E08 + E09 | DATEV bleibt deutsch, UI englisch |
| 200 Sessions + 60fps | E07 + alle | Kein Performance-Regression |
