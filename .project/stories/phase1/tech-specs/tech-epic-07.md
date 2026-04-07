# Tech-Spec: EPIC 07 -- Sync-Engine

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Services/Sync/SyncEngine.swift` | iOS | E07-S01 | actor, syncAll/syncWorkSessions/syncVacationDays |
| `services/sync/SyncEngine.kt` | Android | E07-S02 | Mutex-geschuetzt, syncAll |
| `services/sync/SyncWorker.kt` | Android | E07-S04 | WorkManager CoroutineWorker |

**Modifizierte Dateien:**
- `ServiceContainer.swift/kt` (SyncEngine-Initialisierung in onLogin)
- `FakturusTrackApp.swift` (BGAppRefreshTask Registration, Scene-Phase Trigger)
- `TimeTrackingView.swift` (.refreshable mit SyncEngine)
- `TimeTrackingScreen.kt` (Pull-to-Refresh mit SyncEngine)
- `TimeTrackingViewModel.swift/kt` (SyncEngine-Dependency, finishSession -> sync)

---

## API-Contracts

Siehe Tech-Spec E03 und E04 fuer vollstaendige Request/Response Formate. Hier der Sync-spezifische Flow:

### WorkSession Sync (2 Pfade)

**Pfad A: Pending vorhanden**
```
POST /v1/work-sessions/sync
Body: { "WorkSessions": [...pending...] }
Response: [...alle Server-Sessions...]
```

**Pfad B: Keine Pending**
```
GET /v1/work-sessions
Response: [...alle Server-Sessions...]
```

### VacationDay Sync (Optimierung: GET wenn keine pending)

```
Pfad A: Pending vorhanden
POST /v1/vacation-days/sync
Body: { "VacationDays": [...ALLE lokalen Tage...] }
Response: { "ServerVacationDays": [...], "DeletedIds": [...] }

Pfad B: Keine Pending
GET /v1/vacation-days
Response: [...alle Server-VacationDays...]
```

> **Warum die Optimierung?** Wenn keine lokalen Aenderungen pending sind, genuegt ein einfacher GET um den aktuellen Server-Stand abzuholen. Das spart Netzwerk-Bandbreite und ist konsistent mit dem MAUI-Muster (SyncService.cs prueft ebenfalls ob pending vorhanden sind bevor ein POST gesendet wird). Nur wenn der Nutzer offline VacationDays geaendert hat, wird der volle POST /sync mit allen lokalen Tagen gesendet.

### UserSettings Sync (Last-Write-Wins)

```
GET /v1/settings -> ServerSettings
Vergleich: lokal.updatedAt vs server.updatedAt
  Lokal neuer -> PUT /v1/settings (lokale Werte)
  Server neuer -> Lokale ueberschreiben
