# Screens -- Alle Bildschirme

## Screen-Uebersicht

```
App Start
  │
  ├── [Nicht eingeloggt] ──> Login Screen
  │                              │
  │                        [Login erfolgreich]
  │                              │
  └── [Eingeloggt] ──> Main App (Tab-basiert)
                            │
                            ├── Tab 1: Zeiten
                            │     ├── Active Session Card
                            │     ├── History (Monatsgruppen)
                            │     └── Session Detail Sheet
                            │
                            ├── Tab 2: Urlaub
                            │     ├── Kalender
                            │     └── Resturlaub-Info
                            │
                            ├── Tab 3: Gesamt
                            │     ├── Summary Cards
                            │     ├── Jahresnavigation
                            │     ├── Monatstabelle
                            │     └── Export (PDF/CSV)
                            │
                            └── Tab 4: Einstellungen
                                  ├── Profil
                                  ├── Arbeitszeit
                                  ├── Bundesland
                                  ├── Urlaub
                                  ├── Kalender
                                  ├── Schulferien
                                  └── App-Info
```

---

## 1. Login Screen

### Wireframe

```
┌─────────────────────────────────────┐
│                                     │
│                                     │
│                                     │
│          [Fakturus Logo]            │
│                                     │
│         Fakturus Track              │
│  Arbeitszeit erfassen.              │
│  Einfach. Ueberall.                 │
│                                     │
│                                     │
│  ┌───────────────────────────────┐  │
│  │  🍎 Mit Apple anmelden       │  │
│  └───────────────────────────────┘  │
│                                     │
│  ┌───────────────────────────────┐  │
│  │  G  Mit Google anmelden      │  │
│  └───────────────────────────────┘  │
│                                     │
│  ┌───────────────────────────────┐  │
│  │  📧 Mit E-Mail anmelden      │  │
│  └───────────────────────────────┘  │
│                                     │
│                                     │
│    [Fehler-Meldung, falls noetig]   │
│                                     │
│                                     │
└─────────────────────────────────────┘
```

### Beschreibung
- Zentriertes Layout, viel Weissraum
- App-Logo oben (SF Symbol `clock.badge.checkmark` oder Custom)
- App-Name + Tagline
- 3 Social Login Buttons (Apple, Google, E-Mail)
- Button-Design: Abgerundete Rechtecke mit Provider-Farbe als Akzent
- Fehleranzeige bei fehlgeschlagenem Login
- Kein "Offline nutzen" Button (Login ist Pflicht, aber Token-Cache ermoeglicht Offline nach erstem Login)

### Besonderheiten
- iOS: Apple Sign-In Button muss ASAuthorizationAppleIDButton sein (App Store Requirement)
- Android: Google Sign-In folgt Google Identity Branding Guidelines

---

## 2. Zeiten Screen (Hauptscreen)

### Wireframe -- Mit laufender Session

```
┌─────────────────────────────────────┐
│  Zeiten                    [Sync ↻] │
│─────────────────────────────────────│
│                                     │
│  ┌─────────────────────────────────┐│
│  │  ● Laufende Sitzung            ││
│  │                                 ││
│  │        03:42:18                 ││
│  │                                 ││
│  │  Start: 08:30   Ende: --:--    ││
│  │  Datum: 29.03.2026             ││
│  │  Pause: 30 min                  ││
│  │                                 ││
│  │  [⏸ Pause]  [■ Stop]  [✓ Fertig]││
│  └─────────────────────────────────┘│
│                                     │
│  History                            │
│                                     │
│  ┌─────────────────────────────────┐│
│  │ Maerz 2026     11 | 38:45h  ⌄  ││
│  ├─────────────────────────────────┤│
│  │ Fr 28. 08:30-17:00  P30  8:00h ││
│  │ Do 27. 09:00-17:30  P30  8:00h ││
│  │ Mi 26. 08:00-16:15  P30  7:45h ││
│  └─────────────────────────────────┘│
│                                     │
│  ┌─────────────────────────────────┐│
│  │ Februar 2026   20 | 162:30h  ⌄ ││
│  └─────────────────────────────────┘│
│                                     │
├─────────────────────────────────────┤
│  🕐 Zeiten  ☀ Urlaub  📊 Gesamt  ⚙ │
└─────────────────────────────────────┘
```

### Wireframe -- Ohne aktive Session

```
┌─────────────────────────────────────┐
│  Zeiten                    [Sync ↻] │
│─────────────────────────────────────│
│                                     │
│  ┌─────────────────────────────────┐│
│  │                                 ││
│  │  Bereit fuer den                ││
│  │  naechsten Eintrag              ││
│  │                                 ││
│  │      [▶ Starten]               ││
│  │                                 ││
│  └─────────────────────────────────┘│
│                                     │
│  History                            │
│  ...                                │
```

