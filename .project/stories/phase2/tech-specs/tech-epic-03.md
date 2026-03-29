# Tech-Spec: EPIC 03 -- Urlaub-Tab (Kalender + Vacation CRUD)

## Dateien

### Neue Dateien

| Datei | Plattform | Beschreibung |
|-------|-----------|-------------|
| `Shared/VacationCalendar.swift` | iOS | Custom 7x6 Grid Monatskalender |
| `Features/Vacation/VacationScreen.swift` | iOS | Urlaub-Tab Screen |
| `Features/Vacation/VacationViewModel.swift` | iOS | Calendar State + Toggle-Logik |
| `ui/shared/VacationCalendar.kt` | Android | Custom Grid Monatskalender |
| `features/vacation/VacationScreen.kt` | Android | Urlaub-Tab Screen |
| `features/vacation/VacationViewModel.kt` | Android | Calendar State + Toggle-Logik |
| `features/vacation/VacationViewModelFactory.kt` | Android | Factory |

### Modifizierte Dateien

| Datei | Plattform | Aenderung |
|-------|-----------|-----------|
| `ContentView.swift` | iOS | Placeholder Tab 1 -> VacationScreen() |
| `AppNavigation.kt` | Android | Placeholder "urlaub" -> VacationScreen() |

---

## VacationCalendar -- Technisches Design

### Datenmodell fuer einen Monat

```
MonthData:
  year: Int
  month: Int (1-12)
  daysInMonth: Int (28-31)
  firstWeekdayOffset: Int (0=Mo, 1=Di, ..., 6=So)
  cells: [DayCell] (42 Zellen = 6 Wochen * 7 Tage)

DayCell:
  date: Date/LocalDate? (null fuer leere Zellen)
  dayNumber: Int?
  type: DayType

DayType:
  .empty          -- Leere Zelle (vor/nach Monat)
  .workday        -- Normaler Arbeitstag (antippbar)
  .weekend        -- Wochenende (nicht antippbar, grau)
  .holiday(name)  -- Feiertag (nicht antippbar, lila Punkt)
  .vacation       -- Urlaubstag (cyan Hintergrund, antippbar = entfernen)
  .sickDay        -- Krankheitstag (rot Hintergrund, antippbar = entfernen) [E04]
  .today          -- Heutiger Tag (roter Kreis-Umriss, zusaetzlich zu anderem Typ)
```

### Wochenstart: Montag (KRITISCH)

```swift
// iOS: Calendar-Konfiguration
var calendar = Calendar.current
calendar.firstWeekday = 2  // 2 = Montag

// Offset des 1. Tages im Monat
let firstOfMonth = calendar.date(from: DateComponents(year: year, month: month, day: 1))!
let weekday = calendar.component(.weekday, from: firstOfMonth) // 1=So, 2=Mo, ...
let offset = (weekday - 2 + 7) % 7  // 0=Mo, 1=Di, ..., 6=So
```

```kotlin
// Android: java.time
val firstOfMonth = YearMonth.of(year, month).atDay(1)
val offset = firstOfMonth.dayOfWeek.value - 1  // 0=Mo, 1=Di, ..., 6=So
```

### Arbeitstage aus Bitmask (NICHT hardcoded Mo-Fr)

```swift
// Swift
func isWorkday(_ weekday: Int) -> Bool {
    // weekday: 1=Mo, 2=Di, ..., 7=So (Montag-basiert!)
    return (workDays & (1 << (weekday - 1))) != 0
}
```

```kotlin
// Kotlin
fun isWorkday(dayOfWeek: DayOfWeek, workDays: Int): Boolean {
    val index = dayOfWeek.value - 1  // 0=Mo, 1=Di, ..., 6=So
    return (workDays and (1 shl index)) != 0
}
```

---

## Swift: VacationCalendar.swift

