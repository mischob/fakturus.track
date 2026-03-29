# Ausfuehrungsplan -- Phase 1 in Wellen

## Uebersicht

Die Stories werden in 7 Wellen ausgefuehrt. Jede Welle enthaelt Stories die **parallel** bearbeitet werden koennen. Eine neue Welle startet, sobald ihre Abhaengigkeiten aus vorherigen Wellen erfuellt sind.

**Parallel-Kapazitaet**: iOS-Agent + Android-Agent + Backend/Infra arbeiten gleichzeitig.

```
Woche  1      2      3      4      5      6      7      8      9     10    10.5
      ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┼─────┤
      │ W1   │  W2        │  W3        │  W4  │  W5        │  W6   │ W7  │
      │Setup │ Auth+DB    │ UI+API     │ Sync │ Pausen     │ UX   │Test │
      │      │            │            │      │            │      │     │
```

---

## Welle 1: Projekt-Setup (Woche 1)

**Ziel**: Lauffaehige Projekte auf beiden Plattformen mit Theme und Konfiguration.

**Voraussetzungen**: Keine

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E01-S01 | iOS Xcode-Projekt erstellen | iOS | M | iOS-Agent |
| P1-E01-S02 | Android-Studio-Projekt erstellen | Android | M | Android-Agent |

**Nach Abschluss der Projekte (innerhalb derselben Welle):**

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E01-S03 | iOS Theme & Farben | iOS | S | iOS-Agent |
| P1-E01-S04 | Android Theme & Farben | Android | S | Android-Agent |

**Welle 1 DoD**: Beide Projekte kompilieren und laufen auf Simulator/Emulator. Theme und Formatierungs-Utilities sind vorhanden.

**Parallele Arbeit**: 2 Agents (iOS + Android) gleichzeitig.

---

## Welle 2: Foundation -- Auth + Datenbank + Navigation (Woche 2-3)

**Ziel**: Login funktioniert, lokale Datenbank steht, App-Shell mit Tabs ist sichtbar.

**Voraussetzungen**: Welle 1 abgeschlossen (Projekte + Theme)

### Parallel-Strang A: Authentifizierung

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E02-S01 | iOS AuthManager (MSAL) | iOS | L | iOS-Agent |
| P1-E02-S02 | Android AuthManager (MSAL) | Android | L | Android-Agent |
| P1-E02-S05 | Azure Portal Redirect-URIs | Beide | S | Infra (manuell) |

### Parallel-Strang B: Lokale Datenbank

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E03-S01 | iOS SwiftData Models & Container | iOS | M | iOS-Agent* |
| P1-E03-S02 | Android Room Entities & DAOs | Android | M | Android-Agent* |
| P1-E03-S03 | iOS DTOs | iOS | S | iOS-Agent* |
| P1-E03-S04 | Android DTOs | Android | S | Android-Agent* |

*Strang A und B laufen parallel -- Auth und DB haben keine gegenseitige Abhaengigkeit. Ein Agent kann erst Auth, dann DB machen, oder bei 2 Agents pro Plattform beides gleichzeitig.*

### Parallel-Strang C: App-Shell

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E09-S01 | iOS Tab-Navigation & Lifecycle | iOS | M | iOS-Agent |
| P1-E09-S02 | Android Navigation & Lifecycle | Android | M | Android-Agent |

*Braucht Auth fuer Login-Check, kann aber mit Mock-isAuthenticated begonnen werden.*

### Nach Auth-Abschluss: Login-Screens

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E02-S03 | iOS Login-Screen | iOS | M | iOS-Agent |
| P1-E02-S04 | Android Login-Screen | Android | M | Android-Agent |

**Welle 2 DoD**: Nutzer kann sich einloggen, sieht Tab-Navigation, lokale DB funktioniert. Alles rein lokal, noch kein Sync.

**Parallele Arbeit**: Bis zu 4 unabhaengige Straenge (Auth iOS, Auth Android, DB iOS, DB Android). Plus App-Shell parallel.

---

## Welle 3: Kern-UI -- Timer + History (Woche 4-5)

