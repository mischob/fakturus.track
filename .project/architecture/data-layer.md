# Datenschicht -- Fakturus Track

## Ueberblick

Beide Plattformen nutzen lokale Datenbanken als **primaere Datenquelle fuer die UI**. Das Backend ist die Source of Truth, aber die App zeigt immer lokale Daten an. Die Sync-Engine haelt beides synchron.

```
UI <-- liest --> Lokale DB <-- sync --> Backend API
```

---

## iOS: SwiftData

### Warum SwiftData statt GRDB oder Core Data?

| Option | Vorteile | Nachteile | Entscheidung |
|--------|----------|-----------|-------------|
| **SwiftData** | Native SwiftUI-Integration, @Query, @Model, kein Boilerplate | Neu (iOS 17+), weniger Kontrolle ueber SQL | **Gewaehlt** |
| GRDB | Volle SQL-Kontrolle, bewaehrt | Externe Dependency, kein @Query | Fallback |
| Core Data | Sehr maechtiges Framework | Viel Boilerplate, veraltet gegenueber SwiftData | Nein |

SwiftData ist die richtige Wahl weil:
1. Minimum iOS 17 ist gesetzt -> kein Kompatibilitaetsproblem
2. `@Query` eliminiert manuelle Fetch-Requests
3. `@Model` Macro eliminiert NSManagedObject Boilerplate
4. Automatische UI-Updates bei DB-Aenderungen (wie Room Flows)

### SwiftData Models

```swift
@Model
final class WorkSession {
    @Attribute(.unique) var id: UUID
    var userId: String
    var date: Date
    var startTime: Date
    var stopTime: Date?
    var pauseMinutes: Int
    var calendarEventId: String?
    var createdAt: Date
    var updatedAt: Date
    var syncedAt: Date?
    var isPendingSync: Bool
    var isSynced: Bool
    var isFinished: Bool

    init(
        id: UUID = UUID(),
        userId: String = "",
        date: Date = Date(),
        startTime: Date = Date(),
        stopTime: Date? = nil,
        pauseMinutes: Int = 0,
        calendarEventId: String? = nil,
        isPendingSync: Bool = true,
        isSynced: Bool = false,
        isFinished: Bool = false
    ) {
        self.id = id
        self.userId = userId
        self.date = date
        self.startTime = startTime
        self.stopTime = stopTime
        self.pauseMinutes = pauseMinutes
        self.calendarEventId = calendarEventId
        self.createdAt = Date()
        self.updatedAt = Date()
        self.isPendingSync = isPendingSync
        self.isSynced = isSynced
        self.isFinished = isFinished
    }

    // Computed Properties (nicht persistiert)
    var isRunning: Bool {
        !isFinished && stopTime == nil
    }

    var duration: TimeInterval {
        let end = stopTime ?? Date()
        return end.timeIntervalSince(startTime)
    }

    // DTO-Konvertierung (direkt im Model, kein Mapper)
    func toDTO() -> WorkSessionSyncItem {
        WorkSessionSyncItem(
            id: id.uuidString,
            date: date.formatted(.iso8601.year().month().day()),
            startTime: startTime.ISO8601Format(),
            stopTime: stopTime?.ISO8601Format(),
            pauseMinutes: pauseMinutes
        )
    }

    func update(from dto: WorkSessionDTO) {
        // Server-wins: alle Felder ueberschreiben
        self.date = ISO8601DateFormatter().date(from: dto.date) ?? self.date
        self.startTime = ISO8601DateFormatter().date(from: dto.startTime) ?? self.startTime
        self.stopTime = dto.stopTime.flatMap { ISO8601DateFormatter().date(from: $0) }
        self.pauseMinutes = dto.pauseMinutes ?? 0
        self.updatedAt = Date()
        self.syncedAt = Date()
        self.isPendingSync = false
        self.isSynced = true
        self.isFinished = self.stopTime != nil
    }
}

@Model
final class VacationDay {
    @Attribute(.unique) var id: UUID
    var userId: String
    var date: Date
    var createdAt: Date
    var updatedAt: Date
    var syncedAt: Date?
    var isPendingSync: Bool
    var isSynced: Bool

    init(id: UUID = UUID(), userId: String = "", date: Date) {
        self.id = id
        self.userId = userId
        self.date = date
        self.createdAt = Date()
        self.updatedAt = Date()
        self.isPendingSync = true
        self.isSynced = false
    }
}

// Phase 2: SickDay wird erst in Phase 2 implementiert
@Model
final class SickDay {
    @Attribute(.unique) var id: UUID
    var userId: String
    var date: Date
    var createdAt: Date
    var updatedAt: Date
    var syncedAt: Date?
    var isPendingSync: Bool
    var isSynced: Bool

    init(id: UUID = UUID(), userId: String = "", date: Date) {
        self.id = id
        self.userId = userId
        self.date = date
        self.createdAt = Date()
        self.updatedAt = Date()
        self.isPendingSync = true
        self.isSynced = false
    }
}

@Model
final class UserSettings {
    @Attribute(.unique) var userId: String
    var calendarUrl: String?
    var vacationDaysPerYear: Int
    var workHoursPerWeek: Double
    var workDays: Int  // Bitmask: 1=Mo, 2=Di, 4=Mi, 8=Do, 16=Fr, 32=Sa, 64=So
    var bundesland: String
    var isSynced: Bool
    var isPendingSync: Bool

    init(userId: String) {
        self.userId = userId
        self.calendarUrl = nil
        self.vacationDaysPerYear = 30
        self.workHoursPerWeek = 40.0
        self.workDays = 31  // Mo-Fr = 1+2+4+8+16
        self.bundesland = "NW"
        self.isSynced = false
        self.isPendingSync = true
    }
}

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

// Phase 2: SchoolHolidayPeriod wird erst in Phase 2 implementiert
@Model
final class SchoolHolidayPeriod {
    @Attribute(.unique) var id: UUID
    var name: String
    var startDate: Date
    var endDate: Date
    var year: Int

    init(name: String, startDate: Date, endDate: Date, year: Int) {
        self.id = UUID()
        self.name = name
        self.startDate = startDate
        self.endDate = endDate
        self.year = year
    }
}
```

