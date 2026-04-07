# EPIC 08: Pausenerfassung

## Ziel

ArbZG-konforme Pausenerfassung: Nutzer koennen waehrend einer laufenden Session Pausen starten/stoppen oder nachtraeglich manuell Pausenminuten eingeben. Die Netto-Arbeitszeit wird automatisch berechnet. Bei Ueberschreitung der ArbZG-Schwellen (6h/9h) erscheint ein dezenter Hinweis.

## Abhaengigkeiten

- **E05**: Timer-Screen (ActiveSessionCard muss um Pause-State erweitert werden)
- **E07**: Sync-Engine (PauseMinutes muessen synchronisiert werden)

## Voraussetzung (Backend)

> **Backend muss PauseMinutes-Feld in WorkSession unterstuetzen (Schema-Migration + API-Anpassung). Dies ist eine Backend-Aenderung die VOR Beginn von EPIC 08 deployed sein muss.**

### P1-E08-S00: Backend -- PauseMinutes-Feld hinzufuegen (Prerequisite)

**Als** Entwickler
**moechte ich** dass das Backend ein PauseMinutes-Feld in WorkSession unterstuetzt,
**damit** die nativen Apps Pausenminuten synchronisieren koennen.

**Plattform**: Backend
**Abhaengigkeiten**: Keine
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] PostgreSQL-Migration: `PauseMinutes INT DEFAULT 0` Spalte in `WorkSessions` Tabelle
- [ ] API: POST/PUT /v1/work-sessions akzeptiert `PauseMinutes` Feld
- [ ] API: GET /v1/work-sessions liefert `PauseMinutes` Feld zurueck
- [ ] API: POST /v1/work-sessions/sync verarbeitet `PauseMinutes` korrekt
- [ ] Bestehende Sessions erhalten `PauseMinutes = 0` als Default
- [ ] Deployed auf Produktion bevor EPIC 08 Stories beginnen

## Hintergrund (gesetzlich)

- **ArbZG ss 4**: Bei mehr als 6h Arbeit: mindestens 30min Pause. Bei mehr als 9h: mindestens 45min.
- Pausenerfassung ist gesetzliche Pflicht -- daher im FREE-Tier, nicht hinter Paywall.
- 7 von 11 Wettbewerbern bieten Pausenerfassung.

---

## Stories

### P1-E08-S01: iOS Pause-State im ViewModel

**Als** Nutzer
**moechte ich** waehrend meiner Arbeit Pausen erfassen koennen,
**damit** meine Netto-Arbeitszeit korrekt berechnet wird.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E05-S05 (TimeTrackingViewModel)
**Parallelisierbar mit**: P1-E08-S02 (Android Pause-ViewModel)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] ViewModel-Erweiterung um Pause-State:
  - `isPaused: Bool` -- ob aktuell pausiert wird
  - `currentPauseStart: Date?` -- wann die aktuelle Pause begonnen hat
  - `accumulatedPauseMinutes: Int` -- bisherige Pausenminuten (laufende Pause exklusive)
- [ ] `pauseSession()` Methode:
  - Given Session laeuft (isRunning)
  - When Pause gestartet wird
  - Then `isPaused = true`, `currentPauseStart = Date()`
  - And der Arbeits-Timer wird visuell angehalten
- [ ] `resumeSession()` Methode:
  - Given Session ist pausiert
  - When Pause beendet wird
  - Then Pausendauer berechnen: `Date() - currentPauseStart` (aufgerundet auf volle Minuten)
  - And `accumulatedPauseMinutes += berechnete Minuten`
  - And `activeSession.pauseMinutes = accumulatedPauseMinutes`
  - And `isPaused = false`, `currentPauseStart = nil`
  - And Arbeits-Timer laeuft weiter
- [ ] `finishSession()` Erweiterung:
  - Wenn noch pausiert -> Pause automatisch beenden und aufaddieren
  - `pauseMinutes` korrekt in der finalen Session setzen
- [ ] Mehrere Pausen pro Session:
  - Given Nutzer macht 2 Pausen (20min + 15min)
  - When Session beendet wird
  - Then `pauseMinutes == 35`
- [ ] Netto-Berechnung:
  - `netDuration = (stopTime - startTime) - (pauseMinutes * 60)` Sekunden
  - Given Start 08:00, Stop 17:00, Pause 45min
  - Then Brutto = 9:00h, Netto = 8:15h

**Technische Hinweise**:
- `pauseMinutes` wird in der DB gespeichert (WorkSession Model)
- `currentPauseStart` wird in UserDefaults (iOS) / SharedPreferences (Android) persistiert
- Bei App-Start: Pruefen ob `currentPauseStart` gesetzt ist -> Pause-State wiederherstellen
- Rundung der Pausenminuten: `ceil()` auf volle Minuten

