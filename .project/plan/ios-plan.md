# iOS App -- Entwicklungsplan

## Technologie-Stack

| Bereich | Technologie | Version |
|---------|-------------|---------|
| Sprache | Swift | 6.0 |
| UI Framework | SwiftUI | iOS 17+ |
| Minimum OS | iOS 17 | -- |
| Auth | MSAL (Microsoft.Identity.Client) | Aktuell |
| Netzwerk | URLSession + async/await | Native |
| Datenbank | SwiftData (oder SQLite via GRDB) | Native |
| Architektur | MVVM + Service Layer | -- |
| Dependency Injection | Swift Environment / @Observable | Native |
| Testing | XCTest + Swift Testing | Native |
| CI/CD | Xcode Cloud oder GitHub Actions | -- |

## Architektur

### Ueberblick

Die iOS-App folgt dem gleichen Architektur-Pattern wie fakturus.poi:

```
FakturusTrack/
  App/
    FakturusTrackApp.swift        -- @main, Environment setup
    AppState.swift                -- Globaler App-State (@Observable)
    Configuration.swift           -- API-URLs, B2C-Config
    AppDelegate.swift             -- Push Notifications, Background Tasks
  Models/
    WorkSession.swift             -- Arbeitssitzung
    VacationDay.swift             -- Urlaubstag
    UserSettings.swift            -- Benutzereinstellungen
    OvertimeSummary.swift         -- Ueberstunden-Zusammenfassung
    CalendarEvent.swift           -- Kalender-Event
    SchoolHolidayPeriod.swift     -- Schulferien
    SyncState.swift               -- Sync-Status (pending, synced, conflict)
  Services/
    Auth/
      AuthManager.swift           -- MSAL B2C Integration (wie fakturus.poi)
    API/
      APIClient.swift             -- HTTP Client (wie fakturus.poi)
      APIError.swift              -- Fehler-Typen
      Endpoints/
        WorkSessionAPI.swift      -- /v1/work-sessions Endpunkte
        VacationAPI.swift         -- /v1/vacation-days Endpunkte
        SettingsAPI.swift         -- /v1/settings Endpunkte
        CalendarAPI.swift         -- /v1/calendar Endpunkte
        OvertimeAPI.swift         -- /v1/overtime-summary Endpunkt
    Sync/
      SyncManager.swift           -- Orchestriert Sync-Prozess
      WorkSessionSyncService.swift
      VacationDaySyncService.swift
      SettingsSyncService.swift
      ConflictResolver.swift      -- Server-wins Strategie
    Network/
      NetworkMonitor.swift        -- NWPathMonitor wrapper
    Persistence/
      PersistenceManager.swift    -- SwiftData Container Setup
  ViewModels/
    TimeTrackingViewModel.swift   -- Zeiten-Tab
    OvertimeViewModel.swift       -- Gesamt-Tab
    VacationViewModel.swift       -- Urlaub-Tab
    SettingsViewModel.swift       -- Settings-Tab
  Views/
    Auth/
      LoginView.swift             -- Login-Screen (wie fakturus.poi)
    TimeTracking/
      TimeTrackingView.swift      -- Hauptscreen (Zeiten-Tab)
      ActiveSessionCard.swift     -- Laufende Session mit Timer
      SessionHistoryList.swift    -- History mit Monatsgruppen
      MonthGroupSection.swift     -- Auf-/zuklappbare Monatsgruppe
      SessionRow.swift            -- Einzelne Session in der Liste
      SessionDetailSheet.swift    -- Bearbeitung einer Session
    Overtime/
      OvertimeView.swift          -- Gesamt-Tab
      OvertimeSummaryCards.swift  -- Zusammenfassungs-Karten
      MonthlyOvertimeTable.swift  -- Monatstabelle
    Vacation/
      VacationView.swift          -- Urlaub-Tab
      VacationCalendarView.swift  -- Kalender mit markierbaren Tagen
    Settings/
      SettingsView.swift          -- Settings-Tab
      WorkdaySelector.swift       -- Wochentag-Toggles
      BundeslandPicker.swift      -- Bundesland-Auswahl
    Shared/
      BottomTabBar.swift          -- Tab-Navigation
      OfflineBanner.swift         -- Offline-Anzeige
      SyncStatusView.swift        -- Sync-Indikator
      TimerDisplay.swift          -- Animierter Timer
  Extensions/
    Date+Formatting.swift         -- Deutsche Datum/Zeit-Formatierung
    TimeInterval+Display.swift    -- Dauer-Formatierung (HH:MM)
  Resources/
    Assets.xcassets               -- App-Icons, Farben
    Localizable.xcstrings         -- Deutsch/Englisch Strings
```

### Kern-Patterns (uebernommen von fakturus.poi)

**1. App-Initialisierung:**
```swift
@main
struct FakturusTrackApp: App {
    @State private var appState = AppState()
    @State private var authManager = AuthManager()

    var body: some Scene {
        WindowGroup {
            if authManager.isAuthenticated {
                ContentView()
            } else {
                LoginView()
            }
            .environment(appState)
            .environment(authManager)
        }
    }
}
```

**2. Service-Initialisierung nach Login:**
- APIClient erhaelt AuthManager fuer automatischen Token-Inject
- Services werden erst nach erfolgreicher Authentifizierung initialisiert
- ViewModels erhalten ihre Services ueber Initializer Injection

