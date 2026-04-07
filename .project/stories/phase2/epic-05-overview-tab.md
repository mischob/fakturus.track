# EPIC 05: Gesamt-Tab (Overtime-Dashboard)

## Ziel

Der Gesamt-Tab (Tab 3) zeigt eine Ueberstunden-Uebersicht mit monatlicher Aufschluesselung, Urlaubstage-Summary, Feiertage-Info und Jahresnavigation. Die Daten kommen vom Backend-Endpoint `GET /v1/overtime-summary?year={year}` und werden mit einem JSON-Disk-Cache fuer Offline-Nutzung ergaenzt.

## Abhaengigkeiten

- **Phase 1**: API-Client (fuer GET /v1/overtime-summary), Overtime-Cache-Strategie (siehe data-layer.md)
- **E02 (teilweise)**: Settings fuer Bundesland/Arbeitstage -- aber die OvertimeSummary kommt vom Backend, das die Settings bereits kennt. Der Gesamt-Tab braucht E02 nur fuer die Feiertag-Liste.

**Hinweis**: Der Gesamt-Tab kann unabhaengig von E02/E03/E04 entwickelt werden, da er primaer auf Backend-Daten basiert.

---

## Stories

### P2-E05-S01: iOS OvertimeCard-Komponente

**Als** Nutzer
**moechte ich** meine Ueberstunden, Urlaubstage und Feiertage auf einen Blick sehen,
**damit** ich meinen Arbeitsstatus schnell erfassen kann.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 1 (API-Client)
**Parallelisierbar mit**: P2-E05-S02 (Android), alle E01/E02/E03 Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `OvertimeCard.swift` als wiederverwendbare Karte in `Shared/`
- [ ] Layout:
  - Icon (SF Symbol)
  - Titel (Caption: z.B. "Ueberstunden")
  - Wert (Title Font, farbkodiert: gruen bei positiv, rot bei negativ)
  - Optionaler Untertitel (Caption: z.B. "vs. Vormonat: +1:15h")
- [ ] Props: `title: String`, `value: String`, `subtitle: String?`, `icon: String`, `valueColor: Color`
- [ ] Given die Karte mit title="Ueberstunden", value="+12:30h", valueColor=.green erstellt wird
  Then zeigt sie "+12:30h" in gruener Farbe

**Technische Hinweise**:
- SF Symbols: `clock.badge.checkmark` (Ueberstunden), `sun.max` (Urlaub), `calendar` (Feiertage)
- `.font(.title)` fuer Wert, `.font(.caption)` fuer Titel/Untertitel
- Card: `RoundedRectangle` mit Background und Schatten

---

### P2-E05-S02: Android OvertimeCard-Komponente

**Als** Nutzer
**moechte ich** meine Ueberstunden, Urlaubstage und Feiertage auf einen Blick sehen,
**damit** ich meinen Arbeitsstatus schnell erfassen kann.

**Plattform**: Android
**Abhaengigkeiten**: Phase 1 (API-Client)
**Parallelisierbar mit**: P2-E05-S01 (iOS), alle E01/E02/E03 Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `OvertimeCard.kt` als Composable in `ui/shared/`
- [ ] Material 3 ElevatedCard mit gleicher Struktur wie iOS
- [ ] Parameter: `title: String`, `value: String`, `subtitle: String?`, `icon: ImageVector`, `valueColor: Color`

**Technische Hinweise**:
- `ElevatedCard { Column { Icon; Text(title); Text(value); Text(subtitle) } }`
- Icons: `Icons.Default.Schedule`, `Icons.Default.WbSunny`, `Icons.Default.Event`

---

### P2-E05-S03: iOS Monatstabelle-Komponente

