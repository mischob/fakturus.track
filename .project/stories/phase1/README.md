# Phase 1: Foundation -- Detailplanung

## Scope-Zusammenfassung

Phase 1 liefert eine voll funktionsfaehige Zeiterfassungs-App fuer iOS und Android mit:

- **Authentifizierung**: Azure B2C Login (Apple, Google, E-Mail)
- **Zeiterfassung**: Start/Stop/Pause/Finish-Workflow mit Live-Timer
- **Pausenerfassung**: ArbZG-konforme Pausenerfassung (gesetzliche Pflicht)
- **History**: Monatsgruppierte Uebersicht aller Sessions
- **Session-Verwaltung**: Bearbeiten, Loeschen, manuelle Nacherfassung
- **Offline-First**: Lokale Datenbank mit Background-Sync
- **Sync-Engine**: Bidirektionale Synchronisation mit Server-wins-Strategie

**Zeitraum**: 10.5 Wochen (April -- Mitte Juni 2026)
**Ergebnis**: Interne Beta-Version via TestFlight / Firebase App Distribution

---

## EPIC-Uebersicht

| EPIC | Titel | Wochen | Abhaengigkeiten |
|------|-------|--------|-----------------|
| E01 | Projekt-Setup & Infrastruktur | 1 | -- |
| E02 | Authentifizierung (Azure B2C) | 1-2 | E01 |
| E03 | Lokale Datenschicht | 1-2 | E01 |
| E04 | API-Client & Netzwerk | 1-2 | E01, E02 |
| E05 | Zeiterfassungs-UI (Timer-Screen) | 2 | E03 |
| E06 | History & Session-Verwaltung | 1.5 | E03, E05 |
| E07 | Sync-Engine | 2 | E03, E04 |
| E08 | Pausenerfassung | 1.5 | E05, E07 |
| E09 | App-Shell & Navigation | 0.5 | E01 |
| E10 | Offline-UX & Polish | 1 | E07 |

---

## Abhaengigkeitsdiagramm

```
                    ┌─────────┐
                    │  E01    │
                    │ Projekt │
                    │  Setup  │
                    └────┬────┘
                         │
           ┌─────────────┼─────────────┬──────────────┐
           │             │             │              │
      ┌────▼────┐  ┌─────▼─────┐ ┌────▼────┐  ┌─────▼─────┐
      │  E02    │  │   E03     │ │  E09    │  │   E04*    │
      │  Auth   │  │ Lokale DB │ │App-Shell│  │API-Client │
      │(B2C)   │  │(SwiftData │ │  & Nav  │  │& Netzwerk │
      └────┬────┘  │  / Room)  │ └─────────┘  └─────┬─────┘
           │       └─────┬─────┘                     │
           │             │                           │
           │    ┌────────┼────────┐                  │
           │    │        │        │                  │
           │ ┌──▼───┐ ┌──▼────┐   │                  │
           │ │ E05  │ │ E06   │   │                  │
           │ │Timer │ │History│   │                  │
           │ │Screen│ │& Mgmt │   │                  │
           │ └──┬───┘ └───────┘   │                  │
           │    │                 │                  │
           │    │        ┌────────▼──────────────────▼┐
           │    │        │        E07                 │
           │    │        │    Sync-Engine              │
           │    │        └────────┬───────────────────┘
           │    │                 │
           │ ┌──▼─────────────────▼┐
           │ │        E08          │
           │ │  Pausenerfassung    │
           │ └─────────────────────┘
           │                 │
           │    ┌────────────▼─────┐
           └───►│       E10        │
                │ Offline-UX &     │
                │ Polish           │
                └──────────────────┘

  * E04 haengt von E01 und E02 ab (Token fuer API-Calls)
```

---

## Parallelitaets-Matrix

**Legende**: P = Parallel moeglich, S = Sequentiell (Abhaengigkeit), - = Nicht relevant

|       | E01 | E02 | E03 | E04 | E05 | E06 | E07 | E08 | E09 | E10 |
|-------|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|
| E01   | -   | S   | S   | S   | S   | S   | S   | S   | S   | S   |
| E02   |     | -   | P   | S   | P   | P   | P   | P   | P   | P   |
| E03   |     |     | -   | P   | S   | S   | S   | S   | P   | P   |
| E04   |     |     |     | -   | P   | P   | S   | P   | P   | P   |
| E05   |     |     |     |     | -   | S   | P   | S   | P   | P   |
| E06   |     |     |     |     |     | -   | P   | P   | P   | P   |
| E07   |     |     |     |     |     |     | -   | S   | P   | S   |
| E08   |     |     |     |     |     |     |     | -   | P   | P   |
| E09   |     |     |     |     |     |     |     |     | -   | P   |
| E10   |     |     |     |     |     |     |     |     |     | -   |

**Maximale Parallelitaet innerhalb jeder Welle:**
- iOS und Android Stories fuer das gleiche Feature laufen IMMER parallel
- UI-Stories koennen mit Mock-Daten parallel zur Backend-Integration entwickelt werden
- Verschiedene Screens koennen parallel entwickelt werden sobald die Datenschicht steht

---

## Kritischer Pfad

```
E01 (Setup) -> E03 (Lokale DB) -> E05 (Timer-UI) -> E08 (Pausen) -> E10 (Polish)
     1 Wo          1.5 Wo            2 Wo             1.5 Wo          1 Wo
                                                                    = 7 Wochen

Parallel dazu:
E01 -> E02 (Auth) -> E04 (API) -> E07 (Sync) -> E10 (Polish)
 1 Wo    1.5 Wo       1.5 Wo       2 Wo          1 Wo
                                                = 7 Wochen
```

**Kritischer Pfad: ca. 7 Wochen** (bei optimaler Parallelisierung).
Die verbleibenden 3.5 Wochen sind Puffer fuer:
- Unvorhergesehene Komplexitaet in Sync/Auth
- Azure Portal Konfiguration (Redirect URIs)
- Bug-Fixing und Integration-Testing
- App-Shell Integration und Feinschliff

---

## Dateien in diesem Ordner

| Datei | Inhalt |
|-------|--------|
| [epic-01-project-setup.md](epic-01-project-setup.md) | Projekt-Setup & Infrastruktur |
| [epic-02-authentication.md](epic-02-authentication.md) | Azure B2C Authentifizierung |
| [epic-03-local-database.md](epic-03-local-database.md) | Lokale Datenschicht |
| [epic-04-api-client.md](epic-04-api-client.md) | API-Client & Netzwerk |
| [epic-05-timer-screen.md](epic-05-timer-screen.md) | Zeiterfassungs-UI (Hauptscreen) |
| [epic-06-history.md](epic-06-history.md) | History & Session-Verwaltung |
| [epic-07-sync-engine.md](epic-07-sync-engine.md) | Sync-Engine |
| [epic-08-breaks.md](epic-08-breaks.md) | Pausenerfassung |
| [epic-09-app-shell.md](epic-09-app-shell.md) | App-Shell & Navigation |
| [epic-10-offline-ux.md](epic-10-offline-ux.md) | Offline-UX & Polish |
| [execution-waves.md](execution-waves.md) | Ausfuehrungsplan in Wellen |
