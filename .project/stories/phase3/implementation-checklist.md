# Implementation-Checkliste -- Phase 3

Dieses Dokument muss jeder Entwickler / AI-Agent lesen, bevor er eine Phase-3-Story anfaengt. Es ergaenzt die Checklisten aus Phase 1 und Phase 2 um Phase-3-spezifische Hinweise.

---

## 1. Phase-1- und Phase-2-Checklisten gelten weiterhin

Alle Konventionen aus den vorherigen Phasen gelten unveraendert:
- Namenskonventionen (Dateien, Klassen, Properties, DTOs)
- Ordnerstruktur (neue Features in bestehende Ordner integrieren)
- Code-Stil (Deutsch fuer UI-Strings, Englisch fuer Code)
- Error Handling (ViewModels fangen, SyncEngine loggt)
- Git-Workflow (Branch-Naming, Commit-Messages)
- Definition of Done (Akzeptanzkriterien, Tests, Previews)
- Feiertag-Berechnung (HolidayCalculator nutzen)
- Kalender-Spezifika (Montag als Wochenstart, Bitmask fuer Arbeitstage)

**ACHTUNG**: Phase-1- und Phase-2-Features NICHT brechen! Alle bestehenden Funktionen (Timer, History, Sync, Pausen, Kalender, Export, Settings) muessen weiterhin funktionieren.

---

## 2. Phase-3-spezifische Ordner

### Neue Feature-Ordner

**iOS:**
```
FakturusTrackWidget/           -- Widget Extension Target (NEU)
  TimerWidget.swift
  WidgetTimelineProvider.swift
  SharedDefaults.swift

FakturusTrackWatch/            -- watchOS Target (NEU)
  WatchTimerScreen.swift
  WatchTimerViewModel.swift
  WatchComplication.swift

Shared/
  WatchConnectivityManager.swift  -- Geteilt zwischen iOS + watchOS
  HapticManager.swift
  AppearanceManager.swift

Features/
  Settings/
    AppSettingsSection.swift    -- Neue Sektion in bestehendem Settings-Screen

Services/
  Export/
    DATEVExporter.swift        -- Neuer Exporter neben PDFReportGenerator + CSVExporter
```

**Android:**
```
widget/                        -- Widget Package (NEU)
  TimerWidgetReceiver.kt
  TimerWidgetContent.kt
  TimerWidgetActions.kt

util/
  HapticManager.kt
  AppearanceManager.kt

features/
  settings/
    AppSettingsSection.kt      -- Neue Sektion

services/
  export/
    DATEVExporter.kt
```

---

## 3. App Group & Shared State

### iOS App Group (Widget + Watch + Live Activity)

```
group.com.fakturus.track
```

Diese App Group wird von 4 Targets geteilt:
- Haupt-App
- Widget Extension
- Watch Extension
- Live Activity (Teil der Haupt-App, nutzt aber App Group fuer Widget-Sync)

**SharedDefaults-Pattern:**
```swift
struct SharedDefaults {
    static let suiteName = "group.com.fakturus.track"
    static let defaults = UserDefaults(suiteName: suiteName)!

    // Keys
    static let isTimerRunning = "isTimerRunning"
    static let timerStartDate = "timerStartDate"
    static let isPaused = "isPaused"
    static let pauseMinutes = "pauseMinutes"
    static let todayTotalSeconds = "todayTotalSeconds"
}
```

**Wichtig: State in App Group aktualisieren bei JEDER Timer-Aenderung!**
- Timer Start -> SharedDefaults schreiben + `WidgetCenter.shared.reloadAllTimelines()`
- Timer Stop -> SharedDefaults schreiben + `WidgetCenter.shared.reloadAllTimelines()`
- Timer Pause/Resume -> SharedDefaults schreiben + Widget reload
- Session Finish -> SharedDefaults schreiben + Widget reload

### Android Widget State

```kotlin
// DataStore fuer Widget-State
val Context.widgetDataStore by preferencesDataStore(name = "timer_widget")

object WidgetKeys {
    val IS_TIMER_RUNNING = booleanPreferencesKey("isTimerRunning")
    val TIMER_START_MILLIS = longPreferencesKey("timerStartMillis")
    val IS_PAUSED = booleanPreferencesKey("isPaused")
    val PAUSE_MINUTES = intPreferencesKey("pauseMinutes")
    val TODAY_TOTAL_SECONDS = longPreferencesKey("todayTotalSeconds")
}
```

---

## 4. Dark Mode Implementierung

### Farbstrategie

**KEINE hardcodierten Farben mehr!** Alle Farben muessen semantisch sein:

