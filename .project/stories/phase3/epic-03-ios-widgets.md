# EPIC 03: iOS Widgets (WidgetKit)

## Ziel

Home Screen Widget fuer iOS, das den aktuellen Timer-Status und die heutige Arbeitszeit anzeigt. Quick Actions ermoeglichen Timer-Start/Stop direkt vom Widget aus. Das Widget nutzt App Groups fuer den Datenaustausch mit der Haupt-App.

## Abhaengigkeiten

- **Phase 2 abgeschlossen**: WorkSession-Daten und Timer-Logik muessen stehen
- **Keine Backend-Aenderungen**: Widget liest nur lokale Daten via App Group

---

## Stories

### P3-E03-S01: Widget Target & App Group Setup

**Als** Entwickler
**moechte ich** ein Widget Target im Xcode-Projekt einrichten,
**damit** ich WidgetKit-basierte Home Screen Widgets entwickeln kann.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 iOS-Projekt
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E04-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Widget Extension Target im Xcode-Projekt (`FakturusTrackWidget`)
- [ ] App Group konfiguriert: `group.com.fakturus.track` (geteilt mit Haupt-App und Watch)
- [ ] `SharedDefaults.swift` in geteiltem Framework: Lese-/Schreib-Zugriff auf `UserDefaults(suiteName: "group.com.fakturus.track")`
- [ ] Haupt-App schreibt aktuellen Timer-State in App Group UserDefaults bei jeder State-Aenderung
- [ ] Widget Extension kompiliert und zeigt Placeholder

**Technische Hinweise**:
- Xcode: File > New > Target > Widget Extension
- Signing: Gleiche Team-ID, Widget Extension benoetigt eigene App ID mit App Groups Capability
- `UserDefaults(suiteName: "group.com.fakturus.track")` fuer geteilten State
- `WidgetCenter.shared.reloadAllTimelines()` in Haupt-App aufrufen wenn Timer-State sich aendert

---

### P3-E03-S02: Timer-Status Widget (Small + Medium)

**Als** Nutzer
**moechte ich** meinen aktuellen Timer-Status auf dem Home Screen sehen,
**damit** ich mit einem Blick weiss ob mein Timer laeuft und wie lange ich schon arbeite.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E03-S01 (Widget Target)
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Small Widget** (2x2):
  - Idle: "Bereit" + Startzeit letzter Session oder "Keine Session heute"
  - Running: Live-Timer "03:42" + gruener Status-Punkt
  - Paused: Pausiert-Icon + Pausendauer
  - Tap oeffnet die App
- [ ] **Medium Widget** (4x2):
  - Alles vom Small Widget PLUS:
  - Heutige Gesamtarbeitszeit ("Heute: 6:30h")
  - Startzeit der aktuellen Session ("Seit 08:30")
  - Pausendauer der aktuellen Session
- [ ] Timer im Widget aktualisiert sich regelmaessig (via Timeline, nicht live -- WidgetKit-Beschraenkung)
- [ ] Dark Mode: Widget passt sich automatisch an
- [ ] Given der Timer laeuft seit 08:30
  When der Nutzer auf den Home Screen schaut
  Then zeigt das Widget den laufenden Timer und "Seit 08:30"
- [ ] Given kein Timer laeuft und heute 6:30h gearbeitet wurden
  When der Nutzer auf den Home Screen schaut
  Then zeigt das Medium Widget "Heute: 6:30h" und "Bereit"

**Technische Hinweise**:
- `TimelineProvider` mit `getTimeline(in:completion:)`:
  - Laufender Timer: Timeline-Entries alle 1 Minute (WidgetKit aktualisiert nicht haeufiger)
  - Kein Timer: Timeline mit einem Entry, aktualisiert bei naechstem State-Wechsel
- `Text(Date(), style: .timer)` fuer automatisch aktualisierenden Timer im Widget
- `Widget` mit `.supportedFamilies([.systemSmall, .systemMedium])`
- ContainerRelativeFrame fuer verschiedene Widget-Groessen

---

### P3-E03-S03: Widget Quick Actions (Interactive Widget)

**Als** Nutzer
**moechte ich** meinen Timer direkt vom Widget aus starten oder stoppen koennen,
**damit** ich die App nicht erst oeffnen muss.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E03-S02 (Widget UI)
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E05-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] iOS 17+ Interactive Widgets mit `AppIntent`:
  - **Start-Button**: Erstellt neue Session und startet Timer
  - **Stop-Button**: Stoppt den laufenden Timer
  - **Pause/Weiter-Button**: Pausiert oder setzt Timer fort
- [ ] Medium Widget zeigt Aktions-Buttons (Small Widget oeffnet nur die App)
- [ ] Aktion wird sofort in App Group geschrieben und von Haupt-App beim naechsten Oeffnen uebernommen
- [ ] Given kein Timer laeuft
  When der Nutzer im Medium Widget "Start" tippt
  Then startet der Timer
  And das Widget aktualisiert sich auf "Running" State
- [ ] Given der Timer laeuft
  When der Nutzer im Widget "Stop" tippt
  Then stoppt der Timer

**Technische Hinweise**:
- iOS 17+ `AppIntent` fuer Interactive Widgets (Button-Actions ohne App zu oeffnen)
- `@available(iOS 17, *)` Guard -- auf iOS 16 oeffnet Button die App
- `AppIntentTimelineProvider` statt einfachem `TimelineProvider`
- Intent muss in App Group schreiben UND `WidgetCenter.shared.reloadAllTimelines()` aufrufen
- Haupt-App muss bei App-Start pruefen ob Widget-Actions vorliegen (App Group lesen)