```

---

## Code-Skizzen

### iOS: SyncEngine.swift

```swift
@ModelActor
actor SyncEngine {
    private let apiClient: APIClient
    private let networkMonitor: NetworkMonitor

    private(set) var isSyncing = false
    private(set) var lastSyncDate: Date?
    private(set) var lastError: String?

    init(apiClient: APIClient, networkMonitor: NetworkMonitor, modelContainer: ModelContainer) {
        self.apiClient = apiClient
        self.networkMonitor = networkMonitor
        // @ModelActor erstellt automatisch einen eigenen ModelContext
        // im Actor-Kontext -- kein manuelles setModelContext() noetig
    }

    func syncAll() async {
        guard !isSyncing else { return }
        guard networkMonitor.isConnected else { return }

        isSyncing = true
        lastError = nil
        defer { isSyncing = false }

        do {
            try await syncWorkSessions()
            try await syncVacationDays()
            try await syncUserSettings()
            lastSyncDate = Date()
        } catch {
            lastError = error.localizedDescription
            // Loggen, nicht werfen -- Sync-Fehler sind nicht fatal
            #if DEBUG
            print("[SyncEngine] Error: \(error)")
            #endif
        }
    }

    // MARK: - WorkSessions

    private func syncWorkSessions() async throws {
        // Step 1: Pending sammeln
        // modelContext wird automatisch von @ModelActor bereitgestellt
        let pending = try modelContext.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate {
                $0.isPendingSync && $0.isFinished
            })
        )

        // Step 2: Upload oder Fetch
        let serverSessions: [WorkSessionDTO]
        if !pending.isEmpty {
            let request = SyncWorkSessionsRequest(
                workSessions: pending.map { $0.toDTO() }
            )
            serverSessions = try await apiClient.syncWorkSessions(request)
        } else {
            serverSessions = try await apiClient.getWorkSessions()
        }

        // Step 3: Synced Sessions lokal laden
        let synced = try modelContext.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate { $0.isSynced })
        )

        // Step 4: Server-ID-Set
        let serverIds = Set(serverSessions.map(\.id))

        // Step 5: Set-Differenz -- lokale synced loeschen die nicht mehr auf Server
        for local in synced where !serverIds.contains(local.id.uuidString) {
            modelContext.delete(local)
        }

        // Step 6: Server-Sessions upserten
        for dto in serverSessions {
            let uuid = UUID(uuidString: dto.id) ?? UUID()
            let descriptor = FetchDescriptor<WorkSession>(
                predicate: #Predicate { session in session.id == uuid }
            )
            if let existing = try modelContext.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(WorkSession(from: dto))
            }
        }

        // Step 7: Pending als synced markieren
        for session in pending {
            session.isPendingSync = false
            session.isSynced = true
            session.syncedAt = Date()
        }

        try modelContext.save()
    }

    // MARK: - VacationDays (Optimierung: GET wenn keine pending, POST mit ALLEN wenn pending)

    private func syncVacationDays() async throws {
        let allLocal = try modelContext.fetch(FetchDescriptor<VacationDay>())
        let pending = allLocal.filter { $0.isPendingSync }

        // Optimierung: Nur POST wenn lokale Aenderungen vorhanden,
        // sonst genuegt GET (spart Netzwerk, konsistent mit MAUI-Muster)
        let serverVacationDays: [VacationDayDTO]
        var deletedIds: [String] = []

        if !pending.isEmpty {
            // Pending vorhanden -> ALLE lokalen Tage senden (nicht nur pending!)
            let request = SyncVacationDaysRequest(
                vacationDays: allLocal.map { $0.toDTO() }
            )
            let response = try await apiClient.syncVacationDays(request)
            serverVacationDays = response.serverVacationDays
            deletedIds = response.deletedIds
        } else {
            // Keine Pending -> einfacher GET genuegt
            serverVacationDays = try await apiClient.getVacationDays()
        }

        // DeletedIds verarbeiten
        for deletedId in deletedIds {
            let uuid = UUID(uuidString: deletedId) ?? UUID()
            let descriptor = FetchDescriptor<VacationDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let toDelete = try modelContext.fetch(descriptor).first {
                modelContext.delete(toDelete)
            }
        }

        // Server-VacationDays upserten
        for dto in serverVacationDays {
            let uuid = UUID(uuidString: dto.id) ?? UUID()
            let descriptor = FetchDescriptor<VacationDay>(
                predicate: #Predicate { day in day.id == uuid }
            )
            if let existing = try modelContext.fetch(descriptor).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(VacationDay(from: dto))
            }
        }

        // Alle als synced markieren
        for day in allLocal {
            day.isPendingSync = false
            day.isSynced = true
            day.syncedAt = Date()
        }

        try modelContext.save()
    }

    // MARK: - UserSettings (Last-Write-Wins)

    private func syncUserSettings() async throws {
        let serverSettings = try await apiClient.getUserSettings()
        let localSettings = try modelContext.fetch(FetchDescriptor<UserSettings>()).first

        if let local = localSettings {
            // Einfach Server-Werte uebernehmen (Phase 1: noch keine lokalen Settings-Aenderungen)
            local.vacationDaysPerYear = serverSettings.vacationDaysPerYear
            local.workHoursPerWeek = serverSettings.workHoursPerWeek
            local.workDays = serverSettings.workDays
            local.bundesland = serverSettings.bundesland
            local.calendarUrl = serverSettings.calendarUrl
            local.isSynced = true
            local.isPendingSync = false
        } else {
            let settings = UserSettings(userId: "")
            settings.vacationDaysPerYear = serverSettings.vacationDaysPerYear
            settings.workHoursPerWeek = serverSettings.workHoursPerWeek
            settings.workDays = serverSettings.workDays
            settings.bundesland = serverSettings.bundesland
            settings.calendarUrl = serverSettings.calendarUrl
            settings.isSynced = true
            settings.isPendingSync = false
            modelContext.insert(settings)
        }

        try modelContext.save()
    }
}
```

### Pending-Deletes Konzept (beide Plattformen)

Wenn der Nutzer offline eine Session loescht, muss das Delete beim naechsten Sync an das Backend uebermittelt werden. Dafuer wird eine lokale PendingDelete-Tabelle gefuehrt.

**Swift (SwiftData):**
```swift
@Model
final class PendingDelete {
    @Attribute(.unique) var id: UUID
    var entityId: UUID      // ID des geloeschten Eintrags
    var entityType: String  // "WorkSession", "VacationDay", etc.
    var deletedAt: Date

    init(entityId: UUID, entityType: String) {
        self.id = UUID()
        self.entityId = entityId
        self.entityType = entityType
        self.deletedAt = Date()
    }
}
```

**Kotlin (Room):**
```kotlin
@Entity(tableName = "pending_deletes")
data class PendingDeleteEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val entityId: String,       // ID des geloeschten Eintrags
    val entityType: String,     // "WorkSession", "VacationDay", etc.
    val deletedAt: String = Instant.now().toString()
)

