# Tech-Spec: EPIC 02 -- Einstellungen-Tab (Settings UI + Sync)

## Dateien

### Neue Dateien

| Datei | Plattform | Beschreibung |
|-------|-----------|-------------|
| `Features/Settings/SettingsView.swift` | iOS | InsetGroupedList mit Sektionen |
| `Features/Settings/SettingsViewModel.swift` | iOS | @Observable, Auto-Save, Debounce |
| `Features/Settings/SchoolHolidaysScreen.swift` | iOS | NavigationStack Sub-Screen |
| `Shared/WorkdaySelector.swift` | iOS | 7 Capsule-Buttons, Bitmask |
| `Shared/BundeslandPicker.swift` | iOS | Picker + Feiertag-Count |
| `Shared/HolidayCalculator.swift` | iOS | Gauss-Ostern + alle Bundeslaender |
| `features/settings/SettingsScreen.kt` | Android | LazyColumn mit Sektionen |
| `features/settings/SettingsViewModel.kt` | Android | StateFlow, Debounce |
| `features/settings/SettingsViewModelFactory.kt` | Android | Factory |
| `features/settings/SchoolHolidaysScreen.kt` | Android | Sub-Screen mit Dialog |
| `ui/shared/WorkdaySelector.kt` | Android | FilterChip Row |
| `ui/shared/BundeslandPicker.kt` | Android | ExposedDropdownMenuBox |
| `util/HolidayCalculator.kt` | Android | Gauss-Ostern + alle Bundeslaender |

### Modifizierte Dateien

| Datei | Plattform | Aenderung |
|-------|-----------|-----------|
| `ContentView.swift` | iOS | Placeholder Tab 3 -> SettingsView() |
| `AppNavigation.kt` | Android | Placeholder "einstellungen" -> SettingsScreen() |
| `UserSettings.swift` | iOS | +updatedAt Property |
| `Entities.kt` | Android | +updatedAt in UserSettingsEntity |
| `SyncEngine.swift` | iOS | syncUserSettings() auf Last-Write-Wins |
| `SyncEngine.kt` | Android | syncUserSettings() auf Last-Write-Wins |
| `DTOs.swift` | iOS | +UpdatedAt in UserSettingsDTO |
| `DTOs.kt` | Android | +UpdatedAt in UserSettingsDTO |

---

## HolidayCalculator -- Zentrale Klasse (iOS + Android)

Die Feiertag-Berechnung wird an 5 Stellen gebraucht: BundeslandPicker, VacationCalendar, OverviewScreen, CSVExporter, PDFReportGenerator. Deshalb eine zentrale Utility.

### Swift: `HolidayCalculator.swift`