### Beschreibung
- Large Title: "Zeiten"
- Sync-Button in der Navigation Bar (rechts)
- Active Session Card oben (oder Idle State)
- Darunter scrollbare History, gruppiert nach Monat
- Pull-to-Refresh ueber gesamten Content
- Monatsgruppen auf-/zuklappbar
- Sessions mit Swipe-Aktionen

---

## 3. Urlaub Screen

### Wireframe

```
┌─────────────────────────────────────┐
│  Urlaub                             │
│─────────────────────────────────────│
│                                     │
│  ┌─────────────────────────────────┐│
│  │  Resturlaub                     ││
│  │  25 von 30 Tagen                ││
│  │  [████████████████████░░░░░░░]  ││
│  │  5 genommen                     ││
│  └─────────────────────────────────┘│
│                                     │
│  ┌─────────────────────────────────┐│
│  │         ← Maerz 2026 →         ││
│  │                                 ││
│  │  Mo  Di  Mi  Do  Fr  Sa  So    ││
│  │                   1   2   3    ││
│  │   4   5   6   7   8   9  10    ││
│  │  11  12  13  14  15  16  17    ││
│  │  18  19  20  21  22  23  24    ││
│  │  25  26  27 [28] 29  30  31    ││
│  │                                 ││
│  │  ● Urlaub  ● Feiertag          ││
│  │  ● Krank   ● Schulferien      ││
│  │  ○ Heute                        ││
│  └─────────────────────────────────┘│
│                                     │
│  Kommende Feiertage                 │
│  ┌─────────────────────────────────┐│
│  │  01.05. Maifeiertag             ││
│  │  29.05. Christi Himmelfahrt     ││
│  │  09.06. Pfingstmontag          ││
│  └─────────────────────────────────┘│
│                                     │
├─────────────────────────────────────┤
│  🕐 Zeiten  ☀ Urlaub  📊 Gesamt  ⚙ │
└─────────────────────────────────────┘
```

### Beschreibung
- Large Title: "Urlaub"
- Resturlaub-Fortschrittsbalken oben
- Kalender-Ansicht mit Monatsnavigation
- Tap auf Arbeitstag = Urlaub setzen/entfernen
- Farbkodierung: Cyan (Urlaub), Lila (Feiertag), Orange (Schulferien)
- Wochenenden und Feiertage nicht anwaehlbar
- Legende unter dem Kalender
- Optionale Liste der kommenden Feiertage

---

## 4. Gesamt Screen

### Wireframe

```
┌─────────────────────────────────────┐
│  Gesamt                             │
│─────────────────────────────────────│
│                                     │
│  [Ueberstunden] [Urlaub] [Feiertg] │
│  ┌─────┐ ┌─────┐ ┌─────┐           │
│  │+12:30│ │5/30 │ │ 11  │           │
│  │ gruen│ │25 ueb│ │2026│           │
│  └─────┘ └─────┘ └─────┘           │
│                                     │
│  ┌─────────────────────────────────┐│
│  │  Monatliche Uebersicht         ││
│  │  ← 2025    2026    2027 →      ││
│  └─────────────────────────────────┘│
│                                     │
│  ┌─────────────────────────────────┐│
│  │ Monat  Gearb.  Erw.  +/-       ││
│  │─────────────────────────────────││
│  │ Jan    171:15  168:00 + 3:15   ││
│  │ Feb    158:30  160:00 - 1:30   ││
│  │ Mär    175:45  176:00 - 0:15   ││
│  │ ...                             ││
│  │─────────────────────────────────││
│  │ Gesamt 505:30  504:00 + 1:30   ││
│  └─────────────────────────────────┘│
│                                     │
│  EXPORT                             │
│  ┌─────────────────────────────────┐│
│  │  [PDF-Monatsreport]  [CSV-Export]││
│  │  Monat: ← Maerz 2026 →        ││
│  └─────────────────────────────────┘│
│                                     │
├─────────────────────────────────────┤
│  🕐 Zeiten  ☀ Urlaub  📊 Gesamt  ⚙ │
└─────────────────────────────────────┘
```

### Beschreibung
- Large Title: "Gesamt"
- Horizontal scrollbare Summary Cards oben
- Jahresnavigation (Vor/Zurueck)
- Monatstabelle mit 4 Spalten
- Ueberstunden farbkodiert (gruen/rot)
- Footer mit Gesamtsummen
- **Export-Sektion unterhalb der Tabelle:** PDF-Monatsreport und CSV-Export Buttons mit Monatsauswahl. Hier statt in Settings, weil Export inhaltlich zum Gesamt-Tab gehoert (Uebersicht -> Export als logischer naechster Schritt).
- Schulferien-Info als Untertitel bei Ueberstunden-Card

---

## 5. Einstellungen Screen

### Wireframe