@Dao
interface PendingDeleteDao {
    @Query("SELECT * FROM pending_deletes WHERE entityType = :type")
    suspend fun getByType(type: String): List<PendingDeleteEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(entry: PendingDeleteEntity)

    @Query("DELETE FROM pending_deletes WHERE entityId = :entityId")
    suspend fun deleteByEntityId(entityId: String)
}
```

**Integration in SyncEngine (Pseudocode):**
```
syncWorkSessions():
    // VOR dem Upload: Pending-Deletes abarbeiten
    pendingDeletes = fetch PendingDelete where entityType == "WorkSession"
    for delete in pendingDeletes:
        try: DELETE /v1/work-sessions/{delete.entityId}
        on success: remove from PendingDelete table

    // Normaler Sync-Flow (wie gehabt)...

    // BEIM Upsert: IDs aus PendingDeletes ignorieren
    pendingDeleteIds = Set(pendingDeletes.map { entityId })
    for dto in serverSessions:
        if dto.id NOT IN pendingDeleteIds:
            upsert(dto)
```

---

### Sync-Trigger (iOS, E07-S03)

```swift
// In FakturusTrackApp.swift -- Scene-Phase + 30s Timer
.onChange(of: scenePhase) { _, phase in
    if phase == .active {
        startSyncTimer()
        Task { await services.syncEngine?.syncAll() }
    } else {
        stopSyncTimer()
    }
}

// Background Fetch
func application(_ application: UIApplication,
                 handleEventsForBackgroundURLSession identifier: String) async {
    // BGAppRefreshTask
}

// In AppDelegate oder App:
BGTaskScheduler.shared.register(
    forTaskWithIdentifier: "com.fakturus.track.sync",
    using: nil
) { task in
    Task {
        await services.syncEngine?.syncAll()
        task.setTaskCompleted(success: true)
    }
}
```

### Android: SyncEngine.kt

```kotlin
class SyncEngine(
    private val apiClient: APIClient,
    private val database: AppDatabase,
    private val networkMonitor: NetworkMonitor
) {
    private val _isSyncing = MutableStateFlow(false)
    val isSyncing: StateFlow<Boolean> = _isSyncing.asStateFlow()

    private val _lastSyncDate = MutableStateFlow<Instant?>(null)
    val lastSyncDate: StateFlow<Instant?> = _lastSyncDate.asStateFlow()

    private val mutex = Mutex()

    suspend fun syncAll() {
        if (!mutex.tryLock()) return // Bereits am syncing
        if (!networkMonitor.isConnected.value) { mutex.unlock(); return }

        _isSyncing.value = true
        try {
            syncWorkSessions()
            syncVacationDays()
            syncUserSettings()
            _lastSyncDate.value = Instant.now()
        } catch (e: Exception) {
            Log.e("SyncEngine", "Sync failed", e)
        } finally {
            _isSyncing.value = false
            mutex.unlock()
        }
    }

    private suspend fun syncWorkSessions() {
        val dao = database.workSessionDao()
        val pending = dao.getPendingSessions()

        val serverSessions = if (pending.isNotEmpty()) {
            apiClient.syncWorkSessions(
                SyncWorkSessionsRequest(workSessions = pending.map { it.toDTO() })
            )
        } else {
            apiClient.getWorkSessions()
        }

        val serverIds = serverSessions.map { it.id }.toSet()
        val synced = dao.getSyncedSessions()
        synced.filter { it.id !in serverIds }.forEach { dao.delete(it) }

        serverSessions.forEach { dto ->
            dao.insert(dto.toEntity()) // REPLACE handles upsert
        }
    }

    private suspend fun syncVacationDays() {
        val dao = database.vacationDayDao()
        val allLocal = dao.getAll()
        val pending = allLocal.filter { it.isPendingSync }

        // Optimierung: Nur POST wenn lokale Aenderungen vorhanden,
        // sonst genuegt GET (spart Netzwerk, konsistent mit MAUI-Muster)
        if (pending.isNotEmpty()) {
            // Pending vorhanden -> ALLE lokalen Tage senden (nicht nur pending!)
            val request = SyncVacationDaysRequest(
                vacationDays = allLocal.map { it.toDTO() }
            )
            val response = apiClient.syncVacationDays(request)
            response.deletedIds.forEach { dao.deleteById(it) }
            response.serverVacationDays.forEach { dao.insert(it.toEntity()) }
        } else {
            // Keine Pending -> einfacher GET genuegt
            val serverDays = apiClient.getVacationDays()
            serverDays.forEach { dao.insert(it.toEntity()) }
        }
    }

    private suspend fun syncUserSettings() {
        val settingsDao = database.userSettingsDao()
        val serverSettings = apiClient.getUserSettings()
        settingsDao.upsert(
            UserSettingsEntity(
                userId = "",
                calendarUrl = serverSettings.calendarUrl,
                vacationDaysPerYear = serverSettings.vacationDaysPerYear,
                workHoursPerWeek = serverSettings.workHoursPerWeek,
                workDays = serverSettings.workDays,
                bundesland = serverSettings.bundesland,
                isSynced = true, isPendingSync = false
            )
        )
    }
}
```

### Android: SyncWorker.kt

```kotlin
class SyncWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        val app = applicationContext as FakturusTrackApp
        val syncEngine = app.serviceContainer.syncEngine ?: return Result.failure()
        return try {
            syncEngine.syncAll()
            Result.success()
        } catch (e: Exception) {
            if (runAttemptCount < 3) Result.retry() else Result.failure()
        }
    }
}

