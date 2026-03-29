# Technischer Gesamtplan Phase 2

## Ziel-Dateistruktur am Ende von Phase 2

### iOS -- Neue und modifizierte Dateien

```
FakturusTrack/
  Features/
    Settings/                            NEU (E02)
      SettingsView.swift                 Settings-Tab Screen
      SettingsViewModel.swift            Auto-Save, Debounce, Sync, Logout
      SchoolHolidaysScreen.swift         Sub-Screen: Schulferien CRUD
    Vacation/                            NEU (E03/E04)
      VacationScreen.swift               Urlaub-Tab Screen
      VacationViewModel.swift            Calendar State, Toggle, SickDay-Logik
    Overview/                            NEU (E05)
      OverviewScreen.swift               Gesamt-Tab Screen
      OverviewViewModel.swift            API-Call, Disk-Cache, Jahresnavigation
      MonthlyOvertimeTable.swift         Monatstabelle (4 Spalten)

  Services/
    Export/                              NEU (E06)
      PDFReportGenerator.swift           HTML -> WKWebView PDF
      CSVExporter.swift                  Semikolon-CSV mit UTF-8 BOM
    Sync/
      SyncEngine.swift                   MODIFIZIERT: +syncSickDays(), +Last-Write-Wins Settings

  Shared/
    VacationCalendar.swift               NEU (E03): Custom Monatskalender-Grid
    WorkdaySelector.swift                NEU (E02): 7 Tage-Toggles (Bitmask)
    BundeslandPicker.swift               NEU (E02): Picker + Feiertag-Vorschau
    OvertimeCard.swift                   NEU (E05): Wiederverwendbare Info-Karte
    HolidayCalculator.swift              NEU (E02): Feiertag-Berechnung (Gauss-Ostern)

  Models/
    SickDay.swift                        NEU (E04): @Model, analog VacationDay
    DTOs.swift                           MODIFIZIERT: +SickDay DTOs, +UpdatedAt in SettingsDTO
    UserSettings.swift                   MODIFIZIERT: +updatedAt Property
    PersistenceManager.swift             MODIFIZIERT: Schema V1 -> V2 (SickDay)

  Features/Shell/
    ContentView.swift                    MODIFIZIERT: Placeholder -> echte Screens

  Services/API/
    APIClient+Endpoints.swift            MODIFIZIERT: +SickDay Endpoints, +getOvertimeSummary
```

**Neue Dateien iOS: ~16**
**Modifizierte Dateien iOS: ~6**

---

### Android -- Neue und modifizierte Dateien

```
app/src/main/java/com/fakturus/track/
  features/
    settings/                            NEU (E02)
      SettingsScreen.kt                  Settings-Tab Screen
      SettingsViewModel.kt               Auto-Save, Debounce, Sync, Logout
      SettingsViewModelFactory.kt        Factory
      SchoolHolidaysScreen.kt            Sub-Screen: Schulferien CRUD
    vacation/                            NEU (E03/E04)
      VacationScreen.kt                  Urlaub-Tab Screen
      VacationViewModel.kt              Calendar State, Toggle, SickDay-Logik
      VacationViewModelFactory.kt        Factory
    overview/                            NEU (E05)
      OverviewScreen.kt                  Gesamt-Tab Screen
      OverviewViewModel.kt              API-Call, Disk-Cache, Jahresnavigation
      OverviewViewModelFactory.kt        Factory
      MonthlyOvertimeTable.kt            Monatstabelle

  services/
    export/                              NEU (E06)
      PDFReportGenerator.kt              HTML -> WebView PrintAdapter
      CSVExporter.kt                     Semikolon-CSV mit UTF-8 BOM
    sync/
      SyncEngine.kt                      MODIFIZIERT: +syncSickDays(), +Last-Write-Wins Settings

  ui/shared/
    VacationCalendar.kt                  NEU (E03): Custom Monatskalender-Grid
    WorkdaySelector.kt                   NEU (E02): FilterChip Row (Bitmask)
    BundeslandPicker.kt                  NEU (E02): ExposedDropdown + Feiertag-Info
    OvertimeCard.kt                      NEU (E05): Wiederverwendbare Info-Karte

  util/
    HolidayCalculator.kt                NEU (E02): Feiertag-Berechnung (Gauss-Ostern)

  models/
    Entities.kt                          MODIFIZIERT: +SickDayEntity, +updatedAt in UserSettings
    AppDatabase.kt                       MODIFIZIERT: +SickDayDao, Schema V2->V3, +SchoolHolidayDao
    DTOs.kt                              MODIFIZIERT: +SickDay DTOs, +UpdatedAt in SettingsDTO

  features/shell/
    AppNavigation.kt                     MODIFIZIERT: Placeholder -> echte Screens

  ServiceContainer.kt                    MODIFIZIERT: +MIGRATION_2_3
```

**Neue Dateien Android: ~18**
**Modifizierte Dateien Android: ~6**

---

## Backend-Aenderungen (EPIC 01)

### Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Entities/SickDay.cs` | Entity mit Id, UserId, Date, CreatedAt, UpdatedAt, SyncedAt |
| `Endpoints/SickDays/GetSickDaysEndpoint.cs` | GET /v1/sick-days?from=&to= |
| `Endpoints/SickDays/CreateSickDayEndpoint.cs` | POST /v1/sick-days |
| `Endpoints/SickDays/DeleteSickDayEndpoint.cs` | DELETE /v1/sick-days/{id} |
| `Endpoints/SickDays/SyncSickDaysEndpoint.cs` | POST /v1/sick-days/sync |
| `Migrations/AddSickDayEntity.cs` | EF Core Migration |