### SwiftData @Query Beispiele

```swift
// In der View -- automatische UI-Updates bei DB-Aenderungen
@Query(sort: \WorkSession.date, order: .reverse)
private var allSessions: [WorkSession]

@Query(filter: #Predicate<WorkSession> { $0.isPendingSync && !$0.isSynced })
private var pendingSessions: [WorkSession]

@Query(sort: \VacationDay.date)
private var vacationDays: [VacationDay]
```

---

## Android: Room

### Room Entities

```kotlin
@Entity(tableName = "work_sessions")
data class WorkSessionEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,               // "2026-03-29" (ISO LocalDate)
    val startTime: String,          // "2026-03-29T08:00:00Z" (ISO Instant)
    val stopTime: String? = null,
    val pauseMinutes: Int = 0,
    val calendarEventId: String? = null,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false,
    val isFinished: Boolean = false
) {
    // Computed (nicht persistiert)
    val isRunning: Boolean get() = !isFinished && stopTime == null

    val durationMinutes: Long get() {
        val start = Instant.parse(startTime)
        val end = stopTime?.let { Instant.parse(it) } ?: Instant.now()
        return Duration.between(start, end).toMinutes()
    }

    val monthKey: String get() {
        val localDate = LocalDate.parse(date)
        return localDate.format(DateTimeFormatter.ofPattern("MMMM yyyy", Locale.GERMAN))
    }

    fun toDTO() = WorkSessionSyncItem(
        id = id,
        date = date,
        startTime = startTime,
        stopTime = stopTime,
        pauseMinutes = pauseMinutes
    )
}

@Entity(tableName = "vacation_days")
data class VacationDayEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false
)

// Phase 2: SickDayEntity wird erst in Phase 2 implementiert
@Entity(tableName = "sick_days")
data class SickDayEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false
)

@Entity(tableName = "pending_deletes")
data class PendingDeleteEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val entityId: String,       // ID des geloeschten Eintrags
    val entityType: String,     // "WorkSession", "VacationDay", etc.
    val deletedAt: String = Instant.now().toString()
)

@Entity(tableName = "user_settings")
data class UserSettingsEntity(
    @PrimaryKey val userId: String,
    val calendarUrl: String? = null,
    val vacationDaysPerYear: Int = 30,
    val workHoursPerWeek: Double = 40.0,
    val workDays: Int = 31,
    val bundesland: String = "NW",
    val isSynced: Boolean = false,
    val isPendingSync: Boolean = true
)
```

### Room DAOs