```
┌─────────────────────────────────────┐
│  Einstellungen                      │
│─────────────────────────────────────│
│                                     │
│  ┌─────────────────────────────────┐│
│  │  [Avatar] Max Mustermann       ││
│  │           max@beispiel.de      ││
│  │                     [Abmelden] ││
│  └─────────────────────────────────┘│
│                                     │
│  ARBEITSZEIT                        │
│  ┌─────────────────────────────────┐│
│  │  Stunden/Woche          40.0   ││
│  │  Arbeitstage                   ││
│  │  [Mo][Di][Mi][Do][Fr] Sa  So   ││
│  └─────────────────────────────────┘│
│                                     │
│  STANDORT                           │
│  ┌─────────────────────────────────┐│
│  │  Bundesland     Nordrhein-W.  ⌄││
│  │  11 Feiertage in 2026          ││
│  └─────────────────────────────────┘│
│                                     │
│  URLAUB                             │
│  ┌─────────────────────────────────┐│
│  │  Urlaubstage/Jahr          30  ││
│  └─────────────────────────────────┘│
│                                     │
│  KALENDER                           │
│  ┌─────────────────────────────────┐│
│  │  Kalender-URL           [...]  ││
│  │  Schulferien          3 Eintr. ││
│  └─────────────────────────────────┘│
│                                     │
│  APP                                │
│  ┌─────────────────────────────────┐│
│  │  Version              1.0.0    ││
│  │  Datenschutz                 > ││
│  │  Lizenzen                    > ││
│  └─────────────────────────────────┘│
│                                     │
├─────────────────────────────────────┤
│  🕐 Zeiten  ☀ Urlaub  📊 Gesamt  ⚙ │
└─────────────────────────────────────┘
```

### Beschreibung
- Large Title: "Einstellungen"
- iOS: InsetGroupedList Style
- Android: Material 3 PreferenceScreen / LazyColumn
- Profil-Sektion oben mit Logout-Button
- Gruppierte Einstellungen mit Sektions-Headern
- Arbeitstage als visuelle Tages-Toggles
- Bundesland mit Feiertag-Vorschau
- Schulferien als Link zu Sub-Screen/Sheet

---

## 6. Session Detail Sheet

### Wireframe

```
┌─────────────────────────────────────┐
│  ─────  (Drag Handle)              │
│                                     │
│  Session bearbeiten          [X]    │
│                                     │
│  Datum                              │
│  ┌─────────────────────────────────┐│
│  │  29. Maerz 2026            [📅] ││
│  └─────────────────────────────────┘│
│                                     │
│  Startzeit                          │
│  ┌─────────────────────────────────┐│
│  │  08:30                     [🕐] ││
│  └─────────────────────────────────┘│
│                                     │
│  Endzeit                            │
│  ┌─────────────────────────────────┐│
│  │  17:00                     [🕐] ││
│  └─────────────────────────────────┘│
│                                     │
│  Pause (Minuten)                    │
│  ┌─────────────────────────────────┐│
│  │  30                             ││
│  └─────────────────────────────────┘│
│                                     │
│  Brutto: 8 Stunden 30 Minuten      │
│  Netto:  8 Stunden 00 Minuten      │
│                                     │
│  ┌─────────────┐  ┌────────────┐   │
│  │  Speichern  │  │  Loeschen  │   │
│  └─────────────┘  └────────────┘   │
│                                     │
│       [Abbrechen]                   │
│                                     │
└─────────────────────────────────────┘
```

### Beschreibung
- iOS: Half-Sheet (.presentationDetents([.medium, .large]))
- Android: ModalBottomSheet
- Drag Handle zum Schliessen
- Datum: Native DatePicker (Tap oeffnet Kalender)
- Startzeit/Endzeit: Native TimePicker
- Dauer wird live berechnet (read-only)
- Validierung: Ende > Start, maximal 24h Dauer
- Speichern: Primary Button
- Loeschen: Destructive Button (rot)
- Abbrechen: Text Button

---

## 7. Schulferien-Verwaltung (Sub-Screen/Sheet)

### Wireframe

```
┌─────────────────────────────────────┐
│  Schulferien              [+ Neu]   │
│─────────────────────────────────────│
│                                     │
│  2026                               │
│  ┌─────────────────────────────────┐│
│  │  Osterferien                   ││
│  │  06.04. - 18.04.2026          ││
│  └─────────────────────────────────┘│
│  ┌─────────────────────────────────┐│
│  │  Sommerferien                  ││
│  │  06.07. - 18.08.2026          ││
│  └─────────────────────────────────┘│
│  ┌─────────────────────────────────┐│
│  │  Herbstferien                  ││
│  │  12.10. - 24.10.2026          ││
│  └─────────────────────────────────┘│
│                                     │
│  [Keine Eintraege? Hier tippen]     │
│                                     │
└─────────────────────────────────────┘
```

### Beschreibung
- Liste der Schulferien-Zeitraeume
- "+" Button fuer neuen Eintrag
- Swipe-to-Delete
- Tap zum Bearbeiten (Name, Start-Datum, End-Datum)
- Gruppiert nach Jahr