```swift
import Foundation

struct Holiday {
    let date: Date
    let name: String
}

enum HolidayCalculator {
    /// Alle Feiertage fuer ein Bundesland und Jahr
    static func holidays(bundesland: String, year: Int) -> [Holiday] {
        var result = nationalHolidays(year: year)
        result.append(contentsOf: stateHolidays(bundesland: bundesland, year: year))
        return result.sorted { $0.date < $1.date }
    }

    /// Anzahl Feiertage (fuer BundeslandPicker)
    static func holidayCount(bundesland: String, year: Int) -> Int {
        holidays(bundesland: bundesland, year: year).count
    }

    /// Pruefen ob ein Datum ein Feiertag ist (fuer Kalender)
    static func isHoliday(_ date: Date, bundesland: String, year: Int) -> Bool {
        let cal = Calendar.current
        return holidays(bundesland: bundesland, year: year).contains {
            cal.isDate($0.date, inSameDayAs: date)
        }
    }

    /// Feiertag-Name fuer ein Datum (fuer Long-Press Info)
    static func holidayName(for date: Date, bundesland: String, year: Int) -> String? {
        let cal = Calendar.current
        return holidays(bundesland: bundesland, year: year).first {
            cal.isDate($0.date, inSameDayAs: date)
        }?.name
    }

    // MARK: - Oster-Berechnung (Gauss)

    static func easterSunday(year: Int) -> Date {
        let a = year % 19
        let b = year / 100
        let c = year % 100
        let d = b / 4
        let e = b % 4
        let f = (b + 8) / 25
        let g = (b - f + 1) / 3
        let h = (19 * a + b - d - g + 15) % 30
        let i = c / 4
        let k = c % 4
        let l = (32 + 2 * e + 2 * i - h - k) % 7
        let m = (a + 11 * h + 22 * l) / 451
        let month = (h + l - 7 * m + 114) / 31
        let day = ((h + l - 7 * m + 114) % 31) + 1

        return makeDate(year: year, month: month, day: day)
    }

    // MARK: - Bundesweite Feiertage (9 Stueck)

    private static func nationalHolidays(year: Int) -> [Holiday] {
        let easter = easterSunday(year: year)
        return [
            Holiday(date: makeDate(year: year, month: 1, day: 1), name: "Neujahr"),
            Holiday(date: addDays(easter, -2), name: "Karfreitag"),
            Holiday(date: addDays(easter, 1), name: "Ostermontag"),
            Holiday(date: makeDate(year: year, month: 5, day: 1), name: "Tag der Arbeit"),
            Holiday(date: addDays(easter, 39), name: "Christi Himmelfahrt"),
            Holiday(date: addDays(easter, 50), name: "Pfingstmontag"),
            Holiday(date: makeDate(year: year, month: 10, day: 3), name: "Tag der Deutschen Einheit"),
            Holiday(date: makeDate(year: year, month: 12, day: 25), name: "1. Weihnachtstag"),
            Holiday(date: makeDate(year: year, month: 12, day: 26), name: "2. Weihnachtstag"),
        ]
    }

    // MARK: - Landesspezifische Feiertage

    private static func stateHolidays(bundesland: String, year: Int) -> [Holiday] {
        let easter = easterSunday(year: year)
        var result: [Holiday] = []

        // Heilige Drei Koenige (06.01.): BW, BY, ST
        if ["BW", "BY", "ST"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 1, day: 6),
                                  name: "Heilige Drei Koenige"))
        }

        // Internationaler Frauentag (08.03.): BE
        if bundesland == "BE" {
            result.append(Holiday(date: makeDate(year: year, month: 3, day: 8),
                                  name: "Internationaler Frauentag"))
        }

        // Fronleichnam (Ostern + 60): BW, BY, HE, NW, RP, SL
        if ["BW", "BY", "HE", "NW", "RP", "SL"].contains(bundesland) {
            result.append(Holiday(date: addDays(easter, 60), name: "Fronleichnam"))
        }

        // Mariae Himmelfahrt (15.08.): BY, SL
        if ["BY", "SL"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 8, day: 15),
                                  name: "Mariae Himmelfahrt"))
        }

        // Weltkindertag (20.09.): TH
        if bundesland == "TH" {
            result.append(Holiday(date: makeDate(year: year, month: 9, day: 20),
                                  name: "Weltkindertag"))
        }

        // Reformationstag (31.10.): BB, HB, HH, MV, NI, SH, SN, ST, TH
        if ["BB", "HB", "HH", "MV", "NI", "SH", "SN", "ST", "TH"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 10, day: 31),
                                  name: "Reformationstag"))
        }

        // Allerheiligen (01.11.): BW, BY, NW, RP, SL
        if ["BW", "BY", "NW", "RP", "SL"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 11, day: 1),
                                  name: "Allerheiligen"))
        }

        // Buss- und Bettag (Mittwoch vor 23. Nov): SN
        if bundesland == "SN" {
            result.append(Holiday(date: bussUndBettag(year: year),
                                  name: "Buss- und Bettag"))
        }

        return result
    }

    // MARK: - Buss- und Bettag

    private static func bussUndBettag(year: Int) -> Date {
        // Mittwoch vor dem 23. November
        let nov23 = makeDate(year: year, month: 11, day: 23)
        let cal = Calendar.current
        let weekday = cal.component(.weekday, from: nov23) // 1=So, 2=Mo, ..., 7=Sa
        // Mittwoch = weekday 4
        let daysBack = (weekday - 4 + 7) % 7
        let offset = daysBack == 0 ? 7 : daysBack
        return addDays(nov23, -offset)
    }

    // MARK: - Helpers

    private static func makeDate(year: Int, month: Int, day: Int) -> Date {
        var components = DateComponents()
        components.year = year
        components.month = month
        components.day = day
        return Calendar.current.date(from: components)!
    }

    private static func addDays(_ date: Date, _ days: Int) -> Date {
        Calendar.current.date(byAdding: .day, value: days, to: date)!
    }
}
```

