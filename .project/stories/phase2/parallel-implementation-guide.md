# Guide fuer parallele Implementierung -- Phase 2

## Uebersicht

Phase 2 wird von mehreren AI-Agenten parallel umgesetzt: iOS-Agent, Android-Agent, und ggf. Backend-Agent. Dieser Guide definiert die Contracts, Mock-Strategien und Merge-Reihenfolge.

**Grundregel aus Phase 1**: iOS und Android aendern NIEMALS die gleichen Dateien. Sie koennen immer parallel und in beliebiger Reihenfolge gemergt werden.

---

## 1. Contracts die ZUERST feststehen muessen

### 1.1 HolidayCalculator-Contract (E02-S11)

Die Feiertag-Berechnung wird von 5 Features genutzt. Das Interface muss ZUERST definiert werden:

```
HolidayCalculator:
  holidays(bundesland: String, year: Int) -> [(Date/LocalDate, String)]
  holidayCount(bundesland: String, year: Int) -> Int
  isHoliday(date: Date/LocalDate, bundesland: String, year: Int) -> Bool
  holidayName(date: Date/LocalDate, bundesland: String, year: Int) -> String?
  easterSunday(year: Int) -> Date/LocalDate
```

**Verifizierung**: NW 2026 = 11, BY 2026 = 13, Ostern 2026 = 5. April.

### 1.2 SickDay API-Contract (E01)

Der Backend-Agent definiert die SickDay-Endpoints bevor das Frontend startet:

```
POST /v1/sick-days/sync
  Request:  { "SickDays": [{ "Id", "Date", "CreatedAt", "UpdatedAt", "SyncedAt" }] }
  Response: { "ServerSickDays": [...], "DeletedIds": [...] }

GET  /v1/sick-days?from={date}&to={date}  -> SickDayDTO[]
POST /v1/sick-days                        -> { "Date": "..." } -> SickDayDTO
DELETE /v1/sick-days/{id}                 -> 204
```

### 1.3 SickDay DTO-Contract (E04)

Beide Plattformen muessen die gleichen DTOs implementieren:

```
SickDaySyncItem (Request):
  Id: String, Date: String, CreatedAt: String?, UpdatedAt: String?, SyncedAt: String?

SickDayDTO (Response):
  Id, UserId?, Date, CreatedAt?, UpdatedAt?, SyncedAt?

SyncSickDaysRequest:  { SickDays: [SickDaySyncItem] }
SyncSickDaysResponse: { ServerSickDays: [SickDayDTO], DeletedIds: [String] }
```

### 1.4 OvertimeSummaryDTO-Erweiterung (E01-S04 + E05)

```
OvertimeSummaryDTO (neue optionale Felder):
  SickDaysTaken: Int?/Int = 0

MonthlyOvertimeDTO (neues optionales Feld):
  SickDays: Int?/Int = 0
```

### 1.5 UserSettingsDTO-Erweiterung (E02)

```
UserSettingsDTO (neues Feld fuer Last-Write-Wins):
  UpdatedAt: String?  (ISO 8601 Timestamp)
```

### 1.6 VacationCalendar-Interface (E03)

```
VacationCalendar:
  Input:
    year, month: Int
    vacationDates: Set<Date/LocalDate>
    sickDayDates: Set<Date/LocalDate>     // leer bis E04
    holidays: [Holiday]
    workDays: Int (Bitmask)
  Callbacks:
    onDayTap(date)                        // Tap = Urlaub toggle
    onDayLongPress(date)                  // Long-Press = Kontext-Menue (ab E04)
    onMonthChange(year, month)
```

---

## 2. Mock-Strategien

### 2.1 Urlaub-Tab ohne fertige Settings

Der Kalender braucht Bundesland und Arbeitstage aus UserSettings. Bis E02 fertig ist:

```swift
// iOS Mock
let defaultSettings = UserSettings(userId: "")
// bundesland = "NW", workDays = 31 (Mo-Fr), vacationDaysPerYear = 30
```

```kotlin
// Android Mock
val defaultSettings = UserSettingsEntity(userId = "")
// bundesland = "NW", workDays = 31 (Mo-Fr), vacationDaysPerYear = 30
```

### 2.2 Kalender ohne SickDay-Daten (E03 vor E04)

VacationCalendar wird in E03 ohne Krankheitstage gebaut. In E04 wird es erweitert:

```swift
// E03: sickDayDates = Set<DateComponents>() -- leer
// E04: sickDayDates aus SickDay @Query befuellen
```

Deshalb ist `sickDayDates` von Anfang an als Parameter definiert, aber mit Default = leer.

### 2.3 Gesamt-Tab ohne Backend SickDaysTaken

Das Backend liefert SickDaysTaken moeglicherweise noch nicht wenn E05 beginnt:

```swift
// Swift: Optional mit nil-Default
let sickDaysTaken: Int? // Optional im DTO
let displaySickDays = summary.sickDaysTaken ?? 0
```

```kotlin
// Kotlin: Default 0
val sickDaysTaken: Int = 0  // Default in @Serializable
```

