# Technischer Gesamtplan Phase 3

## Ziel-Dateistruktur am Ende von Phase 3

### iOS -- Neue Dateien und Targets

```
FakturusTrack/
  FakturusTrack/
    App/
      FakturusTrackApp.swift              MODIFIZIERT: +preferredColorScheme, +Live Activity Cleanup
      Theme.swift                         MODIFIZIERT: +fehlende Dark-Mode-Farben pruefen
      AppState.swift                      MODIFIZIERT: +appearance Property

    Features/
      Settings/
        SettingsView.swift                MODIFIZIERT: +App-Sektion (Erscheinungsbild, Personalnr, Rechtliches)
        SettingsViewModel.swift           MODIFIZIERT: +appearance, +personalNumber, +notificationsEnabled
      Overview/
        OverviewScreen.swift              MODIFIZIERT: +DATEV-Export Button
        OverviewViewModel.swift           MODIFIZIERT: +generateDATEVExport()

    Services/
      Export/
        DATEVExporter.swift               NEU (E09): DATEV Lodas Format Generator
      LiveActivity/
        WorkSessionAttributes.swift       NEU (E04): ActivityAttributes + ContentState
        LiveActivityManager.swift         NEU (E04): Start/Update/End Lifecycle

    Shared/
      HapticManager.swift                 NEU (E01): UIFeedbackGenerator Wrapper
      AppearanceManager.swift             NEU (E01/E10): preferredColorScheme Logik
      ErrorBanner.swift                   NEU (E01): Konsistentes Error-Banner
      ShimmerModifier.swift               NEU (E01): Skeleton-Loading Modifier
      SharedDefaults.swift                NEU (E03): App Group UserDefaults Wrapper
      WatchConnectivityManager.swift      NEU (E02): WCSession bidirektionale Kommunikation

    Resources/
      Localizable.xcstrings               NEU (E08): String Catalog (DE primaer, EN Fallback)

  FakturusTrackWidget/                    NEU TARGET (E03+E04)
    WidgetBundle.swift                    Widget + Live Activity Bundle
    TimerWidget.swift                     Small + Medium Widget Views
    WidgetTimelineProvider.swift          Timeline-Logik
    TimerWidgetIntents.swift              AppIntent fuer Interactive Widgets (iOS 17+)

  FakturusTrackWatch/                     NEU TARGET (E02)
    FakturusTrackWatchApp.swift           @main watchOS Entry Point
    WatchTimerScreen.swift                Haupt-UI mit Timer-Steuerung
    WatchTimerViewModel.swift             Watch-seitiger State
    WatchComplicationProvider.swift       WidgetKit Complication Timeline
    WatchComplicationViews.swift          Circular/Rectangular/Inline Views
```

**Neue Dateien iOS: ~19**
**Modifizierte Dateien iOS: ~8**
**Neue Targets: 2 (Widget Extension, watchOS App)**

---

### Android -- Neue Dateien

```
app/src/main/java/com/fakturus/track/
  widget/                                 NEU PACKAGE (E05)
    TimerWidgetReceiver.kt                GlanceAppWidgetReceiver
    TimerWidget.kt                        GlanceAppWidget Composable (Small + Medium)
    TimerWidgetActions.kt                 ActionCallback: Start/Stop/Pause
    WidgetStateHelper.kt                  DataStore Lesen/Schreiben fuer Widget

  util/
    HapticManager.kt                      NEU (E01): Vibration Wrapper
    AppearanceManager.kt                  NEU (E01/E10): Night-Mode Steuerung

  services/
    export/
      DATEVExporter.kt                    NEU (E09): DATEV Lodas Format Generator

  features/
    settings/
      SettingsScreen.kt                   MODIFIZIERT: +App-Sektion
      SettingsViewModel.kt                MODIFIZIERT: +appearance, +personalNumber

    overview/
      OverviewScreen.kt                   MODIFIZIERT: +DATEV-Export Button
      OverviewViewModel.kt                MODIFIZIERT: +generateDATEVExport()

  ui/
    theme/
      Theme.kt                            MODIFIZIERT: +darkTheme Override aus Settings
      Color.kt                            MODIFIZIERT: +Dark-Varianten pruefen

app/src/main/res/
  xml/
    timer_widget_info.xml                 NEU (E05): Widget Metadata
    shortcuts.xml                         NEU (E05): App Shortcuts
  values/
    strings.xml                           MODIFIZIERT (E08): Alle Strings extrahiert (DE)
  values-en/
    strings.xml                           NEU (E08): Englische Uebersetzungen
```

**Neue Dateien Android: ~12**
**Modifizierte Dateien Android: ~8**

---

## Neue Xcode Targets (project.yml Erweiterung)

### Widget Extension Target