**Als** Nutzer
**moechte ich** meine Arbeitszeiten monatlich aufgeschluesselt sehen,
**damit** ich nachvollziehen kann in welchen Monaten ich Ueber- oder Minderstunden hatte.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 1 (API-Client fuer OvertimeSummaryDTO)
**Parallelisierbar mit**: P2-E05-S04 (Android), P2-E05-S01 (OvertimeCard)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `MonthlyOvertimeTable.swift` in `Features/Overview/`
- [ ] Tabelle mit 4 Spalten: Monat | Gearbeitet | Erwartet | +/-
- [ ] Ueberstunden farbkodiert: Gruen bei positiv, Rot bei negativ
- [ ] Footer-Zeile mit Gesamtsummen (fett)
- [ ] Nur Monate bis zum aktuellen Monat anzeigen (bei aktuellem Jahr)
- [ ] Given OvertimeSummary mit 3 Monaten (Jan: +3:15, Feb: -1:30, Maerz: +0:45)
  When die Tabelle gerendert wird
  Then zeigt sie 3 Zeilen mit korrekten Werten und Farben
  And die Footer-Zeile zeigt "+2:30h" als Gesamtsumme
- [ ] Zeitformat: "171:15h" (Stunden:Minuten mit h-Suffix)

**Technische Hinweise**:
- `Grid` oder `VStack { ForEach }` mit festen Spaltenbreiten
- `MonthlyOvertimeDTO` aus Backend-Response nutzen
- Formatierung: `String(format: "%d:%02d", hours, minutes)` + Vorzeichen

---

### P2-E05-S04: Android Monatstabelle-Komponente

**Als** Nutzer
**moechte ich** meine Arbeitszeiten monatlich aufgeschluesselt sehen,
**damit** ich nachvollziehen kann in welchen Monaten ich Ueber- oder Minderstunden hatte.

**Plattform**: Android
**Abhaengigkeiten**: Phase 1 (API-Client)
**Parallelisierbar mit**: P2-E05-S03 (iOS), P2-E05-S02 (OvertimeCard)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `MonthlyOvertimeTable.kt` als Composable in `features/overview/`
- [ ] Gleiche 4-Spalten-Tabelle wie iOS
- [ ] Material 3 Styling (Divider, Typography)
- [ ] Farbkodierung analog zu iOS

**Technische Hinweise**:
- `Column { Row { Text(weight) } }` oder `LazyColumn` fuer Tabellenzeilen
- `Divider()` zwischen Header und Body sowie vor Footer

---

### P2-E05-S05: iOS OverviewViewModel + Disk-Cache

**Als** Nutzer
**moechte ich** die Gesamt-Uebersicht auch offline sehen koennen (letzter bekannter Stand),
**damit** ich nicht auf eine Internetverbindung angewiesen bin.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 1 (API-Client), P2-E05-S01 (OvertimeCard), P2-E05-S03 (Tabelle)
**Parallelisierbar mit**: P2-E05-S06 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `OverviewViewModel.swift` als `@Observable` Klasse:
  - `summary: OvertimeSummaryDTO?`
  - `isLoading: Bool`
  - `error: String?`
  - `selectedYear: Int` (Default: aktuelles Jahr)
  - `lastUpdated: Date?`
  - `isShowingCachedData: Bool`
- [ ] `loadOvertimeSummary(year: Int)` Methode:
  - API-Call: GET /v1/overtime-summary?year={year}
  - Bei Erfolg: Daten anzeigen + in Disk-Cache speichern
  - Bei Fehler: Disk-Cache laden (falls vorhanden) + "Zuletzt aktualisiert: vor X Stunden" anzeigen
  - Bei Fehler ohne Cache: Fehlermeldung anzeigen
- [ ] Disk-Cache:
  - JSON-Datei pro Jahr: `overtime_cache_{year}.json` im Documents-Directory
  - Speichert OvertimeSummaryDTO + Timestamp
  - Kein Expiry (wird bei jedem Fetch ueberschrieben)
- [ ] Jahresnavigation:
  - "← {year-1}" und "{year+1} →" Buttons
  - Aktuelles Jahr markiert
  - Bei Jahreswechsel: neuer API-Call oder Cache laden
- [ ] Given der Nutzer ist offline und oeffnet den Gesamt-Tab
  When gecachte Daten fuer 2026 existieren
  Then werden die gecachten Daten angezeigt
  And ein Hinweis "Zuletzt aktualisiert: vor 3 Stunden" erscheint
- [ ] Given der Nutzer ist online und oeffnet den Gesamt-Tab
  When die API-Daten geladen werden
  Then werden die aktuellen Daten angezeigt (kein Cache-Hinweis)

