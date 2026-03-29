# Android-Architektur -- Fakturus Track

## Technologie-Stack

| Bereich | Technologie | Begruendung |
|---------|-------------|-------------|
| Sprache | Kotlin 2.0+ | Coroutines, Flow, moderne Syntax |
| UI | Jetpack Compose + Material 3 | Deklarativ, wie SwiftUI |
| Datenbank | Room | Bewaehrt, Flow-Integration |
| Auth | MSAL Android | Gleicher B2C-Tenant wie iOS |
| Netzwerk | Ktor Client | Multiplatform-faehig, kein Codegen noetig |
| Serialisierung | kotlinx.serialization | Compile-time, kein Reflection |
| Testing | JUnit 5 + Compose Testing | Standard |
| Build | Gradle + Version Catalog | Standard |

### Warum Ktor statt Retrofit?

Retrofit erfordert ein Interface mit Annotationen, Converter-Factories, und generiert Code. Ktor Client ist:
- Einfacher zu konfigurieren (kein Interface, kein Codegen)
- Nutzt kotlinx.serialization direkt (kein Gson/Moshi noetig)
- Weniger Boilerplate fuer unsere ~8 Endpunkte
- Leichter fuer AI-Agenten zu lesen (alles explizit)

### Warum kein Hilt?

Hilt (Dagger) erzeugt generierte Klassen, erfordert Annotationen (`@Inject`, `@Module`, `@Provides`, `@HiltViewModel`) und macht den Code-Flow fuer AI-Agenten schwer nachvollziehbar. Fakturus Track hat:
- 1 Activity
- 4 Screens mit je 1 ViewModel
- ~5 Services

Das ist manuelle Konstruktor-Injection. Kein DI-Framework noetig.

---

## Projektstruktur

```
app/src/main/java/com/fakturus/track/
  FakturusTrackApp.kt                -- Application-Klasse
  MainActivity.kt                    -- Single Activity, Compose Host
  ServiceContainer.kt               -- Zentrale Service-Initialisierung

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

app/src/test/
  TimeTrackingViewModelTest.kt
  SyncEngineTest.kt
  APIClientTest.kt
```

### Struktur-Entscheidungen

**Entities.kt statt einzelne Entity-Dateien**: Room hat 3 Entities (WorkSession, VacationDay, UserSettings). Die passen in eine Datei mit DAOs.

**DTOs.kt**: Alle API-Typen in einer Datei. `@Serializable` Data Classes sind kompakt.

**Kein domain/ Package**: Keine UseCase-Klassen, keine Domain-Models getrennt von Entities. Die Room-Entities SIND die Domain-Models. Kein Mapping.

**Kein di/ Package**: Kein Hilt. ServiceContainer macht alles explizit.

---

## Kern-Patterns mit Code-Beispielen

### 1. Application + ServiceContainer

```kotlin
class FakturusTrackApp : Application() {
    lateinit var serviceContainer: ServiceContainer
        private set

    override fun onCreate() {
        super.onCreate()
        serviceContainer = ServiceContainer(this)
    }
}
```

```kotlin
class ServiceContainer(private val context: Context) {
    val database = Room.databaseBuilder(
        context, AppDatabase::class.java, "fakturus_track.db"
    ).build()

    val authManager = AuthManager(context)
    val networkMonitor = NetworkMonitor(context)

    // Lazy init nach Login
    var apiClient: APIClient? = null
        private set
    var syncEngine: SyncEngine? = null
        private set

    fun onLogin() {
        val client = APIClient(
            baseUrl = Configuration.apiBaseUrl,
            authManager = authManager
        )
        apiClient = client
        syncEngine = SyncEngine(
            apiClient = client,
            database = database,
            networkMonitor = networkMonitor
        )
    }

    fun onLogout() {
        apiClient = null
        syncEngine = null
    }
}
```

### 2. MainActivity (schlank)

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

### 3. Configuration

```kotlin
object Configuration {
    val apiBaseUrl: String
        get() = if (BuildConfig.DEBUG) {
            "https://10.0.2.2:7001" // Android Emulator -> localhost
        } else {
            "https://api.track.fakturus.com"
        }

    // Azure AD B2C
    const val B2C_TENANT = "fakturus"
    const val B2C_CLIENT_ID = "3fb35bc6-8825-495e-b0a2-18e00352f968"
    const val B2C_POLICY = "B2C_1_BetaSignInOnly"
    const val B2C_REDIRECT_URI = "msauth://com.fakturus.track/{signature-hash}"
    val B2C_SCOPES = listOf(
        "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
    )

    val B2C_AUTHORITY_URL: String
        get() = "https://$B2C_TENANT.b2clogin.com/$B2C_TENANT.onmicrosoft.com/$B2C_POLICY"
}
```

