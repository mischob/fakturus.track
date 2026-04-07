# Tech-Spec: EPIC 04 -- Krankheitstage (Frontend + Sync)

## Dateien

### Neue Dateien

| Datei | Plattform | Beschreibung |
|-------|-----------|-------------|
| `Models/SickDay.swift` | iOS | @Model, analog VacationDay |

### Modifizierte Dateien

| Datei | Plattform | Aenderung |
|-------|-----------|-----------|
| `Models/DTOs.swift` | iOS | +SickDay DTOs (Request + Response) |
| `Models/DTOs.kt` | Android | +SickDay DTOs (Request + Response) |
| `Models/Entities.kt` | Android | +SickDayEntity |
| `Models/AppDatabase.kt` | Android | +SickDayDao, +SickDayEntity in @Database |
| `Services/API/APIClient+Endpoints.swift` | iOS | +SickDay Endpoints |
| `services/api/APIClient.kt` | Android | +SickDay Endpoints |
| `Services/Sync/SyncEngine.swift` | iOS | +syncSickDays(), syncAll() erweitern |
| `services/sync/SyncEngine.kt` | Android | +syncSickDays(), syncAll() erweitern |
| `Shared/VacationCalendar.swift` | iOS | +Long-Press, +sickDay Markierung |
| `ui/shared/VacationCalendar.kt` | Android | +Long-Press, +sickDay Markierung |
| `Features/Vacation/VacationViewModel.swift` | iOS | +sickDays, +toggleSickDay, +switchType |
| `features/vacation/VacationViewModel.kt` | Android | +sickDays, +toggleSickDay, +switchType |
| `Models/PersistenceManager.swift` | iOS | Schema V2 mit SickDay |
| `ServiceContainer.kt` | Android | +MIGRATION_2_3 |

---

## SickDay Model

### iOS: SickDay.swift

```swift
import SwiftData
import Foundation

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

    init(
        id: UUID = UUID(),
        userId: String = "",
        date: Date = Date(),
        isPendingSync: Bool = true,
        isSynced: Bool = false
    ) {
        self.id = id
        self.userId = userId
        self.date = date
        self.createdAt = Date()
        self.updatedAt = Date()
        self.isPendingSync = isPendingSync
        self.isSynced = isSynced
    }

    func toDTO() -> SickDaySyncItem {
        SickDaySyncItem(
            id: id.uuidString,
            date: ISO8601DateFormatter.dateOnly.string(from: date),
            createdAt: createdAt.ISO8601Format(),
            updatedAt: updatedAt.ISO8601Format(),
            syncedAt: syncedAt?.ISO8601Format()
        )
    }

    func update(from dto: SickDayDTO) {
        if let d = ISO8601DateFormatter.dateOnly.date(from: dto.date) { date = d }
        updatedAt = Date()
        syncedAt = Date()
        isPendingSync = false
        isSynced = true
    }

    convenience init(from dto: SickDayDTO) {
        self.init(
            id: UUID(uuidString: dto.id) ?? UUID(),
            userId: dto.userId ?? "",
            date: ISO8601DateFormatter.dateOnly.date(from: dto.date) ?? Date(),
            isPendingSync: false,
            isSynced: true
        )
        self.syncedAt = Date()
    }
}
```

### Android: Erweiterung in Entities.kt

```kotlin
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
) {
    fun toDTO() = SickDaySyncItem(
        id = id, date = date,
        createdAt = createdAt, updatedAt = updatedAt, syncedAt = syncedAt
    )
}
```

---

## DTOs (Erweiterungen)

### Swift: Ergaenzung in DTOs.swift

```swift
// Request
struct SyncSickDaysRequest: Encodable {
    let sickDays: [SickDaySyncItem]

    enum CodingKeys: String, CodingKey {
        case sickDays = "SickDays"
    }
}

struct SickDaySyncItem: Encodable {
    let id: String
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case date = "Date"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

// Response
struct SickDayDTO: Decodable {
    let id: String
    let userId: String?
    let date: String
    let createdAt: String?
    let updatedAt: String?
    let syncedAt: String?

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case userId = "UserId"
        case date = "Date"
        case createdAt = "CreatedAt"
        case updatedAt = "UpdatedAt"
        case syncedAt = "SyncedAt"
    }
}

struct SyncSickDaysResponse: Decodable {
    let serverSickDays: [SickDayDTO]
    let deletedIds: [String]

    enum CodingKeys: String, CodingKey {
        case serverSickDays = "ServerSickDays"
        case deletedIds = "DeletedIds"
    }
}
```

### Kotlin: Ergaenzung in DTOs.kt

