# EPIC 03: Urlaub-Tab (Kalender + Vacation CRUD)

## Ziel

Der Urlaub-Tab (Tab 2) erhaelt eine vollstaendige Kalender-Ansicht zur Verwaltung von Urlaubstagen. Nutzer koennen durch Monate navigieren, Urlaubstage per Tap setzen/entfernen, Feiertage und Wochenenden sehen und ihren Resturlaub ueberblicken. Der Kalender wird spaeter in E04 um Krankheitstage erweitert.

## Abhaengigkeiten

- **Phase 1**: VacationDay-Sync funktioniert bereits (SyncEngine.syncVacationDays)
- **Phase 1**: VacationDay Model/Entity existiert lokal
- **E02-S11**: Feiertag-Berechnungslogik (fuer Feiertag-Markierung im Kalender)
- **E02-S07/S08**: Settings (Bundesland fuer Feiertage, Arbeitstage fuer Wochenend-Erkennung, Urlaubstage/Jahr fuer Resturlaub)

**Hinweis**: Der Kalender kann mit Default-Settings (NW, Mo-Fr, 30 Tage) begonnen werden, bevor E02 komplett ist.

---

## Stories

### P2-E03-S01: iOS VacationCalendar-Komponente (Monatsansicht)

**Als** Nutzer
**moechte ich** einen Kalender sehen in dem ich durch Monate navigieren kann,
**damit** ich meine Urlaubs- und Abwesenheitstage im Ueberblick habe.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E02-S11 (HolidayCalculator), Phase 1 (VacationDay Model)
**Parallelisierbar mit**: P2-E03-S02 (Android), alle E01/E05 Stories
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `VacationCalendar.swift` als Custom SwiftUI View in `Shared/`
- [ ] Monatsansicht mit Header: "← {Monat} {Jahr} →"
- [ ] Wochentags-Kopfzeile: Mo Di Mi Do Fr Sa So
- [ ] Tagesraster (6x7 Grid, da ein Monat bis zu 6 Wochen umfassen kann)
- [ ] Navigation: Pfeil-Buttons zum Vor-/Zurueckblaettern
- [ ] Tages-Markierungen:
  - Normal (Arbeitstag): Schwarze/Weisse Zahl, antippbar
  - Urlaub: Cyan Hintergrund-Kreis
  - Feiertag: Lila Punkt, Name bei Long-Press, NICHT antippbar
  - Wochenende: Graue Zahl, NICHT antippbar
  - Heute: Roter Kreis-Umriss
  - Schulferien: Orangener Unterstrich (optional, nice-to-have)
- [ ] Given der Kalender zeigt Maerz 2026
  When der Nutzer auf "→" tippt
  Then zeigt der Kalender April 2026
- [ ] Given der 1. Mai 2026 (Feiertag) wird angezeigt
  When der Nutzer darauf tippt
  Then passiert nichts (nicht antippbar)
- [ ] Given der Nutzer ist in NW (Bundesland)
  When der Kalender Juni 2026 zeigt
  Then ist Fronleichnam (04.06.) als Feiertag markiert (lila)

**Technische Hinweise**:
- Custom Calendar statt Apple `DatePicker` (mehr Kontrolle ueber Markierungen)
- `LazyVGrid(columns: Array(repeating: GridItem(.flexible()), count: 7))`
- Wochentag des 1. des Monats berechnen fuer korrekte Einrueckung
- Wochenstart: Montag (deutscher Standard, nicht Sonntag!)
- Arbeitstage aus UserSettings.workDays Bitmask ermitteln (nicht hardcoded Mo-Fr)

---

### P2-E03-S02: Android VacationCalendar-Komponente (Monatsansicht)

**Als** Nutzer
**moechte ich** einen Kalender sehen in dem ich durch Monate navigieren kann,
**damit** ich meine Urlaubs- und Abwesenheitstage im Ueberblick habe.

**Plattform**: Android
**Abhaengigkeiten**: P2-E02-S11 (HolidayCalculator), Phase 1 (VacationDayEntity)
**Parallelisierbar mit**: P2-E03-S01 (iOS), alle E01/E05 Stories
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `VacationCalendar.kt` als Custom Composable in `ui/shared/`
- [ ] Gleiche Funktionalitaet wie iOS (Monatsnavigation, Tages-Markierungen)
- [ ] Material 3 Styling (Surface, Shapes, Typography)
- [ ] Wochenstart: Montag
- [ ] Arbeitstage aus UserSettings Bitmask

