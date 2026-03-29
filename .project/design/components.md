# UI-Komponenten -- Wiederverwendbar

## Komponentenkatalog

Jede Komponente wird fuer iOS (SwiftUI) und Android (Jetpack Compose) separat implementiert, folgt aber dem gleichen visuellen Konzept.

---

## 1. ActiveSessionCard

**Beschreibung:** Prominente Karte, die die aktuell laufende oder zuletzt gestoppte Session anzeigt.

### Zustaende

**Idle (keine Session):**
- Einladender Text: "Bereit fuer den naechsten Eintrag"
- Grosser "Starten" Button (Primary Color)

**Running (Session laeuft):**
- Gruener pulsierender Indikator + "Laufende Sitzung"
- Grosser Timer (48pt/48sp monospaced, sekundengenau)
- Startzeit, Datum
- Pausenanzeige: "Pause: X min" (wenn Pausen erfasst, `pause`-Farbe)
- Buttons: "Pause" (Tonal) + "Stop" (Tonal) + "Fertig" (Primary)

**Paused (Session pausiert):** -- NEU (Marktanalyse: Pausenerfassung)
- Gelber pulsierender Indikator + "Pausiert"
- Grosser Pause-Timer (wie viel Pause bisher)
- Buttons: "Weiter" (Primary) + "Fertig" (Primary)

**Stopped (Session gestoppt, nicht abgeschlossen):**
- Orangener Indikator + "Gestoppt"
- Dauer-Anzeige (statisch): Brutto, Pause, Netto
- Startzeit, Endzeit, Datum (editierbar)
- Pausendauer (editierbar, Minuten)
- Buttons: "Fertig" (Primary) + "Verwerfen" (Text Button)

**Editing (Session wird bearbeitet):**
- Date/Time Picker fuer Start und Ende
- "Speichern" (Primary) + "Abbrechen" (Text Button)

### Props/Parameter

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| session | WorkSession? | Aktuelle Session (null = Idle) |
| onStart | () -> Void | Neue Session starten |
| onPause | () -> Void | Session pausieren (Marktanalyse: Pausenerfassung) |
| onResume | () -> Void | Pause beenden, weiterarbeiten |
| onStop | () -> Void | Laufende Session stoppen |
| onFinish | () -> Void | Session abschliessen |
| onSave | (WorkSession) -> Void | Bearbeitete Session speichern |
| onDelete | () -> Void | Session verwerfen |

---

## 2. SessionRow

**Beschreibung:** Kompakte Zeile fuer eine abgeschlossene Session in der History-Liste.

### Layout

```
[Sync-Icon]  Fr 29.03.   08:30 - 17:00  P30  8:00h
```

- Leading: Sync-Status Icon (Cloud-Done / Cloud-Upload)
- Main: Wochentag + Datum | Zeitraum
- Pause: "P30" fuer 30min Pause (compact, `pause`-Farbe, nur wenn > 0)
- Trailing: Nettodauer (monospaced, Bold)

### Interaktion
- Tap: Session-Detail Sheet oeffnen (Bearbeitungsmodus)
- Swipe Links: Loeschen (rot)
- Swipe Rechts: Bearbeiten (blau) -- optional

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| session | WorkSession | Die anzuzeigende Session |
| onTap | () -> Void | Session antippen |
| onDelete | () -> Void | Session loeschen |

---

## 3. MonthGroupSection

**Beschreibung:** Auf- und zuklappbare Sektion, die Sessions eines Monats gruppiert.

### Layout

**Header (zugeklappt):**
```
Maerz 2026          12 Eintraege    42:18h    ⌄
```

**Header (aufgeklappt):**
```
Maerz 2026          12 Eintraege    42:18h    ⌃
├── [SessionRow]
├── [SessionRow]
├── [SessionRow]
└── ...
```

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| monthName | String | "Maerz 2026" |
| sessions | [WorkSession] | Sessions dieses Monats |
| totalDuration | TimeInterval | Gesamtdauer |
| entryCount | Int | Anzahl Eintraege |
| isExpanded | Bool | Auf-/zugeklappt |
| onToggle | () -> Void | Auf-/zuklappen |

---

## 4. OvertimeCard

**Beschreibung:** Zusammenfassungs-Karte fuer Dashboard-Metriken.

### Layout

```
┌──────────────────┐
│  [Icon]          │
│  Ueberstunden    │  <- Titel (Caption)
│  +12:30h         │  <- Wert (Title, farbkodiert)
│  Im Vergleich... │  <- Untertitel (Caption)
└──────────────────┘
```

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| title | String | Karten-Titel |
| value | String | Formatierter Wert |
| subtitle | String? | Optionaler Untertitel |
| icon | SystemImage/Icon | Icon-Name |
| valueColor | Color | Farbe des Wertes |

---

## 5. TimerDisplay

**Beschreibung:** Animierter Timer, der die Dauer einer laufenden Session anzeigt.

### Verhalten
- Aktualisierung jede Sekunde
- Format: `HH:MM:SS`
- Monospaced Font (Ziffern gleiche Breite)
- Gruene Farbe waehrend Lauf
- Pulsierender Punkt (Heartbeat, 2s Zyklus)

### Groessen-Varianten

| Variante | Schriftgroesse | Verwendung |
|----------|---------------|------------|
| Large | 48pt/48sp | ActiveSessionCard |
| Medium | 28pt/28sp | Widget |
| Small | 17pt/16sp | Inline (z.B. Benachrichtigung) |

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| startTime | Date | Startzeitpunkt |
| isRunning | Bool | Timer laeuft? |
| size | TimerSize | .large / .medium / .small |

---

## 6. VacationCalendar

**Beschreibung:** Monatskalender zur Verwaltung von Urlaubstagen.

### Layout