| Semantisch | Light | Dark | Verwendung |
|------------|-------|------|-----------|
| primaryBackground | White | SystemGray6 / Dark | Screen-Hintergrund |
| secondaryBackground | SystemGray6 | SystemGray5 | Card-Hintergrund |
| primaryText | Black | White | Haupttext |
| secondaryText | SystemGray | SystemGray2 | Untertitel |
| accentColor | AppBlue | AppBlue (heller) | Buttons, Links |
| vacationColor | Cyan | Cyan (heller) | Urlaubstage |
| sickColor | Red | Red (heller) | Krankheitstage |
| holidayColor | Purple | Purple (heller) | Feiertage |
| timerRunning | Green | Green | Laufender Timer |
| timerStopped | Orange | Orange | Gestoppter Timer |
| overtimePlus | Green | Green (heller) | Positive Ueberstunden |
| overtimeMinus | Red | Red (heller) | Negative Ueberstunden |

**iOS:** Color Assets in `Assets.xcassets` mit "Any" + "Dark" Variante
**Android:** `colors.xml` + `colors.xml (night)` oder `DynamicColorScheme`

### App-weites Theme-Override

**iOS:**
```swift
// In Root-View (z.B. ContentView oder App struct)
@AppStorage("appearance") private var appearance: String = "system"

var body: some Scene {
    WindowGroup {
        ContentView()
            .preferredColorScheme(colorScheme(for: appearance))
    }
}

func colorScheme(for appearance: String) -> ColorScheme? {
    switch appearance {
    case "light": return .light
    case "dark": return .dark
    default: return nil  // System
    }
}
```

**Android:**
```kotlin
// In Theme.kt
val appearance = settingsDataStore.appearance.collectAsState()
val darkTheme = when (appearance.value) {
    "light" -> false
    "dark" -> true
    else -> isSystemInDarkTheme()
}
FakturusTrackTheme(darkTheme = darkTheme) { ... }
```

---

## 5. Accessibility-Konventionen

### VoiceOver Labels (iOS)

Jede Custom-View MUSS VoiceOver-Labels haben:

```swift
// FALSCH:
Text("\(hours):\(minutes)")

// RICHTIG:
Text("\(hours):\(minutes)")
    .accessibilityLabel("\(hours) Stunden \(minutes) Minuten")
```

### TalkBack Semantics (Android)

```kotlin
// FALSCH:
Text(text = "$hours:$minutes")

// RICHTIG:
Text(
    text = "$hours:$minutes",
    modifier = Modifier.semantics {
        contentDescription = "$hours Stunden $minutes Minuten"
    }
)
```

### Accessibility-Checkliste fuer jede Story

- [ ] Alle interaktiven Elemente haben Labels
- [ ] Keine Information wird NUR durch Farbe vermittelt
- [ ] Fokus-Reihenfolge ist logisch (oben -> unten, links -> rechts)
- [ ] Custom Actions wo noetig (Swipe-Gesten haben alternative Bedienung)
- [ ] Timer: `updatesFrequently` Trait gesetzt (verhindert staendiges Vorlesen)

---

## 6. Lokalisierung-Konventionen

### String-Keys

Konsistentes Naming fuer Lokalisierungs-Keys:

```
{screen}_{element}_{detail}

Beispiele:
times_tab_title = "Zeiten"
times_timer_start = "Starten"
times_timer_stop = "Stoppen"
times_history_entries = "%d Eintraege"
vacation_remaining = "Resturlaub"
overview_overtime = "Ueberstunden"
settings_appearance = "Erscheinungsbild"
error_sync_failed = "Synchronisation fehlgeschlagen"
```

### Regel: Strings von Anfang an lokalisierbar

**Alle neuen User-facing Strings ab Phase 3 MUESSEN sofort mit Lokalisierungs-Keys erstellt werden.** Kein nachtraegliches Extrahieren. Jeder neue String wird direkt in `Localizable.strings` (iOS) bzw. `strings.xml` (Android) angelegt -- auch wenn die englische Uebersetzung erst in E08 kommt. Der Key wird sofort verwendet, der deutsche Wert ist der Default.

### KEIN hardcodierter Text in Views

```swift
// FALSCH:
Text("Starten")

// RICHTIG:
Text(String(localized: "times_timer_start"))
```

```kotlin
// FALSCH:
Text("Starten")

// RICHTIG:
Text(stringResource(R.string.times_timer_start))
```

### Rechtliche Texte (ArbZG-Hinweise)

ArbZG-Hinweise werden auch auf Englisch uebersetzt, aber mit Disclaimer:
```
DE: "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
EN: "Reminder: After 6 hours of work, you are entitled to a break of at least 30 minutes (German Working Hours Act)."
```