**Ziel**: Nutzer kann Arbeitszeit erfassen (Start/Stop/Finish), History sehen, Sessions bearbeiten/loeschen. Alles rein lokal.

**Voraussetzungen**: Welle 2 Strang B (Datenbank) abgeschlossen. Auth und App-Shell koennen noch parallel laufen.

### Parallel-Strang A: Timer-Komponenten (keine DB-Abhaengigkeit)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E05-S01 | iOS TimerDisplay | iOS | S | iOS-Agent |
| P1-E05-S02 | Android TimerDisplay | Android | S | Android-Agent |

### Parallel-Strang B: ViewModels (braucht DB)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E05-S05 | iOS TimeTrackingViewModel | iOS | M | iOS-Agent |
| P1-E05-S06 | Android TimeTrackingViewModel | Android | M | Android-Agent |

### Parallel-Strang C: ActiveSessionCard (braucht Timer + DB Model)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E05-S03 | iOS ActiveSessionCard | iOS | L | iOS-Agent |
| P1-E05-S04 | Android ActiveSessionCard | Android | L | Android-Agent |

### Parallel-Strang D: History-Komponenten (braucht DB Model)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E06-S01 | iOS SessionRow | iOS | S | iOS-Agent |
| P1-E06-S02 | Android SessionRow | Android | S | Android-Agent |
| P1-E06-S03 | iOS MonthGroupSection | iOS | M | iOS-Agent |
| P1-E06-S04 | Android MonthGroup | Android | M | Android-Agent |

### Nach Components + ViewModel: Detail-Sheets

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E06-S05 | iOS SessionDetailSheet | iOS | M | iOS-Agent |
| P1-E06-S06 | Android SessionDetailSheet | Android | M | Android-Agent |

### Zusammenbau (braucht alle obigen):

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E06-S07 | iOS Zeiten-Screen (Zusammenbau) | iOS | M | iOS-Agent |
| P1-E06-S08 | Android Zeiten-Screen (Zusammenbau) | Android | M | Android-Agent |

**Welle 3 DoD**: Kompletter Zeiten-Tab funktioniert lokal. Start/Stop/Finish, History mit Monatsgruppen, Session-Bearbeitung und -Loeschung.

**Parallele Arbeit**: Timer + SessionRow + ViewModel koennen gleichzeitig auf jeder Plattform entwickelt werden.

---

## Welle 4: API-Client + Sync-Engine (Woche 5-7)

**Ziel**: Daten werden zuverlaessig mit dem Backend synchronisiert.

**Voraussetzungen**: Welle 2 Auth (fuer Token), Welle 2 DB, Welle 2 Network Monitor

**Hinweis**: Welle 4 kann **parallel zu Welle 3** gestartet werden, da der API-Client nur Auth + DB braucht, nicht die UI.

### Parallel-Strang A: API-Client (kann parallel zu Welle 3 UI laufen)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E04-S01 | iOS APIClient | iOS | L | iOS-Agent |
| P1-E04-S02 | Android APIClient (Ktor) | Android | L | Android-Agent |
| P1-E04-S03 | iOS NetworkMonitor | iOS | S | iOS-Agent |
| P1-E04-S04 | Android NetworkMonitor | Android | S | Android-Agent |

*NetworkMonitor hat nur E01-Abhaengigkeit und kann schon in Welle 2 gestartet werden.*

### Nach API-Client: SyncEngine

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E07-S01 | iOS SyncEngine | iOS | L | iOS-Agent |
| P1-E07-S02 | Android SyncEngine | Android | L | Android-Agent |

### Nach SyncEngine: Trigger + Integration

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E07-S03 | iOS Sync-Trigger | iOS | M | iOS-Agent |
| P1-E07-S04 | Android Sync-Trigger | Android | M | Android-Agent |
| P1-E07-S05 | Sync-Integration in ViewModels | Beide | S | Beide Agents |

**Welle 4 DoD**: Sessions, Urlaubstage und Settings werden zuverlaessig synchronisiert. Background Sync funktioniert. Pull-to-Refresh loest Sync aus.