### 4. APIClient (Ktor)

```kotlin
class APIClient(
    private val baseUrl: String,
    private val authManager: AuthManager
) {
    private val client = HttpClient(CIO) {
        install(ContentNegotiation) {
            json(Json {
                // PascalCase-Mapping erfolgt ueber @SerialName auf den DTOs
                // KEINE JsonNamingStrategy -- diese ist experimentell und
                // konfligiert mit @SerialName Annotationen (doppeltes Mapping)
                ignoreUnknownKeys = true
                isLenient = true
            })
        }
        install(Logging) {
            level = if (BuildConfig.DEBUG) LogLevel.BODY else LogLevel.NONE
        }
        defaultRequest {
            contentType(ContentType.Application.Json)
        }
    }

    private suspend fun authenticatedRequest(
        method: HttpMethod,
        path: String,
        body: Any? = null,
        queryParams: Map<String, String> = emptyMap()
    ): HttpResponse {
        val token = authManager.acquireTokenSilently()
        return client.request("$baseUrl$path") {
            this.method = method
            header("Authorization", "Bearer $token")
            queryParams.forEach { (key, value) -> parameter(key, value) }
            if (body != null) setBody(body)
        }
    }

    suspend inline fun <reified T> get(
        path: String,
        queryParams: Map<String, String> = emptyMap()
    ): T {
        val response = authenticatedRequest(HttpMethod.Get, path, queryParams = queryParams)
        validateResponse(response)
        return response.body()
    }

    suspend inline fun <reified T, reified B> post(path: String, body: B): T {
        val response = authenticatedRequest(HttpMethod.Post, path, body = body)
        validateResponse(response)
        return response.body()
    }

    suspend fun delete(path: String) {
        val response = authenticatedRequest(HttpMethod.Delete, path)
        validateResponse(response)
    }

    suspend fun <B> put(path: String, body: B) {
        val response = authenticatedRequest(HttpMethod.Put, path, body = body)
        validateResponse(response)
    }

    private fun validateResponse(response: HttpResponse) {
        when (response.status.value) {
            in 200..299 -> return
            401 -> throw APIError.AuthenticationRequired
            403 -> throw APIError.TokenExpired
            404 -> throw APIError.NotFound
            in 400..499 -> throw APIError.ClientError(response.status.value)
            else -> throw APIError.ServerError(response.status.value)
        }
    }

    // === Convenience Methoden (kein eigenes File pro Endpoint) ===

    suspend fun getWorkSessions(): List<WorkSessionDTO> = get("/v1/work-sessions")

    suspend fun syncWorkSessions(request: SyncWorkSessionsRequest): List<WorkSessionDTO> =
        post("/v1/work-sessions/sync", request)

    suspend fun deleteWorkSession(id: String) = delete("/v1/work-sessions/$id")

    suspend fun getVacationDays(): List<VacationDayDTO> = get("/v1/vacation-days")

    suspend fun syncVacationDays(request: SyncVacationDaysRequest): SyncVacationDaysResponse =
        post("/v1/vacation-days/sync", request)

    suspend fun getUserSettings(): UserSettingsDTO = get("/v1/settings")

    suspend fun updateUserSettings(settings: UserSettingsDTO) = put("/v1/settings", settings)

    suspend fun getOvertimeSummary(year: Int): OvertimeSummaryDTO =
        get("/v1/overtime-summary", queryParams = mapOf("year" to year.toString()))
}
```

### 5. ViewModel (pur Kotlin, kein Hilt)

```kotlin
class TimeTrackingViewModel(
    private val database: AppDatabase,
    private val apiClient: APIClient,
    private val syncEngine: SyncEngine
) : ViewModel() {

    // State (Compose beobachtet via collectAsState)
    private val _activeSession = MutableStateFlow<WorkSessionEntity?>(null)
    val activeSession: StateFlow<WorkSessionEntity?> = _activeSession.asStateFlow()

    val sessions: Flow<List<WorkSessionEntity>> =
        database.workSessionDao().getAllOrderedByDate()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    fun startSession() {
        viewModelScope.launch {
            val session = WorkSessionEntity(
                id = UUID.randomUUID().toString(),
                date = LocalDate.now().toString(),
                startTime = Instant.now().toString(),
                stopTime = null,
                isPendingSync = true,
                isSynced = false,
                isFinished = false
            )
            database.workSessionDao().insert(session)
            _activeSession.value = session
        }
    }

    fun stopSession() {
        viewModelScope.launch {
            val session = _activeSession.value ?: return@launch
            val updated = session.copy(stopTime = Instant.now().toString())
            database.workSessionDao().update(updated)
            _activeSession.value = updated
        }
    }

    fun finishSession() {
        viewModelScope.launch {
            val session = _activeSession.value ?: return@launch
            val updated = session.copy(
                isFinished = true,
                isPendingSync = true
            )
            database.workSessionDao().update(updated)
            _activeSession.value = null

            // Trigger sync
            syncEngine.syncWorkSessions()
        }
    }

    fun deleteSession(session: WorkSessionEntity) {
        viewModelScope.launch {
            database.workSessionDao().delete(session)
            if (session.isSynced) {
                try { apiClient.deleteWorkSession(session.id) } catch (_: Exception) {}
            }
        }
    }
}
```