### Modifizierte Dateien

| Datei | Aenderung |
|-------|-----------|
| `AppDbContext.cs` | +DbSet\<SickDay\> SickDays |
| `Endpoints/OvertimeSummaryEndpoint.cs` | +SickDaysTaken Feld, Soll-Stunden-Berechnung anpassen |
| `Models/OvertimeSummaryResponse.cs` | +SickDaysTaken, +MonthlyOvertime[].SickDays |

### API-Endpoints (neu)

| Methode | Pfad | Request | Response |
|---------|------|---------|----------|
| GET | `/v1/sick-days?from=&to=` | Query-Params | `SickDayDTO[]` |
| POST | `/v1/sick-days` | `{ "Date": "2026-03-15" }` | 201 + `SickDayDTO` |
| DELETE | `/v1/sick-days/{id}` | -- | 204 |
| POST | `/v1/sick-days/sync` | `SyncSickDaysRequest` | `SyncSickDaysResponse` |

### API-Endpoints (modifiziert)

| Methode | Pfad | Aenderung |
|---------|------|-----------|
| GET | `/v1/overtime-summary?year=` | +`SickDaysTaken` im Response |

---

## Settings-Sync: Phase 1 -> Phase 2 Upgrade

Phase 1 nutzt Server-wins. Phase 2 aendert auf **Last-Write-Wins**:

```
Phase 1:  GET /v1/settings -> lokale Settings ueberschreiben (immer)
Phase 2:  GET /v1/settings -> UpdatedAt vergleichen
          -> Lokal neuer?  -> PUT /v1/settings
          -> Server neuer? -> Lokal ueberschreiben
```

**Noetige Aenderung in UserSettings**: +`updatedAt: Date/String` Property hinzufuegen.

**Noetige Aenderung in UserSettingsDTO**: +`UpdatedAt` Feld (Backend muss dies auch liefern).

**Noetige Aenderung in SyncEngine**: `syncUserSettings()` komplett umschreiben.

---

## Schema-Migrationen

### iOS (SwiftData V1 -> V2)

```swift
enum SchemaV2: VersionedSchema {
    static var versionIdentifier = Schema.Version(2, 0, 0)
    static var models: [any PersistentModel.Type] {
        [WorkSession.self, VacationDay.self, SickDay.self,
         UserSettings.self, PendingDelete.self, SchoolHolidayPeriod.self]
    }
}

enum MigrationPlan: SchemaMigrationPlan {
    static var schemas: [any VersionedSchema.Type] { [SchemaV1.self, SchemaV2.self] }
    static var stages: [MigrationStage] {
        [.lightweight(fromVersion: SchemaV1.self, toVersion: SchemaV2.self)]
    }
}
```

Leichtgewichtige Migration genuegt: SickDay und SchoolHolidayPeriod sind neue Tabellen, UserSettings bekommt ein neues optionales Feld.

### Android (Room V2 -> V3)

```kotlin
val MIGRATION_2_3 = object : Migration(2, 3) {
    override fun migrate(db: SupportSQLiteDatabase) {
        // SickDay-Tabelle
        db.execSQL("""
            CREATE TABLE IF NOT EXISTS `sick_days` (
                `id` TEXT NOT NULL,
                `userId` TEXT NOT NULL,
                `date` TEXT NOT NULL,
                `createdAt` TEXT NOT NULL,
                `updatedAt` TEXT NOT NULL,
                `syncedAt` TEXT,
                `isPendingSync` INTEGER NOT NULL DEFAULT 1,
                `isSynced` INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY(`id`)
            )
        """)
        // SchoolHolidayPeriod-Tabelle
        db.execSQL("""
            CREATE TABLE IF NOT EXISTS `school_holiday_periods` (
                `id` TEXT NOT NULL,
                `name` TEXT NOT NULL,
                `startDate` TEXT NOT NULL,
                `endDate` TEXT NOT NULL,
                `year` INTEGER NOT NULL,
                PRIMARY KEY(`id`)
            )
        """)
        // UserSettings: updatedAt hinzufuegen
        db.execSQL("ALTER TABLE user_settings ADD COLUMN updatedAt TEXT NOT NULL DEFAULT ''")
    }
}
```

---

## Abhaengigkeitsdiagramm (Datei-Ebene)

```
HolidayCalculator (E02-S11)
    |
    +---> BundeslandPicker (E02-S05/S06)
    +---> VacationCalendar (E03-S01/S02)
    +---> OverviewScreen (E05-S07/S08)
    +---> CSVExporter (E06-S03/S04)
    +---> PDFReportGenerator (E06-S01/S02)

UserSettings (Phase 1, +updatedAt)
    |
    +---> SettingsViewModel (E02-S07/S08)
    +---> VacationViewModel (E03-S07/S08) -- Bundesland, Arbeitstage, Urlaubstage/Jahr
    +---> HolidayCalculator -- Bundesland + Jahr

SickDay Model/Entity (E04-S01/S02)
    |
    +---> SyncEngine.syncSickDays() (E04-S03/S04)
    +---> VacationViewModel (E04-S07)
    +---> APIClient+Endpoints (E04-S01/S02)

VacationCalendar (E03-S01/S02)
    |
    +---> VacationScreen (E03-S07/S08) -- eingebettet
    +---> E04-S05/S06 (Long-Press-Erweiterung fuer Krankheitstage)

OvertimeSummaryDTO (Phase 1, +SickDaysTaken)
    |
    +---> OverviewViewModel (E05-S05/S06) -- API-Call + Cache
    +---> ExportViewModel (E06-S07) -- Datenquelle fuer Report
```