**Parallele Arbeit**: API-Client iOS + Android gleichzeitig, dann SyncEngine iOS + Android gleichzeitig.

---

## Welle 5: Pausenerfassung (Woche 7-8.5)

**Ziel**: ArbZG-konforme Pausenerfassung integriert in den bestehenden Timer-Workflow.

**Voraussetzungen**: Welle 3 (Timer-UI + ViewModel) und Welle 4 (Sync fuer PauseMinutes)

### Parallel-Strang A: ViewModel-Erweiterung

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E08-S01 | iOS Pause-State im ViewModel | iOS | M | iOS-Agent |
| P1-E08-S02 | Android Pause-State im ViewModel | Android | M | Android-Agent |

### Parallel-Strang B: UI-Erweiterung (nach ViewModel)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E08-S03 | iOS ActiveSessionCard Pause-UI | iOS | M | iOS-Agent |
| P1-E08-S04 | Android ActiveSessionCard Pause-UI | Android | M | Android-Agent |

### Parallel-Strang C: ArbZG-Hinweise (nach ViewModel)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E08-S05 | iOS ArbZG-Pausenhinweise | iOS | S | iOS-Agent |
| P1-E08-S06 | Android ArbZG-Pausenhinweise | Android | S | Android-Agent |

### History-Integration (kann parallel zu B/C laufen)

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E08-S07 | Pause in History integrieren | Beide | S | Beide |

**Welle 5 DoD**: Pausen koennen live erfasst und manuell eingegeben werden. ArbZG-Hinweise funktionieren. History zeigt Pausen- und Nettodauer korrekt.

---

## Welle 6: Offline-UX & Polish (Woche 8.5-9.5)

**Ziel**: Die App fuehlt sich auch offline hochwertig an. Sync-Status, Offline-Banner und Fehlerbehandlung sind poliert.

**Voraussetzungen**: Welle 4 (Sync + NetworkMonitor)

### Alle Stories parallel:

| Story-ID | Titel | Plattform | Aufwand | Agent |
|----------|-------|-----------|---------|-------|
| P1-E10-S01 | iOS OfflineBanner | iOS | S | iOS-Agent |
| P1-E10-S02 | Android OfflineBanner | Android | S | Android-Agent |
| P1-E10-S03 | iOS SyncStatusIndicator | iOS | S | iOS-Agent |
| P1-E10-S04 | Android SyncStatusIndicator | Android | S | Android-Agent |
| P1-E10-S05 | Erste Synchronisation nach Login | Beide | S | Beide |
| P1-E10-S06 | Error-Handling Polish | Beide | M | Beide |

**Welle 6 DoD**: Offline-Banner erscheint/verschwindet korrekt. Sync-Status ist sichtbar. Erste Synchronisation nach Login funktioniert. Fehlermeldungen sind nutzerfreundlich.

**Hinweis**: Welle 6 kann teilweise parallel zu Welle 5 laufen (OfflineBanner braucht nur NetworkMonitor, nicht Pausen).

---

## Welle 7: Integration & Testing (Woche 9.5-10.5)

**Ziel**: Alles zusammen testen, Bugs fixen, Beta-Version erstellen.

**Voraussetzungen**: Alle vorherigen Wellen

| Aufgabe | Plattform | Beschreibung |
|---------|-----------|-------------|
| Integrations-Test | Beide | Vollstaendiger Durchlauf aller User-Flows |
| Bug-Fixing | Beide | Gefundene Bugs beheben |
| Performance-Check | Beide | App-Start < 2s, kein UI-Ruckeln, Sync nicht blockierend |
| TestFlight Build | iOS | Interne Beta-Version verteilen |
| Firebase App Distribution | Android | Interne Beta-Version verteilen |
| Regressions-Test MAUI | MAUI | Bestehende MAUI-App funktioniert weiterhin |

### Integrations-Test-Szenarien