### 2.4 Export ohne vollstaendige Daten

PDF/CSV-Export kann mit lokalen Daten gebaut werden, unabhaengig von Backend:

- WorkSessions aus lokaler DB (immer vorhanden)
- VacationDays aus lokaler DB
- SickDays: leere Liste falls noch nicht implementiert
- Holidays: aus HolidayCalculator (rein lokal)

### 2.5 Settings-Sync ohne Backend UpdatedAt

Falls das Backend `UpdatedAt` in UserSettingsDTO noch nicht liefert:

```kotlin
// Fallback: Server-wins (Phase 1 Verhalten)
val serverUpdatedAt = serverSettings.updatedAt?.let { Instant.parse(it) }
if (serverUpdatedAt == null) {
    // Backend liefert noch kein UpdatedAt -> Phase 1 Verhalten (Server-wins)
    settingsDao.upsert(/* server values */)
    return
}
// Ab hier: Last-Write-Wins Logik
```

---

## 3. Integrationspunkte

### 3.1 Placeholder-Screens ersetzen (Welle 5)

Am Ende von Phase 2 werden die Placeholder-Screens durch echte Screens ersetzt:

**iOS ContentView.swift:**
```swift
// Phase 1:
PlaceholderView(icon: "sun.max", title: "Urlaub", message: "Kommt in Phase 2")
// Phase 2:
VacationScreen()
```

**Android AppNavigation.kt:**
```kotlin
// Phase 1:
composable("urlaub") { PlaceholderScreen(title = "Urlaub", icon = Icons.Default.WbSunny) }
// Phase 2:
composable("urlaub") { VacationScreen(services = services) }
```

### 3.2 SyncEngine + SickDays (E04-S03/S04)

Die bestehende SyncEngine wird um `syncSickDays()` erweitert. `syncAll()` ruft es auf:

```kotlin
suspend fun syncAll() {
    // ... bestehender Code ...
    syncPendingDeletes()
    syncWorkSessions()
    syncVacationDays()
    syncSickDays()       // NEU Phase 2
    syncUserSettings()   // MODIFIZIERT: Last-Write-Wins
    // ...
}
```

### 3.3 Settings-Aenderung -> Kalender-Aktualisierung

Wenn der Nutzer das Bundesland in Settings aendert, muss der Kalender neue Feiertage zeigen:

**Loesung**: Kein Event-Bus. Beide Features lesen UserSettings aus der lokalen DB. Wenn Settings geaendert werden, triggert das automatisch ein DB-Update. Beim naechsten Oeffnen des Urlaub-Tabs werden die Settings aus der DB geladen.

- iOS: VacationViewModel laedt Settings aus SwiftData (reaktiv via @Query oder bei onAppear)
- Android: VacationViewModel beobachtet Settings via Room Flow

### 3.4 Export-Sektion im Gesamt-Tab (E06)

Die Export-Sektion wird unterhalb der Monatstabelle im OverviewScreen eingefuegt. Da OverviewScreen in E05 erstellt wird, muss E06 diese Datei modifizieren.

**Konfliktvermeidung**: In E05 wird ein Kommentar-Platzhalter eingefuegt:

```swift
// iOS
// MARK: - Export (E06)
// TODO: Export-Sektion hier einfuegen
```

```kotlin
// Android
// Export section (E06) - placeholder
```

---

## 4. Merge-Reihenfolge

### Phase A: Foundation (Welle 1 -- keine Konflikte)

```
1. E02-S11  HolidayCalculator (iOS + Android)   -- neue Dateien, keine Konflikte
2. E02-S01  iOS Settings-Screen                  -- neue Datei
3. E02-S02  Android Settings-Screen              -- neue Datei
4. E02-S03  iOS WorkdaySelector                  -- neue Datei
5. E02-S04  Android WorkdaySelector              -- neue Datei
6. E02-S05  iOS BundeslandPicker                 -- neue Datei
7. E02-S06  Android BundeslandPicker             -- neue Datei
8. E05-S01  iOS OvertimeCard                     -- neue Datei
9. E05-S02  Android OvertimeCard                 -- neue Datei
10. E05-S03 iOS Monatstabelle                    -- neue Datei
11. E05-S04 Android Monatstabelle                -- neue Datei
```

### Phase B: Logik + Zusammenbau (Welle 2 -- leichte Konflikte)

```
12. E02-S07 iOS SettingsViewModel                -- neue Datei + MODIFIZIERT SyncEngine
13. E02-S08 Android SettingsViewModel            -- neue Datei + MODIFIZIERT SyncEngine
14. E02-S09 iOS Schulferien                      -- neue Datei
15. E02-S10 Android Schulferien                  -- neue Datei
16. E03-S01 iOS VacationCalendar                 -- neue Datei
17. E03-S02 Android VacationCalendar             -- neue Datei
18. E03-S03 iOS Urlaub-Toggle                    -- MODIFIZIERT VacationCalendar
19. E03-S04 Android Urlaub-Toggle                -- MODIFIZIERT VacationCalendar
20. E03-S05 iOS Resturlaub                       -- neue Datei/Komponente
21. E03-S06 Android Resturlaub                   -- neue Datei/Komponente
22. E05-S05 iOS OverviewViewModel                -- neue Datei
23. E05-S06 Android OverviewViewModel            -- neue Datei
24. E05-S07 iOS OverviewScreen                   -- neue Datei
25. E05-S08 Android OverviewScreen               -- neue Datei
```

