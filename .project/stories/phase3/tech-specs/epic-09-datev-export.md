# Tech-Spec: EPIC 09 -- DATEV-Export

## Uebersicht

Neuer Exporter `DATEVExporter` analog zu `CSVExporter` und `PDFReportGenerator`. Client-seitige Generierung, Share-Sheet-Integration. Folgt dem DATEV Lodas Lohnbuchhaltungs-Import-Format.

---

## S01: DATEV-Format Spezifikation

### Dateiformat

```
Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause
12345;01.03.2026;200;8.00;08:30;17:00;30
12345;02.03.2026;200;7.50;09:00;17:00;30
12345;03.03.2026;500;8.00;;;
12345;10.03.2026;400;8.00;;;
```

| Feld | Format | Beschreibung |
|------|--------|-------------|
| Personalnummer | String (max 10 Zeichen) | Aus Settings, konfigurierbar |
| Datum | TT.MM.JJJJ | Deutsches Datumsformat |
| Lohnart | Integer | 200=Arbeit, 400=Urlaub, 500=Krankheit |
| Stunden | Dezimal (Punkt!) | Netto-Arbeitszeit. Bei Urlaub/Krank: Soll-Stunden/Tag |
| Von | HH:MM | Startzeit (leer bei Urlaub/Krank) |
| Bis | HH:MM | Endzeit (leer bei Urlaub/Krank) |
| Pause | Integer (Minuten) | Pausendauer (leer bei Urlaub/Krank) |

### Lohnarten

| Code | Bedeutung | Datenquelle |
|------|-----------|-------------|
| 200 | Gehalt/Arbeit | WorkSession |
| 400 | Urlaub | VacationDay |
| 500 | Krankheit | SickDay |

### Header-Zeile

```
Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause
```

### Encoding & Trennzeichen

- **Encoding**: UTF-8 ohne BOM (DATEV erwartet ASCII-kompatibel)
- **Spaltentrennzeichen**: Semikolon `;`
- **Dezimaltrennzeichen**: Punkt `.` (NICHT Komma -- Unterschied zum normalen CSV-Export!)
- **Zeilenende**: `\r\n` (Windows-Style, fuer DATEV-Kompatibilitaet)
- **Dateiname**: `DATEV_Lohn_{YYYY}-{MM}.csv`

---

## S02: iOS DATEVExporter.swift (NEU)

```swift
// Services/Export/DATEVExporter.swift
import Foundation

enum DATEVExporter {

    static func generateExport(
        month: Int,
        year: Int,
        sessions: [WorkSession],
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        settings: UserSettings,
        personalNumber: String
    ) -> String {
        let cal = Calendar.current

        // Monatsgrenzen
        let startOfMonth = cal.date(from: DateComponents(year: year, month: month, day: 1))!
        let endOfMonth = cal.date(byAdding: DateComponents(month: 1, day: -1), to: startOfMonth)!

        let dateFormatter = DateFormatter()
        dateFormatter.locale = Locale(identifier: "de_DE")
        dateFormatter.dateFormat = "dd.MM.yyyy"

        let timeFormatter = DateFormatter()
        timeFormatter.dateFormat = "HH:mm"

        // Soll-Stunden pro Tag berechnen (Wochenstunden / Arbeitstage pro Woche)
        let workDaysPerWeek = max(1, countWorkDays(settings.workDays))
        let dailyHours = settings.weeklyHours / Double(workDaysPerWeek)

        var lines: [String] = []
        lines.append("Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause")

        let pn = personalNumber.isEmpty ? "00000" : personalNumber

        // Arbeitstage (WorkSessions)
        let monthSessions = sessions.filter { session in
            cal.isDate(session.date, equalTo: startOfMonth, toGranularity: .month) ||
            (session.date >= startOfMonth && session.date <= endOfMonth)
        }
        for session in monthSessions.sorted(by: { $0.date < $1.date }) {
            let dateStr = dateFormatter.string(from: session.date)
            let netHours = Double(session.netDurationMinutes) / 60.0
            let hoursStr = String(format: "%.2f", netHours)  // Punkt als Dezimal!
            let startStr = timeFormatter.string(from: session.startTime)
            let endStr = session.stopTime.map { timeFormatter.string(from: $0) } ?? ""
            let pauseStr = "\(session.pauseMinutes)"

            lines.append("\(pn);\(dateStr);200;\(hoursStr);\(startStr);\(endStr);\(pauseStr)")
        }

        // Urlaubstage
        let monthVacation = vacationDays.filter { day in
            let dayDate = day.date
            return cal.isDate(dayDate, equalTo: startOfMonth, toGranularity: .month)
        }
        for day in monthVacation.sorted(by: { $0.date < $1.date }) {
            let dateStr = dateFormatter.string(from: day.date)
            let hoursStr = String(format: "%.2f", dailyHours)
            lines.append("\(pn);\(dateStr);400;\(hoursStr);;;")
        }

        // Krankheitstage
        let monthSick = sickDays.filter { day in
            let dayDate = day.date
            return cal.isDate(dayDate, equalTo: startOfMonth, toGranularity: .month)
        }
        for day in monthSick.sorted(by: { $0.date < $1.date }) {
            let dateStr = dateFormatter.string(from: day.date)
            let hoursStr = String(format: "%.2f", dailyHours)
            lines.append("\(pn);\(dateStr);500;\(hoursStr);;;")
        }

        return lines.joined(separator: "\r\n") + "\r\n"
    }

    static func fileName(month: Int, year: Int) -> String {
        "DATEV_Lohn_\(year)-\(String(format: "%02d", month)).csv"
    }

    private static func countWorkDays(_ bitmask: Int) -> Int {
        var count = 0
        for i in 0..<7 {
            if bitmask & (1 << i) != 0 { count += 1 }
        }
        return count
    }
}
```