**3. @Observable statt ObservableObject:**
- Nutze Swift 6 `@Observable` Macro (wie fakturus.poi)
- Kein `@Published`, kein `Combine` -- rein SwiftUI/Observation Framework

**4. API-Client mit PascalCase-Support:**
- Backend liefert PascalCase JSON (ASP.NET Standard)
- Custom KeyDecodingStrategy (wie in fakturus.poi APIClient)
- DateDecodingStrategy fuer ISO8601 mit und ohne Millisekunden

## Phase 1: Kern-Zeiterfassung (8 Wochen)

### Sprint 1-2 (Wochen 1-4): Fundament

**Woche 1-2: Projekt-Setup + Auth**
- [ ] Xcode-Projekt erstellen (FakturusTrack)
- [ ] Bundle ID: `com.fakturus.track`
- [ ] Configuration.swift mit B2C-Konfiguration
- [ ] AuthManager.swift von fakturus.poi adaptieren
- [ ] LoginView.swift (Social Login Buttons)
- [ ] Sichere Token-Speicherung (Keychain)
- [ ] AppState.swift Grundstruktur

**Woche 3-4: Lokale Datenschicht + API-Client**
- [ ] SwiftData Models: WorkSession, VacationDay, UserSettings
- [ ] PersistenceManager mit Container-Setup
- [ ] APIClient.swift von fakturus.poi adaptieren (andere BaseURL)
- [ ] WorkSessionAPI Endpunkte implementieren
- [ ] NetworkMonitor (NWPathMonitor)
- [ ] Grundlegende Fehlerbehandlung

### Sprint 3-4 (Wochen 5-8): Zeiterfassung + Sync

**Woche 5-6: Zeiterfassungs-UI**
- [ ] BottomTabBar (4 Tabs)
- [ ] TimeTrackingView (Hauptscreen)
- [ ] ActiveSessionCard mit Live-Timer
- [ ] Start/Stop/Finish Buttons
- [ ] SessionHistoryList mit Monatsgruppen
- [ ] SessionRow mit Swipe-to-Delete
- [ ] SessionDetailSheet (manuelle Bearbeitung)

**Woche 7-8: Sync-System**
- [ ] SyncManager Orchestrierung
- [ ] WorkSessionSyncService (Upload + Download + Merge)
- [ ] Pending-Markierung und Sync-Status
- [ ] ConflictResolver (Server-wins)
- [ ] Automatischer Sync bei Online-Wechsel
- [ ] Periodischer Background Sync
- [ ] Manueller Sync (Pull-to-Refresh)
- [ ] OfflineBanner und SyncStatusView

## Phase 2: Vollstaendige Features (6 Wochen)

### Sprint 5-6 (Wochen 9-12): Gesamt + Urlaub

- [ ] OvertimeView mit Summary-Cards
- [ ] MonthlyOvertimeTable
- [ ] Jahresnavigation
- [ ] VacationView mit Kalender
- [ ] Tap-to-Toggle fuer Urlaubstage
- [ ] Bereichsauswahl (Von-Bis)
- [ ] Feiertag-Markierungen im Kalender
- [ ] VacationDaySyncService

### Sprint 7 (Wochen 13-14): Settings

- [ ] SettingsView (alle Einstellungen)
- [ ] WorkdaySelector (7-Tage-Toggles)
- [ ] BundeslandPicker
- [ ] Wochenstunden / Urlaubstage Eingabe
- [ ] Kalender-URL Eingabe
- [ ] Schulferien-Verwaltung
- [ ] SettingsSyncService
- [ ] Profil-Anzeige (Name, E-Mail)

## Phase 3: Polish (4 Wochen)

- [ ] Home Screen Widget (WidgetKit)
- [ ] Live Activity fuer laufende Session
- [ ] Apple Watch Companion (WatchKit)
- [ ] Dark Mode
- [ ] Haptic Feedback
- [ ] VoiceOver-Optimierung
- [ ] Dynamic Type
- [ ] Animationen und Transitionen
- [ ] Crashlytics / Analytics (optional, DSGVO-konform)

## Besonderheiten iOS

### SwiftData vs. Core Data vs. GRDB
Empfehlung: **SwiftData** (iOS 17+ ist unser Minimum)
- Nahtlose SwiftUI-Integration
- @Model Macro fuer automatisches Change-Tracking
- @Query fuer deklarative Datenabfragen
- Kein separater Persistenz-Layer noetig

Falls SwiftData nicht flexibel genug fuer Sync-Logik: **GRDB** als Alternative
- Volle SQL-Kontrolle
- Bewahrt sich in Offline-first Apps
- Eigene Migration-Verwaltung

### Background App Refresh
```swift
// In AppDelegate
func application(_ application: UIApplication,
                 performFetchWithCompletionHandler completionHandler: @escaping (UIBackgroundFetchResult) -> Void) {
    Task {
        await syncManager.syncPendingChanges()
        completionHandler(.newData)
    }
}
```

### Keychain fuer Token
Gleicher Ansatz wie fakturus.poi: MSAL verwaltet Tokens im Keychain.
Keychain Security Group: `com.fakturus.track`

### Entitlements
- Keychain Sharing (fuer MSAL)
- Background Modes: Background fetch, Remote notifications
- WidgetKit (Phase 3)
