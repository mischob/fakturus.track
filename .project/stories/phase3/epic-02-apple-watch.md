# EPIC 02: Apple Watch Companion App

## Ziel

Minimale Apple Watch App als Companion zur iOS-App. Der Nutzer kann vom Handgelenk aus seinen Timer starten, stoppen, pausieren und die heutige Arbeitszeit sehen. Kein eigenstaendiger Login auf der Watch -- die Watch nutzt die Verbindung zum iPhone (WatchConnectivity).

## Abhaengigkeiten

- **Phase 2 abgeschlossen**: Timer-Logik und Datenmodell muessen stehen
- **Keine Backend-Aenderungen**: Watch kommuniziert nur mit dem iPhone, nicht direkt mit dem Backend

## Design-Entscheidung

**WatchConnectivity** statt eigenstaendiger API-Calls:
- Watch greift NICHT direkt auf die API zu (kein eigener Auth-Token)
- Stattdessen: iPhone sendet Timer-State an Watch, Watch sendet Aktionen an iPhone
- Vorteil: Kein doppelter Auth-Flow, kein separater Sync-State
- Nachteil: Watch funktioniert nur wenn iPhone in Reichweite (akzeptabel fuer V1)

---

## Stories

### P3-E02-S01: watchOS Target & Projekt-Setup

**Als** Entwickler
**moechte ich** ein watchOS Target im bestehenden iOS-Projekt einrichten,
**damit** ich die Watch-App entwickeln kann.

**Plattform**: iOS (watchOS Target)
**Abhaengigkeiten**: Phase 2 iOS-Projekt
**Parallelisierbar mit**: P3-E01-*, P3-E03-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] watchOS Target im Xcode-Projekt angelegt (Minimum watchOS 10)
- [ ] Watch-App startet auf Apple Watch Simulator
- [ ] App-Icon fuer Watch konfiguriert (runde Icon-Variante)
- [ ] WatchConnectivity Framework eingebunden (iPhone + Watch)
- [ ] Shared Framework / App Group fuer Datenaustausch konfiguriert

**Technische Hinweise**:
- Xcode: File > New > Target > watchOS > Watch App
- WatchConnectivity: `WCSession` auf beiden Seiten aktivieren
- App Group: Gleiche Gruppe wie Widgets (`group.com.fakturus.track`)
- Minimum watchOS 10 (kompatibel mit Apple Watch Series 6+)

---

### P3-E02-S02: WatchConnectivity Manager

**Als** Entwickler
**moechte ich** einen zuverlaessigen Kommunikationskanal zwischen iPhone und Watch haben,
**damit** Timer-Aktionen und -Status bidirektional uebertragen werden.

**Plattform**: iOS + watchOS
**Abhaengigkeiten**: P3-E02-S01 (Projekt-Setup)
**Parallelisierbar mit**: P3-E01-*, P3-E03-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `WatchConnectivityManager.swift` in `Shared/` (geteilt zwischen iOS und watchOS Target)
- [ ] iPhone -> Watch: Timer-State-Updates (isRunning, isPaused, startTime, elapsedTime, pauseMinutes)
- [ ] Watch -> iPhone: Aktionen (start, stop, pause, resume, finish)
- [ ] Nutzung von `WCSession.default.transferCurrentComplicationUserInfo(_:)` fuer sofortige Updates
- [ ] Fallback auf `updateApplicationContext(_:)` wenn Complication-Updates nicht moeglich
- [ ] Given der Timer auf dem iPhone laeuft
  When die Watch-App geoeffnet wird
  Then zeigt sie den aktuellen Timer-Status korrekt an
- [ ] Given die Watch eine "Start"-Aktion sendet
  When das iPhone die Nachricht empfaengt
  Then startet der Timer auf dem iPhone

**Technische Hinweise**:
- `WCSession.default.sendMessage(_:replyHandler:errorHandler:)` fuer sofortige Aktionen
- `WCSession.default.updateApplicationContext(_:)` fuer State-Updates (last-write-wins)
- `WCSessionDelegate` auf beiden Seiten implementieren
- Fehler-Handling: Watch-App zeigt "iPhone nicht verbunden" wenn Session inaktiv

---

### P3-E02-S03: Watch Timer-Screen

**Als** Nutzer
**moechte ich** meinen Arbeitstimer auf der Apple Watch sehen und steuern,
**damit** ich nicht mein iPhone aus der Tasche holen muss.

