# EPIC 05: Zeiterfassungs-UI (Timer-Screen)

## Ziel

Der Hauptscreen der App: Nutzer koennen mit einem Tap eine Arbeitssitzung starten, den Live-Timer sehen und die Session stoppen/beenden. Dies ist der Screen, den Nutzer taeglich als erstes sehen und am haeufigsten nutzen.

## Abhaengigkeiten

- **E03**: Lokale Datenschicht (WorkSession Model/Entity fuer Persistierung)
- **E09**: App-Shell (Tab-Navigation fuer Einbettung) -- kann aber unabhaengig als Standalone entwickelt werden

**Hinweis**: Die UI kann mit lokalen Daten (ohne API/Sync) vollstaendig entwickelt und getestet werden. API-Integration erfolgt spaeter durch E07 (Sync).

---

## Stories

### P1-E05-S01: iOS TimerDisplay-Komponente

**Als** Nutzer
**moechte ich** einen animierten Timer sehen,
**damit** ich weiss wie lange ich bereits arbeite.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S03 (Theme/Extensions)
**Parallelisierbar mit**: P1-E05-S02 (Android Timer), alle E02/E03/E04 Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `TimerDisplay.swift` als wiederverwendbare SwiftUI-Komponente:
  - Zeigt verstrichene Zeit als `HH:MM:SS` an
  - Aktualisierung jede Sekunde (via `TimelineView(.periodic(from:, by: 1))`)
  - Monospaced Font (`.monospacedDigit()`)
  - Pulsierender gruener Punkt (2s Zyklus) neben dem Timer wenn laufend
- [ ] Groessen-Varianten via Enum:
  - `.large` (48pt) fuer ActiveSessionCard
  - `.medium` (28pt) fuer spaetere Widget-Nutzung
  - `.small` (17pt) fuer Inline-Nutzung
- [ ] Props: `startTime: Date`, `isRunning: Bool`, `size: TimerSize`
- [ ] Timer stoppt visuell wenn `isRunning == false` (zeigt letzte Dauer)
- [ ] Given Timer wird mit `startTime = vor 1 Stunde` initialisiert und `isRunning = true`
  When 1 Sekunde vergeht
  Then zeigt der Timer "01:00:01" an

**Technische Hinweise**:
- `TimelineView` ist performanter als Timer+State fuer sekundengenaue Updates
- Kein `Timer.publish` noetig -- SwiftUI `TimelineView` ist der native Weg
- Animation fuer pulsierenden Punkt: `.animation(.easeInOut(duration: 1).repeatForever())`

---

### P1-E05-S02: Android TimerDisplay-Komponente

**Als** Nutzer
**moechte ich** einen animierten Timer sehen,
**damit** ich weiss wie lange ich bereits arbeite.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S04 (Theme)
**Parallelisierbar mit**: P1-E05-S01 (iOS Timer), alle E02/E03/E04 Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `TimerDisplay.kt` als Composable:
  - Zeigt verstrichene Zeit als `HH:MM:SS` an
  - Aktualisierung jede Sekunde (via `LaunchedEffect` + `delay(1000)`)
  - `FontFamily.Monospace` fuer gleichbreite Ziffern
  - Pulsierender gruener Punkt (animateFloatAsState, infinite)
- [ ] Groessen-Varianten: `TimerSize.LARGE` (48sp), `MEDIUM` (28sp), `SMALL` (16sp)
- [ ] Parameter: `startTime: Instant`, `isRunning: Boolean`, `size: TimerSize`
- [ ] Timer stoppt visuell wenn `isRunning == false`

**Technische Hinweise**:
- `remember { mutableLongStateOf(0L) }` fuer elapsed time
- `LaunchedEffect(isRunning) { while(isRunning) { delay(1000); elapsed++ } }`
- Pulse-Animation: `rememberInfiniteTransition().animateFloat()`

---

### P1-E05-S03: iOS ActiveSessionCard

