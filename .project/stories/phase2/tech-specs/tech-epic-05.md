# Tech-Spec: EPIC 05 -- Gesamt-Tab (Overtime-Dashboard)

## Dateien

### Neue Dateien

| Datei | Plattform | Beschreibung |
|-------|-----------|-------------|
| `Shared/OvertimeCard.swift` | iOS | Wiederverwendbare Info-Karte |
| `Features/Overview/MonthlyOvertimeTable.swift` | iOS | 4-Spalten-Tabelle |
| `Features/Overview/OverviewScreen.swift` | iOS | Gesamt-Tab Screen |
| `Features/Overview/OverviewViewModel.swift` | iOS | API + Disk-Cache + Jahresnavigation |
| `ui/shared/OvertimeCard.kt` | Android | Info-Karte |
| `features/overview/MonthlyOvertimeTable.kt` | Android | 4-Spalten-Tabelle |
| `features/overview/OverviewScreen.kt` | Android | Gesamt-Tab Screen |
| `features/overview/OverviewViewModel.kt` | Android | API + Disk-Cache |
| `features/overview/OverviewViewModelFactory.kt` | Android | Factory |

### Modifizierte Dateien

| Datei | Plattform | Aenderung |
|-------|-----------|-----------|
| `ContentView.swift` | iOS | Placeholder Tab 2 -> OverviewScreen() |
| `AppNavigation.kt` | Android | Placeholder "gesamt" -> OverviewScreen() |
| `DTOs.swift` | iOS | +SickDaysTaken in OvertimeSummaryDTO |
| `DTOs.kt` | Android | +SickDaysTaken in OvertimeSummaryDTO |

---

## OvertimeSummaryDTO-Erweiterung

### Swift

```swift
// In OvertimeSummaryDTO ergaenzen:
let sickDaysTaken: Int?  // Optional, damit alte Backend-Versionen nicht brechen

// In MonthlyOvertimeDTO ergaenzen:
let sickDays: Int?  // Optional

// CodingKeys ergaenzen:
case sickDaysTaken = "SickDaysTaken"
case sickDays = "SickDays"
```

### Kotlin

```kotlin
// In OvertimeSummaryDTO ergaenzen:
@SerialName("SickDaysTaken") val sickDaysTaken: Int = 0

// In MonthlyOvertimeDTO ergaenzen:
@SerialName("SickDays") val sickDays: Int = 0
```

**Hinweis**: Default-Wert 0 (Swift: Optional nil, Kotlin: Default 0) garantiert Abwaertskompatibilitaet falls das Backend-Feld noch nicht existiert.

---

## OvertimeCard-Komponente

### Swift

```swift
struct OvertimeCard: View {
    let title: String
    let value: String
    let subtitle: String?
    let icon: String           // SF Symbol Name
    let valueColor: Color

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Image(systemName: icon)
                .font(.title3)
                .foregroundStyle(.secondary)

            Text(title)
                .font(.caption)
                .foregroundStyle(.secondary)

            Text(value)
                .font(.title2)
                .fontWeight(.bold)
                .foregroundStyle(valueColor)

            if let subtitle {
                Text(subtitle)
                    .font(.caption2)
                    .foregroundStyle(.tertiary)
            }
        }
        .frame(width: 130, alignment: .leading)
        .padding()
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 12))
    }
}
```

### Kotlin

```kotlin
@Composable
fun OvertimeCard(
    title: String,
    value: String,
    subtitle: String? = null,
    icon: ImageVector,
    valueColor: Color,
    modifier: Modifier = Modifier
) {
    ElevatedCard(modifier = modifier.width(140.dp)) {
        Column(modifier = Modifier.padding(12.dp)) {
            Icon(icon, null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(4.dp))
            Text(title, style = MaterialTheme.typography.labelSmall,
                 color = MaterialTheme.colorScheme.onSurfaceVariant)
            Text(value, style = MaterialTheme.typography.titleLarge,
                 fontWeight = FontWeight.Bold, color = valueColor)
            subtitle?.let {
                Text(it, style = MaterialTheme.typography.labelSmall,
                     color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}
```

---

## Monatstabelle

### Layout

```
 Monat        Gearbeitet   Erwartet     +/-
 ─────────────────────────────────────────────
 Januar       172:30h      168:00h      +4:30h   (gruen)
 Februar      158:45h      160:00h      -1:15h   (rot)
 Maerz        175:00h      176:00h      -1:00h   (rot)
 ─────────────────────────────────────────────
 Gesamt       506:15h      504:00h      +2:15h   (gruen, fett)
```

### Zeitformat-Hilfsfunktion

```swift
// Swift
func formatHours(_ totalHours: Double) -> String {
    let isNegative = totalHours < 0
    let absHours = abs(totalHours)
    let hours = Int(absHours)
    let minutes = Int((absHours - Double(hours)) * 60)
    let sign = isNegative ? "-" : (totalHours > 0 ? "+" : "")
    return "\(sign)\(hours):\(String(format: "%02d", minutes))h"
}
```

```kotlin
// Kotlin
fun formatHours(totalHours: Double): String {
    val isNegative = totalHours < 0
    val absHours = abs(totalHours)
    val hours = absHours.toInt()
    val minutes = ((absHours - hours) * 60).toInt()
    val sign = if (isNegative) "-" else if (totalHours > 0) "+" else ""
    return "$sign$hours:${"%02d".format(minutes)}h"
}
```

---

## Disk-Cache