Zusaetzliches Akzeptanzkriterium fuer Crash-Recovery:
- [ ] `currentPauseStart` in UserDefaults (iOS) / SharedPreferences (Android) speichern:
  - Bei `pauseSession()`: `UserDefaults.standard.set(Date(), forKey: "currentPauseStart")` (iOS)
  - Bei `resumeSession()` / `finishSession()`: Key entfernen
  - Bei App-Start: Pruefen ob Key existiert -> wenn ja, Pause-State wiederherstellen mit gespeichertem Zeitpunkt
- [ ] **Crash-Recovery**: Wenn App waehrend Pause gekillt wird und neu gestartet wird, ist die Pause noch aktiv
  - Given Nutzer startet Pause um 12:00, App wird um 12:15 gekillt
  - When App um 12:30 neu gestartet wird
  - Then ist die Pause noch aktiv mit korrekt laufendem Pause-Timer (seit 12:00)

---

### P1-E08-S02: Android Pause-State im ViewModel

**Als** Nutzer
**moechte ich** waehrend meiner Arbeit Pausen erfassen koennen,
**damit** meine Netto-Arbeitszeit korrekt berechnet wird.

**Plattform**: Android
**Abhaengigkeiten**: P1-E05-S06 (TimeTrackingViewModel)
**Parallelisierbar mit**: P1-E08-S01 (iOS Pause-ViewModel)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] ViewModel-Erweiterung analog iOS:
  - `isPaused: StateFlow<Boolean>`
  - `currentPauseStart: Instant?` (in-memory)
  - `accumulatedPauseMinutes: Int`
- [ ] `pauseSession()`, `resumeSession()`, erweiterte `finishSession()` -- analog iOS
- [ ] Mehrere Pausen, korrekte Netto-Berechnung -- analog iOS

**Technische Hinweise**:
- `Duration.between(currentPauseStart, Instant.now()).toMinutes()` fuer Pausendauer
- `kotlin.math.ceil()` fuer Rundung
- `currentPauseStart` in SharedPreferences persistieren (analog iOS UserDefaults)
- Bei App-Start: Pruefen ob SharedPreferences Key existiert -> Pause-State wiederherstellen
- [ ] **Crash-Recovery**: Wenn App waehrend Pause gekillt wird und neu gestartet wird, ist die Pause noch aktiv

---

### P1-E08-S03: iOS ActiveSessionCard -- Pause-UI

**Als** Nutzer
**moechte ich** auf der Active-Session-Karte Pausen starten und beenden koennen,
**damit** ich meine Pausen unkompliziert erfassen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E08-S01 (Pause-ViewModel), P1-E05-S03 (ActiveSessionCard)
**Parallelisierbar mit**: P1-E08-S04 (Android Pause-UI)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Neuer Zustand "Paused"** in der ActiveSessionCard:
  - Gelber pulsierender Indikator + Text "Pausiert"
  - Pause-Timer anzeigen (wie lange die aktuelle Pause dauert)
  - Bisherige Gesamtpause anzeigen: "Pause bisher: X min"
  - Buttons: "Weiter" (Primary) + "Fertig" (Primary)

- [ ] **Running-State Erweiterung**:
  - Pause-Button aktivieren (war bisher Placeholder)
  - "Pause" Button (Tonal, gelb/orange): Tap -> `viewModel.pauseSession()`
  - Pausenanzeige "Pause: X min" nur wenn `pauseMinutes > 0`

- [ ] **Stopped-State Erweiterung**:
  - Pause-Feld (TextField, Minuten) editierbar
  - Netto-Dauer wird live neu berechnet bei Pause-Aenderung

- [ ] Visueller Flow:
  - Given Session laeuft (Running)
  - When Nutzer tippt "Pause"
  - Then Card wechselt zu Paused-State (gelber Indikator, Pause-Timer)
  - When Nutzer tippt "Weiter"
  - Then Card wechselt zurueck zu Running-State
  - And Pausendauer wurde aufaddiert

- [ ] Haptic Feedback bei Pause/Resume

**Technische Hinweise**:
- Neuer Pause-Timer: `TimerDisplay(startTime: currentPauseStart, isRunning: isPaused, size: .medium)`
- Gelbe Farbe: `timer-paused` aus Assets
- Uebergangsanimation zwischen Zustaenden: `.animation(.spring())`

---

### P1-E08-S04: Android ActiveSessionCard -- Pause-UI

**Als** Nutzer
**moechte ich** auf der Active-Session-Karte Pausen starten und beenden koennen,
**damit** ich meine Pausen unkompliziert erfassen kann.

**Plattform**: Android
**Abhaengigkeiten**: P1-E08-S02 (Pause-ViewModel), P1-E05-S04 (ActiveSessionCard)
**Parallelisierbar mit**: P1-E08-S03 (iOS Pause-UI)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Neuer "Paused"-Zustand analog iOS:
  - Gelber Indikator + "Pausiert"
  - Pause-Timer, bisherige Gesamtpause
  - "Weiter" + "Fertig" Buttons
