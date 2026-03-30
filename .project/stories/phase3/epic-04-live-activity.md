# EPIC 04: iOS Live Activity & Dynamic Island

## Ziel

Waehrend einer laufenden Arbeitssitzung zeigt eine Live Activity den Timer-Status auf dem Sperrbildschirm und in der Dynamic Island (iPhone 14 Pro+). Der Nutzer sieht seine Arbeitszeit ohne die App zu oeffnen. Bei Timer-Stop wird die Live Activity automatisch beendet.

## Abhaengigkeiten

- **Phase 2 abgeschlossen**: Timer-Logik muss stehen
- **P3-E03-S01**: App Group Setup wird geteilt (gleiche SharedDefaults)
- **Keine Backend-Aenderungen**: Rein client-seitig

## Design-Entscheidung

Live Activity und Widget teilen sich die App Group und SharedDefaults, aber sind technisch getrennte Features:
- Widget = statische Snapshots mit Timeline
- Live Activity = live-aktualisierte Darstellung mit Push oder lokalen Updates

---

## Stories

### P3-E04-S01: ActivityAttributes & Live Activity Setup

**Als** Entwickler
**moechte ich** die Live Activity Infrastruktur einrichten,
**damit** Timer-Sessions als Live Activity dargestellt werden koennen.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 (Timer), P3-E03-S01 (App Group, falls schon erledigt)
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `WorkSessionAttributes.swift` definiert:
  - `ActivityAttributes`: startTime (Date)
  - `ContentState`: isRunning (Bool), isPaused (Bool), elapsedSeconds (Int), pauseMinutes (Int)
- [ ] `Info.plist`: `NSSupportsLiveActivities = YES`
- [ ] Live Activity kann programmatisch gestartet und gestoppt werden
- [ ] Given der Timer startet
  When `Activity.request()` aufgerufen wird
  Then erscheint eine Live Activity auf dem Sperrbildschirm

**Technische Hinweise**:
- `import ActivityKit`
- `ActivityAttributes` Struct mit Codable `ContentState`
- `Activity<WorkSessionAttributes>.request(attributes:content:)` zum Starten
- `activity.update(ActivityContent(state:staleDate:))` zum Aktualisieren
- `activity.end(ActivityContent(state:staleDate:), dismissalPolicy:)` zum Beenden

---

### P3-E04-S02: Lock Screen Live Activity UI

**Als** Nutzer
**moechte ich** meinen laufenden Timer auf dem Sperrbildschirm sehen,
**damit** ich meine Arbeitszeit ohne Entsperren des iPhones pruefen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E04-S01
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E05-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Lock Screen Banner zeigt:
  - Fakturus Track Icon (links)
  - "Arbeitszeit" Label
  - Live-Timer: "03:42:18" (aktualisiert sich jede Sekunde via `Text(timerInterval:)`)
  - Status: "Seit 08:30" (Startzeit)
  - Bei Pause: "Pausiert" + Pausendauer
- [ ] Expanded View (Long Press auf Live Activity):
  - Timer gross zentriert
  - Startzeit, Pausendauer, Netto-Arbeitszeit
  - "In App oeffnen" Link
- [ ] Dark Mode kompatibel
- [ ] Given der Timer laeuft
  When der Nutzer den Sperrbildschirm sieht
  Then zeigt die Live Activity den aktuellen Timer-Stand

**Technische Hinweise**:
- `ActivityConfiguration(for: WorkSessionAttributes.self)` in Widget-Bundle
- Lock Screen: Standard Layout mit `DynamicIslandExpandedRegion`
- `Text(timerInterval: startDate...Date.distantFuture, countsDown: false)` fuer Live-Timer
- Kompakt halten: Lock Screen hat begrenzten Platz

---

### P3-E04-S03: Dynamic Island Integration

**Als** Nutzer mit iPhone 14 Pro oder neuer
**moechte ich** meinen Timer in der Dynamic Island sehen,
**damit** ich in jeder App meinen Arbeitszeitstatus sehe.

**Plattform**: iOS (iPhone 14 Pro+)
**Abhaengigkeiten**: P3-E04-S01
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E05-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Compact View** (Minimal-Darstellung in der Dynamic Island):
  - Leading: Fakturus-Icon (klein)
  - Trailing: Timer "03:42" oder Pause-Icon
- [ ] **Expanded View** (nach Tap auf Dynamic Island):
  - Timer gross zentriert
  - "Seit 08:30" Startzeit
  - Pause-Info falls pausiert
- [ ] **Minimal View** (wenn andere Live Activities aktiv):
  - Nur Timer "03:42" oder Punkt-Indikator
- [ ] Tap auf Dynamic Island oeffnet die App
- [ ] Given der Timer laeuft auf einem iPhone 14 Pro
  When der Nutzer eine andere App benutzt
  Then zeigt die Dynamic Island den Timer kompakt an
- [ ] Given der Nutzer tippt auf die Dynamic Island
  Then zeigt sich die erweiterte Ansicht

**Technische Hinweise**:
- Alle drei Views sind Pflicht: `compactLeading`, `compactTrailing`, `expanded`, `minimal`
- `DynamicIslandExpandedRegion(.leading/.trailing/.center/.bottom)` fuer Layout
- Timer: `Text(timerInterval:)` funktioniert auch in Dynamic Island
- Graceful Degradation: Auf iPhones ohne Dynamic Island nur Lock Screen Live Activity

---

### P3-E04-S04: Live Activity Lifecycle-Management

**Als** Entwickler
**moechte ich** den Lifecycle der Live Activity zuverlaessig steuern,
**damit** keine verwaisten Live Activities entstehen und der Nutzer korrekte Informationen sieht.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E04-S01, P3-E04-S02, P3-E04-S03
**Parallelisierbar mit**: P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Live Activity wird gestartet wenn: Timer startet (Start-Button oder Widget-Action)
- [ ] Live Activity wird aktualisiert wenn: Pause/Resume, State-Aenderungen
- [ ] Live Activity wird beendet wenn:
  - Timer gestoppt wird
  - Session mit "Fertig" abgeschlossen wird
  - App laenger als 12 Stunden inaktiv (System-Limit)
- [ ] Bei App-Start: Pruefen ob verwaiste Live Activity existiert und ggf. bereinigen
- [ ] Given der Timer wird auf dem iPhone gestoppt
  When die Live Activity noch sichtbar ist
  Then wird sie beendet und verschwindet vom Sperrbildschirm/Dynamic Island
- [ ] Given die App wird nach 12h Inaktivitaet geoeffnet
  When noch eine alte Live Activity existiert
  Then wird sie bereinigt

**Technische Hinweise**:
- `Activity<WorkSessionAttributes>.activities` zum Auflisten aktiver Activities
- Max. 1 Live Activity gleichzeitig (fuer Timer gibt es immer nur eine aktive Session)
- `activity.end(dismissalPolicy: .immediate)` bei Stop
- `activity.end(dismissalPolicy: .after(.now + 60))` bei Finish (bleibt 1 Min sichtbar)
- Bei App-Start: Bereinigung via `for activity in Activity<WorkSessionAttributes>.activities { activity.end(...) }`