### 6. ViewModel-Factory (statt Hilt)

```kotlin
// Einfache Factory -- kein @HiltViewModel, kein @Inject
class TimeTrackingViewModelFactory(
    private val services: ServiceContainer
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        @Suppress("UNCHECKED_CAST")
        return TimeTrackingViewModel(
            database = services.database,
            apiClient = services.apiClient!!,
            syncEngine = services.syncEngine!!
        ) as T
    }
}

// Verwendung in Compose:
@Composable
fun TimeTrackingScreen(services: ServiceContainer) {
    val viewModel: TimeTrackingViewModel = viewModel(
        factory = TimeTrackingViewModelFactory(services)
    )
    // ...
}
```

### 7. Compose Screen

```kotlin
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TimeTrackingScreen(services: ServiceContainer) {
    val viewModel: TimeTrackingViewModel = viewModel(
        factory = TimeTrackingViewModelFactory(services)
    )
    val activeSession by viewModel.activeSession.collectAsState()
    val sessions by viewModel.sessions.collectAsState(initial = emptyList())
    val isRefreshing by viewModel.isLoading.collectAsState()

    val pullRefreshState = rememberPullToRefreshState()

    Scaffold(
        topBar = {
            TopAppBar(title = { Text("Zeiten") })
        }
    ) { padding ->
        PullToRefreshBox(
            state = pullRefreshState,
            isRefreshing = isRefreshing,
            onRefresh = { viewModel.sync() },
            modifier = Modifier.padding(padding)
        ) {
            LazyColumn(
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                // Active Session Card
                item {
                    ActiveSessionCard(
                        session = activeSession,
                        onStart = viewModel::startSession,
                        onStop = viewModel::stopSession,
                        onFinish = viewModel::finishSession
                    )
                }

                // Gruppiert nach Monat
                val grouped = sessions.groupBy { it.monthKey }
                grouped.forEach { (month, monthSessions) ->
                    stickyHeader {
                        MonthGroupHeader(
                            month = month,
                            count = monthSessions.size,
                            totalDuration = monthSessions.sumOf { it.durationMinutes }
                        )
                    }
                    items(monthSessions, key = { it.id }) { session ->
                        SessionRow(
                            session = session,
                            onDelete = { viewModel.deleteSession(session) }
                        )
                    }
                }
            }
        }
    }
}
```

---

## State Management Konzept

### StateFlow + Compose

| State-Typ | Mechanismus | Beispiel |
|-----------|------------|---------|
| **UI-State** | `MutableStateFlow` im ViewModel | isLoading, error, activeSession |
| **DB-State** | Room `Flow<List<T>>` | Sessions, VacationDays |
| **Global** | `StateFlow` im ServiceContainer | isAuthenticated, isOnline |

Kein Redux, kein MVI. ViewModels exponieren StateFlows, Compose beobachtet sie.

---

## Room Database (kompakt)

```kotlin
@Database(
    entities = [WorkSessionEntity::class, VacationDayEntity::class, UserSettingsEntity::class],
    version = 1,
    exportSchema = true
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun workSessionDao(): WorkSessionDao
    abstract fun vacationDayDao(): VacationDayDao
    abstract fun userSettingsDao(): UserSettingsDao
}

@Entity(tableName = "work_sessions")
data class WorkSessionEntity(
    @PrimaryKey val id: String,
    val userId: String = "",
    val date: String,               // ISO date "2026-03-29"
    val startTime: String,          // ISO datetime
    val stopTime: String? = null,
    val calendarEventId: String? = null,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false,
    val isFinished: Boolean = false
)

@Dao
interface WorkSessionDao {
    @Query("SELECT * FROM work_sessions ORDER BY date DESC, startTime DESC")
    fun getAllOrderedByDate(): Flow<List<WorkSessionEntity>>

    @Query("SELECT * FROM work_sessions WHERE isPendingSync = 1 AND isFinished = 1")
    suspend fun getPendingSessions(): List<WorkSessionEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(session: WorkSessionEntity)

    @Update
    suspend fun update(session: WorkSessionEntity)

    @Delete
    suspend fun delete(session: WorkSessionEntity)

    @Query("DELETE FROM work_sessions WHERE id = :id")
    suspend fun deleteById(id: String)
}
```

