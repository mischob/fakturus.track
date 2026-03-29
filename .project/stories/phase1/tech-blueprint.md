# Technischer Gesamtplan Phase 1

## Ziel-Dateistruktur am Ende von Phase 1

### iOS -- FakturusTrack

```
FakturusTrack/
  FakturusTrack.xcodeproj
  FakturusTrack/
    App/
      FakturusTrackApp.swift              E01-S01  Entry Point, Auth-Check, Environment-Setup
      AppState.swift                      E01-S01  Globaler App-Zustand (@Observable)
      ServiceContainer.swift              E01-S01  Service-Lifecycle (onLogin/onLogout)
      Configuration.swift                 E01-S01  B2C-Config, API-URLs, Scopes

    Services/
      Auth/
        AuthManager.swift                 E02-S01  MSAL B2C Integration, Token-Management
      API/
        APIClient.swift                   E04-S01  HTTP Client mit PascalCase + Token-Injection
        APIClient+Endpoints.swift         E04-S01  Extension: Convenience-Methoden pro Endpoint
      Sync/
        SyncEngine.swift                  E07-S01  actor, syncAll/syncWorkSessions/syncVacationDays
      Network/
        NetworkMonitor.swift              E04-S03  NWPathMonitor Wrapper (@Observable)

    Features/
      Auth/
        LoginView.swift                   E02-S03  3 Login-Buttons, Loading/Error State
      TimeTracking/
        TimeTrackingView.swift            E06-S07  Zeiten-Tab: ActiveCard + History + Pull-to-Refresh
        TimeTrackingViewModel.swift       E05-S05  State + Logik (start/stop/finish/pause/delete)
        ActiveSessionCard.swift           E05-S03  Idle/Running/Stopped/Paused States
        TimerDisplay.swift                E05-S01  HH:MM:SS mit TimelineView, pulsierender Punkt
        SessionRow.swift                  E06-S01  Kompakte Zeile: Sync-Icon, Zeitraum, Netto-Dauer
        MonthGroup.swift                  E06-S03  Expand/Collapse, Header mit Summen
        SessionDetailSheet.swift          E06-S05  Half-Sheet, editierbare Felder, Validierung
      Shell/
        ContentView.swift                 E09-S01  TabView mit 4 Tabs
        PlaceholderView.swift             E09-S01  "Kommt in Phase 2" + ggf. Logout-Button

    Shared/
      OfflineBanner.swift                 E10-S01  Gelber Banner, Slide-In/Out Animation
      SyncStatusView.swift                E10-S03  4 Zustaende: Synced/Syncing/Pending/Error
      ArbZGBanner.swift                   E08-S05  6h/9h/10h Hinweise
      InitialSyncView.swift               E10-S05  Ladebildschirm nach Login

    Models/
      WorkSession.swift                   E03-S01  @Model, computed properties, toDTO(), update(from:)
      VacationDay.swift                   E03-S01  @Model
      UserSettings.swift                  E03-S01  @Model mit Defaults
      DTOs.swift                          E03-S03  Alle API Request/Response Typen
      PersistenceManager.swift            E03-S01  ModelContainer + Schema V1

    Extensions/
      Date+Formatting.swift               E01-S03  Deutsche Datumsformate
      TimeInterval+Display.swift          E01-S03  formattedHHMMSS, formattedHHMM

    Resources/
      Assets.xcassets                     E01-S03  Farben (Light+Dark), App-Icon Placeholder

  FakturusTrackTests/
    TimeTrackingViewModelTests.swift      E05-S05  Unit Tests fuer ViewModel
    SyncEngineTests.swift                 E07-S01  Unit Tests mit Mock-APIClient
```

**Datei-Anzahl iOS: ~28 Dateien** (inkl. Tests, exkl. Xcode-Projekt-Dateien)

---

### Android -- FakturusTrack

