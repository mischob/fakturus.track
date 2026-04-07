# Tech-Spec: EPIC 06 -- Barrierefreiheit (Accessibility)

## Uebersicht

Keine neuen Dateien. Reine Modifikation bestehender Views/Screens: Accessibility-Labels, -Hints, -Values, Dynamic Type Support, Kontrast-Korrekturen. Systematischer Audit aller 4 Tabs.

---

## S01: iOS VoiceOver Audit & Fixes

### Zeiten-Tab (TimeTrackingView + Subviews)

```swift
// TimerDisplay.swift -- Timer braucht beschreibendes Label
Text(timerText)
    .font(.system(.largeTitle, design: .monospaced))
    .accessibilityLabel(accessibleTimerText)
    .accessibilityAddTraits(.updatesFrequently)

// Computed property:
private var accessibleTimerText: String {
    let h = totalSeconds / 3600
    let m = (totalSeconds % 3600) / 60
    if h > 0 {
        return "\(h) Stunden \(m) Minuten Arbeitszeit"
    }
    return "\(m) Minuten Arbeitszeit"
}

// ActiveSessionCard.swift -- Buttons mit klaren Labels
Button(action: onStart) {
    Label("Starten", systemImage: "play.fill")
}
.accessibilityHint("Startet die Zeiterfassung")

Button(action: onStop) {
    Label("Stoppen", systemImage: "stop.fill")
}
.accessibilityHint("Stoppt die laufende Sitzung")

Button(action: onFinish) {
    Label("Fertig", systemImage: "checkmark.circle.fill")
}
.accessibilityHint("Schliesst die Sitzung ab und speichert sie")

// SessionRow.swift -- Zusammenfassung der Session
HStack { /* Zeitraum, Dauer, Sync-Status */ }
    .accessibilityElement(children: .combine)
    .accessibilityLabel("\(weekdayString) \(dateString), \(startString) bis \(endString), \(netDurationString) Nettoarbeitszeit")
    .accessibilityHint("Doppeltippen fuer Details")

// MonthGroup.swift -- Monatsheader
HStack { /* Monatsname, Anzahl, Gesamtstunden */ }
    .accessibilityElement(children: .combine)
    .accessibilityLabel("\(monthName), \(count) Eintraege, \(totalHoursString)")
    .accessibilityAddTraits(.isHeader)
    .accessibilityHint(isExpanded ? "Doppeltippen zum Zuklappen" : "Doppeltippen zum Aufklappen")
```

### Urlaub-Tab (VacationScreen + VacationCalendar)

```swift
// VacationCalendar.swift -- Jeder Kalendertag braucht Label
ForEach(daysInMonth, id: \.self) { date in
    DayCell(date: date, type: dayType(for: date))
        .accessibilityLabel(accessibleDayLabel(date))
        .accessibilityHint(dayHint(date))
}

private func accessibleDayLabel(_ date: Date) -> String {
    let formatter = DateFormatter()
    formatter.locale = Locale(identifier: "de_DE")
    formatter.dateFormat = "EEEE, d. MMMM"
    let dateStr = formatter.string(from: date)

    switch dayType(for: date) {
    case .vacation: return "\(dateStr), Urlaubstag"
    case .sick: return "\(dateStr), Krankheitstag"
    case .holiday(let name): return "\(dateStr), Feiertag, \(name)"
    case .workday: return "\(dateStr), Arbeitstag"
    case .weekend: return "\(dateStr), Wochenende"
    }
}

// Resturlaub-Anzeige
Text("\(remaining)/\(total)")
    .accessibilityLabel("\(remaining) von \(total) Urlaubstagen verbleibend")
```

### Gesamt-Tab (OverviewScreen)

```swift
// Ueberstunden-Card
OvertimeCard(value: overtimeHours)
    .accessibilityLabel("Ueberstunden \(overtimeFormatted)")

// MonthlyOvertimeTable -- Tabellenzeile
HStack { /* Monat, Soll, Ist, Diff */ }
    .accessibilityElement(children: .combine)
    .accessibilityLabel("\(monthName): \(sollHours) Stunden Soll, \(istHours) Stunden Ist, \(diffFormatted) Differenz")
```

