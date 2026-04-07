# EPIC 03: Lokale Datenschicht

## Ziel

Lokale Datenbanken (SwiftData auf iOS, Room auf Android) mit allen Phase-1-relevanten Entities. Sessions koennen lokal erstellt, gelesen, aktualisiert und geloescht werden. Die Datenschicht bildet die Grundlage fuer Offline-First.

## Abhaengigkeiten

- **E01**: Projekt-Setup (Dependencies: SwiftData ist nativ, Room via KSP)

---

## Stories

### P1-E03-S01: iOS SwiftData Models & Container

**Als** Entwickler
**moechte ich** SwiftData-Models fuer alle Phase-1-Entities,
**damit** Daten lokal persistiert und reaktiv in der UI angezeigt werden.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S01
**Parallelisierbar mit**: P1-E03-S02 (Android Room), P1-E02-S01 (Auth)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `WorkSession` als `@Model` implementiert:
  - Felder: `id` (UUID, unique), `userId`, `date`, `startTime`, `stopTime?`, `pauseMinutes`, `calendarEventId?`, `createdAt`, `updatedAt`, `syncedAt?`, `isPendingSync`, `isSynced`, `isFinished`
  - Computed: `isRunning` (nicht finished und kein stopTime), `duration` (TimeInterval)
  - `toDTO()` Methode fuer Sync
  - `update(from dto:)` Methode fuer Server-wins Merge
- [ ] `VacationDay` als `@Model` implementiert:
  - Felder: `id` (UUID, unique), `userId`, `date`, `createdAt`, `updatedAt`, `syncedAt?`, `isPendingSync`, `isSynced`
- [ ] `UserSettings` als `@Model` implementiert:
  - Felder: `userId` (unique), `calendarUrl?`, `vacationDaysPerYear`, `workHoursPerWeek`, `workDays` (Bitmask Int), `bundesland`, `isSynced`, `isPendingSync`
  - Default-Werte: 30 Tage, 40h, Mo-Fr (31), "NW"
- [ ] `PersistenceManager.swift`:
  - Erstellt `ModelContainer` mit allen Models
  - Schema Version V1 definiert
  - Container im App-Lifecycle korrekt eingebunden (`.modelContainer()`)
- [ ] Grundlegende CRUD-Operationen funktionieren:
  - Given eine leere Datenbank
  - When eine WorkSession erstellt wird
  - Then ist sie ueber `@Query` abrufbar
  - When sie aktualisiert wird
  - Then sind die Aenderungen sofort sichtbar
  - When sie geloescht wird
  - Then ist sie nicht mehr abrufbar

**Technische Hinweise**:
- Siehe `.project/architecture/data-layer.md` fuer vollstaendige Model-Definitionen
- `@Attribute(.unique)` fuer Primary Keys
- ISO8601-Datumskonvertierung in `toDTO()` und `update(from:)` beachten
- SwiftData `ModelContext` wird via Environment bereitgestellt

---

### P1-E03-S02: Android Room Entities, DAOs & Database

**Als** Entwickler
**moechte ich** Room-Entities und DAOs fuer alle Phase-1-Entities,
**damit** Daten lokal persistiert und als Flow in der UI beobachtet werden.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S02
**Parallelisierbar mit**: P1-E03-S01 (iOS SwiftData), P1-E02-S02 (Auth)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `WorkSessionEntity` als Room `@Entity` implementiert:
  - Felder analog zu iOS (String-basierte Datums/Zeit-Felder im ISO-Format)
  - `PrimaryKey`: `id` (String = UUID)
  - Computed: `isRunning`, `durationMinutes`, `monthKey`
  - `toDTO()` Methode
- [ ] `VacationDayEntity` als Room `@Entity` implementiert
- [ ] `UserSettingsEntity` als Room `@Entity` implementiert (gleiche Defaults wie iOS)
- [ ] `WorkSessionDao` Interface:
  - `getAllOrderedByDate(): Flow<List<WorkSessionEntity>>`
  - `getPendingSessions(): List<WorkSessionEntity>` (suspend, fuer Sync)
  - `getSyncedSessions(): List<WorkSessionEntity>` (suspend, fuer Sync)
  - `insert(session)` mit `OnConflictStrategy.REPLACE`
  - `update(session)`
  - `delete(session)`
  - `deleteSyncedNotIn(keepIds: List<String>)`