### Kotlin: `HolidayCalculator.kt`

```kotlin
package com.fakturus.track.util

import java.time.DayOfWeek
import java.time.LocalDate
import java.time.temporal.TemporalAdjusters

data class Holiday(val date: LocalDate, val name: String)

object HolidayCalculator {

    fun holidays(bundesland: String, year: Int): List<Holiday> {
        return (nationalHolidays(year) + stateHolidays(bundesland, year))
            .sortedBy { it.date }
    }

    fun holidayCount(bundesland: String, year: Int): Int =
        holidays(bundesland, year).size

    fun isHoliday(date: LocalDate, bundesland: String): Boolean =
        holidays(bundesland, date.year).any { it.date == date }

    fun holidayName(date: LocalDate, bundesland: String): String? =
        holidays(bundesland, date.year).firstOrNull { it.date == date }?.name

    // Gauss-Osterformel
    fun easterSunday(year: Int): LocalDate {
        val a = year % 19
        val b = year / 100
        val c = year % 100
        val d = b / 4
        val e = b % 4
        val f = (b + 8) / 25
        val g = (b - f + 1) / 3
        val h = (19 * a + b - d - g + 15) % 30
        val i = c / 4
        val k = c % 4
        val l = (32 + 2 * e + 2 * i - h - k) % 7
        val m = (a + 11 * h + 22 * l) / 451
        val month = (h + l - 7 * m + 114) / 31
        val day = ((h + l - 7 * m + 114) % 31) + 1
        return LocalDate.of(year, month, day)
    }

    private fun nationalHolidays(year: Int): List<Holiday> {
        val easter = easterSunday(year)
        return listOf(
            Holiday(LocalDate.of(year, 1, 1), "Neujahr"),
            Holiday(easter.minusDays(2), "Karfreitag"),
            Holiday(easter.plusDays(1), "Ostermontag"),
            Holiday(LocalDate.of(year, 5, 1), "Tag der Arbeit"),
            Holiday(easter.plusDays(39), "Christi Himmelfahrt"),
            Holiday(easter.plusDays(50), "Pfingstmontag"),
            Holiday(LocalDate.of(year, 10, 3), "Tag der Deutschen Einheit"),
            Holiday(LocalDate.of(year, 12, 25), "1. Weihnachtstag"),
            Holiday(LocalDate.of(year, 12, 26), "2. Weihnachtstag"),
        )
    }

    private fun stateHolidays(bundesland: String, year: Int): List<Holiday> {
        val easter = easterSunday(year)
        val result = mutableListOf<Holiday>()

        if (bundesland in listOf("BW", "BY", "ST"))
            result += Holiday(LocalDate.of(year, 1, 6), "Heilige Drei Koenige")

        if (bundesland == "BE")
            result += Holiday(LocalDate.of(year, 3, 8), "Internationaler Frauentag")

        if (bundesland in listOf("BW", "BY", "HE", "NW", "RP", "SL"))
            result += Holiday(easter.plusDays(60), "Fronleichnam")

        if (bundesland in listOf("BY", "SL"))
            result += Holiday(LocalDate.of(year, 8, 15), "Mariae Himmelfahrt")

        if (bundesland == "TH")
            result += Holiday(LocalDate.of(year, 9, 20), "Weltkindertag")

        if (bundesland in listOf("BB", "HB", "HH", "MV", "NI", "SH", "SN", "ST", "TH"))
            result += Holiday(LocalDate.of(year, 10, 31), "Reformationstag")

        if (bundesland in listOf("BW", "BY", "NW", "RP", "SL"))
            result += Holiday(LocalDate.of(year, 11, 1), "Allerheiligen")

        if (bundesland == "SN")
            result += Holiday(bussUndBettag(year), "Buss- und Bettag")

        return result
    }

    private fun bussUndBettag(year: Int): LocalDate {
        // Mittwoch vor dem 23. November
        val nov23 = LocalDate.of(year, 11, 23)
        return nov23.with(TemporalAdjusters.previous(DayOfWeek.WEDNESDAY))
    }
}
```

### Stichproben-Test (zur Verifizierung)