**Plattform**: watchOS
**Abhaengigkeiten**: P3-E02-S02 (WatchConnectivity)
**Parallelisierbar mit**: P3-E01-*, P3-E03-*, P3-E05-*
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Haupt-Screen der Watch-App zeigt:
  - **Idle State**: "Bereit" + grosser Start-Button (gruener Kreis mit Play-Icon)
  - **Running State**: Live-Timer (HH:MM:SS), Startzeit, Pause/Stop-Buttons
  - **Paused State**: Timer angehalten, Pausendauer sichtbar, Weiter/Stop-Buttons
  - **Stopped State**: Endzeit sichtbar, Fertig-Button
- [ ] Timer aktualisiert sich live (jede Sekunde)
- [ ] Buttons sind gross genug fuer Apple Watch (min. 44pt Tappable Area)
- [ ] Given kein Timer laeuft
  When der Nutzer "Start" auf der Watch tippt
  Then startet der Timer auf dem iPhone UND der Watch
  And die Watch zeigt den laufenden Timer
- [ ] Given der Timer laeuft auf dem iPhone
  When der Nutzer die Watch-App oeffnet
  Then zeigt die Watch den aktuellen Timer-Stand
- [ ] Given die Watch nicht mit dem iPhone verbunden ist
  When der Nutzer die Watch-App oeffnet
  Then zeigt die Watch "iPhone nicht verbunden"
- [ ] Digital Crown: Keine Funktion (kein Scrollen noetig)
- [ ] Heutige Gesamtarbeitszeit unterhalb des Timers (falls Daten verfuegbar)

**Technische Hinweise**:
- SwiftUI fuer watchOS (identische Syntax wie iOS, aber kleinere Views)
- `TimelineView(.animation)` fuer Live-Timer-Update (alternativ: Timer.publish)
- Kompaktes Layout: Timer gross in der Mitte, Buttons darunter
- `.containerBackground()` fuer watchOS 10 Hintergrund
- `Text(timerInterval: startDate...Date.distantFuture)` fuer automatisches Timer-Update (watchOS 10+)

---

### P3-E02-S04: Watch Complication

**Als** Nutzer
**moechte ich** meinen Timer-Status auf dem Watch-Face sehen,
**damit** ich mit einem Blick sehe ob mein Timer laeuft.

**Plattform**: watchOS
**Abhaengigkeiten**: P3-E02-S02 (WatchConnectivity)
**Parallelisierbar mit**: P3-E01-*, P3-E03-*, P3-E05-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] WidgetKit-basierte Complication (watchOS 10+)
- [ ] Unterstuetzte Familien: `.accessoryCircular`, `.accessoryRectangular`, `.accessoryInline`
- [ ] **Circular**: Timer-Icon (laufend: gruener Punkt, gestoppt: grauer Punkt)
- [ ] **Rectangular**: "Arbeit: 03:42" oder "Bereit" (kurzer Status-Text)
- [ ] **Inline**: "Arbeit 03:42h" oder "Bereit"
- [ ] Tap auf Complication oeffnet die Watch-App
- [ ] Given der Timer laeuft
  When der Nutzer auf sein Watch-Face schaut
  Then zeigt die Complication den aktuellen Timer-Stand

**Technische Hinweise**:
- watchOS 10+: WidgetKit-basierte Complications (NICHT mehr ClockKit)
- `Widget` Struct mit `TimelineProvider`
- `WCSession.transferCurrentComplicationUserInfo(_:)` fuer sofortige Complication-Updates
- App Group fuer geteilten State zwischen Watch-App und Complication

---

### P3-E02-S05: Watch-Integration testen

**Als** Entwickler
**moechte ich** die Watch-iPhone-Kommunikation zuverlaessig testen,
**damit** keine Race Conditions oder Sync-Probleme im Produktivbetrieb auftreten.

**Plattform**: iOS + watchOS
**Abhaengigkeiten**: P3-E02-S03, P3-E02-S04
**Parallelisierbar mit**: P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Testszenarien:
  1. Timer auf iPhone starten -> Watch zeigt Timer
  2. Timer auf Watch starten -> iPhone zeigt Timer
  3. Pause auf Watch -> iPhone zeigt Pause
  4. Stop auf iPhone -> Watch zeigt Stop
  5. iPhone-App im Hintergrund -> Watch-Aktion funktioniert trotzdem
  6. Watch ausser Reichweite -> Fehlermeldung auf Watch
  7. Watch kommt zurueck in Reichweite -> State synchronisiert sich
- [ ] Kein Datenverlust: Watch-Aktionen gehen nicht verloren bei kurzer Unterbrechung