---

## 7. Widget-Spezifika

### WidgetKit Timeline-Strategie (iOS)

```
Timer laeuft:
  -> Timeline mit Entries alle 1 Minute fuer die naechsten 60 Minuten
  -> Danach: System fragt neuen Timeline an
  -> PLUS: App triggert WidgetCenter.reloadAllTimelines() bei State-Aenderung

Timer laeuft nicht:
  -> Einzelner Entry (statisch)
  -> Aktualisierung nur bei State-Aenderung (App triggert Reload)
```

### Glance Update-Strategie (Android)

> **Wichtig**: Android Glance Widgets haben ein **Minimum-Update-Intervall von 15 Minuten**. Der Timer kann daher nicht live im Widget zaehlen. Stattdessen wird "Laeuft seit HH:MM" mit der Startzeit angezeigt (z.B. "Laeuft seit 08:30"). Das Widget aktualisiert sich bei manuellen State-Aenderungen sofort via BroadcastReceiver.

```
Timer laeuft:
  -> WorkManager Periodic (15 Min Minimum) fuer Hintergrund-Updates
  -> BroadcastReceiver fuer sofortige Updates bei State-Aenderung
  -> App ruft GlanceAppWidget.update() bei Timer-Aenderungen auf
  -> Anzeige: "Laeuft seit HH:MM" (Startzeit), NICHT live zaehlendes HH:MM:SS

Timer laeuft nicht:
  -> Kein periodisches Update noetig
  -> Update nur bei State-Aenderung
```

---

## 8. Performance-Budget

| Metrik | Ziel | Messung | Werkzeug |
|--------|------|---------|---------|
| Cold Start | < 1.0s | Time-to-interactive | Instruments / Android Profiler |
| Tab-Wechsel | < 200ms | Perceived latency | Stopwatch / Profiler |
| History Scroll (200 Items) | 60fps | Frame rate | Core Animation / GPU Inspector |
| Kalender Monatswechsel | < 100ms | Perceived latency | Stopwatch |
| Memory (normale Nutzung) | < 50MB iOS, < 80MB Android | Peak usage | Memory Profiler |
| Memory Leaks | 0 | Leak count | Instruments Leaks / Android Profiler |
| Sync (50 Sessions) | < 3s | API round-trip | Network Profiler |

### Lazy Loading Pflicht

- History: Nur sichtbare Monate laden (nicht alle Sessions auf einmal)
- Kalender: Nur aktueller Monat + 1 Voraus
- Gesamt-Tab: Cache nutzen, API nur bei Pull-to-Refresh oder Tab-Wechsel nach > 5 Min

---

## 9. DATEV-Export Format

### Minimale Spezifikation (wird in E09-S01 finalisiert)

```csv
Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause
12345;01.03.2026;200;8.00;08:30;17:00;30
12345;02.03.2026;200;8.00;09:00;17:30;30
12345;03.03.2026;500;8.00;;;
12345;10.03.2026;400;8.00;;;
```

Lohnarten (konfigurierbar in den DATEV-Einstellungen, Default-Werte):
- 200 = Gehalt/Arbeit
- 400 = Urlaub
- 500 = Krankheit

> **Beta-Hinweis**: DATEV-Export ist als Beta-Feature markiert. Format muss mit einem Steuerberater validiert werden bevor es als stabil gilt. Lohnarten sind von Anfang an konfigurierbar (nicht hardcoded), da sie je nach Steuerberater/Unternehmen variieren.

**Encoding**: UTF-8 (kein BOM -- DATEV erwartet ASCII/ANSI)
**Dezimaltrennzeichen**: Punkt (NICHT Komma -- Unterschied zu CSV-Export!)
**Spaltentrennzeichen**: Semikolon

---

## 10. Definition of Done (Phase 3 Story)

Eine Phase-3-Story ist "Done" wenn:

- [ ] Alle Akzeptanzkriterien aus dem EPIC-Dokument erfuellt
- [ ] Phase-1- und Phase-2-Features funktionieren weiterhin (Regressions-Check)
- [ ] Code kompiliert ohne Warnungen
- [ ] Dark Mode funktioniert (falls UI-Story)
- [ ] VoiceOver/TalkBack Labels vorhanden (falls UI-Story)
- [ ] Strings in Lokalisierungsdateien (keine hardcodierten Texte)
- [ ] Neue Screens haben funktionierende Previews
- [ ] Neue Dateien im richtigen Ordner/Target
- [ ] Widget/Watch: State in App Group aktualisiert
- [ ] Performance: Keine spuerbare Verschlechterung des App-Starts oder Scrollings
