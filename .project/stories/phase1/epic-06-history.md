# EPIC 06: History & Session-Verwaltung

## Ziel

Nutzer koennen alle vergangenen Arbeitssitzungen in einer nach Monaten gruppierten Liste einsehen, einzelne Sessions bearbeiten oder loeschen. Die History ist der zweite wesentliche Bestandteil des Zeiten-Tabs.

## Abhaengigkeiten

- **E03**: Lokale Datenschicht (Sessions lesen)
- **E05**: Timer-Screen (ActiveSessionCard und ViewModel als Kontext)

---

## Stories

### P1-E06-S01: iOS SessionRow-Komponente

**Als** Nutzer
**moechte ich** jede vergangene Arbeitssitzung kompakt in einer Zeile sehen,
**damit** ich schnell einen Ueberblick ueber meine Arbeitszeiten bekomme.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E03-S01 (WorkSession Model), P1-E01-S03 (Formatierung)
**Parallelisierbar mit**: P1-E06-S02 (Android SessionRow), P1-E05-* (Timer-Screen)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `SessionRow.swift` als SwiftUI-Komponente:
  - Layout: `[Sync-Icon]  Fr 29.03.   08:30 - 17:00  P30  8:00h`
  - Leading: Sync-Status Icon:
    - `isSynced`: Cloud mit Haken (gruen, `cloud.fill`)
    - `isPendingSync`: Cloud mit Pfeil (gelb, `icloud.and.arrow.up`)
    - Weder noch: Kein Icon
  - Mitte: Wochentag + Datum, Zeitraum (Start - Ende)
  - Pause: "P30" kompakt in `pause`-Farbe (nur wenn pauseMinutes > 0)
  - Trailing: Netto-Dauer (Brutto minus Pause) in Bold, monospaced
- [ ] Tap: Ruft `onTap()` Closure auf (oeffnet Detail-Sheet)
- [ ] Swipe-to-Delete: Rote Loeschen-Aktion
  - Given eine Session wird nach links gewischt
  - When "Loeschen" angetippt wird
  - Then wird `onDelete()` aufgerufen
- [ ] Netto-Dauer korrekt berechnet:
  - Given Session mit Start 08:00, Ende 17:00, Pause 30min
  - Then wird "8:30h" angezeigt (Netto = 9h - 0.5h = 8.5h = 8:30h)

**Technische Hinweise**:
- `.swipeActions(edge: .trailing) { Button(role: .destructive) { ... } }`
- Dauer-Berechnung: `(stopTime - startTime) - (pauseMinutes * 60)` in Sekunden

---

### P1-E06-S02: Android SessionRow-Komponente

**Als** Nutzer
**moechte ich** jede vergangene Arbeitssitzung kompakt in einer Zeile sehen,
**damit** ich schnell einen Ueberblick bekomme.

**Plattform**: Android
**Abhaengigkeiten**: P1-E03-S02 (WorkSession Entity), P1-E01-S04 (Formatierung)
**Parallelisierbar mit**: P1-E06-S01 (iOS SessionRow), P1-E05-* (Timer-Screen)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `SessionRow.kt` als Composable:
  - Gleiches Layout-Konzept wie iOS (visuell konsistent)
  - Material 3 ListItem oder custom Row
  - Sync-Status Icons (Material Icons: `Cloud`, `CloudUpload`)
  - Pause-Anzeige in `PauseColor`
- [ ] Tap: `onClick` Lambda (oeffnet Detail-Sheet)
- [ ] SwipeToDismiss:
  - `SwipeToDismissBox` mit `DismissDirection.EndToStart`
  - Roter Hintergrund mit Muelleimer-Icon
  - `onDelete` Lambda wird aufgerufen
- [ ] Netto-Dauer korrekt berechnet (analog iOS)

**Technische Hinweise**:
- Material 3 `SwipeToDismissBox` (oder `SwipeToDismissBoxValue`)
- `ListItem` Composable fuer Standard-Layout

---