**Als** Nutzer
**moechte ich** eine prominente Karte sehen die meine aktuelle Arbeitssitzung zeigt,
**damit** ich mit einem Blick meinen Arbeitsstatus erkenne.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E05-S01 (TimerDisplay), P1-E03-S01 (WorkSession Model)
**Parallelisierbar mit**: P1-E05-S04 (Android ActiveSessionCard)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `ActiveSessionCard.swift` implementiert mit 3 Zustaenden:

  **Idle State** (keine aktive Session):
  - [ ] Text "Bereit fuer den naechsten Eintrag"
  - [ ] Grosser "Starten" Button (Primary Color, prominent)
  - [ ] Tap auf "Starten":
    - Given keine aktive Session existiert
    - When Nutzer auf "Starten" tippt
    - Then wird eine neue WorkSession erstellt mit `startTime = Date()`, `date = heute`, `isPendingSync = true`
    - And der Timer startet

  **Running State** (Session laeuft):
  - [ ] Gruener pulsierender Indikator + Text "Laufende Sitzung"
  - [ ] TimerDisplay (.large) mit laufender Zeit
  - [ ] Anzeige: Startzeit (z.B. "Start: 08:30"), Datum
  - [ ] Pause-Anzeige: "Pause: X min" (wenn Pausen > 0, in `pause`-Farbe)
  - [ ] 3 Buttons:
    - "Pause" (Tonal) -- wird in E08 implementiert, hier Placeholder
    - "Stop" (Tonal): Setzt `stopTime = Date()`, Timer haelt an
    - "Fertig" (Primary): Schliesst Session ab (`isFinished = true`)

  **Stopped State** (gestoppt, nicht abgeschlossen):
  - [ ] Orangener Indikator + "Gestoppt"
  - [ ] Statische Dauer-Anzeige: Brutto, Netto
  - [ ] Editierbare Felder: Startzeit, Endzeit (via TimePicker), Datum (via DatePicker)
  - [ ] Pausenfeld (Minuten, numerisch) -- wird in E08 erweitert
  - [ ] 2 Buttons:
    - "Fertig" (Primary): Session abschliessen
    - "Verwerfen" (Text, destructive): Session loeschen mit Bestaetigung

- [ ] Card-Design: Abgerundete Ecken, leichter Schatten, Padding
- [ ] Haptic Feedback bei Start/Stop/Finish (`.impact(.medium)`)

**Technische Hinweise**:
- Props: `session: WorkSession?`, Closures fuer `onStart`, `onStop`, `onFinish`, `onSave`, `onDelete`
- DatePicker: `.datePickerStyle(.compact)` oder Sheet
- TimePicker: `DatePicker("", selection:, displayedComponents: .hourAndMinute)`
- Pause-Button ist in diesem EPIC ein Placeholder (deaktiviert oder versteckt), wird in E08 aktiviert

---

### P1-E05-S04: Android ActiveSessionCard

**Als** Nutzer
**moechte ich** eine prominente Karte sehen die meine aktuelle Arbeitssitzung zeigt,
**damit** ich mit einem Blick meinen Arbeitsstatus erkenne.

**Plattform**: Android
**Abhaengigkeiten**: P1-E05-S02 (TimerDisplay), P1-E03-S02 (WorkSession Entity)
**Parallelisierbar mit**: P1-E05-S03 (iOS ActiveSessionCard)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `ActiveSessionCard.kt` als Composable implementiert
- [ ] Gleiche 3 Zustaende wie iOS (Idle, Running, Stopped)
- [ ] Idle: "Bereit fuer den naechsten Eintrag" + grosser FilledButton "Starten"
- [ ] Running: Gruener Punkt + TimerDisplay + Stop/Fertig Buttons
- [ ] Stopped: Orangener Punkt + statische Dauer + editierbare Felder + Fertig/Verwerfen
- [ ] Material 3 Card mit Elevation
- [ ] Haptic Feedback: `LocalHapticFeedback.current.performHapticFeedback()`
- [ ] DatePicker: `DatePickerDialog` (Material 3)
- [ ] TimePicker: `TimePickerDialog` (Material 3)
- [ ] Pause-Button als Placeholder (E08)

**Technische Hinweise**:
- Material 3 `ElevatedCard` oder `Card`
- `rememberDatePickerState()`, `rememberTimePickerState()`
- Button-Reihe: `Row(horizontalArrangement = Arrangement.spacedBy(8.dp))`