```
app/src/main/java/com/fakturus/track/
  FakturusTrackApp.kt                    E01-S02  Application-Klasse
  MainActivity.kt                        E01-S02  Single Activity, Compose Host
  ServiceContainer.kt                    E01-S02  Service-Lifecycle (onLogin/onLogout)
  Configuration.kt                       E01-S02  B2C-Config, API-URLs

  services/
    auth/
      AuthManager.kt                     E02-S02  MSAL SingleAccountPublicClientApplication
    api/
      APIClient.kt                       E04-S02  Ktor Client + Endpoint-Methoden
      APIError.kt                        E04-S02  Sealed class
    sync/
      SyncEngine.kt                      E07-S02  Mutex-geschuetzt, syncAll
      SyncWorker.kt                      E07-S04  WorkManager CoroutineWorker
    network/
      NetworkMonitor.kt                  E04-S04  ConnectivityManager Wrapper

  features/
    auth/
      LoginScreen.kt                     E02-S04  3 Login-Buttons, Loading/Error State
    timetracking/
      TimeTrackingScreen.kt              E06-S08  LazyColumn: ActiveCard + MonthGroups
      TimeTrackingViewModel.kt           E05-S06  StateFlow-basiert, start/stop/finish/pause
      TimeTrackingViewModelFactory.kt    E05-S06  ViewModelProvider.Factory
      ActiveSessionCard.kt               E05-S04  Idle/Running/Stopped/Paused States
      TimerDisplay.kt                    E05-S02  LaunchedEffect + delay, Pulse-Animation
      SessionRow.kt                      E06-S02  ListItem mit SwipeToDismiss
      MonthGroup.kt                      E06-S04  AnimatedVisibility, Header mit Summen
      SessionDetailSheet.kt              E06-S06  ModalBottomSheet, DatePicker, TimePicker
    shell/
      MainScreen.kt                      E09-S02  Scaffold + BottomBar + NavHost
      AppNavigation.kt                   E09-S02  NavHost mit 4 Routes
      BottomNavBar.kt                    E09-S02  NavigationBar mit 4 Items
      PlaceholderScreen.kt               E09-S02  "Kommt in Phase 2" + Logout

  models/
    Entities.kt                          E03-S02  Room @Entity: WorkSession, VacationDay, UserSettings
    AppDatabase.kt                       E03-S02  RoomDatabase + alle DAOs
    DTOs.kt                              E03-S04  @Serializable Datenklassen mit @SerialName

  ui/
    theme/
      Theme.kt                           E01-S04  Material 3 Color Scheme
      Color.kt                           E01-S04  Named Colors (Timer, Sync, Offline)
      Type.kt                            E01-S04  Typography
    shared/
      OfflineBanner.kt                   E10-S02  AnimatedVisibility, Warning-Banner
      SyncStatusIndicator.kt             E10-S04  4 Zustaende, Rotations-Animation
      ArbZGBanner.kt                     E08-S06  6h/9h/10h Hinweise
      InitialSyncScreen.kt              E10-S05  Ladebildschirm nach Login

  util/
    DateFormatting.kt                    E01-S04  Deutsche Formate, java.time

app/src/main/res/
  raw/
    auth_config.json                     E01-S02  MSAL B2C Konfiguration

app/src/test/
  TimeTrackingViewModelTest.kt           E05-S06  Unit Tests
  SyncEngineTest.kt                      E07-S02  Unit Tests

gradle/
  libs.versions.toml                     E01-S02  Version Catalog
```

**Datei-Anzahl Android: ~32 Dateien** (inkl. Tests, exkl. Gradle-Dateien)

---

## Datei-Entstehung pro Story (Reihenfolge)

### Welle 1: Setup

| Story | iOS Dateien | Android Dateien |
|-------|-------------|-----------------|
| E01-S01 | FakturusTrackApp.swift, AppState.swift, ServiceContainer.swift, Configuration.swift | -- |
| E01-S02 | -- | FakturusTrackApp.kt, MainActivity.kt, ServiceContainer.kt, Configuration.kt, auth_config.json, libs.versions.toml |
| E01-S03 | Assets.xcassets, Date+Formatting.swift, TimeInterval+Display.swift | -- |
| E01-S04 | -- | Theme.kt, Color.kt, Type.kt, DateFormatting.kt |

### Welle 2: Auth + DB + Navigation

| Story | iOS Dateien | Android Dateien |
|-------|-------------|-----------------|
| E02-S01 | AuthManager.swift | -- |
| E02-S02 | -- | AuthManager.kt |
| E02-S03 | LoginView.swift | -- |
| E02-S04 | -- | LoginScreen.kt |
| E03-S01 | WorkSession.swift, VacationDay.swift, UserSettings.swift, PersistenceManager.swift | -- |
| E03-S02 | -- | Entities.kt, AppDatabase.kt |
| E03-S03 | DTOs.swift | -- |
| E03-S04 | -- | DTOs.kt |
| E09-S01 | ContentView.swift, PlaceholderView.swift | -- |
| E09-S02 | -- | MainScreen.kt, AppNavigation.kt, BottomNavBar.kt, PlaceholderScreen.kt |

### Welle 3: Timer + History

| Story | iOS Dateien | Android Dateien |
|-------|-------------|-----------------|
| E05-S01 | TimerDisplay.swift | -- |
| E05-S02 | -- | TimerDisplay.kt |
| E05-S03 | ActiveSessionCard.swift | -- |
| E05-S04 | -- | ActiveSessionCard.kt |
| E05-S05 | TimeTrackingViewModel.swift | -- |
| E05-S06 | -- | TimeTrackingViewModel.kt, TimeTrackingViewModelFactory.kt |
| E06-S01 | SessionRow.swift | -- |
| E06-S02 | -- | SessionRow.kt |
| E06-S03 | MonthGroup.swift | -- |
| E06-S04 | -- | MonthGroup.kt |
| E06-S05 | SessionDetailSheet.swift | -- |
| E06-S06 | -- | SessionDetailSheet.kt |
| E06-S07 | TimeTrackingView.swift | -- |
| E06-S08 | -- | TimeTrackingScreen.kt |

### Welle 4: API + Sync

