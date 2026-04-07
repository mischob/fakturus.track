# Tech-Spec: EPIC 09 -- App-Shell & Navigation

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Features/Shell/ContentView.swift` | iOS | E09-S01 | TabView mit 4 Tabs |
| `Features/Shell/PlaceholderView.swift` | iOS | E09-S01 | "Kommt in Phase 2" Screen |
| `features/shell/MainScreen.kt` | Android | E09-S02 | Scaffold + BottomBar |
| `features/shell/AppNavigation.kt` | Android | E09-S02 | NavHost mit 4 Routes |
| `features/shell/BottomNavBar.kt` | Android | E09-S02 | NavigationBar |
| `features/shell/PlaceholderScreen.kt` | Android | E09-S02 | "Kommt in Phase 2" |

**Modifizierte Dateien:**
- `FakturusTrackApp.swift` (Auth-Check: LoginView vs ContentView)
- `MainActivity.kt` (Auth-Check: LoginScreen vs MainScreen)

---

## Code-Skizzen

### iOS: ContentView.swift

```swift
struct ContentView: View {
    @State private var selectedTab = 0

    var body: some View {
        TabView(selection: $selectedTab) {
            TimeTrackingView()
                .tabItem {
                    Label("Zeiten", systemImage: "clock")
                }
                .tag(0)

            PlaceholderView(
                title: "Urlaub",
                icon: "sun.max",
                description: "Urlaubsverwaltung kommt in Phase 2"
            )
            .tabItem {
                Label("Urlaub", systemImage: "sun.max")
            }
            .tag(1)

            PlaceholderView(
                title: "Gesamt",
                icon: "chart.bar",
                description: "Uebersicht kommt in Phase 2"
            )
            .tabItem {
                Label("Gesamt", systemImage: "chart.bar")
            }
            .tag(2)

            settingsPlaceholder
                .tabItem {
                    Label("Einstellungen", systemImage: "gearshape")
                }
                .tag(3)
        }
    }

    @ViewBuilder
    private var settingsPlaceholder: some View {
        @Environment(AuthManager.self) var authManager
        NavigationStack {
            VStack(spacing: 32) {
                Spacer()
                Image(systemName: "gearshape")
                    .font(.system(size: 48))
                    .foregroundStyle(.secondary)
                Text("Einstellungen kommt in Phase 2")
                    .foregroundStyle(.secondary)
                Spacer()
                Button("Abmelden", role: .destructive) {
                    authManager.logout()
                }
                .buttonStyle(.bordered)
            }
            .padding()
            .navigationTitle("Einstellungen")
        }
    }
}
```

### iOS: PlaceholderView.swift

```swift
struct PlaceholderView: View {
    let title: String
    let icon: String
    let description: String

    var body: some View {
        NavigationStack {
            VStack(spacing: 16) {
                Spacer()
                Image(systemName: icon)
                    .font(.system(size: 48))
                    .foregroundStyle(.secondary)
                Text(description)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
                Spacer()
            }
            .padding()
            .navigationTitle(title)
        }
    }
}
```

### iOS: FakturusTrackApp.swift (aktualisiert)

```swift
@main
struct FakturusTrackApp: App {
    @State private var services = ServiceContainer()
    @Environment(\.scenePhase) private var scenePhase

    var body: some Scene {
        WindowGroup {
            Group {
                if services.authManager.isAuthenticated {
                    ContentView()
                } else {
                    LoginView()
                }
            }
            .environment(services.authManager)
            .environment(services.networkMonitor)
            .onChange(of: services.authManager.isAuthenticated) { _, isAuth in
                if isAuth {
                    services.onLogin()
                } else {
                    services.onLogout()
                }
            }
            .onChange(of: scenePhase) { _, phase in
                if phase == .active {
                    // Sync-Trigger (E07-S03)
                }
            }
        }
        .modelContainer(PersistenceManager.container)
    }
}
```

### Android: MainScreen.kt

```kotlin
@Composable
fun MainScreen(services: ServiceContainer) {
    val navController = rememberNavController()

    Scaffold(
        bottomBar = { BottomNavBar(navController) }
    ) { padding ->
        AppNavigation(
            navController = navController,
            services = services,
            modifier = Modifier.padding(padding)
        )
    }
}
```

### Android: AppNavigation.kt

```kotlin
@Composable
fun AppNavigation(
    navController: NavHostController,
    services: ServiceContainer,
    modifier: Modifier = Modifier
) {
    NavHost(
        navController = navController,
        startDestination = "zeiten",
        modifier = modifier
    ) {
        composable("zeiten") {
            TimeTrackingScreen(services)
        }
        composable("urlaub") {
            PlaceholderScreen(
                title = "Urlaub",
                icon = Icons.Default.WbSunny,
                description = "Urlaubsverwaltung kommt in Phase 2"
            )
        }
        composable("gesamt") {
            PlaceholderScreen(
                title = "Gesamt",
                icon = Icons.Default.BarChart,
                description = "Uebersicht kommt in Phase 2"
            )
        }
        composable("einstellungen") {
            SettingsPlaceholderScreen(
                onLogout = {
                    CoroutineScope(Dispatchers.Main).launch {
                        services.authManager.logout()
                    }
                }
            )
        }
    }
}
```

### Android: BottomNavBar.kt

```kotlin
data class BottomNavItem(
    val route: String,
    val label: String,
    val icon: ImageVector
)