- [ ] Running-State: Pause-Button aktiviert, Pausenanzeige bei > 0
- [ ] Stopped-State: Pause-Feld editierbar, Netto-Berechnung live
- [ ] Haptic Feedback
- [ ] Uebergangsanimation: `AnimatedContent` oder `Crossfade`

**Technische Hinweise**:
- Material 3 Color `TimerPaused` fuer Indikator
- `AnimatedContent(targetState = sessionState)` fuer State-Transitions

---

### P1-E08-S05: iOS ArbZG-Pausenhinweise

**Als** Nutzer
**moechte ich** bei langen Arbeitszeiten an meine Pausenpflicht erinnert werden,
**damit** ich gesetzeskonform arbeite.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E08-S01 (Pause-ViewModel)
**Parallelisierbar mit**: P1-E08-S06 (Android Hinweise)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] **6-Stunden-Hinweis**:
  - Given Session laeuft seit 6 Stunden UND bisherige Pausen < 30 Minuten
  - When die 6-Stunden-Schwelle erreicht wird
  - Then erscheint ein dezenter In-App-Banner:
    "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
  - And Banner kann mit "Verstanden" geschlossen werden
  - And Banner wird nur einmal pro Session angezeigt

- [ ] **9-Stunden-Hinweis**:
  - Given Session laeuft seit 9 Stunden UND bisherige Pausen < 45 Minuten
  - When die 9-Stunden-Schwelle erreicht wird
  - Then Banner: "Erinnerung: Nach 9 Stunden Arbeit betraegt die Mindestpause 45 Minuten."

- [ ] **10-Stunden-Hinweis** (Hoechstarbeitszeit):
  - Given Session laeuft seit 10 Stunden (Netto-Arbeitszeit)
  - Then Banner: "Hinweis: Sie arbeiten seit 10 Stunden. Die gesetzliche Hoechstarbeitszeit betraegt 10 Stunden."

- [ ] Hinweise sind **informativ, nicht einschraenkend** (Timer laeuft weiter)
- [ ] Jeder Hinweis wird maximal einmal pro Session angezeigt

**Technische Hinweise**:
- Netto-Arbeitszeit = Aktuelle Dauer minus Pausenminuten
- Banner als Overlay oder zwischen ActiveSessionCard und History
- Tracking via `@State` Booleans: `hasShown6hHint`, `hasShown9hHint`, `hasShown10hHint`
- Pruefung im Timer-Update-Zyklus oder via `onChange(of: timer)`

---

### P1-E08-S06: Android ArbZG-Pausenhinweise

**Als** Nutzer
**moechte ich** bei langen Arbeitszeiten an meine Pausenpflicht erinnert werden,
**damit** ich gesetzeskonform arbeite.

**Plattform**: Android
**Abhaengigkeiten**: P1-E08-S02 (Pause-ViewModel)
**Parallelisierbar mit**: P1-E08-S05 (iOS Hinweise)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Gleiche 3 Hinweis-Schwellen wie iOS (6h, 9h, 10h)
- [ ] Dezenter Banner (Material 3 Banner oder Snackbar) statt Modal
- [ ] Einmalig pro Session
- [ ] Informativ, nicht einschraenkend

**Technische Hinweise**:
- Material 3 `Banner` Composable oder custom `Card` mit Warning-Color
- State im ViewModel: `hasShown6hHint`, etc.
- Netto-Berechnung: `Duration.between(startTime, now).toMinutes() - pauseMinutes`

---

### P1-E08-S07: Pause in History-Anzeige integrieren (Beide Plattformen)

**Als** Nutzer
**moechte ich** in der History sehen wie lange meine Pausen waren,
**damit** ich meine Arbeitszeit korrekt nachvollziehen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E06-S01/S02 (SessionRow), P1-E08-S01/S02 (Pause-State)
**Parallelisierbar mit**: P1-E08-S05/S06 (ArbZG-Hinweise)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] SessionRow zeigt Pausendauer an (bereits in E06 als "P30" spezifiziert)
- [ ] MonthGroupSection: Gesamtdauer ist die Summe der NETTO-Dauern
  - Given 3 Sessions im Maerz: 8h+30min Pause, 9h+45min Pause, 8h+30min Pause
  - Then Brutto: 25h, Netto: 23:15h
  - And MonthGroup zeigt "23:15h" an
- [ ] SessionDetailSheet: Brutto- und Netto-Anzeige
  - Brutto: `Endzeit - Startzeit`
  - Netto: `Brutto - Pausen`
  - Beides wird angezeigt, klar unterschieden

**Technische Hinweise**:
- Netto-Berechnung ist bereits in den Models/Entities definiert (computed property)
- MonthGroup-Summe: `sessions.reduce(0) { $0 + $1.netDuration }`
