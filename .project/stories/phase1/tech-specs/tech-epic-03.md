# Tech-Spec: EPIC 03 -- Lokale Datenschicht

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Models/WorkSession.swift` | iOS | E03-S01 | @Model mit computed props, toDTO(), update(from:) |
| `Models/VacationDay.swift` | iOS | E03-S01 | @Model |
| `Models/UserSettings.swift` | iOS | E03-S01 | @Model mit Defaults |
| `Models/PersistenceManager.swift` | iOS | E03-S01 | ModelContainer + Schema V1 |
| `Models/DTOs.swift` | iOS | E03-S03 | Alle API-Typen in einer Datei |
| `models/Entities.kt` | Android | E03-S02 | Room @Entity + DAOs: WorkSession, VacationDay, UserSettings |
| `models/AppDatabase.kt` | Android | E03-S02 | RoomDatabase |
| `models/DTOs.kt` | Android | E03-S04 | @Serializable + @SerialName + Konvertierungs-Extensions |

---

## API-Contracts (DTOs)

Die DTOs spiegeln die Backend-API wider. Das Backend nutzt PascalCase.

### WorkSession Sync

**Request:** `POST /v1/work-sessions/sync`
```json
{
  "WorkSessions": [
    {
      "Id": "550e8400-e29b-41d4-a716-446655440000",
      "Date": "2026-03-29",
      "StartTime": "2026-03-29T08:00:00Z",
      "StopTime": "2026-03-29T17:00:00Z",
      "PauseMinutes": 30
    }
  ]
}
```

**Response:** `200 OK` -- Array aller Server-Sessions
```json
[
  {
    "Id": "550e8400-e29b-41d4-a716-446655440000",
    "UserId": "abc-123",
    "Date": "2026-03-29",
    "StartTime": "2026-03-29T08:00:00Z",
    "StopTime": "2026-03-29T17:00:00Z",
    "PauseMinutes": 30,
    "CreatedAt": "2026-03-29T08:00:00Z",
    "UpdatedAt": "2026-03-29T17:01:00Z",
    "SyncedAt": "2026-03-29T17:01:00Z"
  }
]
```

### VacationDay Sync

**Request:** `POST /v1/vacation-days/sync`
```json
{
  "VacationDays": [
    {
      "Id": "...",
      "Date": "2026-04-10",
      "CreatedAt": "2026-03-01T10:00:00Z",
      "UpdatedAt": "2026-03-01T10:00:00Z",
      "SyncedAt": null
    }
  ]
}
```

**Response:**
```json
{
  "ServerVacationDays": [ { "Id": "...", "Date": "2026-04-10", ... } ],
  "DeletedIds": [ "old-id-1", "old-id-2" ]
}
```

### UserSettings

**GET /v1/settings:**
```json
{
  "CalendarUrl": null,
  "VacationDaysPerYear": 30,
  "WorkHoursPerWeek": 40.0,
  "WorkDays": 31,
  "Bundesland": "NW"
}
```

---

## Code-Skizzen

### iOS: WorkSession.swift

```swift
import SwiftData
import Foundation

@Model
final class WorkSession {
    @Attribute(.unique) var id: UUID
    var userId: String
    var date: Date
    var startTime: Date
    var stopTime: Date?
    var pauseMinutes: Int
    var calendarEventId: String?   // lokal, nicht gesynct
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

    // MARK: - Computed

    var isRunning: Bool { !isFinished && stopTime == nil }

    /// Brutto-Dauer in Sekunden
    var duration: TimeInterval {
        let end = stopTime ?? Date()
        return end.timeIntervalSince(startTime)
    }

    /// Netto-Dauer in Sekunden (abzgl. Pause)
    var netDuration: TimeInterval {
        max(0, duration - Double(pauseMinutes * 60))
    }

    /// Netto-Dauer in Minuten
    var netDurationMinutes: Int {
        Int(netDuration / 60)
    }

    /// Gruppierungsschluessel "Maerz 2026"
    var monthKey: String { date.monthYearString }

    // MARK: - DTO Konvertierung

    func toDTO() -> WorkSessionSyncItem {
        WorkSessionSyncItem(
            id: id.uuidString,
            date: ISO8601DateFormatter.dateOnly.string(from: date),
            startTime: startTime.ISO8601Format(),
            stopTime: stopTime?.ISO8601Format(),
            pauseMinutes: pauseMinutes
        )
    }

    func update(from dto: WorkSessionDTO) {
        if let d = ISO8601DateFormatter.dateOnly.date(from: dto.date) { date = d }
        if let t = ISO8601DateFormatter().date(from: dto.startTime) { startTime = t }
        stopTime = dto.stopTime.flatMap { ISO8601DateFormatter().date(from: $0) }
        pauseMinutes = dto.pauseMinutes ?? 0
        updatedAt = Date()
        syncedAt = Date()
        isPendingSync = false
        isSynced = true
        isFinished = stopTime != nil
    }