---

## S03: Android DATEVExporter.kt (NEU)

```kotlin
// services/export/DATEVExporter.kt
package com.fakturus.track.services.export

import com.fakturus.track.models.SickDayEntity
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.models.VacationDayEntity
import com.fakturus.track.models.WorkSessionEntity
import java.time.Instant
import java.time.LocalDate
import java.time.YearMonth
import java.time.ZoneId
import java.time.format.DateTimeFormatter

object DATEVExporter {

    fun generateExport(
        month: Int,
        year: Int,
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        settings: UserSettingsEntity,
        personalNumber: String
    ): String {
        val ym = YearMonth.of(year, month)
        val from = ym.atDay(1).toString()
        val to = ym.atEndOfMonth().toString()

        val dateFormatter = DateTimeFormatter.ofPattern("dd.MM.yyyy")
        val pn = personalNumber.ifEmpty { "00000" }

        // Soll-Stunden pro Tag
        val workDaysPerWeek = countWorkDays(settings.workDays).coerceAtLeast(1)
        val dailyHours = settings.weeklyHours / workDaysPerWeek

        val sb = StringBuilder()
        sb.appendLine("Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause")

        // Arbeitstage
        val monthSessions = sessions
            .filter { it.date >= from && it.date <= to }
            .sortedBy { it.date }

        for (session in monthSessions) {
            val dateStr = LocalDate.parse(session.date).format(dateFormatter)
            val netHours = session.netDurationMinutes / 60.0
            val hoursStr = "%.2f".format(netHours)
            val startStr = formatTime(session.startTime)
            val endStr = session.stopTime?.let { formatTime(it) } ?: ""
            sb.append("$pn;$dateStr;200;$hoursStr;$startStr;$endStr;${session.pauseMinutes}\r\n")
        }

        // Urlaubstage
        val monthVacation = vacationDays
            .filter { it.date >= from && it.date <= to }
            .sortedBy { it.date }

        for (day in monthVacation) {
            val dateStr = LocalDate.parse(day.date).format(dateFormatter)
            val hoursStr = "%.2f".format(dailyHours)
            sb.append("$pn;$dateStr;400;$hoursStr;;;\r\n")
        }

        // Krankheitstage
        val monthSick = sickDays
            .filter { it.date >= from && it.date <= to }
            .sortedBy { it.date }

        for (day in monthSick) {
            val dateStr = LocalDate.parse(day.date).format(dateFormatter)
            val hoursStr = "%.2f".format(dailyHours)
            sb.append("$pn;$dateStr;500;$hoursStr;;;\r\n")
        }

        return sb.toString()
    }

    fun fileName(month: Int, year: Int): String =
        "DATEV_Lohn_${year}-${"%02d".format(month)}.csv"

    private fun formatTime(isoInstant: String): String {
        return try {
            val instant = Instant.parse(isoInstant)
            val time = instant.atZone(ZoneId.systemDefault()).toLocalTime()
            time.format(DateTimeFormatter.ofPattern("HH:mm"))
        } catch (_: Exception) { "" }
    }

    private fun countWorkDays(bitmask: Int): Int {
        var count = 0
        for (i in 0 until 7) {
            if (bitmask and (1 shl i) != 0) count++
        }
        return count
    }
}
```

---

## S04: UI-Integration

### iOS: OverviewViewModel + OverviewScreen

