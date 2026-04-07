# iOS Design -- Human Interface Guidelines Konform

## Navigation

### Tab Bar (Bottom)

Standard UITabBar / SwiftUI TabView mit 4 Tabs:

```
┌─────────────────────────────────────────────┐
│                                             │
│              [Content Area]                 │
│                                             │
├─────────────────────────────────────────────┤
│  🕐 Zeiten   ☀️ Urlaub   📊 Gesamt   ⚙️ Settings │
│  (aktiv)                                     │
└─────────────────────────────────────────────┘
```

- Translucent Background (Blur-Effekt)
- Aktiver Tab: `primary` Farbe (ausgefuelltes Icon)
- Inaktiver Tab: `gray-500` (Outline-Icon)
- Badge auf Zeiten-Tab bei laufender Session (gruener Punkt)

### Navigation Bar (Top)

- Large Title Style fuer Hauptseiten ("Zeiten", "Urlaub", "Gesamt", "Einstellungen")
- Collapsed Title beim Scrollen
- Keine Zurueck-Navigation auf Top-Level (Tab-basiert)
- Sheets fuer Detail-Ansichten (keine Push-Navigation)

---

## iOS-spezifische Patterns

### 1. Grouped List Style
Fuer Settings und Listenansichten verwenden wir den iOS-typischen "Inset Grouped" Stil:

```swift
List {
    Section("Arbeitszeit") {
        Stepper("Stunden/Woche: \(hours)", value: $hours)
        // ...
    }
}
.listStyle(.insetGrouped)
```

### 2. Sheets statt Navigation Push
Modale Bearbeitung von Sessions und Einstellungen:

```swift
.sheet(item: $selectedSession) { session in
    SessionDetailSheet(session: session)
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
}
```

### 3. Swipe Actions
Natuerliche Wisch-Gesten fuer Bearbeitung und Loeschung:

```swift
.swipeActions(edge: .trailing, allowsFullSwipe: true) {
    Button(role: .destructive) { deleteSession(session) } label: {
        Label("Loeschen", systemImage: "trash")
    }
}
.swipeActions(edge: .leading) {
    Button { editSession(session) } label: {
        Label("Bearbeiten", systemImage: "pencil")
    }
    .tint(.blue)
}
```

### 4. Confirmation Dialogs
iOS-native Bestaetigungsdialoge fuer kritische Aktionen:

```swift
.confirmationDialog("Session loeschen?", isPresented: $showDelete) {
    Button("Loeschen", role: .destructive) { /* ... */ }
    Button("Abbrechen", role: .cancel) { }
}
```

### 5. Pull to Refresh
```swift
List { /* ... */ }
    .refreshable {
        await syncManager.syncAll()
    }
```

---

## Komponenten-Spezifikation (iOS)

### Active Session Card

```
┌────────────────────────────────────────┐
│                                        │
│        ● Laufende Sitzung              │
│                                        │
│           03:42:18                      │
│     (grosse, monospaced Ziffern)       │
│                                        │
│    Start: 08:30      Ende: --:--       │
│    Datum: 29.03.2026                   │
│                                        │
│  ┌──────────┐  ┌──────────┐           │
│  │  ■ Stop  │  │ ✓ Finish │           │
│  └──────────┘  └──────────┘           │
│                                        │
└────────────────────────────────────────┘
```

- Karte mit leichtem Schatten, `primary-light` Hintergrundakzent am oberen Rand
- Gruener pulsierender Punkt neben "Laufende Sitzung"
- Timer in 48pt monospaced (SF Pro Rounded)
- Start/Ende in 15pt Regular
- Buttons: Rounded Rectangle, 44pt Hoehe (Apple Minimum Touch Target)

### Idle State (keine aktive Session)

```
┌────────────────────────────────────────┐
│                                        │
│    Bereit fuer den naechsten Eintrag   │
│                                        │
│       ┌──────────────────┐             │
│       │  ▶ Starten       │             │
│       └──────────────────┘             │
│                                        │
└────────────────────────────────────────┘
```

- Grosser, prominenter Start-Button
- `primary` Farbe, Rounded Rectangle
- SF Symbol `play.fill` vor "Starten"

### Month Group Section

