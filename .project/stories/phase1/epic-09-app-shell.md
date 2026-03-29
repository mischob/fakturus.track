# EPIC 09: App-Shell & Navigation

## Ziel

Die aeussere Huelle der App: Tab-Navigation mit 4 Tabs, App-Lifecycle-Management und die korrekte Verknuepfung aller Screens. In Phase 1 sind nur der Zeiten-Tab funktional -- die anderen 3 Tabs zeigen Platzhalter ("Kommt in Phase 2").

## Abhaengigkeiten

- **E01**: Projekt-Setup (Grundstruktur)

**Hinweis**: Kann frueh und unabhaengig von der Feature-Entwicklung begonnen werden.

---

## Stories

### P1-E09-S01: iOS Tab-Navigation & App-Lifecycle

**Als** Nutzer
**moechte ich** ueber eine Tab-Leiste zwischen den App-Bereichen navigieren koennen,
**damit** ich schnell zu den verschiedenen Funktionen gelange.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S01, P1-E02-S01 (Auth fuer Login-Check)
**Parallelisierbar mit**: P1-E09-S02 (Android Navigation), alle Feature-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `FakturusTrackApp.swift` (`@main`):
  - `@State private var appState = AppState()`
  - `@State private var authManager = AuthManager()`
  - Body: Wenn `authManager.isAuthenticated` -> `ContentView`, sonst `LoginView`
  - `.environment(appState)`, `.environment(authManager)`
  - `.modelContainer(PersistenceManager.container)`
- [ ] `AppState.swift` als `@Observable`:
  - Globaler State (z.B. `selectedTab: Int`)
  - Spaeter: SyncEngine-Referenz, Shared-State
- [ ] `ContentView.swift` mit TabView:
  - Tab 1: Zeiten (Icon: `clock`, Label: "Zeiten") -> `TimeTrackingView()`
  - Tab 2: Urlaub (Icon: `sun.max`, Label: "Urlaub") -> Placeholder ("Kommt in Phase 2")
  - Tab 3: Gesamt (Icon: `chart.bar`, Label: "Gesamt") -> Placeholder
  - Tab 4: Einstellungen (Icon: `gearshape`, Label: "Einstellungen") -> Placeholder mit Logout-Button
- [ ] Placeholder-Tabs:
  - Zentrierter Text: "Kommt in Phase 2"
  - Icon passend zum Tab
  - Einstellungen-Placeholder: Zusaetzlich "Abmelden"-Button (funktional via authManager.logout())
- [ ] App-Lifecycle:
  - `@Environment(\.scenePhase)` beobachten
  - Bei `.active`: Sync starten (wenn SyncEngine verfuegbar)
  - Bei `.background`: Sync stoppen, State speichern

**Technische Hinweise**:
- TabView mit `.tag()` und `$appState.selectedTab`
- Placeholder als eigene Views (wiederverwendbar, spaeter ersetzt)
- Logout in Placeholder-Settings: `Button("Abmelden") { authManager.logout() }`

---

### P1-E09-S02: Android Navigation & App-Lifecycle

**Als** Nutzer
**moechte ich** ueber eine Bottom-Navigation zwischen den App-Bereichen navigieren koennen,
**damit** ich schnell zu den verschiedenen Funktionen gelange.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S02, P1-E02-S02 (Auth fuer Login-Check)
**Parallelisierbar mit**: P1-E09-S01 (iOS Navigation), alle Feature-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `MainActivity.kt`:
  - `enableEdgeToEdge()`
  - ServiceContainer aus Application holen
  - `isAuthenticated.collectAsState()` -> if/else (MainScreen vs LoginScreen)
- [ ] `AppNavigation.kt` mit Navigation Compose:
  - `NavHost` mit 4 Routes: "zeiten", "urlaub", "gesamt", "einstellungen"
  - Zeiten -> `TimeTrackingScreen()`
  - Urlaub -> Placeholder Composable
  - Gesamt -> Placeholder Composable
  - Einstellungen -> Placeholder mit Logout-Button
- [ ] `BottomNavBar.kt`:
  - Material 3 `NavigationBar` mit 4 `NavigationBarItem`s
  - Icons: `Schedule`, `WbSunny`, `BarChart`, `Settings`
  - Labels: "Zeiten", "Urlaub", "Gesamt", "Einstellungen"
  - Selected-State korrekt verwaltet
- [ ] `MainScreen.kt` als Scaffold:
  - `bottomBar = { BottomNavBar(...) }`
  - Content: `AppNavigation(navController)`
- [ ] Placeholder-Screens:
  - Zentrierter Text + Icon
  - Einstellungen: "Abmelden" Button (TextButton, onClick -> authManager.logout())
- [ ] Lifecycle:
  - `LifecycleEventObserver` fuer ON_START/ON_STOP (optional fuer Sync-Trigger)

**Technische Hinweise**:
- `rememberNavController()` im MainScreen
- `NavigationBarItem(selected = currentRoute == route, onClick = { navController.navigate(route) })`
- Kein nested NavHost -- flache Navigation mit 4 Top-Level-Destinations
- `currentBackStackEntryAsState()` fuer Selected-State
