# Ausfuehrungsplan -- Phase 2 in Wellen

## Uebersicht

Phase 2 wird in 6 Wellen ausgefuehrt. Die Wellen nutzen maximale Parallelitaet: bis zu 3 Agents (iOS, Android, Backend) arbeiten gleichzeitig, und verschiedene Tabs werden parallel entwickelt.

**Parallel-Kapazitaet**: iOS-Agent + Android-Agent + Backend-Agent (fuer E01).

```
Woche  11     12     13     14     15     16     17     18     19    20
      ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┼─────┤
      │ W1         │ W2              │ W3        │ W4         │  W5 │ W6
      │Backend+    │ Kalender+       │ Krank+    │ Export     │Intg │Test
      │Settings+   │ Gesamt          │ Gesamt    │            │     │(1Wo)
      │Gesamt-UI   │                 │ Zusammenb.│            │     │
```

---

## Welle 1: Backend + Settings + Gesamt-Komponenten (Woche 11-12)

**Ziel**: Backend SickDay-Endpoints stehen bereit. Settings-UI ist implementiert. Gesamt-Tab Grundkomponenten sind gebaut.

**Voraussetzungen**: Phase 1 abgeschlossen.

### Parallel-Strang A: Backend SickDay (Backend-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E01-S01 | SickDay Entity & Migration | Backend | M |
| P2-E01-S02 | SickDay CRUD-Endpoints | Backend | M |
| P2-E01-S03 | SickDay Sync-Endpoint | Backend | M |
| P2-E01-S04 | OvertimeSummary um SickDays erweitern | Backend | M |

### Parallel-Strang B: Settings-UI (iOS-Agent + Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E02-S11 | Feiertag-Berechnungslogik | Beide | M |
| P2-E02-S01 | iOS Settings-Screen Grundstruktur | iOS | M |
| P2-E02-S02 | Android Settings-Screen Grundstruktur | Android | M |
| P2-E02-S03 | iOS WorkdaySelector | iOS | S |
| P2-E02-S04 | Android WorkdaySelector | Android | S |
| P2-E02-S05 | iOS BundeslandPicker | iOS | M |
| P2-E02-S06 | Android BundeslandPicker | Android | M |

### Parallel-Strang C: Gesamt-Tab Komponenten (kann parallel zu B laufen)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E05-S01 | iOS OvertimeCard | iOS | S |
| P2-E05-S02 | Android OvertimeCard | Android | S |
| P2-E05-S03 | iOS Monatstabelle | iOS | M |
| P2-E05-S04 | Android Monatstabelle | Android | M |

**Welle 1 DoD**: SickDay-API funktioniert (Postman-Test). Settings-Screen zeigt alle Felder. OvertimeCard und Monatstabelle rendern korrekt mit Mock-Daten.

**Parallele Arbeit**: 3 Agents (Backend + iOS + Android). iOS/Android arbeiten abwechselnd an Settings und Gesamt-Komponenten (beides sind UI-Aufgaben ohne gegenseitige Abhaengigkeit).

**Hinweis zur iOS/Android-Verteilung**: Jeder Agent kann in Welle 1 zuerst die Settings-Stories und dann die Gesamt-Komponenten machen (oder umgekehrt). Die Reihenfolge innerhalb eines Agents ist flexibel, da Settings und Gesamt unabhaengig sind.

---

## Welle 2: Kalender + Settings-Logik + Gesamt-ViewModel (Woche 13-14.5)

**Ziel**: Urlaub-Kalender funktioniert mit Feiertagen. Settings sind speicherbar und synchronisierbar. Gesamt-Tab zeigt echte API-Daten.

**Voraussetzungen**: Welle 1 (Settings-UI, Gesamt-Komponenten, Feiertag-Berechnung)