```yaml
FakturusTrackWidget:
  type: app-extension
  platform: iOS
  sources:
    - path: FakturusTrackWidget
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
  dependencies:
    - target: FakturusTrack
      embed: false
```

### watchOS App Target

```yaml
FakturusTrackWatch:
  type: application
  platform: watchOS
  deploymentTarget:
    watchOS: "10.0"
  sources:
    - path: FakturusTrackWatch
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

### Haupt-App Capabilities erweitern

```yaml
# Zusaetzlich zu bestehenden Entitlements:
FakturusTrack:
  entitlements:
    properties:
      com.apple.security.application-groups:
        - group.com.fakturus.track      # NEU: Fuer Widget + Watch + Live Activity
      # Bestehend:
      keychain-access-groups:
        - $(AppIdentifierPrefix)com.fakturus.track
  info:
    properties:
      NSSupportsLiveActivities: true    # NEU: Fuer Live Activity
```

---

## Android-spezifische Konfiguration

### build.gradle Aenderungen

```kotlin
// Neue Dependencies in libs.versions.toml
[versions]
glance = "1.1.1"
glance-appwidget = "1.1.1"
datastore = "1.1.1"

[libraries]
glance-appwidget = { group = "androidx.glance", name = "glance-appwidget", version.ref = "glance-appwidget" }
glance-material3 = { group = "androidx.glance", name = "glance-material3", version.ref = "glance" }
datastore-preferences = { group = "androidx.datastore", name = "datastore-preferences", version.ref = "datastore" }
```

### AndroidManifest.xml Erweiterungen

```xml
<!-- Widget Receiver -->
<receiver
    android:name=".widget.TimerWidgetReceiver"
    android:exported="true">
    <intent-filter>
        <action android:name="android.appwidget.action.APPWIDGET_UPDATE" />
    </intent-filter>
    <meta-data
        android:name="android.appwidget.provider"
        android:resource="@xml/timer_widget_info" />
</receiver>

<!-- App Shortcuts -->
<meta-data
    android:name="android.app.shortcuts"
    android:resource="@xml/shortcuts" />
```

---

## Abhaengigkeitsdiagramm (Phase 3 Dateien)

```
SharedDefaults.swift (App Group)
    |
    +---> TimerWidget (liest Timer-State)
    +---> LiveActivityManager (liest/schreibt)
    +---> WatchConnectivityManager (liest/schreibt)
    +---> TimeTrackingViewModel (SCHREIBT bei jeder Timer-Aenderung)

WatchConnectivityManager.swift
    |
    +---> FakturusTrackApp.swift (Aktivierung auf iPhone-Seite)
    +---> WatchTimerViewModel.swift (Watch-Seite empfaengt/sendet)
    +---> TimeTrackingViewModel (empfaengt Watch-Aktionen)

HapticManager.swift / HapticManager.kt
    |
    +---> ActiveSessionCard (Start/Stop/Pause Buttons)
    +---> VacationCalendar (Urlaubstag-Toggle)
    +---> SessionRow (Delete-Action)

AppearanceManager / Theme-Override
    |
    +---> FakturusTrackApp (Root-Level colorScheme)
    +---> SettingsViewModel (Schreibt "appearance" Key)
    +---> Theme.kt (darkTheme Boolean aus DataStore)

DATEVExporter.swift / DATEVExporter.kt
    |
    +---> OverviewViewModel.generateDATEVExport()
    +---> OverviewScreen (Button + Share Sheet)

LiveActivityManager.swift
    |
    +---> TimeTrackingViewModel (Start/Stop/Pause -> Activity Update)
    +---> FakturusTrackApp (Cleanup bei App-Start)

WidgetStateHelper.kt (Android DataStore)
    |
    +---> TimerWidget (liest Widget-State)
    +---> TimerWidgetActions (schreibt nach Action)
    +---> TimeTrackingViewModel (SCHREIBT bei Timer-Aenderung)
```

---

## Integrationspunkte mit Phase 1+2 Code

### TimeTrackingViewModel -- Zentrale Erweiterung

Das bestehende `TimeTrackingViewModel` wird der Hauptintegrationspunkt fuer Phase 3. Jede Timer-Aenderung muss zusaetzlich:

1. **SharedDefaults schreiben** (iOS) / **DataStore schreiben** (Android) -- fuer Widget
2. **WidgetCenter.reloadAllTimelines()** (iOS) / **GlanceAppWidget.updateAll()** (Android) -- Widget-Update triggern
3. **LiveActivityManager.update()** (iOS nur) -- Live Activity aktualisieren
4. **WatchConnectivityManager.sendTimerState()** (iOS nur) -- Watch informieren
5. **HapticManager.play()** (beide) -- Haptisches Feedback

Diese Aufrufe werden als Erweiterung in die bestehenden `startSession()`, `stopSession()`, `pauseSession()`, `resumeSession()`, `finishSession()` Methoden integriert.

### OverviewViewModel -- DATEV-Export

Neue Methode `generateDATEVExport(month:year:)` nach bestehendem Pattern von `generateCSV()` und `generatePDF()`.

### SettingsViewModel -- Neue Properties

- `appearance: String` (system/light/dark) -- UserDefaults (iOS) / DataStore (Android)
- `personalNumber: String` -- fuer DATEV-Export, wird in UserSettings gespeichert
- `notificationsEnabled: Bool` -- fuer ArbZG-Hinweise Toggle

### FakturusTrackApp -- Root-Level Aenderungen

```swift
// Neue Zeilen in FakturusTrackApp:
@AppStorage("appearance") private var appearance = "system"