### Phase C: Krankheitstage (Welle 3 -- modifiziert bestehende Dateien)

```
26. E04-S01 iOS SickDay Model + DTOs            -- neue Datei + MODIFIZIERT DTOs.swift
27. E04-S02 Android SickDay Entity + DTOs        -- MODIFIZIERT Entities.kt, DTOs.kt, AppDatabase.kt
28. E04-S03 iOS SyncEngine SickDay               -- MODIFIZIERT SyncEngine.swift
29. E04-S04 Android SyncEngine SickDay           -- MODIFIZIERT SyncEngine.kt
30. E04-S05 iOS Kalender Long-Press              -- MODIFIZIERT VacationCalendar.swift
31. E04-S06 Android Kalender Long-Press          -- MODIFIZIERT VacationCalendar.kt
32. E04-S07 VacationViewModel erweitern          -- MODIFIZIERT VacationViewModel (beide)
33. E03-S07 iOS VacationScreen Zusammenbau       -- neue Datei
34. E03-S08 Android VacationScreen Zusammenbau   -- neue Datei
```

### Phase D: Export (Welle 4 -- wenig Konflikte)

```
35. E06-S01 iOS PDF                              -- neue Datei
36. E06-S02 Android PDF                          -- neue Datei
37. E06-S03 iOS CSV                              -- neue Datei
38. E06-S04 Android CSV                          -- neue Datei
39. E06-S05 iOS Export-UI                        -- MODIFIZIERT OverviewScreen.swift
40. E06-S06 Android Export-UI                    -- MODIFIZIERT OverviewScreen.kt
```

### Phase E: Integration (Welle 5)

```
41. Tab-Integration                              -- MODIFIZIERT ContentView.swift, AppNavigation.kt
42. Schema-Migration                             -- MODIFIZIERT PersistenceManager, ServiceContainer
```

---

## 5. Kritische Dateien (Merge-Konflikte erwartet)

| Datei | Aendernde Stories | Reihenfolge |
|-------|-------------------|-------------|
| `SyncEngine.swift` | E02-S07 (Settings LWW), E04-S03 (SickDay) | E02 -> E04 |
| `SyncEngine.kt` | E02-S08 (Settings LWW), E04-S04 (SickDay) | E02 -> E04 |
| `DTOs.swift` | E04-S01 (SickDay), E05 (OvertimeSummary) | E04 -> E05 |
| `DTOs.kt` | E04-S02 (SickDay), E05 (OvertimeSummary) | E04 -> E05 |
| `Entities.kt` | E04-S02 (SickDay) | Einmalig |
| `AppDatabase.kt` | E04-S02 (SickDay DAO) | Einmalig |
| `VacationCalendar.swift` | E03-S01 (erstellt), E04-S05 (Long-Press) | E03 -> E04 |
| `VacationCalendar.kt` | E03-S02 (erstellt), E04-S06 (Long-Press) | E03 -> E04 |
| `OverviewScreen.swift` | E05-S07 (erstellt), E06-S05 (Export-UI) | E05 -> E06 |
| `OverviewScreen.kt` | E05-S08 (erstellt), E06-S06 (Export-UI) | E05 -> E06 |
| `ContentView.swift` | Welle 5 (Tab-Integration) | Einmalig am Ende |
| `AppNavigation.kt` | Welle 5 (Tab-Integration) | Einmalig am Ende |

---

## 6. Konfliktvermeidung

### Regel 1: Placeholder-Parameter von Anfang an

Wenn eine Komponente spaeter erweitert wird, alle Parameter von Anfang an definieren:

```swift
// VacationCalendar in E03 -- sickDayDates schon als Parameter, Default = leer
struct VacationCalendar: View {
    let sickDayDates: Set<DateComponents> = []  // E04 befuellt dies
    let onDayLongPress: (Date) -> Void = { _ in }  // E04 implementiert dies
}
```

### Regel 2: Export-Platzhalter in OverviewScreen

```swift
// E05-S07: OverviewScreen.swift
ScrollView {
    // ... Summary Cards, Tabelle ...

    // MARK: - Export (wird in E06 befuellt)
    // ExportSection() -- E06-S05 fuegt hier ein
}
```

### Regel 3: SyncEngine sequentiell erweitern

`syncAll()` wird zweimal modifiziert (E02 + E04). Reihenfolge einhalten:
1. E02: `syncUserSettings()` auf Last-Write-Wins aendern
2. E04: `syncSickDays()` hinzufuegen

### Regel 4: iOS und Android parallel = kein Konflikt

Wie in Phase 1: iOS und Android aendern NIEMALS die gleichen Dateien. Innerhalb einer Plattform gibt es die Reihenfolgen oben.