### P1-E06-S03: iOS MonthGroupSection

**Als** Nutzer
**moechte ich** meine Arbeitszeiten nach Monaten gruppiert sehen,
**damit** ich die Uebersicht behalte und gezielt in bestimmte Monate schauen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E06-S01 (SessionRow)
**Parallelisierbar mit**: P1-E06-S04 (Android MonthGroup)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `MonthGroupSection.swift`:
  - Header zeigt: Monatsname + Jahr, Anzahl Eintraege, Gesamtdauer, Expand/Collapse-Icon
  - Beispiel: "Maerz 2026     12 Eintraege    42:18h    v"
  - Tap auf Header: Gruppe auf-/zuklappen (mit Animation)
  - Gesamtdauer = Summe aller Netto-Dauern der Sessions im Monat
  - Aufgeklappt: Liste von `SessionRow`-Eintraegen
- [ ] Aktueller Monat ist beim Laden aufgeklappt, aeltere zugeklappt
- [ ] Animation: `.animation(.spring(), value: isExpanded)` fuer smooth Expand

**Technische Hinweise**:
- `DisclosureGroup` oder custom Toggle mit `Section`
- Gruppierung: Dictionary `[String: [WorkSession]]` nach `monthYearString`
- Sortierung: neuester Monat zuerst, innerhalb des Monats neueste Session zuerst

---

### P1-E06-S04: Android MonthGroup

**Als** Nutzer
**moechte ich** meine Arbeitszeiten nach Monaten gruppiert sehen,
**damit** ich die Uebersicht behalte.

**Plattform**: Android
**Abhaengigkeiten**: P1-E06-S02 (SessionRow)
**Parallelisierbar mit**: P1-E06-S03 (iOS MonthGroup)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `MonthGroup.kt` als Composable:
  - Gleiche Funktionalitaet wie iOS (Header mit Stats, Expand/Collapse)
  - Material 3 Design: Card-basiert oder Surface mit Divider
  - `AnimatedVisibility` fuer Expand/Collapse
  - Rotate-Animation fuer Expand-Icon
- [ ] LazyColumn-kompatibel (muss mit `items()` funktionieren)
- [ ] Aktueller Monat aufgeklappt, aeltere zugeklappt

**Technische Hinweise**:
- `AnimatedVisibility(visible = isExpanded)` fuer Content
- Sticky Headers via `stickyHeader {}` in LazyColumn (optional, je nach Design)
- Gruppierung: `groupBy { it.monthKey }` auf der Entity-Liste

---

### P1-E06-S05: iOS SessionDetailSheet

**Als** Nutzer
**moechte ich** eine Session antippen und deren Details bearbeiten koennen,
**damit** ich Fehler korrigieren oder nachtraeglich Zeiten anpassen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E05-S05 (ViewModel fuer Speichern/Loeschen), P1-E01-S03 (Theme)
**Parallelisierbar mit**: P1-E06-S06 (Android SessionDetail)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SessionDetailSheet.swift` als Half-Sheet (`.presentationDetents([.medium, .large])`):
  - Drag Handle oben
  - Titel "Session bearbeiten" + Schliessen-Button (X)
- [ ] Editierbare Felder:
  - Datum: DatePicker (Kalender-Ansicht)
  - Startzeit: DatePicker (hourAndMinute)
  - Endzeit: DatePicker (hourAndMinute)
  - Pause (Minuten): TextField mit `.keyboardType(.numberPad)`
- [ ] Live-Berechnung:
  - Brutto-Dauer: `Endzeit - Startzeit` (read-only, automatisch)
  - Netto-Dauer: `Brutto - Pausenminuten` (read-only, automatisch)
- [ ] Validierung:
  - Given Endzeit wird vor Startzeit gesetzt
  - Then erscheint Inline-Fehler "Endzeit muss nach Startzeit liegen"
  - And "Speichern"-Button ist deaktiviert
  - Given Dauer > 24h
  - Then Warnung "Dauer ueber 24 Stunden"
- [ ] Buttons:
  - "Speichern" (Primary): Ruft ViewModel.updateSession() auf, schliesst Sheet
  - "Loeschen" (Destructive, rot): Bestaetigung via Alert, dann ViewModel.deleteSession()
  - "Abbrechen" (Text): Sheet schliessen ohne Aenderungen
- [ ] Undo nach Loeschen:
  - Snackbar/Toast "Session geloescht" mit "Rueckgaengig"-Aktion (5 Sekunden)

**Technische Hinweise**:
- `.sheet(item: $selectedSession)` am List-Container
- Bearbeitungs-State mit `@State` Kopien der Felder (nicht direkt am Model)
- Loeschen-Bestaetigung: `.confirmationDialog("Session loeschen?")`

---

### P1-E06-S06: Android SessionDetailSheet

**Als** Nutzer
**moechte ich** eine Session antippen und deren Details bearbeiten koennen,
**damit** ich Fehler korrigieren kann.

**Plattform**: Android
**Abhaengigkeiten**: P1-E05-S06 (ViewModel), P1-E01-S04 (Theme)
**Parallelisierbar mit**: P1-E06-S05 (iOS SessionDetail)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SessionDetailSheet.kt` als `ModalBottomSheet`:
  - DragHandle oben
  - Titel + Close-IconButton