```kotlin
@Serializable
data class SyncSickDaysRequest(
    @SerialName("SickDays") val sickDays: List<SickDaySyncItem>
)

@Serializable
data class SickDaySyncItem(
    @SerialName("Id") val id: String,
    @SerialName("Date") val date: String,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
)

@Serializable
data class SickDayDTO(
    @SerialName("Id") val id: String,
    @SerialName("UserId") val userId: String? = null,
    @SerialName("Date") val date: String,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
) {
    fun toEntity() = SickDayEntity(
        id = id, userId = userId ?: "", date = date,
        createdAt = createdAt ?: Instant.now().toString(),
        updatedAt = updatedAt ?: Instant.now().toString(),
        syncedAt = Instant.now().toString(),
        isPendingSync = false, isSynced = true
    )
}

@Serializable
data class SyncSickDaysResponse(
    @SerialName("ServerSickDays") val serverSickDays: List<SickDayDTO>,
    @SerialName("DeletedIds") val deletedIds: List<String>
)
```

---

## APIClient-Erweiterungen

### Swift

```swift
extension APIClient {
    // SickDays
    func getSickDays(from: Date, to: Date) async throws -> [SickDayDTO] {
        let fromStr = ISO8601DateFormatter.dateOnly.string(from: from)
        let toStr = ISO8601DateFormatter.dateOnly.string(from: to)
        return try await get("/v1/sick-days?from=\(fromStr)&to=\(toStr)")
    }

    func syncSickDays(_ request: SyncSickDaysRequest) async throws -> SyncSickDaysResponse {
        try await post("/v1/sick-days/sync", body: request)
    }
}
```

### Kotlin

```kotlin
// In APIClient.kt ergaenzen:
suspend fun getSickDays(from: String, to: String): List<SickDayDTO> =
    get("/v1/sick-days", queryParams = mapOf("from" to from, "to" to to))

suspend fun syncSickDays(request: SyncSickDaysRequest): SyncSickDaysResponse =
    post("/v1/sick-days/sync", request)
```

---

## SyncEngine: syncSickDays()

Identisch zu syncVacationDays(). ALLE lokalen Tage senden, nicht nur pending.

### Kotlin

```kotlin
private suspend fun syncSickDays() {
    val dao = database.sickDayDao()
    val allLocal = dao.getAll()
    val pending = allLocal.filter { it.isPendingSync }

    if (pending.isNotEmpty()) {
        val request = SyncSickDaysRequest(
            sickDays = allLocal.map { it.toDTO() }
        )
        val response = apiClient.syncSickDays(request)

        response.deletedIds.forEach { dao.deleteById(it) }
        response.serverSickDays.forEach { dto -> dao.insert(dto.toEntity()) }
    } else {
        // Kein pending -> nur Server-Stand holen und lokal abgleichen (Set-Differenz)
        val serverDays = apiClient.getSickDays("2000-01-01", "2099-12-31")
        val localSynced = dao.getAll().filter { it.isSynced }

        // Lokale SickDays loeschen die nicht mehr auf dem Server sind (Set-Differenz)
        val serverIds = serverDays.map { it.id }.toSet()
        localSynced.filter { it.id !in serverIds }.forEach { dao.deleteById(it.id) }

        // Server-SickDays upserten (neue hinzufuegen, bestehende aktualisieren)
        serverDays.forEach { dto -> dao.insert(dto.toEntity()) }
    }
}
```

syncAll() erweitern um `syncSickDays()` nach `syncVacationDays()`.

---

## Long-Press Kontext-Menue

### Datenfluss

```
Long-Press auf leeren Arbeitstag (z.B. 15. Maerz)
    |
    v
Kontext-Menue oeffnet sich:
  [Urlaub-Icon] "Urlaub"
  [Krank-Icon]  "Krank"
    |
    +-- "Urlaub" gewaehlt -> toggleVacationDay(15.03.) [bestehende Logik]
    +-- "Krank" gewaehlt  -> toggleSickDay(15.03.)
    |
    v
toggleSickDay:
  SickDay(date: 15.03., isPendingSync: true) -> in DB speichern
  sickDayDates Set aktualisieren -> UI zeigt roten Kreis
  SyncEngine.syncSickDays() triggern

---

Long-Press auf markierten Tag (z.B. 15. Maerz = Urlaub)
    |
    v
Kontext-Menue:
  "Typ wechseln"  -- Urlaub -> Krank (oder umgekehrt)
  "Entfernen"      -- Markierung komplett loeschen
    |
    +-- "Typ wechseln" -> switchAbsenceType(15.03.)
    |     1. VacationDay fuer 15.03. loeschen
    |     2. SickDay fuer 15.03. erstellen
    |     3. Beide Syncs triggern
    |
    +-- "Entfernen" -> toggleVacationDay(15.03.) [entfernt]
```