| Bundesland | Jahr | Erwartete Anzahl | Begruendung |
|-----------|------|-----------------|-------------|
| NW | 2026 | 11 | 9 bundesweit + Fronleichnam + Allerheiligen |
| BY | 2026 | 13 | 9 + Dreikoenige + Fronleichnam + Mariae Himmelfahrt + Allerheiligen |
| HH | 2026 | 10 | 9 + Reformationstag |
| SN | 2026 | 11 | 9 + Reformationstag + Buss-und-Bettag |
| BE | 2026 | 10 | 9 + Internationaler Frauentag |
| TH | 2026 | 11 | 9 + Weltkindertag + Reformationstag |

**Ostern 2026**: 5. April (Ostersonntag). Verifizierung: Karfreitag = 3. April, Ostermontag = 6. April.

---

## Settings-Sync: Last-Write-Wins

### Datenfluss

```
Nutzer aendert Wochenstunden: 40 -> 32
    |
    v
SettingsViewModel: Debounce 500ms
    |
    v
Lokal speichern: updatedAt = now(), isPendingSync = true
    |
    v
SyncEngine.syncUserSettings() triggern
    |
    v
GET /v1/settings -> Server-UpdatedAt lesen
    |
    +-- Lokal neuer? -> PUT /v1/settings (lokale Werte hochladen)
    +-- Server neuer? -> Lokale Settings ueberschreiben
    +-- Gleich? -> Nichts tun
    |
    v
isPendingSync = false, isSynced = true
```

### SyncEngine-Aenderung (Kotlin)

```kotlin
private suspend fun syncUserSettings() {
    val settingsDao = database.userSettingsDao()
    val local = settingsDao.getSettingsOnce() ?: return

    val serverSettings = apiClient.getUserSettings()

    val localUpdatedAt = Instant.parse(local.updatedAt)
    val serverUpdatedAt = serverSettings.updatedAt?.let { Instant.parse(it) }

    if (serverUpdatedAt == null || localUpdatedAt.isAfter(serverUpdatedAt)) {
        // Lokal ist neuer -> hochladen
        apiClient.updateUserSettings(local.toDTO())
    } else if (serverUpdatedAt.isAfter(localUpdatedAt)) {
        // Server ist neuer -> lokal ueberschreiben
        settingsDao.upsert(local.copy(
            calendarUrl = serverSettings.calendarUrl,
            vacationDaysPerYear = serverSettings.vacationDaysPerYear,
            workHoursPerWeek = serverSettings.workHoursPerWeek,
            workDays = serverSettings.workDays,
            bundesland = serverSettings.bundesland,
            updatedAt = serverSettings.updatedAt ?: local.updatedAt,
            isSynced = true,
            isPendingSync = false
        ))
    }
    // Gleich -> nichts tun
}
```

---

## Bundesland-Enum (Shared Definition)

Beide Plattformen nutzen die gleiche Liste:

```
BW = Baden-Wuerttemberg
BY = Bayern
BE = Berlin
BB = Brandenburg
HB = Bremen
HH = Hamburg
HE = Hessen
MV = Mecklenburg-Vorpommern
NI = Niedersachsen
NW = Nordrhein-Westfalen
RP = Rheinland-Pfalz
SL = Saarland
SN = Sachsen
ST = Sachsen-Anhalt
SH = Schleswig-Holstein
TH = Thueringen
```

---

## Testbare Kriterien

1. HolidayCalculator: NW 2026 = 11 Feiertage
2. HolidayCalculator: BY 2026 = 13 Feiertage
3. HolidayCalculator: Ostern 2026 = 5. April
4. HolidayCalculator: Buss-und-Bettag 2026 = 18. November (Mittwoch vor 23. Nov)
5. WorkdaySelector: workDays=31 (Mo-Fr), Tap auf Fr -> workDays=15 (Mo-Do)
6. Settings Auto-Save: Aenderung -> 500ms Debounce -> lokal gespeichert
7. Settings Sync: lokale Aenderung -> PUT an Server
8. Settings Sync: Server neuer -> lokale Werte ueberschrieben
9. Schulferien: Erstellen, Bearbeiten, Loeschen -> Sync

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| UpdatedAt-Feld fehlt in Backend-Response | Mittel | Null-Check; bei null = Server-wins (Phase 1 Verhalten) |
| Schulferien-API nicht vorhanden | Niedrig | Nur lokal speichern, Sync spaeter nachruestbar |
| Bitmask-Inkonsistenz iOS/Android | Niedrig | Gleiche Definition in beiden HolidayCalculators testen |