```swift
struct VacationCalendar: View {
    let year: Int
    let month: Int
    let vacationDates: Set<DateComponents>  // Tag-Lookup via year/month/day
    let sickDayDates: Set<DateComponents>   // Phase 2 E04
    let holidays: [Holiday]
    let workDays: Int                        // Bitmask
    let onDayTap: (Date) -> Void
    let onDayLongPress: (Date) -> Void      // Phase 2 E04

    @State private var displayedMonth: Int
    @State private var displayedYear: Int

    var body: some View {
        VStack(spacing: 8) {
            // Monats-Header mit Navigation
            HStack {
                Button(action: previousMonth) {
                    Image(systemName: "chevron.left")
                }
                Spacer()
                Text(monthYearString)
                    .font(.headline)
                Spacer()
                Button(action: nextMonth) {
                    Image(systemName: "chevron.right")
                }
            }
            .padding(.horizontal)

            // Wochentags-Header
            HStack(spacing: 0) {
                ForEach(["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"], id: \.self) { day in
                    Text(day)
                        .font(.caption2)
                        .frame(maxWidth: .infinity)
                        .foregroundStyle(.secondary)
                }
            }

            // Tages-Grid (6 x 7)
            LazyVGrid(columns: Array(repeating: GridItem(.flexible()), count: 7),
                      spacing: 4) {
                ForEach(cells, id: \.id) { cell in
                    DayCellView(cell: cell,
                                onTap: { if let d = cell.date { onDayTap(d) } },
                                onLongPress: { if let d = cell.date { onDayLongPress(d) } })
                }
            }
        }
    }

    private var cells: [DayCell] {
        // 1. Offset berechnen (leere Zellen vor dem 1.)
        // 2. Tage des Monats mit Typ befuellen
        // 3. Rest auffuellen bis 42
        buildCells(year: displayedYear, month: displayedMonth)
    }
}
```

### DayCellView (iOS)

```swift
struct DayCellView: View {
    let cell: DayCell
    let onTap: () -> Void
    let onLongPress: () -> Void

    var body: some View {
        ZStack {
            // Hintergrund
            switch cell.type {
            case .vacation:
                Circle().fill(Color.cyan.opacity(0.3))
            case .sickDay:
                Circle().fill(Color.red.opacity(0.3))
            case .today:
                Circle().stroke(Color.red, lineWidth: 2)
            default:
                Color.clear
            }

            // Text
            if let dayNumber = cell.dayNumber {
                Text("\(dayNumber)")
                    .font(.body)
                    .foregroundStyle(cell.type == .weekend ? .tertiary :
                                    cell.type == .holiday ? .purple : .primary)
            }

            // Feiertag-Indikator
            if case .holiday = cell.type {
                Circle()
                    .fill(Color.purple)
                    .frame(width: 4, height: 4)
                    .offset(y: 12)
            }
        }
        .frame(height: 40)
        .contentShape(Rectangle())
        .onTapGesture {
            guard cell.isTappable else { return }
            onTap()
        }
        .onLongPressGesture {
            guard cell.isTappable || cell.type == .vacation || cell.type == .sickDay else { return }
            onLongPress()
        }
    }
}
```

---

## Kotlin: VacationCalendar.kt

```kotlin
@Composable
fun VacationCalendar(
    year: Int,
    month: Int,
    vacationDates: Set<LocalDate>,
    sickDayDates: Set<LocalDate> = emptySet(),  // E04
    holidays: List<Holiday>,
    workDays: Int,
    onDayTap: (LocalDate) -> Unit,
    onDayLongPress: (LocalDate) -> Unit = {},   // E04
    onMonthChange: (Int, Int) -> Unit           // (year, month)
) {
    Column {
        // Monats-Header
        Row(
            modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = { /* previous month */ }) {
                Icon(Icons.Default.ChevronLeft, "Vorheriger Monat")
            }
            Text(
                "${Month.of(month).getDisplayName(TextStyle.FULL, Locale.GERMAN)} $year",
                style = MaterialTheme.typography.titleMedium
            )
            IconButton(onClick = { /* next month */ }) {
                Icon(Icons.Default.ChevronRight, "Naechster Monat")
            }
        }

        // Wochentags-Header
        Row(modifier = Modifier.fillMaxWidth()) {
            listOf("Mo", "Di", "Mi", "Do", "Fr", "Sa", "So").forEach { day ->
                Text(
                    day,
                    modifier = Modifier.weight(1f),
                    textAlign = TextAlign.Center,
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        // Grid (6 x 7)
        val cells = buildCells(year, month, vacationDates, sickDayDates, holidays, workDays)
        for (row in cells.chunked(7)) {
            Row(modifier = Modifier.fillMaxWidth()) {
                row.forEach { cell ->
                    DayCellComposable(
                        cell = cell,
                        modifier = Modifier.weight(1f).height(40.dp),
                        onTap = { cell.date?.let(onDayTap) },
                        onLongPress = { cell.date?.let(onDayLongPress) }
                    )
                }
            }
        }
    }
}
```