```kotlin
@Dao
interface WorkSessionDao {
    @Query("SELECT * FROM work_sessions ORDER BY date DESC, startTime DESC")
    fun getAllOrderedByDate(): Flow<List<WorkSessionEntity>>

    @Query("SELECT * FROM work_sessions WHERE isPendingSync = 1 AND isFinished = 1")
    suspend fun getPendingSessions(): List<WorkSessionEntity>

    @Query("SELECT * FROM work_sessions WHERE isSynced = 1")
    suspend fun getSyncedSessions(): List<WorkSessionEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(session: WorkSessionEntity)

    @Update
    suspend fun update(session: WorkSessionEntity)

    @Delete
    suspend fun delete(session: WorkSessionEntity)

    @Query("DELETE FROM work_sessions WHERE id NOT IN (:keepIds) AND isSynced = 1")
    suspend fun deleteSyncedNotIn(keepIds: List<String>)
}

@Dao
interface VacationDayDao {
    @Query("SELECT * FROM vacation_days ORDER BY date")
    fun getAllOrderedByDate(): Flow<List<VacationDayEntity>>

    @Query("SELECT * FROM vacation_days")
    suspend fun getAll(): List<VacationDayEntity>  // Fuer Sync: ALLE Tage senden

    @Query("SELECT * FROM vacation_days WHERE isPendingSync = 1")
    suspend fun getPendingDays(): List<VacationDayEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(day: VacationDayEntity)

    @Delete
    suspend fun delete(day: VacationDayEntity)

    @Query("DELETE FROM vacation_days WHERE id = :id")
    suspend fun deleteById(id: String)  // Fuer DeletedIds aus Sync-Response
}

// Phase 2: SickDayDao wird erst in Phase 2 implementiert
@Dao
interface SickDayDao {
    @Query("SELECT * FROM sick_days ORDER BY date")
    fun getAllOrderedByDate(): Flow<List<SickDayEntity>>

    @Query("SELECT * FROM sick_days")
    suspend fun getAll(): List<SickDayEntity>  // Fuer Sync: ALLE Tage senden

    @Query("SELECT * FROM sick_days WHERE isPendingSync = 1")
    suspend fun getPendingDays(): List<SickDayEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(day: SickDayEntity)

    @Delete
    suspend fun delete(day: SickDayEntity)

    @Query("DELETE FROM sick_days WHERE id = :id")
    suspend fun deleteById(id: String)  // Fuer DeletedIds aus Sync-Response
}

@Dao
interface PendingDeleteDao {
    @Query("SELECT * FROM pending_deletes WHERE entityType = :type")
    suspend fun getByType(type: String): List<PendingDeleteEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(entry: PendingDeleteEntity)

    @Query("DELETE FROM pending_deletes WHERE entityId = :entityId")
    suspend fun deleteByEntityId(entityId: String)
}

@Dao
interface UserSettingsDao {
    @Query("SELECT * FROM user_settings LIMIT 1")
    fun getSettings(): Flow<UserSettingsEntity?>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(settings: UserSettingsEntity)
}
```

---

## DTOs (API Request/Response Typen)

### Swift (eine Datei: `DTOs.swift`)

```swift
// Request-Typen
struct SyncWorkSessionsRequest: Encodable {
    let workSessions: [WorkSessionSyncItem]
}

struct WorkSessionSyncItem: Encodable {
    let id: String
    let date: String
    let startTime: String
    let stopTime: String?
    let pauseMinutes: Int
}

struct SyncVacationDaysRequest: Encodable {
    let vacationDays: [VacationDaySyncItem]
}

struct VacationDaySyncItem: Encodable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct SyncSickDaysRequest: Encodable {
    let sickDays: [SickDaySyncItem]
}

struct SickDaySyncItem: Encodable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

// Response-Typen
struct WorkSessionDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let startTime: String
    let stopTime: String?
    let pauseMinutes: Int?
    let calendarEventId: String?
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct VacationDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct SyncVacationDaysResponse: Decodable {
    let serverVacationDays: [VacationDayDTO]
    let deletedIds: [String]  // IDs der auf dem Server geloeschten Urlaubstage
}

struct SickDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?
}

struct SyncSickDaysResponse: Decodable {
    let serverSickDays: [SickDayDTO]
    let deletedIds: [String]  // IDs der auf dem Server geloeschten Krankheitstage
}

struct UserSettingsDTO: Codable {
    let calendarUrl: String?
    let vacationDaysPerYear: Int
    let workHoursPerWeek: Double
    let workDays: Int
    let bundesland: String
}

struct OvertimeSummaryDTO: Decodable {
    let totalOvertimeHours: Double
    let monthlyOvertime: [MonthlyOvertimeDTO]
    let vacationDaysTaken: Int
    let vacationDaysRemaining: Int
    let vacationDaysPerYear: Int
    let holidaysTaken: Int
    let schoolHolidayHoursNotWorked: Double
}

struct MonthlyOvertimeDTO: Decodable {
    let year: Int
    let month: Int
    let monthName: String
    let overtimeHours: Double
    let workedHours: Double
    let expectedHours: Double
}
```

### Kotlin (eine Datei: `DTOs.kt`)

```kotlin
// Identische Struktur wie Swift, mit @Serializable + @SerialName
@Serializable
data class SyncWorkSessionsRequest(
    @SerialName("WorkSessions") val workSessions: List<WorkSessionSyncItem>
)

@Serializable
data class WorkSessionSyncItem(
    @SerialName("Id") val id: String,
    @SerialName("Date") val date: String,
    @SerialName("StartTime") val startTime: String,
    @SerialName("StopTime") val stopTime: String? = null,
    @SerialName("PauseMinutes") val pauseMinutes: Int = 0
)

// ... analog zu Swift DTOs mit @SerialName Annotationen
```