val bottomNavItems = listOf(
    BottomNavItem("zeiten", "Zeiten", Icons.Default.Schedule),
    BottomNavItem("urlaub", "Urlaub", Icons.Default.WbSunny),
    BottomNavItem("gesamt", "Gesamt", Icons.Default.BarChart),
    BottomNavItem("einstellungen", "Einstellungen", Icons.Default.Settings),
)

@Composable
fun BottomNavBar(navController: NavHostController) {
    val currentEntry by navController.currentBackStackEntryAsState()
    val currentRoute = currentEntry?.destination?.route

    NavigationBar {
        bottomNavItems.forEach { item ->
            NavigationBarItem(
                selected = currentRoute == item.route,
                onClick = {
                    navController.navigate(item.route) {
                        popUpTo(navController.graph.startDestinationId) { saveState = true }
                        launchSingleTop = true
                        restoreState = true
                    }
                },
                icon = { Icon(item.icon, contentDescription = item.label) },
                label = { Text(item.label) }
            )
        }
    }
}
```

### Android: PlaceholderScreen.kt

```kotlin
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PlaceholderScreen(title: String, icon: ImageVector, description: String) {
    Scaffold(
        topBar = { TopAppBar(title = { Text(title) }) }
    ) { padding ->
        Column(
            modifier = Modifier.fillMaxSize().padding(padding),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Icon(icon, null, modifier = Modifier.size(48.dp), tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(16.dp))
            Text(description, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsPlaceholderScreen(onLogout: () -> Unit) {
    Scaffold(
        topBar = { TopAppBar(title = { Text("Einstellungen") }) }
    ) { padding ->
        Column(
            modifier = Modifier.fillMaxSize().padding(padding),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Icon(Icons.Default.Settings, null, Modifier.size(48.dp), MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(16.dp))
            Text("Einstellungen kommt in Phase 2", color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(32.dp))
            TextButton(onClick = onLogout, colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error)) {
                Text("Abmelden")
            }
        }
    }
}
```

---

## Datenfluss

```
FakturusTrackApp / MainActivity
    |
    | authManager.isAuthenticated?
    |
    +-- false -> LoginView / LoginScreen
    |
    +-- true -> ContentView / MainScreen
                    |
                    +-- TabView / NavigationBar
                    |     |
                    |     +-- Tab 0: Zeiten -> TimeTrackingView/Screen
                    |     +-- Tab 1: Urlaub -> PlaceholderView/Screen
                    |     +-- Tab 2: Gesamt -> PlaceholderView/Screen
                    |     +-- Tab 3: Einstellungen -> Placeholder + Logout
                    |
                    +-- Logout -> authManager.logout()
                              -> isAuthenticated = false
                              -> ServiceContainer.onLogout()
                              -> UI wechselt zu LoginView/LoginScreen
```

---

## Testbare Kriterien

- [ ] iOS: ContentView zeigt 4 Tabs mit korrekten Icons und Labels
- [ ] iOS: Tab-Wechsel funktioniert, State bleibt pro Tab erhalten
- [ ] iOS: Einstellungen-Tab hat funktionalen Logout-Button
- [ ] iOS: Logout setzt isAuthenticated=false, zeigt LoginView
- [ ] Android: MainScreen zeigt NavigationBar mit 4 Items
- [ ] Android: Navigation zwischen Routes funktioniert (State restored)
- [ ] Android: Einstellungen Logout funktioniert
- [ ] Android: Back-Button auf Zeiten-Tab beendet App (nicht zurueck zum Login)

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| iOS TabView State-Verlust bei Tab-Wechsel | Niedrig | @State in ContentView haelt ViewModel-Referenzen |
| Android Navigation: Back-Stack Probleme | Mittel | `popUpTo + launchSingleTop + restoreState` korrekt konfigurieren |
| iOS: ScenePhase nicht zuverlaessig bei Background | Niedrig | BGAppRefreshTask als Backup (E07-S03) |