---

## Datenfluss: Tap-to-Toggle

```
Nutzer tippt auf 15. Juli (leerer Arbeitstag)
    |
    v
VacationViewModel.toggleVacationDay(date: 15.07.2026)
    |
    v
VacationDay existiert fuer diesen Tag?
    |
    +-- Nein: Neuen VacationDay erstellen
    |         id = UUID(), date = 15.07.2026, isPendingSync = true
    |         -> In SwiftData/Room speichern
    |         -> vacationDates Set aktualisieren -> UI aktualisiert sofort
    |         -> Resturlaub-Counter -1
    |         -> SyncEngine.syncVacationDays() triggern
    |
    +-- Ja:  VacationDay loeschen
             -> Aus SwiftData/Room loeschen
             -> vacationDates Set aktualisieren -> UI aktualisiert sofort
             -> Resturlaub-Counter +1
             -> SyncEngine.syncVacationDays() triggern
```

### Toggle-Logik (Swift)

```swift
func toggleVacationDay(date: Date) {
    let cal = Calendar.current
    let startOfDay = cal.startOfDay(for: date)

    // Existiert ein VacationDay fuer dieses Datum?
    let descriptor = FetchDescriptor<VacationDay>(
        predicate: #Predicate { day in
            day.date >= startOfDay && day.date < cal.date(byAdding: .day, value: 1, to: startOfDay)!
        }
    )

    if let existing = try? modelContext.fetch(descriptor).first {
        // Entfernen
        modelContext.delete(existing)
    } else {
        // Erstellen
        let vacationDay = VacationDay(date: startOfDay)
        modelContext.insert(vacationDay)
    }

    try? modelContext.save()
    Task { await syncEngine.syncVacationDays() }
}
```

### Toggle-Logik (Kotlin)

```kotlin
fun toggleVacationDay(date: LocalDate) {
    viewModelScope.launch {
        val existing = database.vacationDayDao().getAll()
            .firstOrNull { it.date == date.toString() }

        if (existing != null) {
            database.vacationDayDao().delete(existing)
        } else {
            database.vacationDayDao().insert(
                VacationDayEntity(date = date.toString())
            )
        }

        syncEngine?.syncVacationDays()
    }
}
```

---

## Resturlaub-Berechnung

```
verbleibend = urlaubstageProJahr - anzahlVacationDays(imAktuellenJahr)
genommen = anzahlVacationDays(imAktuellenJahr)
prozent = genommen / urlaubstageProJahr
```

Die Berechnung basiert auf lokalen Daten (nicht Backend), damit sie offline funktioniert.

---

## Testbare Kriterien

1. Kalender zeigt korrekten Monat mit richtigem Offset (1. Tag am richtigen Wochentag)
2. Wochenstart ist Montag
3. Feiertage sind lila markiert und nicht antippbar
4. Wochenenden sind grau und nicht antippbar
5. Tap auf Arbeitstag erstellt VacationDay -> cyan Markierung
6. Tap auf markierten Urlaubstag loescht VacationDay -> normale Anzeige
7. Resturlaub-Counter aktualisiert bei Toggle
8. Monatsnavigation: Pfeil vor/zurueck wechselt korrekt (inkl. Jahreswechsel)
9. Arbeitstage kommen aus Bitmask (nicht hardcoded Mo-Fr)
10. Warnung bei 0 Resturlaub, aber Tag wird trotzdem markiert

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| Custom Calendar ist komplex und dauert laenger | Hoch | MVP ohne Schulferien-Markierung, ohne Long-Press (erst in E04) |
| Kalender-Grid passt nicht auf kleine Screens | Mittel | Zellen-Hoehe dynamisch berechnen basierend auf verfuegbarem Platz |
| Performance bei vielen VacationDays | Niedrig | Set-Lookup statt Array-Filter; vacationDates als Set\<DateComponents\>/Set\<LocalDate\> |
| Wochenstart-Bug (Sonntag statt Montag) | Mittel | Expliziter Test mit bekanntem Datum (z.B. 1. Juni 2026 = Montag -> Offset 0) |