---

## Caching-Strategie

### Kein separater Cache

Es gibt **keinen separaten HTTP-Cache oder In-Memory-Cache**. Begruendung:
- Die lokale Datenbank IST der Cache
- Alle Daten werden lokal persistiert
- UI liest immer aus der lokalen DB
- Sync aktualisiert die lokale DB

### Overtime-Summary (JSON Disk-Cache)

Die Overtime-Summary wird vom Backend geladen, aber die letzte Response wird als **einfacher JSON-Disk-Cache** lokal gespeichert. Begruendung:
- Die Berechnung ist komplex (Feiertage, Schulferien, Teilzeit) -- Backend bleibt Source of Truth
- Offline zeigt der Gesamt-Tab den letzten bekannten Stand mit "Zuletzt aktualisiert: vor X Stunden" Hinweis
- Ein komplett leerer Tab bei Offline waere schlechte UX fuer eine "Offline-first" App

**Implementierung (beide Plattformen):**
- Eine JSON-Datei pro Jahr: `overtime_cache_{year}.json`
- Speicherort: App Documents Directory (iOS) / App Files Directory (Android)
- Bei erfolgreicher API-Response: JSON + Timestamp speichern
- Bei Offline/Fehler: Cache laden, "Zuletzt aktualisiert" Hinweis anzeigen
- Kein Expiry -- wird bei jedem erfolgreichen Fetch ueberschrieben

```swift
// OvertimeViewModel -- mit Disk-Cache Fallback
func loadOvertimeSummary(year: Int) async {
    isLoading = true
    do {
        summary = try await apiClient.getOvertimeSummary(year: year)
        // Cache auf Disk speichern
        try OvertimeCache.save(summary: summary!, year: year)
        lastUpdated = Date()
        isShowingCachedData = false
    } catch {
        // Fallback: Letzten Cache-Stand laden
        if let cached = try? OvertimeCache.load(year: year) {
            summary = cached.summary
            lastUpdated = cached.timestamp
            isShowingCachedData = true
        } else {
            self.error = "Uebersicht konnte nicht geladen werden"
        }
    }
    isLoading = false
}
```

```kotlin
// OvertimeViewModel (Kotlin) -- analog
suspend fun loadOvertimeSummary(year: Int) {
    _isLoading.value = true
    try {
        val result = apiClient.getOvertimeSummary(year)
        _summary.value = result
        overtimeCache.save(result, year)
        _lastUpdated.value = Instant.now()
        _isShowingCachedData.value = false
    } catch (e: Exception) {
        val cached = overtimeCache.load(year)
        if (cached != null) {
            _summary.value = cached.summary
            _lastUpdated.value = cached.timestamp
            _isShowingCachedData.value = true
        } else {
            _error.value = "Uebersicht konnte nicht geladen werden"
        }
    }
    _isLoading.value = false
}
```

---

## Migration von bestehenden Daten

### Es gibt keine Daten-Migration

Alle Daten liegen im Backend (PostgreSQL). Beim ersten Login:

1. User meldet sich mit bestehendem B2C-Account an
2. Erster Sync laedt alle Daten vom Backend
3. Lokale DB wird befuellt
4. User sieht sofort alle seine Daten

Die MAUI-App SQLite-Datenbank (`fakturus_track.db`) wird nicht migriert. Sie ist inkompatibel (EF Core Schema vs. SwiftData/Room Schema).

### Voraussetzung

Vor Wechsel zur nativen App muss die MAUI-App alle Daten synchronisiert haben. Die MAUI-App prüft dies und warnt den User bei ausstehenden Syncs.

---

## Datenbank-Schema Versionierung

### iOS (SwiftData)

SwiftData unterstuetzt Schema-Versionierung mit `VersionedSchema`:

```swift
enum SchemaV1: VersionedSchema {
    static var versionIdentifier = Schema.Version(1, 0, 0)
    static var models: [any PersistentModel.Type] {
        [WorkSession.self, VacationDay.self, SickDay.self, UserSettings.self, SchoolHolidayPeriod.self]
    }
}
```

### Android (Room)

Room `exportSchema = true` generiert JSON-Schema-Dateien. Migrationen:

```kotlin
val MIGRATION_1_2 = object : Migration(1, 2) {
    override fun migrate(db: SupportSQLiteDatabase) {
        db.execSQL("ALTER TABLE work_sessions ADD COLUMN newColumn TEXT")
    }
}
```

In Phase 1 starten beide Plattformen mit Version 1. Migrationen werden bei Bedarf in spaeteren Phasen hinzugefuegt.