### Parallel-Strang A: Urlaub-Kalender

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E03-S01 | iOS VacationCalendar | iOS | L |
| P2-E03-S02 | Android VacationCalendar | Android | L |
| P2-E03-S03 | iOS Urlaubstage Toggle | iOS | M |
| P2-E03-S04 | Android Urlaubstage Toggle | Android | M |
| P2-E03-S05 | iOS Resturlaub-Anzeige | iOS | S |
| P2-E03-S06 | Android Resturlaub-Anzeige | Android | S |

### Parallel-Strang B: Settings-Logik + Schulferien

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E02-S07 | iOS SettingsViewModel + Sync | iOS | M |
| P2-E02-S08 | Android SettingsViewModel + Sync | Android | M |
| P2-E02-S09 | iOS Schulferien-Verwaltung | iOS | M |
| P2-E02-S10 | Android Schulferien-Verwaltung | Android | M |

### Parallel-Strang C: Gesamt-ViewModel + Zusammenbau

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E05-S05 | iOS OverviewViewModel + Cache | iOS | M |
| P2-E05-S06 | Android OverviewViewModel + Cache | Android | M |
| P2-E05-S07 | iOS Overview-Screen Zusammenbau | iOS | M |
| P2-E05-S08 | Android Overview-Screen Zusammenbau | Android | M |

**Welle 2 DoD**: Kalender zeigt Monate mit Feiertagen. Urlaub kann per Tap gesetzt/entfernt werden. Settings werden gespeichert und synchronisiert. Gesamt-Tab zeigt echte API-Daten mit Offline-Cache.

**Parallele Arbeit**: iOS-Agent macht Kalender + Settings parallel (verschiedene Features). Android-Agent spiegelt iOS. Gesamt-ViewModel kann gleichzeitig laufen, wenn Agent-Kapazitaet vorhanden.

**Hinweis**: Welle 2 ist die groesste Welle (am meisten Stories). Der Kalender (E03) ist der aufwaendigste Teil. Falls noetig, kann der Gesamt-Tab (Strang C) in Welle 3 verschoben werden -- er blockiert nur Export (E06).

---

## Welle 3: Krankheitstage + Urlaub-/Gesamt-Screen-Zusammenbau (Woche 15-16)

**Ziel**: Krankheitstage sind im Kalender integriert. Urlaub- und Gesamt-Screens sind vollstaendig.

**Voraussetzungen**: Welle 1 (Backend SickDay), Welle 2 (Kalender, Gesamt-ViewModel)

### Parallel-Strang A: Krankheitstage (Data Layer + Sync)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E04-S01 | iOS SickDay Model + DTO + API | iOS | M |
| P2-E04-S02 | Android SickDay Entity + DTO + API | Android | M |
| P2-E04-S03 | iOS SyncEngine SickDay-Sync | iOS | M |
| P2-E04-S04 | Android SyncEngine SickDay-Sync | Android | M |

### Nach Strang A: Krankheitstage UI-Integration

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E04-S05 | iOS Kalender + Long-Press | iOS | L |
| P2-E04-S06 | Android Kalender + Long-Press | Android | L |
| P2-E04-S07 | VacationViewModel erweitern | Beide | M |

### Parallel-Strang B: Urlaub-Screen Zusammenbau (falls nicht in W2)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E03-S07 | iOS Urlaub-Screen Zusammenbau | iOS | M |
| P2-E03-S08 | Android Urlaub-Screen Zusammenbau | Android | M |

**Welle 3 DoD**: Krankheitstage koennen per Long-Press im Kalender gesetzt werden. SickDay-Sync funktioniert. Urlaub-Tab und Gesamt-Tab sind vollstaendig nutzbar.

---

## Welle 4: Export (Woche 17-18)

**Ziel**: PDF-Report und CSV-Export funktionieren. Share-Sheet-Integration ist vollstaendig.

**Voraussetzungen**: Welle 2/3 (Gesamt-Screen, WorkSession/VacationDay/SickDay Daten verfuegbar)

### Parallel-Strang A: Generatoren

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E06-S01 | iOS PDF-Report | iOS | L |
| P2-E06-S02 | Android PDF-Report | Android | L |
| P2-E06-S03 | iOS CSV-Export | iOS | M |
| P2-E06-S04 | Android CSV-Export | Android | M |