### Einstellungen-Tab

```swift
// WorkdaySelector.swift -- Tage-Toggles
Toggle(isOn: binding) {
    Text(dayName)
}
.accessibilityLabel("\(dayName)")
.accessibilityValue(isActive ? "aktiviert" : "deaktiviert")

// BundeslandPicker.swift
Picker("Bundesland", selection: $bundesland) { /* ... */ }
    .accessibilityHint("Waehlt das Bundesland fuer die Feiertagsberechnung")
```

---

## S02: Android TalkBack Audit & Fixes

### Gleiche Screens, Compose Semantics

```kotlin
// TimerDisplay.kt
Text(
    text = timerText,
    modifier = Modifier.semantics {
        contentDescription = "$hours Stunden $minutes Minuten Arbeitszeit"
        liveRegion = LiveRegionMode.Polite  // Aenderungen dezent ankuendigen
    }
)

// ActiveSessionCard.kt -- Buttons
Button(onClick = onStart) {
    Text("Starten")
}
// Compose Button hat automatisch Role.Button -- aber Hint fehlt:
// In Compose gibt es kein direktes "hint" -- stattdessen:
Modifier.semantics { contentDescription = "Timer starten. Startet die Zeiterfassung" }

// SessionRow.kt -- Zusammenfassung
Row(
    modifier = Modifier
        .semantics(mergeDescendants = true) {
            contentDescription = "$weekday $dateStr, $startStr bis $endStr, $netDuration Nettoarbeitszeit"
        }
        .clickable { onSelect(session) }
) { /* ... */ }

// MonthGroup.kt -- Header
Text(
    text = monthName,
    modifier = Modifier.semantics {
        heading()
        contentDescription = "$monthName, $count Eintraege, $totalHours Stunden"
    }
)
```

### Icons mit contentDescription

```kotlin
// Ueberall wo Icons ohne Text stehen:
Icon(
    imageVector = Icons.Default.Sync,
    contentDescription = "Synchronisation ausstehend"  // NICHT null!
)

// Dekorative Icons:
Icon(
    imageVector = Icons.Default.Circle,
    contentDescription = null  // Wird von TalkBack ignoriert
)
```

---

## S03: iOS Dynamic Type

### Hauptregel: Keine hardcodierten Font-Sizes

```swift
// FALSCH:
Text("Timer").font(.system(size: 48))

// RICHTIG:
Text("Timer").font(.largeTitle)

// Fuer Groessen die mitskalieren muessen:
@ScaledMetric(relativeTo: .body) private var iconSize: CGFloat = 24
@ScaledMetric(relativeTo: .body) private var cardPadding: CGFloat = 16

Image(systemName: "timer")
    .frame(width: iconSize, height: iconSize)
```

### Problemstellen identifizieren und fixen

```swift
// TimerDisplay.swift -- Timer-Anzeige soll skalieren, aber nicht zu gross werden
Text(timerText)
    .font(.system(.largeTitle, design: .monospaced))
    .minimumScaleFactor(0.5)  // Notfall-Schrumpfung bei XXL
    .lineLimit(1)

// SessionRow.swift -- Feste Hoehen durch flexible ersetzen
// FALSCH:
HStack { ... }.frame(height: 60)

// RICHTIG:
HStack { ... }
    .padding(.vertical, 8)
    // Hoehe ergibt sich aus Inhalt + Dynamic Type

// VacationCalendar.swift -- Grid-Zellen muessen wachsen
LazyVGrid(columns: Array(repeating: GridItem(.flexible(minimum: 36)), count: 7)) {
    // minimum statt fixed, damit Dynamic Type Platz hat
}
```

---

## S04: Android Schriftgroessen-Anpassung

### Hauptregel: MaterialTheme.typography statt hardcodierte Groessen

```kotlin
// FALSCH:
Text(text = "Timer", fontSize = 48.sp)

// RICHTIG:
Text(text = "Timer", style = MaterialTheme.typography.displayMedium)

// Falls custom Size noetig: sp verwenden (nicht dp!)
Text(text = "03:42", fontSize = 32.sp)  // sp skaliert mit System-Schriftgroesse
```

### Layout-Anpassungen fuer 200% Schriftgroesse