// In ServiceContainer.onLogin():
fun scheduleSyncWorker(context: Context) {
    val request = PeriodicWorkRequestBuilder<SyncWorker>(15, TimeUnit.MINUTES)
        .setConstraints(
            Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()
        )
        .build()
    WorkManager.getInstance(context)
        .enqueueUniquePeriodicWork("sync", ExistingPeriodicWorkPolicy.KEEP, request)
}

fun cancelSyncWorker(context: Context) {
    WorkManager.getInstance(context).cancelUniqueWork("sync")
}
```

---

## Datenfluss

```
Sync-Trigger (App-Start, Netzwerk-Wiederherstellung, Pull-to-Refresh, 30s Timer, Background)
    |
    v
SyncEngine.syncAll()
    |
    +-- Guard: !isSyncing && isConnected
    |
    +-- syncWorkSessions():
    |     1. Pending aus DB (isPendingSync=true, isFinished=true)
    |     2. POST /sync mit Pending ODER GET /work-sessions
    |     3. Response = alle Server-Sessions
    |     4. Set-Differenz: lokale synced loeschen die nicht auf Server
    |     5. Upsert: Server-Sessions in lokale DB
    |     6. Pending als synced markieren
    |
    +-- syncVacationDays():
    |     1. ALLE lokalen VacationDays laden, pending filtern
    |     2a. Pending vorhanden -> POST /vacation-days/sync mit ALLEN
    |     2b. Keine Pending -> GET /vacation-days
    |     3. DeletedIds lokal loeschen (nur bei POST-Pfad)
    |     4. Server-Tage upserten
    |
    +-- syncUserSettings():
          1. GET /settings
          2. Lokal ueberschreiben (Phase 1: Server-wins)
```

---

## Testbare Kriterien

- [ ] iOS: SyncEngine synced nicht parallel (actor Isolation)
- [ ] iOS: SyncEngine synced nicht wenn offline
- [ ] iOS: syncWorkSessions POST /sync wenn pending vorhanden
- [ ] iOS: syncWorkSessions GET wenn keine pending
- [ ] iOS: Set-Differenz loescht lokale Sessions die nicht auf Server
- [ ] iOS: Upsert: existierende Session wird aktualisiert, neue eingefuegt
- [ ] iOS: VacationDays: ALLE lokalen werden gesendet, DeletedIds verarbeitet
- [ ] iOS: Pull-to-Refresh triggert syncAll()
- [ ] Android: SyncEngine.syncAll() mit Mutex-Schutz
- [ ] Android: SyncWorker retried bis 3x bei Fehler
- [ ] Android: WorkManager Periodic Job mit 15min Intervall
- [ ] Beide: finishSession() triggert Sync
- [ ] Beide: Netzwerk-Wiederherstellung triggert Sync

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| SwiftData actor-Isolation Konflikte mit ModelContext | Gering (durch @ModelActor geloest) | `@ModelActor` Macro wird bereits verwendet -- ModelContext wird automatisch im Actor-Kontext erstellt |
| Sync waehrend App-Kill: Dateninkonsistenz | Mittel | Atomare DB-Operationen, Sync-Status-Flags korrekt setzen |
| Server liefert 500 bei Sync | Mittel | Fehler loggen, naechsten Trigger abwarten, Daten bleiben lokal |
| Grosse Datenmenge beim Erst-Sync (1000+ Sessions) | Niedrig | Server paginiert bereits, ggf. batch-weise verarbeiten |
| WorkManager SyncWorker hat keinen Zugriff auf ServiceContainer | Erwartet | `applicationContext as FakturusTrackApp` fuer Zugang |
| Background Fetch iOS: selten ausgefuehrt | Erwartet | In-App 30s Timer ist der primaere Sync-Mechanismus |
