# Phase 2: Features -- Detailplanung

## Scope-Zusammenfassung

Phase 2 erweitert die in Phase 1 gebaute Kern-Zeiterfassungs-App um die verbleibenden Tabs und Features, die fuer Feature-Paritaet mit dem Web-Frontend und Store-Readiness noetig sind:

- **Einstellungen-Tab**: Arbeitszeit-Konfiguration, Bundesland, Urlaubstage, Kalender-URL, Schulferien, Profil/Logout
- **Urlaub-Tab**: Kalender-Ansicht, Urlaubstage setzen/entfernen, Feiertage anzeigen, Krankheitstage
- **Gesamt-Tab**: Ueberstunden-Dashboard, Monatsvergleich, Urlaubsuebersicht, Feiertagsliste
- **Export**: PDF-Monatsreport, CSV-Export, Share-Sheet-Integration
- **Krankheitstage**: Neue Entity (Backend + Frontend + Sync), Long-Press-Kontextmenue
- **Backend-Erweiterungen**: SickDay Entity + Endpoints

**Zeitraum**: 9 Wochen (Mitte Juni -- Mitte August 2026)
**Ergebnis**: Feature-komplette Beta-Version fuer erweiterte Nutzergruppe (10-20 Personen)

---

## EPIC-Uebersicht

| EPIC | Titel | Geschaetzte Dauer | Abhaengigkeiten |
|------|-------|-------------------|-----------------|
| E01 | Backend: SickDay Entity & Endpoints | 1 Woche | Phase 1 abgeschlossen |
| E02 | Einstellungen-Tab (Settings UI + Sync) | 2 Wochen | Phase 1 (Settings-Sync) |
| E03 | Urlaub-Tab (Kalender + Vacation CRUD) | 2.5 Wochen | Phase 1 (VacationDay-Sync) |
| E04 | Krankheitstage (Frontend + Sync) | 1.5 Wochen | E01 (Backend), E03 (Kalender) |
| E05 | Gesamt-Tab (Overtime-Dashboard) | 2 Wochen | E02 (Settings fuer Berechnungen) |
| E06 | Export (PDF/CSV + Share) | 1.5 Wochen | E05 (Gesamt-Tab fuer Kontext) |

---

## Abhaengigkeitsdiagramm

```
                         ┌──────────────────┐
                         │   Phase 1        │
                         │  abgeschlossen   │
                         └────────┬─────────┘
                                  │
                ┌─────────────────┼──────────────────┐
                │                 │                   │
         ┌──────▼──────┐  ┌──────▼──────┐  ┌────────▼────────┐
         │    E01      │  │    E02      │  │      E05*       │
         │  Backend    │  │ Einstellungen│  │   Gesamt-Tab    │
         │  SickDay    │  │    Tab      │  │  (Overtime API  │
         │  Endpoints  │  │             │  │   braucht nur   │
         └──────┬──────┘  └──────┬──────┘  │   Phase 1 API)  │
                │                │         └────────┬────────┘
                │                │                   │
         ┌──────▼──────┐        │           ┌───────▼───────┐
         │    E03      │        │           │     E06       │
         │ Urlaub-Tab  │◄───────┘           │    Export     │
         │ (Kalender)  │  (braucht Settings │  (PDF/CSV)    │
         └──────┬──────┘   fuer Feiertage)  └───────────────┘
                │
         ┌──────▼──────┐
         │    E04      │
         │Krankheitstage│
         │(Kalender +   │
         │ SickDay-Sync)│
         └──────────────┘

  * E05 kann schon mit Phase-1-API (GET /v1/overtime-summary) starten,
    braucht E02 nur fuer korrekte Feiertag-/Arbeitstage-Anzeige.
    UI kann mit aktuellen Backend-Defaults gebaut werden.
```

---

## Parallelitaets-Matrix

**Legende**: P = Parallel moeglich, S = Sequentiell (Abhaengigkeit), ~ = Teilweise parallel (UI mit Mocks)

|       | E01 | E02 | E03 | E04 | E05 | E06 |
|-------|-----|-----|-----|-----|-----|-----|
| E01   | -   | P   | ~   | S   | P   | P   |
| E02   |     | -   | ~   | P   | ~   | P   |
| E03   |     |     | -   | S   | P   | P   |
| E04   |     |     |     | -   | P   | P   |
| E05   |     |     |     |     | -   | S   |
| E06   |     |     |     |     |     | -   |

**Erklaerung der Parallelitaet:**
- **E01 (Backend) || E02 (Settings) || E05 (Gesamt)**: Drei komplett unabhaengige Straenge
- **E03 (Urlaub) ~ E02 (Settings)**: Urlaub-Kalender braucht Bundesland fuer Feiertage, kann aber mit Default-Bundesland beginnen
- **E04 (Krank) nach E01 + E03**: Braucht Backend-Endpoints UND Kalender-Komponente
- **E06 (Export) nach E05 (Gesamt)**: Export ist logisch an den Gesamt-Tab gebunden

**Maximale Parallelitaet:**
- iOS-Agent und Android-Agent arbeiten IMMER parallel am gleichen Feature
- Bis zu 3 EPICs koennen gleichzeitig in Arbeit sein (E01 + E02 + E05)
- UI kann mit Mock-Daten parallel zum Backend entwickelt werden

---

## Kritischer Pfad

```
Pfad A (laengster):
E01 Backend (1 Wo) -> E03 Urlaub-Tab (2.5 Wo) -> E04 Krankheitstage (1.5 Wo) -> Integration (0.5 Wo)
= 5.5 Wochen

Pfad B:
E02 Einstellungen (2 Wo) -> E05 Gesamt-Tab (2 Wo) -> E06 Export (1.5 Wo) -> Integration (0.5 Wo)
= 6 Wochen  <-- KRITISCHER PFAD

Pfad C (kuerzester):
E01 Backend (1 Wo) -> parallel zu E02/E05
= 1 Woche (kein eigener kritischer Pfad)
```

**Kritischer Pfad: 6 Wochen** bei optimaler Parallelisierung.
Puffer: 9 - 6 = **3 Wochen** fuer:
- Kalender-Komponente Komplexitaet (Custom Calendar View)
- PDF-Generierung plattformspezifisch (PDFKit vs Android PDF API)
- Integration-Testing und Bug-Fixing
- Beta-Test mit erweiterter Nutzergruppe

---

## Dateien in diesem Ordner

| Datei | Inhalt |
|-------|--------|
| [epic-01-backend-sickday.md](epic-01-backend-sickday.md) | Backend: SickDay Entity & Endpoints |
| [epic-02-settings-tab.md](epic-02-settings-tab.md) | Einstellungen-Tab (Settings UI + Sync) |
| [epic-03-vacation-tab.md](epic-03-vacation-tab.md) | Urlaub-Tab (Kalender + Vacation CRUD) |
| [epic-04-sick-days.md](epic-04-sick-days.md) | Krankheitstage (Frontend + Sync) |
| [epic-05-overview-tab.md](epic-05-overview-tab.md) | Gesamt-Tab (Overtime-Dashboard) |
| [epic-06-export.md](epic-06-export.md) | Export (PDF/CSV + Share) |
| [execution-waves.md](execution-waves.md) | Ausfuehrungsplan in Wellen |
| [implementation-checklist.md](implementation-checklist.md) | Konventionen und Checkliste |