### Nach Generatoren: UI + ViewModel

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P2-E06-S07 | OverviewViewModel um Export-Methoden erweitern | Beide | M |
| P2-E06-S05 | iOS Export-UI + Share | iOS | M |
| P2-E06-S06 | Android Export-UI + Share | Android | M |

**Welle 4 DoD**: PDF-Monatsreport wird korrekt generiert und kann geteilt werden. CSV-Export funktioniert fuer Monat/Quartal/Jahr. Share-Sheet oeffnet sich mit korrektem Dateityp.

**Parallele Arbeit**: PDF und CSV koennen gleichzeitig entwickelt werden (verschiedene Dateien, keine Abhaengigkeit).

---

## Welle 5: Integration & Tab-Verdrahtung (Woche 18.5-19)

**Ziel**: Alle Tabs sind vollstaendig in die App-Shell integriert. Navigation zwischen Tabs funktioniert. Placeholder-Screens aus Phase 1 sind durch echte Screens ersetzt.

**Voraussetzungen**: Wellen 1-4

| Aufgabe | Plattform | Beschreibung |
|---------|-----------|-------------|
| Tab-Integration | Beide | Placeholder-Screens durch echte Screens ersetzen |
| Settings-Sync bei App-Start | Beide | Settings werden beim Start synchronisiert |
| SickDay-Sync in SyncAll | Beide | syncSickDays() in syncAll() integriert |
| Cross-Tab-Konsistenz | Beide | Bundesland-Aenderung in Settings wirkt auf Kalender-Feiertage |
| Edge-Case: Offline-Kalender | Beide | Urlaubstage/Krankheitstage setzen ohne Netzwerk |
| Edge-Case: Leerer Zustand | Beide | Gesamt-Tab ohne Daten, Kalender ohne Urlaub |
| Profil-Daten im Settings-Tab | Beide | Name/E-Mail aus B2C Claims korrekt anzeigen |

**Welle 5 DoD**: Alle 4 Tabs funktionieren vollstaendig. Tab-Wechsel ist fluessig. Daten sind konsistent ueber Tabs hinweg (z.B. Urlaub im Kalender = Urlaub in Gesamt-Tab).

---

## Welle 6: Testing & Bug-Fixing (Woche 19-20)

**Ziel**: Feature-komplette Beta-Version fuer erweiterte Testgruppe.

**Voraussetzungen**: Alle vorherigen Wellen

| Aufgabe | Plattform | Beschreibung |
|---------|-----------|-------------|
| Integrations-Test | Beide | Vollstaendiger Durchlauf aller User-Flows |
| Bug-Fixing | Beide | Gefundene Bugs beheben |
| Performance-Check | Beide | Kalender-Scrolling fluessig, Tab-Wechsel < 200ms |
| TestFlight Build | iOS | Beta-Version fuer erweiterte Testgruppe |
| Firebase App Distribution | Android | Beta-Version fuer erweiterte Testgruppe |
| Regressions-Test Phase 1 | Beide | Timer, History, Sync, Pausen funktionieren noch korrekt |

### Integrations-Test-Szenarien (Phase 2)

1. **Einstellungen aendern**: Wochenstunden 40->32, Arbeitstage Mo-Fr->Mo-Do, Bundesland NW->BY -> Sync -> Gesamt-Tab aktualisiert
2. **Urlaub eintragen**: 5 Urlaubstage markieren -> Resturlaub korrekt -> Sync -> Gesamt-Tab zeigt 5 Urlaubstage
3. **Krankheitstag**: Long-Press -> Krank -> Tap = entfernen -> Long-Press -> Typ wechseln (Urlaub <-> Krank)
4. **Gesamt-Tab offline**: Flugmodus -> Gesamt-Tab oeffnen -> Cached Daten mit Hinweis
5. **Jahresnavigation**: 2026 -> 2025 -> 2027 -> Daten korrekt fuer jedes Jahr
6. **PDF-Report**: Maerz 2026 exportieren -> PDF oeffnen -> Inhalt pruefen -> Teilen
7. **CSV-Export**: Quartal Q1 2026 -> CSV -> In Excel oeffnen -> Semikolon + Komma korrekt
8. **Schulferien**: 3 Ferien erstellen -> Bearbeiten -> Loeschen -> Sync
9. **Bundesland-Wechsel**: NW->BY -> Kalender zeigt neue Feiertage -> Gesamt-Tab aktualisiert
10. **Offline-Urlaub**: Flugmodus -> 3 Urlaubstage setzen -> Netzwerk an -> Sync -> Backend hat alle 3