```
┌────────────────────────────────────────┐
│  Maerz 2026                  42:18h   │
│  12 Eintraege            ⌄ aufklappen │
├────────────────────────────────────────┤
│  Fr 29.03.  08:30 - 17:00   8:30h    │
│  Do 28.03.  09:00 - 17:30   8:30h    │
│  Mi 27.03.  08:00 - 16:15   8:15h    │
│  ...                                   │
└────────────────────────────────────────┘
```

- DisclosureGroup Style (nativ iOS)
- Monatsname in Title-Groesse
- Gesamtstunden rechtsbuendig in Success/Danger-Farbe
- Einzelne Sessions als Reihen mit Wochentag-Kuerzel

### Session Row

```
┌────────────────────────────────────────┐
│  Fr 29.03.   08:30 - 17:00    8:30h  │
│              ┃ [synced ✓]             │
└────────────────────────────────────────┘
```

- Kompakt: Wochentag + Datum | Start-Ende | Dauer
- Sync-Status als dezentes Icon (Haekchen oder Pfeil)
- Swipe-Gesten fuer Bearbeiten/Loeschen

### Overtime Summary Cards

```
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Ueberstunden │ │   Urlaub     │ │  Feiertage   │
│              │ │              │ │              │
│  +12:30h     │ │  5 / 30      │ │     11       │
│  (gruen)     │ │  25 uebrig   │ │  in 2026     │
└──────────────┘ └──────────────┘ └──────────────┘
```

- Horizontales ScrollView oder 3er-Grid
- Jede Card: Icon + Titel + grosser Wert + Untertitel
- Farbkodierung: Ueberstunden gruen/rot, Urlaub blau, Feiertage lila

---

## iOS-spezifische Features

### Dynamic Island / Live Activity (Phase 3)

Waehrend einer laufenden Session zeigt die Live Activity:

```
┌─ Dynamic Island ──────────────────────────┐
│  🕐 Fakturus Track    03:42:18  ■ Stop   │
└───────────────────────────────────────────┘
```

- Kompakte Ansicht: Timer + App-Name
- Erweiterte Ansicht: Timer + Start-Zeit + Stop-Button
- Lock Screen Widget: Gleicher Inhalt

### Home Screen Widget (Phase 3)

**Small Widget (2x2):**
```
┌──────────────┐
│  Fakturus    │
│   03:42      │
│  ▶ Starten   │
└──────────────┘
```

**Medium Widget (4x2):**
```
┌────────────────────────────┐
│  Fakturus Track            │
│  Heute: 6:30h              │
│  🟢 Laufend: 03:42:18     │
│  ■ Stop                    │
└────────────────────────────┘
```

### Apple Watch (Phase 3)

Minimale Companion App:
- Complication: Heutige Arbeitszeit
- App: Start/Stop Button + Timer
- Keine History, keine Settings

---

## Farb-Adaption fuer iOS

### System Colors nutzen wo moeglich

```swift
extension Color {
    static let trackPrimary = Color("TrackPrimary") // Asset Catalog
    static let trackSuccess = Color.green           // iOS System Green
    static let trackDanger = Color.red              // iOS System Red
    static let trackSecondary = Color.secondary     // iOS System Secondary
}
```

### Tint Color
App-weite Tint Color: `#1A5CFF` (Fakturus Blau)
Wird automatisch fuer Buttons, Links und Akzente verwendet.

---

## Accessibility (iOS)

### VoiceOver
- Alle interaktiven Elemente haben `.accessibilityLabel`
- Timer wird als "Laufende Sitzung, drei Stunden und zweiundvierzig Minuten" vorgelesen
- Swipe-Aktionen haben Labels
- Custom Actions fuer komplexe Interaktionen

### Dynamic Type
- Alle Texte unterstuetzen Dynamic Type
- Layout passt sich an groessere Schrift an
- Mindestens "Accessibility Large" unterstuetzen

### Reduce Motion
- Respektiere `UIAccessibility.isReduceMotionEnabled`
- Timer-Puls-Animation deaktivieren wenn aktiv
- Keine automatischen Animationen

### Beispiel
```swift
Text("03:42:18")
    .font(.system(.largeTitle, design: .monospaced))
    .monospacedDigit()
    .accessibilityLabel("Drei Stunden, zweiundvierzig Minuten und achtzehn Sekunden")
    .accessibilityAddTraits(.updatesFrequently)
```