| Story | iOS Dateien | Android Dateien |
|-------|-------------|-----------------|
| E04-S01 | APIClient.swift, APIClient+Endpoints.swift | -- |
| E04-S02 | -- | APIClient.kt, APIError.kt |
| E04-S03 | NetworkMonitor.swift | -- |
| E04-S04 | -- | NetworkMonitor.kt |
| E07-S01 | SyncEngine.swift | -- |
| E07-S02 | -- | SyncEngine.kt |
| E07-S03 | (modifiziert FakturusTrackApp.swift, TimeTrackingView.swift) | -- |
| E07-S04 | -- | SyncWorker.kt, (modifiziert ServiceContainer.kt) |
| E07-S05 | (modifiziert TimeTrackingViewModel.swift) | (modifiziert TimeTrackingViewModel.kt) |

### Welle 5: Pausen

| Story | iOS Dateien | Android Dateien |
|-------|-------------|-----------------|
| E08-S01 | (modifiziert TimeTrackingViewModel.swift) | -- |
| E08-S02 | -- | (modifiziert TimeTrackingViewModel.kt) |
| E08-S03 | (modifiziert ActiveSessionCard.swift) | -- |
| E08-S04 | -- | (modifiziert ActiveSessionCard.kt) |
| E08-S05 | ArbZGBanner.swift | -- |
| E08-S06 | -- | ArbZGBanner.kt |
| E08-S07 | (modifiziert SessionRow.swift, MonthGroup.swift) | (modifiziert SessionRow.kt, MonthGroup.kt) |

### Welle 6: Polish

| Story | iOS Dateien | Android Dateien |
|-------|-------------|-----------------|
| E10-S01 | OfflineBanner.swift | -- |
| E10-S02 | -- | OfflineBanner.kt |
| E10-S03 | SyncStatusView.swift | -- |
| E10-S04 | -- | SyncStatusIndicator.kt |
| E10-S05 | InitialSyncView.swift | InitialSyncScreen.kt |
| E10-S06 | (modifiziert diverse Views/ViewModels) | (modifiziert diverse Screens/ViewModels) |

---

## Abhaengigkeiten zwischen Dateien/Modulen

```
Configuration.swift/kt
    |
    +---> AuthManager (B2C-Config)
    +---> APIClient (baseURL)

AuthManager
    |
    +---> APIClient (Token-Beschaffung)
    +---> ServiceContainer (onLogin/onLogout Lifecycle)

Models (WorkSession, VacationDay, UserSettings)
    |
    +---> TimeTrackingViewModel (CRUD-Operationen)
    +---> SyncEngine (Sync-Flags lesen/schreiben)
    +---> DTOs (toDTO(), update(from:) Konvertierung)

DTOs
    |
    +---> APIClient (Request/Response Typen)
    +---> SyncEngine (Serialisierung)

APIClient
    |
    +---> SyncEngine (HTTP-Calls)
    +---> TimeTrackingViewModel (Delete-Call wenn online)

NetworkMonitor
    |
    +---> SyncEngine (Guard: nur syncing wenn online)
    +---> OfflineBanner (UI: Banner ein/ausblenden)
    +---> Sync-Trigger (Netzwerk-Wiederherstellung)

SyncEngine
    |
    +---> TimeTrackingViewModel (finishSession -> sync)
    +---> Sync-Trigger (automatische Ausloeser)
    +---> SyncStatusView (isSyncing State)

TimeTrackingViewModel
    |
    +---> ActiveSessionCard (activeSession State)
    +---> TimeTrackingView/Screen (allSessions, Aktionen)
    +---> SessionDetailSheet (update/delete Aktionen)
```

---

## Build-Konfiguration

### iOS: Xcode Project

**Targets:**
- `FakturusTrack` (iOS App, min iOS 17.0)
- `FakturusTrackTests` (Unit Tests)

**Schemes:**
- `FakturusTrack-Debug` (localhost API, verbose Logging)
- `FakturusTrack-Release` (prod API, error-only Logging)

**SPM Dependencies:**
- `microsoft-authentication-library-for-objc` (MSAL)

**Capabilities:**
- Keychain Sharing (`com.fakturus.track`)
- Background Modes (fetch)

**Signing:**
- Bundle ID: `com.fakturus.track`
- Team: Fakturus

### Android: Gradle

**Module:** Single `app` Module (kein Multi-Module fuer 4 Screens)

**Build Variants:**
- `debug` (10.0.2.2 API, verbose Logging)
- `release` (prod API, no Logging, ProGuard/R8)

**Version Catalog (`libs.versions.toml`):**
- Compose BOM (aktuell)
- Ktor 3.x (CIO + ContentNegotiation + Serialization + Logging)
- Room 2.6+ (runtime + ktx + compiler via KSP)
- MSAL Android 5.x
- kotlinx-serialization 1.7+
- WorkManager 2.9+
- Navigation Compose

**Plugins:**
- `kotlin-android`
- `kotlin-serialization`
- `com.google.devtools.ksp` (fuer Room)

**Manifest:**
- `BrowserTabActivity` (MSAL Redirect)
- `ACCESS_NETWORK_STATE` Permission
- `android:usesCleartextTraffic="false"`
