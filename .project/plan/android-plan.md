# Android App -- Entwicklungsplan

## Technologie-Stack

| Bereich | Technologie | Version |
|---------|-------------|---------|
| Sprache | Kotlin | 2.0+ |
| UI Framework | Jetpack Compose | Material 3 |
| Minimum SDK | API 33 (Android 13) | -- |
| Auth | MSAL Android | Aktuell |
| Netzwerk | Ktor Client oder Retrofit + OkHttp | -- |
| Datenbank | Room (SQLite) | Aktuell |
| Architektur | MVVM + Service Layer | -- |
| DI | Manuelle Konstruktor-Injection (ServiceContainer) | -- |
| Testing | JUnit 5 + Compose Testing | -- |
| CI/CD | GitHub Actions | -- |

## Architektur

### Ueberblick

Die Android-App folgt dem gleichen Architektur-Pattern wie fakturus.poi Android:

```
app/src/main/java/com/fakturus/track/
  FakturusTrackApp.kt                -- Application-Klasse
  MainActivity.kt                    -- Single Activity, Compose Host
  ServiceContainer.kt               -- Zentrale Service-Initialisierung (manuelle DI)

  services/
    auth/
      AuthManager.kt                -- MSAL Android Integration
    api/
      APIClient.kt                  -- Ktor HTTP Client
      APIError.kt                   -- Sealed class fuer Fehler
    sync/
      SyncEngine.kt                 -- Orchestriert alle Syncs
      SyncWorker.kt                 -- WorkManager Worker
    network/
      NetworkMonitor.kt             -- ConnectivityManager Wrapper

  features/
    timetracking/
      TimeTrackingScreen.kt         -- Compose Screen
      TimeTrackingViewModel.kt      -- State + Logik
      ActiveSessionCard.kt          -- Compose Component
      SessionRow.kt                 -- Compose Component
      SessionDetailSheet.kt         -- BottomSheet
      MonthGroup.kt                 -- Expandable Section
    overtime/
      OvertimeScreen.kt
      OvertimeViewModel.kt
    vacation/
      VacationScreen.kt
      VacationViewModel.kt
      VacationCalendar.kt
    settings/
      SettingsScreen.kt
      SettingsViewModel.kt
    auth/
      LoginScreen.kt

  models/
    Entities.kt                     -- Room Entities (alle in einer Datei)
    DTOs.kt                         -- API Request/Response Typen
    AppDatabase.kt                  -- Room Database + DAOs

  ui/
    navigation/
      AppNavigation.kt              -- NavHost + Routes
      BottomNavBar.kt
    theme/
      Theme.kt                      -- Material 3 Theme
      Color.kt
      Type.kt
    shared/
      OfflineBanner.kt
      SyncStatusIndicator.kt
      TimerDisplay.kt

  util/
    DateFormatting.kt
```

### Kern-Patterns (uebernommen von fakturus.poi Android)

**1. Single Activity + Compose Navigation (kein Hilt):**
```kotlin
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        val services = (application as FakturusTrackApp).serviceContainer
        setContent {
            FakturusTrackTheme {
                val isAuthenticated by services.authManager.isAuthenticated.collectAsState()
                if (isAuthenticated) {
                    MainScreen(services)
                } else {
                    LoginScreen(authManager = services.authManager)
                }
            }
        }
    }
}
```

**2. ServiceContainer (manuelle DI statt Hilt):**
```kotlin
class ServiceContainer(private val context: Context) {
    val database = Room.databaseBuilder(
        context, AppDatabase::class.java, "fakturus_track.db"
    ).build()
    val authManager = AuthManager(context)
    val networkMonitor = NetworkMonitor(context)

    var apiClient: APIClient? = null
        private set
    var syncEngine: SyncEngine? = null
        private set

    fun onLogin() {
        val client = APIClient(baseUrl = Configuration.apiBaseUrl, authManager = authManager)
        apiClient = client
        syncEngine = SyncEngine(apiClient = client, database = database, networkMonitor = networkMonitor)
    }
    fun onLogout() { apiClient = null; syncEngine = null }
}
```

**3. WorkManager fuer Background Sync:**
```kotlin
class SyncWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {
    override suspend fun doWork(): Result {
        // Sync pending changes
        return Result.success()
    }
}
```