- [ ] Editierbare Felder:
  - Datum: OutlinedTextField + DatePickerDialog (Tap oeffnet Dialog)
  - Startzeit: OutlinedTextField + TimePickerDialog
  - Endzeit: OutlinedTextField + TimePickerDialog
  - Pause: OutlinedTextField mit `KeyboardType.Number`
- [ ] Live-Berechnung von Brutto und Netto (analog iOS)
- [ ] Validierung analog iOS (Endzeit > Startzeit, Max 24h)
- [ ] Buttons: "Speichern" (FilledButton), "Loeschen" (TextButton, Error-Color), "Abbrechen" (TextButton)
- [ ] Loeschen-Bestaetigung via AlertDialog
- [ ] Undo-Snackbar nach Loeschen (SnackbarHost, 5s)

**Technische Hinweise**:
- `rememberModalBottomSheetState()` fuer Sheet-State
- `derivedStateOf` fuer Live-Berechnung von Brutto/Netto
- `SnackbarHostState` fuer Undo-Funktionalitaet

---

### P1-E06-S07: iOS Zeiten-Screen (Zusammenbau)

**Als** Nutzer
**moechte ich** den kompletten Zeiten-Tab sehen (Active Card + History),
**damit** ich alle Zeiterfassungs-Funktionen an einem Ort habe.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E05-S03 (ActiveSessionCard), P1-E06-S03 (MonthGroupSection), P1-E06-S05 (DetailSheet), P1-E05-S05 (ViewModel)
**Parallelisierbar mit**: P1-E06-S08 (Android Zeiten-Screen)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `TimeTrackingView.swift`:
  - NavigationStack mit Large Title "Zeiten"
  - Toolbar: Sync-Button rechts (Placeholder, Funktion kommt in E07/E10)
  - Content (ScrollView oder List):
    1. ActiveSessionCard oben
    2. "History" Abschnitts-Header
    3. MonthGroupSections fuer jeden Monat
  - Pull-to-Refresh (`.refreshable {}` -- Sync-Logik kommt in E07)
- [ ] Sessions werden via `@Query` aus SwiftData geladen:
  - Sortierung: `date` descending
  - Gruppierung nach Monat im View
- [ ] Aktueller Monat scrollt automatisch in den sichtbaren Bereich
- [ ] Leerer State: "Noch keine Eintraege. Starten Sie Ihre erste Arbeitssitzung!"
- [ ] Session-Tap oeffnet SessionDetailSheet

**Technische Hinweise**:
- `@Query(sort: \WorkSession.date, order: .reverse) private var sessions: [WorkSession]`
- Gruppierung: `Dictionary(grouping: sessions.filter { $0.isFinished })` nach Monat
- ActiveSession: `sessions.first { !$0.isFinished }` oder via ViewModel