- [ ] `VacationDayDao` Interface:
  - `getAllOrderedByDate(): Flow<List<VacationDayEntity>>`
  - `getAll(): List<VacationDayEntity>` (suspend, fuer Sync -- ALLE senden!)
  - `insert(day)` mit REPLACE
  - `delete(day)`
  - `deleteById(id: String)`
- [ ] `UserSettingsDao` Interface:
  - `getSettings(): Flow<UserSettingsEntity?>`
  - `upsert(settings)` mit REPLACE
- [ ] `AppDatabase` extends `RoomDatabase`:
  - Alle Entities registriert
  - `exportSchema = true`
  - Version 1
- [ ] Database-Erstellung im ServiceContainer:
  - `Room.databaseBuilder(context, AppDatabase::class.java, "fakturus_track.db").build()`
- [ ] Grundlegende CRUD-Operationen funktionieren (Unit-Test oder manuell):
  - Given eine leere Datenbank
  - When eine WorkSession eingefuegt wird
  - Then liefert der Flow sie zurueck

**Technische Hinweise**:
- Siehe `.project/architecture/data-layer.md` fuer vollstaendige Entity/DAO-Definitionen
- `@SerialName` Annotationen fuer DTOs (PascalCase Backend)
- String-basierte Timestamps (`Instant.now().toString()`) statt Long/Date
- Room KSP Annotation Processor korrekt konfigurieren in build.gradle.kts

---

### P1-E03-S03: iOS DTOs (API Request/Response Typen)

**Als** Entwickler
**moechte ich** typisierte DTOs fuer alle API-Endpunkte,
**damit** JSON-Serialisierung/Deserialisierung typsicher erfolgt.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S01
**Parallelisierbar mit**: P1-E03-S04 (Android DTOs), P1-E03-S01 (DB)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `DTOs.swift` mit allen Request/Response-Typen:
  - `WorkSessionDTO` (Decodable): id, userId?, date, startTime, stopTime?, pauseMinutes?, createdAt?, updatedAt?, syncedAt?
  - `WorkSessionSyncItem` (Encodable): id, date, startTime, stopTime?, pauseMinutes
  - `SyncWorkSessionsRequest` (Encodable): workSessions Array
  - `VacationDayDTO` (Decodable): id, userId?, date, createdAt?, updatedAt?, syncedAt?
  - `VacationDaySyncItem` (Encodable): id, date, createdAt?, updatedAt?, syncedAt?
  - `SyncVacationDaysRequest` (Encodable)
  - `SyncVacationDaysResponse` (Decodable): serverVacationDays, deletedIds
  - `UserSettingsDTO` (Codable): calendarUrl?, vacationDaysPerYear, workHoursPerWeek, workDays, bundesland
  - `OvertimeSummaryDTO` (Decodable): totalOvertimeHours, monthlyOvertime, vacationDaysTaken, etc.
  - `MonthlyOvertimeDTO` (Decodable): year, month, monthName, overtimeHours, workedHours, expectedHours
- [ ] Alle DTOs nutzen PascalCase-Konvertierung via custom KeyDecodingStrategy (nicht per DTO)

**Technische Hinweise**:
- Siehe `.project/architecture/data-layer.md` Abschnitt "DTOs"
- `calendarEventId` ist NICHT im DTO (lokales Feld, nicht vom Backend gesynct)

---

### P1-E03-S04: Android DTOs (API Request/Response Typen)

**Als** Entwickler
**moechte ich** typisierte DTOs fuer alle API-Endpunkte,
**damit** JSON-Serialisierung/Deserialisierung typsicher erfolgt.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S02
**Parallelisierbar mit**: P1-E03-S03 (iOS DTOs), P1-E03-S02 (DB)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `DTOs.kt` mit `@Serializable` Datenklassen:
  - Alle Typen analog zu iOS DTOs
  - `@SerialName("PascalCase")` Annotationen fuer jedes Feld
  - Default-Werte fuer optionale Felder (`= null`, `= 0`)
- [ ] Konvertierungs-Extensions:
  - `WorkSessionEntity.toDTO()` -> `WorkSessionSyncItem`
  - `WorkSessionDTO.toEntity()` -> `WorkSessionEntity`
  - Analog fuer VacationDay, UserSettings

**Technische Hinweise**:
- kotlinx-serialization Plugin in build.gradle.kts aktivieren
- Siehe `.project/architecture/data-layer.md` Abschnitt "Kotlin DTOs"