### Struktur

```json
// overtime_cache_2026.json
{
  "summary": { ... OvertimeSummaryDTO ... },
  "timestamp": "2026-03-29T10:00:00Z"
}
```

### Swift

```swift
enum OvertimeCache {
    struct CachedSummary: Codable {
        let summary: OvertimeSummaryDTO
        let timestamp: Date
    }

    static func save(summary: OvertimeSummaryDTO, year: Int) throws {
        let cached = CachedSummary(summary: summary, timestamp: Date())
        let data = try JSONEncoder().encode(cached)
        try data.write(to: cacheURL(year: year))
    }

    static func load(year: Int) -> CachedSummary? {
        guard let data = try? Data(contentsOf: cacheURL(year: year)),
              let cached = try? JSONDecoder().decode(CachedSummary.self, from: data)
        else { return nil }
        return cached
    }

    private static func cacheURL(year: Int) -> URL {
        FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("overtime_cache_\(year).json")
    }
}
```

### Kotlin

```kotlin
class OvertimeCache(private val context: Context) {
    @Serializable
    data class CachedSummary(
        val summary: OvertimeSummaryDTO,
        val timestamp: String
    )

    private val json = Json { ignoreUnknownKeys = true }

    fun save(summary: OvertimeSummaryDTO, year: Int) {
        val cached = CachedSummary(summary, Instant.now().toString())
        val file = File(context.filesDir, "overtime_cache_$year.json")
        file.writeText(json.encodeToString(CachedSummary.serializer(), cached))
    }

    fun load(year: Int): CachedSummary? {
        val file = File(context.filesDir, "overtime_cache_$year.json")
        if (!file.exists()) return null
        return try {
            json.decodeFromString(CachedSummary.serializer(), file.readText())
        } catch (_: Exception) { null }
    }
}
```

---

## Datenfluss

```
Nutzer oeffnet Gesamt-Tab (Jahr = 2026)
    |
    v
OverviewViewModel.loadOvertimeSummary(year: 2026)
    |
    v
API Call: GET /v1/overtime-summary?year=2026
    |
    +-- Erfolg:
    |     summary = response
    |     OvertimeCache.save(response, 2026)
    |     isShowingCachedData = false
    |     lastUpdated = now()
    |
    +-- Fehler (Netzwerk/Server):
          OvertimeCache.load(2026)
          |
          +-- Cache vorhanden:
          |     summary = cached.summary
          |     lastUpdated = cached.timestamp
          |     isShowingCachedData = true
          |     -> Hinweis: "Zuletzt aktualisiert: vor X Stunden"
          |
          +-- Kein Cache:
                error = "Uebersicht konnte nicht geladen werden"

---

Nutzer navigiert zu 2025:
    |
    v
selectedYear = 2025
loadOvertimeSummary(year: 2025)  // gleicher Flow
```

---

## OverviewScreen-Aufbau

```
ScrollView {
  // 1. Summary Cards (horizontal scrollbar)
  ScrollView(.horizontal) {
    HStack {
      OvertimeCard(title: "Ueberstunden", value: "+12:30h",
                   icon: "clock.badge.checkmark", valueColor: .green)
      OvertimeCard(title: "Urlaub", value: "5 / 30",
                   icon: "sun.max", valueColor: .primary)
      OvertimeCard(title: "Feiertage", value: "3",
                   icon: "calendar", valueColor: .primary)
      OvertimeCard(title: "Krankheitstage", value: "2",
                   icon: "cross.circle", valueColor: .red)
    }
  }

  // 2. Jahresnavigation
  HStack {
    Button("2025") { selectedYear = 2025 }
    Text("2026").bold()
    Button("2027") { selectedYear = 2027 }
  }

  // 3. Monatstabelle
  MonthlyOvertimeTable(months: summary.monthlyOvertime)

  // 4. Cache-Hinweis (wenn offline)
  if isShowingCachedData {
    Text("Zuletzt aktualisiert: \(lastUpdated.relativeDescription)")
      .font(.caption)
      .foregroundStyle(.secondary)
  }

  // 5. Export-Sektion (Platzhalter fuer E06)
  // wird in E06 befuellt
}
```

---

## Testbare Kriterien

1. OvertimeCard zeigt Wert in korrekter Farbe (gruen/rot)
2. Monatstabelle: 3 Monate mit Gesamtsumme -> Footer korrekt
3. Zeitformat: 12.5 Stunden -> "+12:30h"
4. Zeitformat: -1.25 Stunden -> "-1:15h"
5. Online: API-Daten angezeigt, kein Cache-Hinweis
6. Offline mit Cache: Gecachte Daten + "Zuletzt aktualisiert" Hinweis
7. Offline ohne Cache: Fehlermeldung
8. Jahresnavigation: 2026 -> 2025 -> neuer API-Call
9. Pull-to-Refresh triggert neuen API-Call
10. SickDaysTaken wird angezeigt (oder 0 wenn Backend-Feld fehlt)

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| Backend liefert SickDaysTaken noch nicht | Mittel | Optional/Default 0, Card zeigt "0" |
| Disk-Cache JSON-Format aendert sich | Niedrig | try/catch beim Laden, alter Cache wird ignoriert |
| Viele Monate (12) passen nicht auf den Screen | Niedrig | ScrollView, Tabelle scrollt mit |
| API-Call dauert lang beim Jahreswechsel | Niedrig | Loading-Indicator, altes Jahr bleibt sichtbar bis neues geladen |