1. **Erster Login**: Neuer Nutzer -> Login -> Erst-Sync -> Zeiten-Tab (leer oder mit Daten)
2. **Taegliche Nutzung**: Starten -> Pause -> Weiter -> Stop -> Bearbeiten -> Fertig -> Sync
3. **Offline-Arbeit**: Flugmodus -> Session erfassen -> Flugmodus aus -> Automatischer Sync
4. **Mehrere Sessions**: 5 Sessions erstellen -> History pruefen -> Monatsgruppen korrekt
5. **Session loeschen**: Swipe-Delete -> Undo -> Sync -> Backend pruefen
6. **ArbZG-Hinweise**: 6h+ Session laufen lassen -> Banner erscheint
7. **Token-Ablauf**: 60+ Minuten warten -> API-Call -> Token wird silent erneuert
8. **Pull-to-Refresh**: Daten im Backend aendern -> Pull-to-Refresh -> Aenderungen sichtbar
9. **Netzwerk-Wechsel**: WLAN -> Mobilfunk -> Offline -> WLAN -> Sync korrekt
10. **App-Kill und Neustart**: Session laufen lassen -> App killen -> Oeffnen -> Session noch da

**Welle 7 DoD**: Beta-Versionen auf TestFlight und Firebase App Distribution. Alle 10 Testszenarien bestanden. Keine kritischen Bugs.

---

## Zusammenfassung: Story-Counts pro Welle

| Welle | Stories | Aufwand (S+M+L) | Wochen |
|-------|---------|-----------------|--------|
| W1 | 4 | 2S + 2M | 1 |
| W2 | 10 | 2S + 4M + 4L | 2 |
| W3 | 12 | 2S + 6M + 4L | 2 |
| W4 | 9 | 3S + 2M + 4L | 2.5* |
| W5 | 7 | 3S + 4M | 1.5 |
| W6 | 6 | 4S + 2M | 1 |
| W7 | -- | Bug-Fixing | 1 |
| **Gesamt** | **48** | | **~10.5** |

*Welle 3 und 4 ueberlappen sich zeitlich (API-Client parallel zu UI).

---

## Kritischer Pfad (minimale Gesamtdauer)

```
W1 (1 Wo) -> W2 Auth (1.5 Wo) -> W4 API+Sync (2.5 Wo) -> W5 Pausen (1.5 Wo) -> W7 Test (1 Wo)
= 7.5 Wochen auf dem kritischen Pfad

Parallel dazu:
W1 (1 Wo) -> W2 DB (1 Wo) -> W3 UI (2 Wo) -> [wartet auf Sync] -> W5 Pausen -> W6 Polish
= braucht Sync-Engine aus W4, daher nicht kritischer als Pfad oben
```

**Puffer**: 10.5 - 7.5 = **3 Wochen Puffer** fuer:
- Azure B2C Konfigurationsprobleme
- Unerwartete MSAL-Komplexitaet
- Sync-Edge-Cases
- Performance-Optimierung
- Code-Review und Qualitaetssicherung

---

## Diagramm: Parallelitaet ueber Zeit

```
Woche:  1     2     3     4     5     6     7     8     9    10   10.5
iOS:   [S01] [Auth──────] [Timer─UI──────] [API──] [Sync─] [Pause──] [Polish] [Test]
       [S03]  [DB──] [DTO]  [VM] [Card]    [Clnt]  [SynE]  [VM+UI]  [Banner]
              [Nav─]       [Row] [Group]           [Trig]  [ArbZG]  [SyncSt]
                           [Detail][Assem]                  [Hist]

Andr:  [S02] [Auth──────] [Timer─UI──────] [API──] [Sync─] [Pause──] [Polish] [Test]
       [S04]  [DB──] [DTO]  [VM] [Card]    [Clnt]  [SynE]  [VM+UI]  [Banner]
              [Nav─]       [Row] [Group]           [Trig]  [ArbZG]  [SyncSt]
                           [Detail][Assem]                  [Hist]

Infra:        [Azure─URIs]
```

**Lesehinweis**: Jede Zeile zeigt was ein Agent in der jeweiligen Woche bearbeitet. Bloecke die vertikal uebereinander stehen laufen parallel.