WindowGroup {
    // ... bestehender Code ...
}
.preferredColorScheme(colorScheme(for: appearance))
```

---

## Keine Backend-Aenderungen

Phase 3 ist vollstaendig client-seitig. Kein einziger neuer API-Endpoint. Die DATEV-Datei wird lokal generiert, genau wie PDF und CSV in Phase 2.

---

## Neue SPM/Gradle Dependencies

### iOS
- Keine neuen SPM Dependencies
- WidgetKit, ActivityKit, WatchConnectivity sind System-Frameworks

### Android
- `androidx.glance:glance-appwidget:1.1.1` -- Compose-basierte Widgets
- `androidx.glance:glance-material3:1.1.1` -- Material 3 fuer Glance
- `androidx.datastore:datastore-preferences:1.1.1` -- Widget-State (falls noch nicht vorhanden)

---

## Datei-Entstehung pro Welle

### Welle 1: Polish + Watch + Widgets + Performance (Woche 20-21)

| Strang | iOS Dateien (NEU) | iOS Dateien (MOD) | Android Dateien (NEU) | Android Dateien (MOD) |
|--------|-------------------|--------------------|-----------------------|----------------------|
| E01 Polish | HapticManager.swift, ShimmerModifier.swift, ErrorBanner.swift | Theme.swift, diverse Views (Farb-Audit) | HapticManager.kt | Color.kt, Theme.kt, diverse Screens |
| E02 Watch | FakturusTrackWatchApp.swift, WatchTimerScreen.swift, WatchTimerViewModel.swift, WatchComplicationProvider.swift, WatchComplicationViews.swift, WatchConnectivityManager.swift | FakturusTrackApp.swift (+WCSession) | -- | -- |
| E03 Widget | WidgetBundle.swift, TimerWidget.swift, WidgetTimelineProvider.swift, TimerWidgetIntents.swift, SharedDefaults.swift | TimeTrackingViewModel.swift (+SharedDefaults write) | -- | -- |
| E04 Live Activity | WorkSessionAttributes.swift, LiveActivityManager.swift | TimeTrackingViewModel.swift (+LA trigger) | -- | -- |
| E05 Android Widget | -- | -- | TimerWidgetReceiver.kt, TimerWidget.kt, TimerWidgetActions.kt, WidgetStateHelper.kt, timer_widget_info.xml, shortcuts.xml | TimeTrackingViewModel.kt (+DataStore write), AndroidManifest.xml |
| E07 Performance | -- | Diverse (Lazy Loading, Background Queries) | -- | Diverse (Baseline Profile, remember) |

### Welle 2: A11y + Lokalisierung + Settings (Woche 22)

| Strang | iOS Dateien (NEU) | iOS Dateien (MOD) | Android Dateien (NEU) | Android Dateien (MOD) |
|--------|-------------------|--------------------|-----------------------|----------------------|
| E06 A11y | -- | ALLE View-Dateien (+accessibilityLabel/Hint/Value) | -- | ALLE Screen/Composable-Dateien (+semantics) |
| E08 Lokalisierung | Localizable.xcstrings | ALLE View-Dateien (String -> localized) | values-en/strings.xml | values/strings.xml (Extraktion), ALLE Screens |
| E10 Settings | AppearanceManager.swift | SettingsView.swift, SettingsViewModel.swift, FakturusTrackApp.swift | AppearanceManager.kt | SettingsScreen.kt, SettingsViewModel.kt, Theme.kt |

### Welle 3: DATEV-Export (Woche 22.5-23)

| Strang | iOS Dateien (NEU) | iOS Dateien (MOD) | Android Dateien (NEU) | Android Dateien (MOD) |
|--------|-------------------|--------------------|-----------------------|----------------------|
| E09 DATEV | DATEVExporter.swift | OverviewViewModel.swift, OverviewScreen.swift | DATEVExporter.kt | OverviewViewModel.kt, OverviewScreen.kt |