```kotlin
// SessionRow.kt -- Kein festes height
Row(
    modifier = Modifier
        .fillMaxWidth()
        .padding(vertical = 8.dp)  // Padding statt fester Hoehe
) { /* ... */ }

// VacationCalendar.kt -- Flexible Zellen
val minCellSize = with(LocalDensity.current) {
    (36.dp * fontScale).coerceAtLeast(36.dp)
}
```

---

## S05: Kontrast-Pruefung

### Bekannte Problemstellen (aus Theme.swift/Color.kt)

| Farbe | Light BG | Dark BG | Problem | Loesung |
|-------|----------|---------|---------|---------|
| `vacation` (Cyan 0x06B6D4) | Weiss | Dunkel | Zu wenig Kontrast auf Weiss | Dunkler: 0x0891B2 (Light) |
| `secondaryText` (gray500) | Weiss | Dunkel | Grenzwertig auf Weiss | gray600 (0x4B5563) fuer Light |
| `timerRunning` (Green) | Weiss | Dunkel | Hellgruen auf Weiss ist schlecht | 0x15803D statt 0x1DB954 fuer Text |

### Farbkodierung + Text-Label

```swift
// FALSCH: Nur Farbe als Information
Circle().fill(dayType == .vacation ? .cyan : .clear)

// RICHTIG: Farbe + Text-Label
ZStack {
    Circle().fill(dayType == .vacation ? Theme.vacation : .clear)
    Text(dayType == .vacation ? "U" : (dayType == .sick ? "K" : ""))
        .font(.caption2)
        .foregroundStyle(.white)
}
```

### Werkzeuge fuer Audit

- **iOS**: Xcode Accessibility Inspector > Audit > Color Contrast
- **Android**: Android Studio Layout Inspector > Accessibility Scanner
- **Cross-Platform**: WebAIM Contrast Checker (manuell fuer Custom Colors)
- **Minimum**: WCAG AA = 4.5:1 fuer normalen Text, 3:1 fuer grossen Text (>=18pt)

---

## Reduce Motion / Animationen

Alle Animationen muessen `@Environment(\.accessibilityReduceMotion)` (iOS) bzw. `Settings.Global.ANIMATOR_DURATION_SCALE` (Android) respektieren. Bei aktivierter Einstellung Animationen deaktivieren oder auf Crossfade reduzieren.

### iOS

```swift
@Environment(\.accessibilityReduceMotion) private var reduceMotion

// Beispiel: Animation nur wenn erlaubt
withAnimation(reduceMotion ? nil : .spring(duration: 0.3)) {
    isExpanded.toggle()
}

// Oder: Crossfade statt Slide
.transition(reduceMotion ? .opacity : .slide)
```

### Android

```kotlin
// Animator Duration Scale pruefen (0.0 = Animationen deaktiviert)
val animatorScale = Settings.Global.getFloat(
    context.contentResolver,
    Settings.Global.ANIMATOR_DURATION_SCALE,
    1.0f
)
val reduceMotion = animatorScale == 0.0f

// In Compose:
val animationSpec = if (reduceMotion) {
    snap<Float>()
} else {
    tween<Float>(durationMillis = 300)
}
```

**Regel**: Jede Animation in Phase 3 (E01 Animationen, Widget-Transitionen, Loading-States) MUSS diese Pruefung enthalten.

---

## Checkliste pro View (Muster fuer AI-Agent)

Fuer **jede bestehende View-Datei** folgende Pruefung durchfuehren:

1. Haben alle Buttons ein `.accessibilityLabel`/`contentDescription`?
2. Haben alle Icons ein Label (oder explizit `nil`/`null` fuer dekorative)?
3. Haben Custom-Composites ein `.accessibilityElement(children: .combine)` / `mergeDescendants`?
4. Sind Header als `.isHeader` / `heading()` markiert?
5. Gibt es `updatesFrequently` Traits fuer Timer-Texte?
6. Sind alle Schriftgroessen via Typography-Styles oder `.font(.body)` statt hardcodiert?
7. Gibt es feste Hoehen die bei Dynamic Type brechen koennten?
8. Ist die Fokus-Reihenfolge logisch (oben->unten, links->rechts)?