---

### P1-E05-S05: iOS TimeTrackingViewModel

**Als** Entwickler
**moechte ich** die Geschaeftslogik der Zeiterfassung vom UI getrennt haben,
**damit** die View schlank bleibt und die Logik testbar ist.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E03-S01 (SwiftData Models)
**Parallelisierbar mit**: P1-E05-S06 (Android ViewModel), P1-E05-S01/S03 (UI-Komponenten)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `TimeTrackingViewModel.swift` als `@Observable` Klasse:
  - `activeSession: WorkSession?` -- aktuell laufende/gestoppte Session
  - `allSessions: [WorkSession]` -- alle Sessions (via @Query in View, nicht im VM)
  - `isLoading: Bool`
  - `error: String?`
- [ ] `startSession()` Methode:
  - Erstellt neue WorkSession mit `startTime = Date()`, `date = Date()`, `isPendingSync = true`
  - Speichert in SwiftData
  - Setzt `activeSession`
- [ ] `stopSession()` Methode:
  - Setzt `activeSession.stopTime = Date()`
  - Aktualisiert lokale DB
- [ ] `finishSession()` Methode:
  - Setzt `activeSession.isFinished = true`
  - Markiert als `isPendingSync = true`
  - Setzt `activeSession = nil`
  - Triggert Sync (wenn SyncEngine verfuegbar -- spaeter in E07)
- [ ] `updateSession(date:, startTime:, stopTime:)` Methode:
  - Validierung: `stopTime > startTime`, maximal 24h Dauer
  - Aktualisiert lokale DB
  - Markiert als `isPendingSync = true`
- [ ] `deleteSession(session:)` Methode:
  - Loescht aus lokaler DB
  - (Sync-Handling kommt in E07)
- [ ] Beim App-Start: Pruefen ob eine nicht-abgeschlossene Session existiert und als `activeSession` setzen

**Technische Hinweise**:
- ModelContext wird via init injiziert oder via Environment
- Kein UseCase-Pattern (ADR-006) -- Logik direkt im ViewModel
- `activeSession` ist die Session mit `isFinished == false` (maximal eine)

---

### P1-E05-S06: Android TimeTrackingViewModel

**Als** Entwickler
**moechte ich** die Geschaeftslogik der Zeiterfassung vom UI getrennt haben,
**damit** die View schlank bleibt und die Logik testbar ist.

**Plattform**: Android
**Abhaengigkeiten**: P1-E03-S02 (Room Entities/DAOs)
**Parallelisierbar mit**: P1-E05-S05 (iOS ViewModel), P1-E05-S02/S04 (UI-Komponenten)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `TimeTrackingViewModel.kt` Klasse:
  - `activeSession: StateFlow<WorkSessionEntity?>` -- aktuell laufende Session
  - `allSessions: Flow<List<WorkSessionEntity>>` -- aus Room DAO
  - `isLoading: StateFlow<Boolean>`
  - `error: StateFlow<String?>`
- [ ] `startSession()` suspend:
  - Erstellt neue WorkSessionEntity, speichert via DAO
  - Aktualisiert `_activeSession`
- [ ] `stopSession()` suspend:
  - Setzt stopTime, update via DAO
- [ ] `finishSession()` suspend:
  - Setzt isFinished = true, isPendingSync = true
  - Setzt activeSession = null
- [ ] `updateSession(entity, date, startTime, stopTime)` suspend:
  - Validierung analog zu iOS
  - Update via DAO
- [ ] `deleteSession(entity)` suspend
- [ ] Beim Start: aktive Session aus DB laden
- [ ] Coroutine-Scope: `viewModelScope` (oder manueller CoroutineScope im ServiceContainer)

**Technische Hinweise**:
- Kein ViewModel-Klasse von AndroidX (kein Hilt) -- einfache Klasse mit CoroutineScope
- Alternativ: ViewModel mit `ViewModelProvider.Factory` falls Lifecycle-Handling gewuenscht
- Room Flow fuer `allSessions` sorgt fuer automatische UI-Updates