**Technische Hinweise**:
- Cache: `JSONEncoder().encode(summary)` -> `FileManager.default.urls(for: .documentDirectory)`
- Timestamp als separate Property oder in Wrapper-Struct speichern
- Pull-to-Refresh: `.refreshable { await loadOvertimeSummary(year: selectedYear) }`

---

### P2-E05-S06: Android OverviewViewModel + Disk-Cache

**Als** Nutzer
**moechte ich** die Gesamt-Uebersicht auch offline sehen koennen,
**damit** ich nicht auf eine Internetverbindung angewiesen bin.

**Plattform**: Android
**Abhaengigkeiten**: Phase 1 (API-Client), P2-E05-S02/S04 (UI-Komponenten)
**Parallelisierbar mit**: P2-E05-S05 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `OverviewViewModel.kt` mit StateFlow analog zu iOS
- [ ] Gleiche Disk-Cache-Strategie (JSON-Datei pro Jahr)
- [ ] Jahresnavigation mit Vor-/Zurueck-Buttons
- [ ] Gleiche Offline-Fallback-Logik wie iOS

**Technische Hinweise**:
- Cache: `Json.encodeToString(summary)` -> `context.filesDir`
- `kotlinx.serialization` fuer JSON-Serialisierung
- Pull-to-Refresh: `PullToRefreshBox` (Material 3)

---

### P2-E05-S07: iOS Overview-Screen Zusammenbau

**Als** Nutzer
**moechte ich** den Gesamt-Tab als vollstaendigen Screen mit allen Informationen nutzen koennen,
**damit** ich einen umfassenden Ueberblick ueber mein Arbeitsjahr habe.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E05-S01 (OvertimeCard), P2-E05-S03 (Tabelle), P2-E05-S05 (ViewModel)
**Parallelisierbar mit**: P2-E05-S08 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `OverviewScreen.swift` in `Features/Overview/`
- [ ] Large Title: "Gesamt"
- [ ] Screen-Aufbau (ScrollView):
  1. Summary Cards (horizontal scrollbar): Ueberstunden, Urlaub (genommen/gesamt), Feiertage, Krankheitstage
  2. Jahresnavigation: "← 2025   2026   2027 →"
  3. Monatstabelle
  4. Cache-Hinweis (wenn offline: "Zuletzt aktualisiert: vor X Stunden")
  5. Export-Sektion (wird in E06 ergaenzt -- hier Platzhalter)
- [ ] Pull-to-Refresh fuer gesamten Screen
- [ ] Given der Gesamt-Tab wird geoeffnet
  When die API-Daten geladen werden
  Then zeigen die Summary Cards die korrekten Werte
  And die Monatstabelle zeigt alle Monate bis zum aktuellen
- [ ] Given der Nutzer navigiert zu 2025
  When die Daten geladen werden
  Then zeigt die Tabelle alle 12 Monate von 2025

**Technische Hinweise**:
- Summary Cards in `ScrollView(.horizontal) { HStack { OvertimeCard; OvertimeCard; ... } }`
- Krankheitstage-Card: Wert = `summary.sickDaysTaken` (kommt aus E01-S04 Backend-Erweiterung)
- Wenn Backend-Feld `SickDaysTaken` noch nicht verfuegbar: 0 anzeigen (graceful degradation)

---

### P2-E05-S08: Android Overview-Screen Zusammenbau

**Als** Nutzer
**moechte ich** den Gesamt-Tab als vollstaendigen Screen mit allen Informationen nutzen koennen,
**damit** ich einen umfassenden Ueberblick ueber mein Arbeitsjahr habe.

**Plattform**: Android
**Abhaengigkeiten**: P2-E05-S02 (OvertimeCard), P2-E05-S04 (Tabelle), P2-E05-S06 (ViewModel)
**Parallelisierbar mit**: P2-E05-S07 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `OverviewScreen.kt` in `features/overview/`
- [ ] LazyColumn mit gleichen Sektionen wie iOS
- [ ] PullToRefreshBox fuer Refresh
- [ ] Jahresnavigation als Row mit IconButtons

**Technische Hinweise**:
- Summary Cards: `LazyRow { items(cards) { OvertimeCard() } }`
- `PullToRefreshBox(isRefreshing = isLoading) { LazyColumn { ... } }`