---

## Gradle Dependencies (Version Catalog)

```toml
# gradle/libs.versions.toml
[versions]
kotlin = "2.0.21"
compose-bom = "2024.12.01"
ktor = "3.0.3"
room = "2.6.1"
msal = "5.4.0"
serialization = "1.7.3"

[libraries]
# Compose
compose-bom = { group = "androidx.compose", name = "compose-bom", version.ref = "compose-bom" }
compose-ui = { group = "androidx.compose.ui", name = "ui" }
compose-material3 = { group = "androidx.compose.material3", name = "material3" }

# Ktor
ktor-client-core = { group = "io.ktor", name = "ktor-client-core", version.ref = "ktor" }
ktor-client-cio = { group = "io.ktor", name = "ktor-client-cio", version.ref = "ktor" }
ktor-client-content-negotiation = { group = "io.ktor", name = "ktor-client-content-negotiation", version.ref = "ktor" }
ktor-serialization-json = { group = "io.ktor", name = "ktor-serialization-kotlinx-json", version.ref = "ktor" }

# Room
room-runtime = { group = "androidx.room", name = "room-runtime", version.ref = "room" }
room-ktx = { group = "androidx.room", name = "room-ktx", version.ref = "room" }
room-compiler = { group = "androidx.room", name = "room-compiler", version.ref = "room" }

# Auth
msal = { group = "com.microsoft.identity.client", name = "msal", version.ref = "msal" }

# Serialization
serialization-json = { group = "org.jetbrains.kotlinx", name = "kotlinx-serialization-json", version.ref = "serialization" }

# WorkManager
work-runtime = { group = "androidx.work", name = "work-runtime-ktx", version = "2.9.1" }
```

**Bewusst minimale Dependencies:**
- Kein Hilt/Dagger
- Kein Retrofit (Ktor stattdessen)
- Kein Gson/Moshi (kotlinx.serialization)
- Kein Coil/Glide (keine Bilder in der App)
- Kein Timber (android.util.Log reicht)

---

## Testing-Strategie

### Was testen?

| Bereich | Prioritaet | Framework |
|---------|-----------|-----------|
| ViewModels | Hoch | JUnit 5 + Turbine (Flow testing) |
| SyncEngine | Hoch | JUnit 5 + Mock APIClient |
| Room DAOs | Mittel | Instrumented Test mit In-Memory DB |
| Compose Screens | Niedrig | Compose Testing |

### ViewModel-Test Beispiel

```kotlin
@Test
fun `startSession creates new pending session`() = runTest {
    val db = Room.inMemoryDatabaseBuilder(context, AppDatabase::class.java).build()
    val vm = TimeTrackingViewModel(
        database = db,
        apiClient = MockAPIClient(),
        syncEngine = MockSyncEngine()
    )

    vm.startSession()

    val session = vm.activeSession.first()
    assertNotNull(session)
    assertFalse(session!!.isFinished)
    assertTrue(session.isPendingSync)
}
```

---

## Android-spezifische Konfiguration

### ProGuard/R8 (release)

```proguard
# Ktor
-keep class io.ktor.** { *; }
-dontwarn io.ktor.**

# MSAL
-keep class com.microsoft.identity.** { *; }

# Room Entities
-keep class com.fakturus.track.models.* { *; }

# kotlinx.serialization
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.AnnotationsKt
-keepclassmembers class com.fakturus.track.models.** {
    *** Companion;
}
```

### WorkManager fuer Background Sync

```kotlin
class SyncWorker(
    context: Context,
    params: WorkerParameters,
    private val syncEngine: SyncEngine
) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        return try {
            syncEngine.syncAll()
            Result.success()
        } catch (e: Exception) {
            if (runAttemptCount < 3) Result.retry()
            else Result.failure()
        }
    }
}

// Registrierung (in ServiceContainer.onLogin):
val syncRequest = PeriodicWorkRequestBuilder<SyncWorker>(15, TimeUnit.MINUTES)
    .setConstraints(
        Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()
    )
    .build()

WorkManager.getInstance(context).enqueueUniquePeriodicWork(
    "sync", ExistingPeriodicWorkPolicy.KEEP, syncRequest
)
```