**Technische Hinweise**:
- `LazyVerticalGrid(columns = GridCells.Fixed(7))`
- `java.time.LocalDate` fuer Datumsberechnungen
- `DayOfWeek.MONDAY` als Wochenstart
- Monat-Navigation mit `YearMonth.plusMonths(1)` / `minusMonths(1)`

---

### P2-E03-S03: iOS Urlaubstage setzen/entfernen (Tap-to-Toggle)

**Als** Nutzer
**moechte ich** durch Tippen auf einen Tag Urlaub eintragen oder entfernen koennen,
**damit** die Urlaubserfassung schnell und intuitiv ist.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E03-S01 (VacationCalendar), Phase 1 (VacationDay Model + Sync)
**Parallelisierbar mit**: P2-E03-S04 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Tap auf leeren Arbeitstag = Urlaubstag erstellen:
  - VacationDay mit `date = getippter Tag`, `isPendingSync = true` wird in SwiftData gespeichert
  - Tag wird sofort cyan markiert
  - Resturlaub-Zaehler wird um 1 reduziert
- [ ] Tap auf markierten Urlaubstag = Urlaubstag entfernen:
  - VacationDay wird lokal geloescht (PendingDelete erstellen fuer Sync)
  - Tag wird sofort normal dargestellt
  - Resturlaub-Zaehler wird um 1 erhoeht
- [ ] Wochenenden und Feiertage: Tap wird ignoriert (kein visuelles Feedback)
- [ ] Given der Nutzer hat 25 von 30 Resturlaub
  When er den 15. Juli als Urlaub markiert
  Then wird ein VacationDay erstellt
  And Resturlaub zeigt 24 von 30
- [ ] Given der Nutzer hat 0 Resturlaub
  When er einen weiteren Tag markieren will
  Then wird eine Warnung angezeigt: "Kein Resturlaub mehr verfuegbar"
  And der Tag wird TROTZDEM markiert (Warnung, kein Blocker -- Nutzer weiss es besser)
- [ ] Sync wird automatisch nach Toggle getriggert

**Technische Hinweise**:
- VacationDay-Sync: Beim naechsten syncVacationDays() werden ALLE lokalen Tage gesendet
- PendingDelete fuer Loeschungen die noch nicht synchronisiert wurden
- Haptic Feedback bei Toggle (.impact(.light))

---

### P2-E03-S04: Android Urlaubstage setzen/entfernen (Tap-to-Toggle)

**Als** Nutzer
**moechte ich** durch Tippen auf einen Tag Urlaub eintragen oder entfernen koennen,
**damit** die Urlaubserfassung schnell und intuitiv ist.

**Plattform**: Android
**Abhaengigkeiten**: P2-E03-S02 (VacationCalendar), Phase 1 (VacationDayEntity + Sync)
**Parallelisierbar mit**: P2-E03-S03 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Gleiche Toggle-Logik wie iOS
- [ ] VacationDayEntity erstellen/loeschen via Room DAO
- [ ] Sofortige UI-Aktualisierung (Room Flow)
- [ ] Haptic Feedback bei Toggle
- [ ] Warnung bei 0 Resturlaub (Snackbar)

**Technische Hinweise**:
- `dao.insert()` / `dao.deleteById()` fuer Toggle
- `HapticFeedback.performHapticFeedback(HapticFeedbackType.LongPress)`

---

### P2-E03-S05: iOS Resturlaub-Anzeige

**Als** Nutzer
**moechte ich** meinen verbleibenden Urlaubsanspruch prominent sehen,
**damit** ich weiss wie viele Tage ich noch nehmen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E03-S01 (Kalender-Screen), Phase 1 (VacationDay Model, UserSettings)
**Parallelisierbar mit**: P2-E03-S06 (Android)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Card oberhalb des Kalenders:
  - "Resturlaub" als Titel
  - "{verbleibend} von {gesamt} Tagen" (z.B. "25 von 30 Tagen")
  - Fortschrittsbalken (ProgressView)
  - "{genommen} genommen" als Untertitel
- [ ] Berechnung: `verbleibend = urlaubstageProJahr - anzahlVacationDays(imAktuellenJahr)`
- [ ] Given der Nutzer hat 30 Urlaubstage/Jahr und 5 markierte VacationDays in 2026
  When der Urlaub-Tab geoeffnet wird
  Then zeigt die Card "25 von 30 Tagen" mit 16.7% Fortschritt