```swift
// OverviewViewModel.swift -- Neue Methode (analog generateCSV)
func generateDATEVExport(month: Int, year: Int) {
    guard let modelContext else { return }
    exportError = nil

    do {
        let sessions = try modelContext.fetch(FetchDescriptor<WorkSession>())
        let vacationDays = try modelContext.fetch(FetchDescriptor<VacationDay>())
        let sickDays = try modelContext.fetch(FetchDescriptor<SickDay>())
        let settings = try modelContext.fetch(FetchDescriptor<UserSettings>()).first
            ?? UserSettings()

        // Personalnummer aus Settings (neues Feld) oder leer
        let personalNumber = settings.personalNumber ?? ""

        let datev = DATEVExporter.generateExport(
            month: month, year: year,
            sessions: sessions, vacationDays: vacationDays,
            sickDays: sickDays, settings: settings,
            personalNumber: personalNumber
        )

        let fileName = DATEVExporter.fileName(month: month, year: year)
        let tempURL = FileManager.default.temporaryDirectory.appendingPathComponent(fileName)
        try datev.write(to: tempURL, atomically: true, encoding: .utf8)
        exportedFileURL = tempURL
    } catch {
        exportError = "DATEV-Export fehlgeschlagen: \(error.localizedDescription)"
    }
}
```

### Android: OverviewViewModel.kt

```kotlin
// OverviewViewModel.kt -- Neue Methode (analog generateCSV)
fun generateDATEVExport(month: Int) {
    val db = database ?: return
    val ctx = context ?: return

    viewModelScope.launch {
        _uiState.value = _uiState.value.copy(isExporting = true)
        try {
            val year = _uiState.value.selectedYear
            val ym = YearMonth.of(year, month)
            val from = ym.atDay(1).toString()
            val to = ym.atEndOfMonth().toString()

            val sessions = db.workSessionDao().let { dao ->
                dao.getPendingSessions().plus(dao.getSyncedSessions())
                    .distinctBy { it.id }
                    .filter { it.date >= from && it.date <= to }
            }
            val vacationDays = db.vacationDayDao().getAll().filter {
                it.date >= from && it.date <= to
            }
            val sickDays = db.sickDayDao().getAll().filter {
                it.date >= from && it.date <= to
            }
            val settings = db.userSettingsDao().getSettingsOnce()
                ?: UserSettingsEntity(userId = "")

            val personalNumber = settings.personalNumber ?: ""

            val datev = DATEVExporter.generateExport(
                month = month, year = year,
                sessions = sessions, vacationDays = vacationDays,
                sickDays = sickDays, settings = settings,
                personalNumber = personalNumber
            )

            val fileName = DATEVExporter.fileName(month, year)
            val file = File(ctx.cacheDir, fileName)
            file.writeText(datev, Charsets.UTF_8)

            _uiState.value = _uiState.value.copy(
                isExporting = false,
                exportedFile = file,
                exportMimeType = "text/csv"
            )
        } catch (e: Exception) {
            Log.e("OverviewViewModel", "DATEV export failed", e)
            _uiState.value = _uiState.value.copy(isExporting = false)
        }
    }
}
```

### UI: Neuer Button im Export-Bereich

```swift
// OverviewScreen.swift -- Unter den bestehenden PDF/CSV Buttons:
Button {
    // Monatsauswahl-Sheet oeffnen oder direkt aktuellen Monat exportieren
    viewModel.generateDATEVExport(
        month: Calendar.current.component(.month, from: Date()),
        year: viewModel.selectedYear
    )
} label: {
    HStack {
        Label("DATEV-Export", systemImage: "doc.text")
        Spacer()
        Text("PRO")
            .font(.caption)
            .padding(.horizontal, 6)
            .padding(.vertical, 2)
            .background(Theme.primary.opacity(0.2))
            .clipShape(RoundedRectangle(cornerRadius: 4))
    }
}
```

---

## UserSettings Erweiterung: personalNumber

### iOS: UserSettings.swift

```swift
// Neues optionales Property:
@Attribute var personalNumber: String?
```

Keine Schema-Migration noetig -- neue optionale Properties sind leichtgewichtig.

### Android: UserSettingsEntity + Migration

```kotlin
// In Entities.kt:
@Entity(tableName = "user_settings")
data class UserSettingsEntity(
    // ... bestehende Felder ...
    val personalNumber: String? = null  // NEU
)

// Migration 4->5:
val MIGRATION_4_5 = object : Migration(4, 5) {
    override fun migrate(db: SupportSQLiteDatabase) {
        db.execSQL("ALTER TABLE user_settings ADD COLUMN personalNumber TEXT DEFAULT NULL")
    }
}
```