### iOS: Context Menu

```swift
.contextMenu {
    if cell.type == .workday || cell.type == .today {
        Button { toggleVacationDay(cell.date!) } label: {
            Label("Urlaub", systemImage: "sun.max.fill")
        }
        Button { toggleSickDay(cell.date!) } label: {
            Label("Krank", systemImage: "cross.circle.fill")
        }
    }
    if cell.type == .vacation || cell.type == .sickDay {
        Button { switchAbsenceType(cell.date!) } label: {
            Label("Typ wechseln", systemImage: "arrow.triangle.2.circlepath")
        }
        Button(role: .destructive) { removeAbsence(cell.date!) } label: {
            Label("Entfernen", systemImage: "trash")
        }
    }
}
```

### Android: DropdownMenu bei Long-Press

```kotlin
var showMenu by remember { mutableStateOf(false) }

Box {
    DayCellContent(cell)

    DropdownMenu(expanded = showMenu, onDismissRequest = { showMenu = false }) {
        if (cell.type == DayType.Workday || cell.type == DayType.Today) {
            DropdownMenuItem(
                text = { Text("Urlaub") },
                leadingIcon = { Icon(Icons.Default.WbSunny, null) },
                onClick = { onDayTap(cell.date!!); showMenu = false }
            )
            DropdownMenuItem(
                text = { Text("Krank") },
                leadingIcon = { Icon(Icons.Default.LocalHospital, null) },
                onClick = { onDayLongPress(cell.date!!); showMenu = false }
            )
        }
        // ... Typ wechseln / Entfernen fuer markierte Tage
    }
}

Modifier.combinedClickable(
    onClick = { if (cell.isTappable) onDayTap(cell.date!!) },
    onLongClick = {
        if (cell.date != null) {
            performHapticFeedback(HapticFeedbackType.LongPress)
            showMenu = true
        }
    }
)
```

---

## switchAbsenceType-Logik

```swift
// Swift
func switchAbsenceType(date: Date) {
    let cal = Calendar.current
    let startOfDay = cal.startOfDay(for: date)

    // Ist es Urlaub?
    let vacDescriptor = FetchDescriptor<VacationDay>(
        predicate: #Predicate { $0.date >= startOfDay && $0.date < cal.date(byAdding: .day, value: 1, to: startOfDay)! }
    )
    if let vacDay = try? modelContext.fetch(vacDescriptor).first {
        modelContext.delete(vacDay)
        modelContext.insert(SickDay(date: startOfDay))
        try? modelContext.save()
        return
    }

    // Ist es Krank?
    let sickDescriptor = FetchDescriptor<SickDay>(
        predicate: #Predicate { $0.date >= startOfDay && $0.date < cal.date(byAdding: .day, value: 1, to: startOfDay)! }
    )
    if let sickDay = try? modelContext.fetch(sickDescriptor).first {
        modelContext.delete(sickDay)
        modelContext.insert(VacationDay(date: startOfDay))
        try? modelContext.save()
        return
    }
}
```

---

## Testbare Kriterien

1. SickDay erstellen -> lokal gespeichert, isPendingSync = true
2. SickDay Sync: ALLE lokalen Tage werden gesendet (nicht nur pending)
3. SickDay Sync: DeletedIds werden lokal geloescht
4. Long-Press auf leeren Tag -> Kontext-Menue mit "Urlaub" und "Krank"
5. Long-Press auf Urlaub-Tag -> "Typ wechseln" wechselt zu Krank
6. Long-Press auf Krank-Tag -> "Typ wechseln" wechselt zu Urlaub
7. Tap auf Krank-Tag -> entfernt Markierung
8. Feiertage und Wochenenden: Long-Press wird ignoriert
9. Kalender-Legende zeigt Krank (roter Punkt)
10. Resturlaub-Counter aendert sich NICHT bei Krankheitstag-Toggle

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| Backend SickDay-Endpoints nicht fertig | Mittel | SickDays nur lokal speichern, Sync-Call mit try/catch skippen |
| Context Menu UX nicht intuitiv | Mittel | Alternativ: Segmented Control am oberen Rand ("Urlaub / Krank") als Modus-Schalter |
| Typ-Wechsel erzeugt Race Condition bei Sync | Niedrig | Loeschen + Erstellen in einer DB-Transaktion; Sync erst danach triggern |