```
       ← Maerz 2026 →

Mo  Di  Mi  Do  Fr  Sa  So
                 1   2   3
 4   5   6   7   8   9  10
11  12  13  14  15  16  17
18  19  20  21  22  23  24
25  26  27  28  29  30  31
```

### Tages-Markierungen

| Typ | Visuell | Interaktion |
|-----|---------|-------------|
| Normal (Arbeitstag) | Schwarze Zahl | Tap = Urlaub setzen, Long-Press = Kontext-Menue |
| Urlaub | Cyan Hintergrund-Kreis | Tap = Urlaub entfernen |
| Krankheitstag | Roter Hintergrund-Kreis (`sick-day`) | Tap = entfernen (Marktanalyse: neuer Typ) |
| Feiertag | Lila Punkt + Name | Nicht antippbar |
| Wochenende | Graue Zahl | Nicht antippbar (kein Arbeitstag) |
| Schulferien | Orangener Unterstrich | Tap = Urlaub setzen |
| Heute | Roter Kreis-Umriss | Normal interagierbar |

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| month | Int | Monat (1-12) |
| year | Int | Jahr |
| vacationDays | Set<DateOnly> | Markierte Urlaubstage |
| sickDays | Set<DateOnly> | Markierte Krankheitstage (Marktanalyse: neuer Typ) |
| holidays | [(DateOnly, String)] | Feiertage mit Namen |
| workDays | Int (Bitmask) | Arbeitstage des Nutzers |
| onToggleDay | (DateOnly) -> Void | Tag antippen (Urlaub) |
| onSetSickDay | (DateOnly) -> Void | Tag als Krankheitstag markieren |
| onMonthChange | (Int, Int) -> Void | Monat wechseln |

---

## 7. WorkdaySelector

**Beschreibung:** Visuelle Auswahl der Arbeitstage (Mo-So).

### Layout

```
  [Mo]  [Di]  [Mi]  [Do]  [Fr]  Sa   So
  ████  ████  ████  ████  ████
```

- Aktive Tage: Filled Pill (Primary Color)
- Inaktive Tage: Outline Pill (Gray)
- iOS: Toggle-Buttons oder SegmentedControl-Style
- Android: FilterChip Row

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| workDays | Int (Bitmask) | Aktuelle Auswahl |
| onChange | (Int) -> Void | Bitmask geaendert |

---

## 8. BundeslandPicker

**Beschreibung:** Dropdown/Picker fuer die Bundesland-Auswahl.

### Optionen

```
Baden-Wuerttemberg (BW)
Bayern (BY)
Berlin (BE)
Brandenburg (BB)
Bremen (HB)
Hamburg (HH)
Hessen (HE)
Mecklenburg-Vorpommern (MV)
Niedersachsen (NI)
Nordrhein-Westfalen (NW)     <- Standard
Rheinland-Pfalz (RP)
Saarland (SL)
Sachsen (SN)
Sachsen-Anhalt (ST)
Schleswig-Holstein (SH)
Thueringen (TH)
```

- iOS: Picker (Wheel oder Menu Style)
- Android: ExposedDropdownMenuBox

### Zusatz-Info
Unter dem Picker anzeigen: "X Feiertage in {Jahr}" basierend auf der Auswahl.

---

## 9. OfflineBanner

**Beschreibung:** Dezenter Hinweis wenn die App offline ist.

### Layout

```
┌────────────────────────────────────────────┐
│  ⚠ Offline -- Aenderungen werden lokal     │
│    gespeichert und spaeter synchronisiert  │
└────────────────────────────────────────────┘
```

- Gelber/Oranger Hintergrund (`warning-light`)
- Am oberen Bildschirmrand, unter der Navigation Bar
- Verschwindet automatisch wenn online
- Animation: Slide-In von oben, Slide-Out nach oben

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| isOffline | Bool | Offline-Status |

---

## 10. SyncStatusIndicator

**Beschreibung:** Zeigt den aktuellen Sync-Status an.

### Zustaende

| Zustand | Icon | Text |
|---------|------|------|
| Synced | Grüner Haken | "Synchronisiert" |
| Syncing | Rotierende Pfeile | "Synchronisiere..." |
| Pending | Gelber Pfeil | "Ausstehend" |
| Error | Rotes Kreuz | "Sync fehlgeschlagen" |

### Verwendung
- In der Navigation Bar (klein, rechts)
- In Session-Rows (Leading Icon)
- Als Toast/Snackbar nach manuellem Sync

---

## 11. SessionDetailSheet

**Beschreibung:** Modale Ansicht (Sheet/BottomSheet) zur Bearbeitung einer Session.

### Layout

```
┌────── Session bearbeiten ─────── [X] ┐
│                                       │
│  Datum        [29.03.2026      ⌄]    │
│                                       │
│  Startzeit    [08:30           ⌄]    │
│                                       │
│  Endzeit      [17:00           ⌄]    │
│                                       │
│  Pause (Min)  [30              ]     │
��                                       │
│  Brutto       8:30h                   │
│  Netto        8:00h                   │
│                                       │
│  ┌───────────────┐  ┌────────────┐   │
│  │   Speichern   │  │  Loeschen  │   │
│  └───────────────┘  └────────────┘   │
│                                       │
│        [Abbrechen]                    │
└───────────────────────────────────────┘
```

- iOS: .sheet mit .presentationDetents([.medium])
- Android: ModalBottomSheet
- Native Date/Time Picker pro Plattform
- Dauer wird automatisch berechnet (read-only)
- Validierung: Ende > Start, maximal 24h

### Props

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| session | WorkSession | Zu bearbeitende Session |
| onSave | (WorkSession) -> Void | Speichern |
| onDelete | () -> Void | Loeschen |
| onDismiss | () -> Void | Schliessen/Abbrechen |