- [ ] Warnung (< 5 Tage verbleibend): Fortschrittsbalken wird orange/rot

**Technische Hinweise**:
- VacationDays fuer das aktuelle Jahr aus SwiftData zaehlen
- `ProgressView(value: Float(genommen), total: Float(gesamt))`
- `.tint(verbleibend < 5 ? .orange : .accentColor)`

---

### P2-E03-S06: Android Resturlaub-Anzeige

**Als** Nutzer
**moechte ich** meinen verbleibenden Urlaubsanspruch prominent sehen,
**damit** ich weiss wie viele Tage ich noch nehmen kann.

**Plattform**: Android
**Abhaengigkeiten**: P2-E03-S02 (Kalender-Screen), Phase 1 (VacationDayEntity, UserSettingsEntity)
**Parallelisierbar mit**: P2-E03-S05 (iOS)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Material 3 Card oberhalb des Kalenders
- [ ] Gleiche Anzeige und Berechnung wie iOS
- [ ] `LinearProgressIndicator` fuer Fortschrittsbalken
- [ ] Farbwechsel bei < 5 Tagen verbleibend

**Technische Hinweise**:
- `LinearProgressIndicator(progress = genommen.toFloat() / gesamt.toFloat())`
- VacationDays aus Room Flow zaehlen (`.filter { it.date.year == currentYear }`)

---

### P2-E03-S07: iOS Urlaub-Screen Zusammenbau + ViewModel

**Als** Nutzer
**moechte ich** den Urlaub-Tab als vollstaendigen Screen nutzen koennen,
**damit** ich Urlaub komfortabel verwalten kann.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E03-S01 (Kalender), P2-E03-S03 (Toggle), P2-E03-S05 (Resturlaub)
**Parallelisierbar mit**: P2-E03-S08 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `VacationScreen.swift` als vollstaendiger Tab-Screen in `Features/Vacation/`
- [ ] `VacationViewModel.swift` als `@Observable` Klasse:
  - `vacationDays: [VacationDay]` (via @Query in View)
  - `settings: UserSettings` (fuer Bundesland, Arbeitstage, Urlaubstage/Jahr)
  - `currentMonth: Int`, `currentYear: Int`
  - `holidays: [(Date, String)]` (berechnet via HolidayCalculator)
  - `toggleVacationDay(date:)` Methode
  - `resturlaubCount: Int` (computed)
- [ ] Screen-Aufbau (scrollbar):
  1. Resturlaub-Card
  2. VacationCalendar
  3. Legende (Farbbedeutungen)
  4. Kommende Feiertage (Liste der naechsten 3-5 Feiertage)
- [ ] Large Title: "Urlaub"
- [ ] Given der Nutzer oeffnet den Urlaub-Tab
  When die Daten geladen sind
  Then wird der aktuelle Monat angezeigt
  And Resturlaub wird korrekt berechnet
  And Feiertage des Bundeslandes sind markiert

**Technische Hinweise**:
- ScrollView { VStack { ResturlaubCard; VacationCalendar; Legend; Holidays } }
- Feiertag-Liste: naechste Feiertage ab heute filtern und sortieren
- VacationDay-Set fuer schnelle Lookup: `Set(vacationDays.map { Calendar.current.startOfDay(for: $0.date) })`

---

### P2-E03-S08: Android Urlaub-Screen Zusammenbau + ViewModel

**Als** Nutzer
**moechte ich** den Urlaub-Tab als vollstaendigen Screen nutzen koennen,
**damit** ich Urlaub komfortabel verwalten kann.

**Plattform**: Android
**Abhaengigkeiten**: P2-E03-S02 (Kalender), P2-E03-S04 (Toggle), P2-E03-S06 (Resturlaub)
**Parallelisierbar mit**: P2-E03-S07 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `VacationScreen.kt` in `features/vacation/`
- [ ] `VacationViewModel.kt` mit StateFlow analog zu iOS
- [ ] LazyColumn mit gleichen Sektionen wie iOS
- [ ] Room Flow fuer VacationDays und Settings

**Technische Hinweise**:
- LazyColumn { item { ResturlaubCard }; item { Calendar }; item { Legend }; items(holidays) { ... } }
- `LocalDate.parse(entity.date)` fuer Datum-Vergleiche