    convenience init(from dto: WorkSessionDTO) {
        self.init(
            id: UUID(uuidString: dto.id) ?? UUID(),
            userId: dto.userId ?? "",
            date: ISO8601DateFormatter.dateOnly.date(from: dto.date) ?? Date(),
            startTime: ISO8601DateFormatter().date(from: dto.startTime) ?? Date(),
            stopTime: dto.stopTime.flatMap { ISO8601DateFormatter().date(from: $0) },
            pauseMinutes: dto.pauseMinutes ?? 0,
            isPendingSync: false,
            isSynced: true,
            isFinished: true
        )
        self.syncedAt = Date()
    }
}

// ISO8601 Helper
extension ISO8601DateFormatter {
    static let dateOnly: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withFullDate]
        return f
    }()
}
```

### iOS: PersistenceManager.swift

```swift
import SwiftData

enum PersistenceManager {
    static let schema = Schema([
        WorkSession.self,
        VacationDay.self,
        UserSettings.self
    ])

    static let container: ModelContainer = {
        let config = ModelConfiguration(schema: schema, isStoredInMemoryOnly: false)
        return try! ModelContainer(for: schema, configurations: [config])
    }()

    /// Fuer Tests: In-Memory Container
    static func testContainer() -> ModelContainer {
        let config = ModelConfiguration(schema: schema, isStoredInMemoryOnly: true)
        return try! ModelContainer(for: schema, configurations: [config])
    }
}
```

### iOS: DTOs.swift

```swift
// MARK: - Request Types

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

// MARK: - Response Types

struct WorkSessionDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let startTime: String
    let stopTime: String?
    let pauseMinutes: Int?
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
    let deletedIds: [String]
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

### Android: Entities.kt (kompakt -- alles in einer Datei)

```kotlin
@Entity(tableName = "work_sessions")
data class WorkSessionEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,           // "2026-03-29"
    val startTime: String,      // "2026-03-29T08:00:00Z"
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
    val isRunning: Boolean get() = !isFinished && stopTime == null

    val durationMinutes: Long get() {
        val start = Instant.parse(startTime)
        val end = stopTime?.let { Instant.parse(it) } ?: Instant.now()
        return Duration.between(start, end).toMinutes()
    }

    val netDurationMinutes: Long get() = maxOf(0, durationMinutes - pauseMinutes)

    val monthKey: String get() {
        val ld = LocalDate.parse(date)
        return ld.format(DateTimeFormatter.ofPattern("MMMM yyyy", Locale.GERMAN))
    }

    fun toDTO() = WorkSessionSyncItem(
        id = id, date = date, startTime = startTime,
        stopTime = stopTime, pauseMinutes = pauseMinutes
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
) {
    fun toDTO() = VacationDaySyncItem(
        id = id, date = date,
        createdAt = createdAt, updatedAt = updatedAt, syncedAt = syncedAt
    )
}

@Entity(tableName = "user_settings")
data class UserSettingsEntity(
    @PrimaryKey val userId: String,
    val calendarUrl: String? = null,
    val vacationDaysPerYear: Int = 30,
    val workHoursPerWeek: Double = 40.0,
    val workDays: Int = 31,          // Bitmask: Mo-Fr = 31
    val bundesland: String = "NW",
    val isSynced: Boolean = false,
    val isPendingSync: Boolean = true
)
```

### Android: AppDatabase.kt (DAOs + Database in einer Datei)

```kotlin
@Dao
interface WorkSessionDao {
    @Query("SELECT * FROM work_sessions ORDER BY date DESC, startTime DESC")
    fun getAllOrderedByDate(): Flow<List<WorkSessionEntity>>

    @Query("SELECT * FROM work_sessions WHERE isPendingSync = 1 AND isFinished = 1")
    suspend fun getPendingSessions(): List<WorkSessionEntity>

    @Query("SELECT * FROM work_sessions WHERE isSynced = 1")
    suspend fun getSyncedSessions(): List<WorkSessionEntity>

    @Query("SELECT * FROM work_sessions WHERE isFinished = 0 LIMIT 1")
    suspend fun getActiveSession(): WorkSessionEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(session: WorkSessionEntity)

    @Update
    suspend fun update(session: WorkSessionEntity)

    @Delete
    suspend fun delete(session: WorkSessionEntity)

    @Query("DELETE FROM work_sessions WHERE id = :id")
    suspend fun deleteById(id: String)
}

@Dao
interface VacationDayDao {
    @Query("SELECT * FROM vacation_days ORDER BY date")
    fun getAllOrderedByDate(): Flow<List<VacationDayEntity>>

    @Query("SELECT * FROM vacation_days")
    suspend fun getAll(): List<VacationDayEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(day: VacationDayEntity)

    @Delete
    suspend fun delete(day: VacationDayEntity)

    @Query("DELETE FROM vacation_days WHERE id = :id")
    suspend fun deleteById(id: String)
}

@Dao
interface UserSettingsDao {
    @Query("SELECT * FROM user_settings LIMIT 1")
    fun getSettings(): Flow<UserSettingsEntity?>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(settings: UserSettingsEntity)
}

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
```