**Welle 6 DoD**: Beta-Versionen auf TestFlight und Firebase App Distribution. Alle 10 Testszenarien bestanden. Keine kritischen Bugs. Performance: Tab-Wechsel fluessig, Kalender scrollt ohne Ruckeln.

---

## Zusammenfassung: Story-Counts pro Welle

| Welle | Stories | Aufwand-Schwerpunkt | Wochen |
|-------|---------|---------------------|--------|
| W1 | 15 | 4S + 7M + 4M(Backend) | 2 |
| W2 | 16 | 2S + 10M + 4L | 2.5 |
| W3 | 9 | 5M + 2L + 2M | 2 |
| W4 | 7 | 2M + 3L + 2M | 2 |
| W5 | -- | Integration | 0.5 |
| W6 | -- | Testing | 1 |
| **Gesamt** | **47 Stories** | | **~9 Wochen** |

---

## Diagramm: Parallelitaet ueber Zeit

```
Woche:  11    12    13    14    15    16    17    18    19   19.5
Back:  [SickDay Entity───] [SickDay Sync+OT]
       [E01-S01] [S02+S03]  [S04]

iOS:   [Feiertag][SettingsUI──][WorkdaySel][BundeslandPkr]
       [OTCard] [MonthTable]
                              [Calendar──────────][Toggle][Resturlaub]
                              [SettingsVM][Schulferien]
                              [OverviewVM][OverviewScreen]
                                                  [SickModel][SickSync]
                                                  [LongPress Kalender──]
                                                  [VacScreen]
                                                                [PDF──────][CSV]
                                                                [ExportVM][ExportUI]
                                                                              [Intg][Test]

Andr:  (spiegelt iOS -- gleiche Stories parallel)
```

**Lesehinweis**: Jede Zeile zeigt die zeitliche Abfolge der Stories pro Agent. Vertikal uebereinanderliegende Bloecke laufen parallel.

---

## Kritischer Pfad (Wellen-Perspektive)

```
W1 (2 Wo) -> W2 (2.5 Wo) -> W3 (2 Wo) -> W4 (2 Wo) = 8.5 Wochen (kein Puffer!)

Optimistischer Pfad (wenn Kalender schnell fertig):
W1 (2 Wo) -> W2+W3 ueberlappend (3 Wo) -> W4 (1.5 Wo) -> W5+W6 (1.5 Wo) = 8 Wochen

Realistischer Pfad (mit Puffer):
W1 (2 Wo) -> W2 (2.5 Wo) -> W3 (1.5 Wo) -> W4 (1.5 Wo) -> W5+W6 (1.5 Wo) = 9 Wochen
```

**Risiko**: Der Kalender (E03-S01/S02) ist die komplexeste Einzelkomponente. Falls er laenger als geplant dauert, verschieben sich W3 und W4. Mitigation: Kalender frueh in W2 starten, ggf. vereinfachte Version (ohne Schulferien-Markierung) als MVP.

**Hinweis zur Testphase**: Welle 6 wurde bewusst auf 1 Woche erhoehen (statt 0.5 Wochen). Bei 47 Stories und plattformuebergreifender Integration ist eine halbe Woche fuer Testing und Bug-Fixing zu knapp. Der Puffer ist vorhanden: 6 Wochen kritischer Pfad bei 9 Wochen Gesamtzeit.