---

### P1-E06-S08: Android Zeiten-Screen (Zusammenbau)

**Als** Nutzer
**moechte ich** den kompletten Zeiten-Tab sehen,
**damit** ich alle Zeiterfassungs-Funktionen an einem Ort habe.

**Plattform**: Android
**Abhaengigkeiten**: P1-E05-S04 (ActiveSessionCard), P1-E06-S04 (MonthGroup), P1-E06-S06 (DetailSheet), P1-E05-S06 (ViewModel)
**Parallelisierbar mit**: P1-E06-S07 (iOS Zeiten-Screen)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `TimeTrackingScreen.kt` als Composable:
  - TopAppBar mit "Zeiten" Titel
  - Sync-IconButton in TopAppBar (Placeholder)
  - LazyColumn mit:
    1. `item { ActiveSessionCard(...) }`
    2. MonthGroup-Items fuer jeden Monat
  - PullToRefresh (Material 3 `pullToRefresh` Modifier)
- [ ] Sessions aus Room DAO via Flow (`.collectAsState()`)
- [ ] Gruppierung nach `monthKey`
- [ ] Leerer State: Text + Icon (Material 3 Empty State Pattern)
- [ ] Session-Tap oeffnet SessionDetailSheet

**Technische Hinweise**:
- `val sessions by viewModel.allSessions.collectAsState(initial = emptyList())`
- `val grouped = sessions.filter { it.isFinished }.groupBy { it.monthKey }`
- LazyColumn: kein nested LazyColumn! MonthGroup muss `items()` direkt nutzen

---

### P1-E06-S09: Manuelle Session-Erfassung (Beide Plattformen)

**Als** Nutzer
**moechte ich** Arbeitssitzungen manuell nacherfassen koennen,
**damit** ich vergessene oder nicht automatisch gestartete Zeiten nachtragen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E06-S05/S06 (SessionDetailSheet), P1-E05-S05/S06 (ViewModel)
**Parallelisierbar mit**: P1-E06-S07/S08 (Zeiten-Screen)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] "+"-Button im History-Bereich oder in der ActiveSessionCard (wenn idle/kein Timer laeuft)
  - iOS: Toolbar-Button oder FAB-aehnlicher Button
  - Android: FloatingActionButton oder IconButton in TopAppBar
- [ ] Tap auf "+" oeffnet SessionDetailSheet im **Create-Modus** (statt Edit-Modus):
  - Titel "Neue Session" statt "Session bearbeiten"
  - Alle Felder leer bzw. mit sinnvollen Defaults:
    - Datum: heute
    - Startzeit: leer (Pflichtfeld)
    - Endzeit: leer (Pflichtfeld)
    - Pause: 0 Minuten
- [ ] Nutzer kann Datum, Start, Ende, Pause manuell eingeben
- [ ] Validierung analog Edit-Modus:
  - Endzeit muss nach Startzeit liegen
  - Dauer darf 24h nicht ueberschreiten
  - Start- und Endzeit sind Pflichtfelder
- [ ] "Speichern" erstellt neue WorkSession:
  - `id`: neue UUID
  - `isFinished = true`
  - `isPendingSync = true`
  - `isSynced = false`
- [ ] Session wird als pending gespeichert und beim naechsten Sync hochgeladen
- [ ] Given Nutzer hat gestern vergessen zu tracken
  - When Nutzer tippt "+" und gibt Datum gestern, 08:00-17:00, 30min Pause ein
  - Then erscheint neue Session in der History unter dem korrekten Monat
  - And Session wird beim naechsten Sync zum Backend hochgeladen

**Technische Hinweise**:
- SessionDetailSheet bekommt einen `mode: .create | .edit` Parameter
- Im Create-Modus: "Speichern" ruft `viewModel.createSession(...)` statt `updateSession(...)` auf
- ViewModel.createSession() erstellt WorkSession, speichert lokal, triggert Sync