### Android: DTOs.kt

```kotlin
// Request Types
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

@Serializable
data class SyncVacationDaysRequest(
    @SerialName("VacationDays") val vacationDays: List<VacationDaySyncItem>
)

@Serializable
data class VacationDaySyncItem(
    @SerialName("Id") val id: String,
    @SerialName("Date") val date: String,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
)

// Response Types
@Serializable
data class WorkSessionDTO(
    @SerialName("Id") val id: String,
    @SerialName("UserId") val userId: String? = null,
    @SerialName("Date") val date: String,
    @SerialName("StartTime") val startTime: String,
    @SerialName("StopTime") val stopTime: String? = null,
    @SerialName("PauseMinutes") val pauseMinutes: Int = 0,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
) {
    fun toEntity() = WorkSessionEntity(
        id = id, userId = userId ?: "", date = date,
        startTime = startTime, stopTime = stopTime,
        pauseMinutes = pauseMinutes,
        createdAt = createdAt ?: Instant.now().toString(),
        updatedAt = updatedAt ?: Instant.now().toString(),
        syncedAt = Instant.now().toString(),
        isPendingSync = false, isSynced = true, isFinished = stopTime != null
    )
}

@Serializable
data class VacationDayDTO(
    @SerialName("Id") val id: String,
    @SerialName("UserId") val userId: String? = null,
    @SerialName("Date") val date: String,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
) {
    fun toEntity() = VacationDayEntity(
        id = id, userId = userId ?: "", date = date,
        createdAt = createdAt ?: Instant.now().toString(),
        updatedAt = updatedAt ?: Instant.now().toString(),
        syncedAt = Instant.now().toString(),
        isPendingSync = false, isSynced = true
    )
}

@Serializable
data class SyncVacationDaysResponse(
    @SerialName("ServerVacationDays") val serverVacationDays: List<VacationDayDTO>,
    @SerialName("DeletedIds") val deletedIds: List<String>
)

@Serializable
data class UserSettingsDTO(
    @SerialName("CalendarUrl") val calendarUrl: String? = null,
    @SerialName("VacationDaysPerYear") val vacationDaysPerYear: Int = 30,
    @SerialName("WorkHoursPerWeek") val workHoursPerWeek: Double = 40.0,
    @SerialName("WorkDays") val workDays: Int = 31,
    @SerialName("Bundesland") val bundesland: String = "NW"
)
```

---

## Datenfluss

```
E03 ist die Grundlage. Kein eigener User-Facing Flow, aber alle anderen
EPICs greifen auf diese Schicht zu:

ViewModel --> WorkSession.swift / Entities.kt --> SQLite (SwiftData / Room)
    ^                                                |
    |                                                |
    +-- @Query / Flow<List> --- automatische UI-Updates

SyncEngine --> WorkSession.toDTO() --> DTOs.swift/kt --> APIClient --> Backend
           <-- WorkSessionDTO --> WorkSession.update(from:) / .toEntity()
```

---

## Testbare Kriterien

- [ ] iOS: WorkSession erstellen, lesen, aktualisieren, loeschen via ModelContext
- [ ] iOS: `WorkSession.isRunning` ist `true` wenn `isFinished == false && stopTime == nil`
- [ ] iOS: `WorkSession.netDuration` berechnet korrekt: 9h Brutto - 30min Pause = 8.5h
- [ ] iOS: `WorkSession.toDTO()` erzeugt korrektes WorkSessionSyncItem
- [ ] iOS: `@Query` liefert Sessions sortiert nach Datum (absteigend)
- [ ] Android: Room Entity insert + query via DAO
- [ ] Android: `WorkSessionEntity.durationMinutes` berechnet korrekt
- [ ] Android: `WorkSessionEntity.monthKey` liefert "Maerz 2026"
- [ ] Android: `WorkSessionDTO.toEntity()` setzt alle Felder korrekt
- [ ] Android: `Flow<List<WorkSessionEntity>>` emittiert bei DB-Aenderung
- [ ] Beide: DTOs serialisieren/deserialisieren korrekt mit PascalCase-Feldnamen

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| SwiftData @Query Performance bei vielen Sessions | Niedrig | FetchDescriptor mit Limit/Offset |
| Room KSP Annotation Processor Fehler | Mittel | KSP-Version genau an Room-Version anpassen |
| ISO8601 Parsing mit/ohne Millisekunden | Hoch | Mehrere DateFormatter mit Fallback-Kette |
| SwiftData Schema-Migration spaeter | Niedrig | VersionedSchema von Anfang an vorbereiten |