## Phase 1: Kern-Zeiterfassung (8 Wochen)

### Sprint 1-2 (Wochen 1-4): Fundament

**Woche 1-2: Projekt-Setup + Auth**
- [ ] Android Studio Projekt erstellen
- [ ] Package: `com.fakturus.track`
- [ ] ServiceContainer mit manueller DI
- [ ] MSAL Android Integration
- [ ] LoginScreen mit Social Login Buttons
- [ ] AuthManager mit Token-Verwaltung
- [ ] Sichere Token-Speicherung (EncryptedSharedPreferences)

**Woche 3-4: Datenschicht + API**
- [ ] Room Database mit Entities und DAOs
- [ ] Retrofit/Ktor API-Service
- [ ] AuthInterceptor fuer Bearer Token
- [ ] Repository-Implementierungen
- [ ] NetworkMonitor (ConnectivityManager)
- [ ] Fehlerbehandlung (Result-Pattern)

### Sprint 3-4 (Wochen 5-8): Zeiterfassung + Sync

**Woche 5-6: Zeiterfassungs-UI**
- [ ] Material 3 Theme (Farben, Typografie)
- [ ] BottomNavBar (4 Tabs)
- [ ] TimeTrackingScreen
- [ ] ActiveSessionCard mit animiertem Timer
- [ ] Start/Stop/Finish Buttons
- [ ] SessionHistoryList (LazyColumn mit Sticky Headers)
- [ ] SessionRow mit Swipe-to-Dismiss
- [ ] SessionDetailSheet (BottomSheet)

**Woche 7-8: Sync-System**
- [ ] SyncManager mit WorkManager
- [ ] SyncWorker fuer Background Sync
- [ ] Periodic Work Request (30min Minimum fuer WorkManager)
- [ ] OneTimeWorkRequest fuer sofortigen Sync
- [ ] Conflict Resolution
- [ ] Offline/Online State Handling
- [ ] Pull-to-Refresh

## Phase 2: Vollstaendige Features (6 Wochen)

### Sprint 5-6 (Wochen 9-12): Gesamt + Urlaub

- [ ] OvertimeScreen mit Material 3 Cards
- [ ] MonthlyOvertimeTable (LazyColumn)
- [ ] Jahresnavigation
- [ ] VacationScreen mit Kalender-Composable
- [ ] Custom CalendarView mit Tap-Auswahl
- [ ] Feiertag-Markierungen
- [ ] VacationDay Sync

### Sprint 7 (Wochen 13-14): Settings

- [ ] SettingsScreen (Material 3 ListItems)
- [ ] WorkdaySelector (FilterChips)
- [ ] BundeslandPicker (DropdownMenu)
- [ ] Numerische Eingabefelder
- [ ] Settings Sync
- [ ] Profil-Anzeige

## Phase 3: Polish (4 Wochen)

- [ ] Home Screen Widget (Glance/AppWidget)
- [ ] App Shortcut fuer Quick-Start
- [ ] Dynamic Color (Material You)
- [ ] Dark Theme
- [ ] Haptic Feedback (HapticFeedback Composable)
- [ ] TalkBack-Optimierung
- [ ] Animationen (animate*AsState)
- [ ] Edge-to-Edge Design

## Android-spezifische Besonderheiten

### Room Database
```kotlin
@Database(
    entities = [WorkSessionEntity::class, VacationDayEntity::class, UserSettingsEntity::class],
    version = 1,
    exportSchema = true
)
abstract class TrackDatabase : RoomDatabase() {
    abstract fun workSessionDao(): WorkSessionDao
    abstract fun vacationDayDao(): VacationDayDao
    abstract fun userSettingsDao(): UserSettingsDao
}
```

### WorkManager vs. Foreground Service
- **WorkManager** fuer periodischen Sync (Minimum 15min)
- Fuer kuerzere Intervalle: In-App Timer wenn App aktiv
- Kein Foreground Service noetig (wir sind keine Tracking-App)

### ProGuard/R8 Regeln
- MSAL-spezifische Keep-Rules
- Retrofit/Ktor Model-Klassen nicht obfuscaten
- Room-Entities behalten

### Signing & Release
- Signing-Keys in GitHub Secrets oder lokaler Keystore
- Separate debug/release Konfigurationen
- versionCode automatisch hochzaehlen (CI)
