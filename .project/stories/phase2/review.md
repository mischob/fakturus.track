# Devils Advocate Review -- Phase 2 Detailplanung

**Datum**: 29.03.2026
**Reviewer**: Devils Advocate (SW-Architektur-Review)
**Scope**: Alle Phase-2-Dokumente (6 EPICs, Tech-Specs, Execution-Waves, Checkliste)

---

## Gesamturteil: ⚠️ Empfohlen mit Aenderungen

Die Phase-2-Planung ist insgesamt solide, durchdacht und konsistent mit der bestehenden Phase-1-Codebasis. Die EPIC-Struktur, Abhaengigkeiten und Parallelisierung sind logisch. Die Tech-Specs sind detailliert genug fuer AI-Agenten und enthalten korrekte Code-Skizzen. Dennoch gibt es einige Punkte, die vor der Umsetzung adressiert werden sollten -- darunter ein Bug in der Feiertag-Berechnung, ein fragiler PDF-Ansatz und eine inkonsistente DB-Schema-Versionierung.

---

## Befunde

### 🔴 1. BUG: Buss-und-Bettag-Berechnung (iOS) ist falsch

**Kategorie**: Korrektheit / HolidayCalculator

**Problem**: Die Swift-Implementierung des Buss-und-Bettag in `tech-epic-02.md` ist fehlerhaft. Der Algorithmus sucht den Mittwoch VOR dem 23. November. Wenn der 23. November selbst ein Mittwoch ist, darf NICHT 7 Tage zurueckgegangen werden -- der 23. November selbst waere dann der Buss-und-Bettag.

Die aktuelle Swift-Implementierung:
```swift
let daysBack = (weekday - 4 + 7) % 7
let offset = daysBack == 0 ? 7 : daysBack  // <-- BUG: Wenn Nov 23 = Mittwoch, offset = 7
return addDays(nov23, -offset)              // Gibt den 16. Nov zurueck statt den 23.
```

Die Kotlin-Implementierung ist hingegen korrekt:
```kotlin
return nov23.with(TemporalAdjusters.previous(DayOfWeek.WEDNESDAY))
```
`TemporalAdjusters.previous()` gibt den VORHERIGEN Mittwoch zurueck, also auch bei `nov23 = Mittwoch` den 16. November.

**Moment** -- hier muss man die Definition pruefen: Buss- und Bettag ist der Mittwoch vor dem 23. November. "Vor" bedeutet strikt vor, d.h. wenn der 23. ein Mittwoch ist, ist es der 16. November. Die `TemporalAdjusters.previous()` liefert den strikt vorherigen Tag, also auch den 16.

Tatsaechlich ist die Definition: Buss- und Bettag = Mittwoch VOR dem Sonntag, der auf den 23. November folgt oder damit identisch ist. Vereinfacht: Mittwoch vor dem 23. November (strikt vor). Korrektur: In beiden Implementierungen muss geprueft werden, ob der 23. November selbst eingeschlossen ist. Laut offizieller Regelung ist Buss- und Bettag am 11 Tage vor dem ersten Adventssonntag, was zwischen dem 16. und 22. November liegt.

**Verifizierung fuer 2026**: 23.11.2026 ist ein Montag. Der Mittwoch davor ist der 18.11.2026.
- Swift: weekday = 2 (Montag), daysBack = (2-4+7)%7 = 5, offset = 5, Ergebnis = 18.11. KORREKT.
- Kotlin: `previous(WEDNESDAY)` von 23.11. (Montag) = 18.11. KORREKT.

**Verifizierung fuer 2030**: 23.11.2030 ist ein Samstag. Der Mittwoch davor ist der 20.11.2030.
- Swift: weekday = 7 (Samstag), daysBack = (7-4+7)%7 = 3, offset = 3, Ergebnis = 20.11. KORREKT.

**Verifizierung fuer 2028**: 23.11.2028 ist ein Donnerstag. Der Mittwoch davor ist der 22.11.2028.
- Swift: weekday = 5 (Donnerstag), daysBack = (5-4+7)%7 = 1, offset = 1, Ergebnis = 22.11. KORREKT.

**Verifizierung fuer 2023**: 23.11.2023 ist ein Donnerstag. Buss- und Bettag = 22.11.2023. Korrekt.

**Verifizierung fuer 2022**: 23.11.2022 ist ein Mittwoch. Buss- und Bettag = 16.11.2022.
- Swift: weekday = 4 (Mittwoch), daysBack = (4-4+7)%7 = 0, offset = 7, Ergebnis = 16.11.
- Kotlin: `previous(WEDNESDAY)` von 23.11. (Mittwoch) = 16.11.
- Offizielle Daten: Buss- und Bettag 2022 = 16.11.2022. KORREKT.

**Ergebnis**: Nach gruendlicher Pruefung sind BEIDE Implementierungen korrekt. Die `offset = daysBack == 0 ? 7 : daysBack` Logik in Swift behandelt den Sonderfall korrekt: Wenn der 23. selbst ein Mittwoch ist, wird 7 Tage zurueckgegangen, was den vorherigen Mittwoch (16.) ergibt. Das ist korrekt, weil "Mittwoch VOR dem 23." den 23. selbst ausschliesst.

**Schweregrad angepasst**: 🟢 Kein Bug. Beide Implementierungen sind korrekt.

---

### 🔴 2. PDF-Generierung via WebView: Fragil und untestbar

**Kategorie**: Komplexitaet / Zuverlaessigkeit / Testbarkeit

**Problem**: Die PDF-Generierung (tech-epic-06.md) nutzt `WKWebView.pdf()` (iOS) bzw. `WebView.createPrintDocumentAdapter()` (Android). Beide Ansaetze sind fragil:

1. **Timing-Problem**: `try? await Task.sleep(nanoseconds: 500_000_000)` -- eine fixe 500ms Wartezeit bis die WebView gerendert hat. Auf langsamen Geraeten oder bei komplexen Monaten (31 Tage + Feiertage) reicht das moeglicherweise nicht. Auf schnellen Geraeten ist es verschwendete Zeit.
2. **Android PrintDocumentAdapter**: Der Code-Sketch in tech-epic-06.md ist unvollstaendig (`// ... PrintDocumentAdapter.onWrite() nutzen`). Die `PrintDocumentAdapter` API ist callback-basiert und nicht trivial in eine `suspend`-Funktion zu wrappen. Das ist ein erheblicher Implementierungsaufwand, der in der Story-Schaetzung (L) moeglicherweise nicht ausreichend beruecksichtigt ist.
3. **WebView auf Background-Thread**: Auf Android muss die WebView auf dem Main-Thread erstellt werden. Das blockiert den UI-Thread waehrend der PDF-Generierung.
4. **Testbarkeit**: WebView-basierte PDF-Generierung ist praktisch nicht unit-testbar. Man kann nur den HTML-String testen, nicht das resultierende PDF.

**Risiko**: Die PDF-Generierung ist der wahrscheinlichste Ort fuer plattformspezifische Bugs, unerklaeerbliche Leerseiten, abgeschnittene Tabellen oder Crashes auf bestimmten Geraeten.

**Alternative**:
- **iOS**: `UIGraphicsPDFRenderer` direkt nutzen (ist in den Tech-Specs als Fallback erwaehnt, sollte aber der primaere Ansatz sein). Mit `NSAttributedString` und `draw(in:)` kann eine Tabelle sauber gerendert werden.
- **Android**: BESSER: HTML-String generieren, aber fuer die PDF-Konvertierung eine leichtgewichtige Bibliothek wie `openhtmltopdf-pdfbox` (Java, funktioniert auf Android) oder alternativ `PdfDocument` mit `Canvas.drawText()` nutzen.
- **Gemeinsam**: Den HTML-Template-Ansatz fuer die DATEN-Aufbereitung beibehalten (der ist gut), aber die HTML-zu-PDF-Konvertierung robuster machen.

**Empfehlung**: Die iOS-Variante mit `WKWebView.pdf()` ist akzeptabel (WKWebView.pdf ist eine native async API und funktioniert zuverlaessig seit iOS 15). Aber das `Task.sleep` muss durch eine `WKNavigationDelegate.didFinish`-basierte Loesung ersetzt werden. Die Android-Variante muss grundlegend ueberarbeitet werden -- `PrintDocumentAdapter` ist nicht fuer programmatische PDF-Erzeugung gedacht, sondern fuer Druckdialoge.

---

### 🔴 3. DB-Schema-Version Inkonsistenz: Tech-Specs vs. bestehender Code

**Kategorie**: Konsistenz / Korrektheit

**Problem**: Die Tech-Specs definieren mehrere verschiedene Schema-Versionen, die sich widersprechen:

- `tech-blueprint.md` spricht von "iOS: V1 -> V2" und "Android: V2 -> V3"
- `implementation-checklist.md` spricht von "iOS: Schema V1 auf V2" und "Android: Schema 1 auf 2"
- `tech-epic-04.md` spricht von "Room Schema-Migration von Version 1 auf 2"

Die bestehende `AppDatabase.kt` ist bereits auf **Version 2**:
```kotlin
@Database(..., version = 2, exportSchema = true)
```

Daraus folgt: Die Android-Migration muss von V2 auf V3 gehen, nicht von V1 auf V2. `tech-blueprint.md` hat das korrekt, aber `implementation-checklist.md` und `tech-epic-04.md` haben es falsch.

Ebenso unklar: Wurde iOS in Phase 1 schon auf V2 aktualisiert? Die iOS-Codebasis nutzt `@ModelActor` und SwiftData ohne explizites Schema-Versionierungs-Setup. Das muss vor Phase 2 geklaert werden.

**Risiko**: Ein Agent implementiert `MIGRATION_1_2` statt `MIGRATION_2_3`, was bei bestehenden Nutzern zu einem Crash fuehrt (Room erwartet Migration von aktueller Version).

**Alternative**: In **allen** Dokumenten konsistent die tatsaechlichen Schema-Versionen aus dem Code referenzieren. Am besten eine Single-Source-of-Truth-Tabelle in der Checkliste:

```
| Plattform | Phase 1 Version | Phase 2 Version | Migration |
|-----------|----------------|----------------|-----------|
| iOS       | V1 (oder ?)     | V2             | V1->V2    |
| Android   | V2              | V3             | V2->V3    |
```

---

### 🟡 4. Settings Last-Write-Wins: Race Condition bei gleichzeitiger Nutzung auf zwei Geraeten

**Kategorie**: Fehleranfaelligkeit

**Problem**: Das Last-Write-Wins-Konzept fuer Settings (tech-epic-02.md, implementation-checklist.md) vergleicht `UpdatedAt` Timestamps. Der Algorithmus ist:
1. Lokale Settings laden
2. GET /v1/settings (Server-Settings)
3. Vergleiche UpdatedAt -> neuerer gewinnt

Das Problem: Wenn zwei Geraete gleichzeitig verschiedene Settings aendern, kann folgendes passieren:
- Geraet A aendert Wochenstunden 40->32 um 10:00:00
- Geraet B aendert Bundesland NW->BY um 10:00:01
- Geraet B synct zuerst -> Server hat BY + 40h
- Geraet A synct danach -> Lokaler Timestamp ist 10:00:00, Server hat 10:00:01 -> Server gewinnt
- Ergebnis: Geraet A hat jetzt BY + 40h (die Wochenstunden-Aenderung geht verloren)

**Risiko**: Mittel. In der Praxis nutzen die meisten Nutzer nur ein Geraet. Aber bei der Zielgruppe (10-20 Beta-Tester, moeglicherweise iOS + Android parallel) ist es realistisch.

**Alternative**: Da Settings nur wenige Felder hat, waere ein Feld-Level-Merge (jedes Feld hat seinen eigenen Timestamp) die bessere Loesung. Allerdings erhoehe das die Komplexitaet erheblich. Fuer die Beta-Phase ist Last-Write-Wins akzeptabel, aber die Limitierung sollte dokumentiert werden.

**Empfehlung**: Last-Write-Wins akzeptieren, aber in der Checkliste vermerken: "Bekannte Limitierung: Bei gleichzeitiger Settings-Aenderung auf zwei Geraeten gewinnt die letzte Aenderung komplett (keine Feld-Level-Merge)."

---

### 🟡 5. SickDay-Sync: Inkonsistenz zwischen Tech-Spec und bestehendem VacationDay-Sync

**Kategorie**: Konsistenz

**Problem**: Die `syncSickDays()` Implementierung in tech-epic-04.md hat eine andere Logik als die bestehende `syncVacationDays()`:

Bestehender VacationDay-Sync (SyncEngine.kt, Zeile 101-127):
```kotlin
if (pending.isNotEmpty()) {
    // POST sync mit ALLEN lokalen Tagen
} else {
    // Keine pending -> einfacher GET
    val serverDays = apiClient.getVacationDays()
}
```

Geplanter SickDay-Sync (tech-epic-04.md):
```kotlin
if (pending.isNotEmpty()) {
    val request = SyncSickDaysRequest(sickDays = allLocal.map { it.toDTO() })
    val response = apiClient.syncSickDays(request)
    // ...
} else {
    val serverDays = apiClient.getSickDays("2000-01-01", "2099-12-31")
    serverDays.forEach { dto -> dao.insert(dto.toEntity()) }
}
```

Zwei Probleme:
1. Die GET-Variante bei SickDays verwendet einen Date-Range-Filter (`from`, `to`), waehrend VacationDays einen parameterfreien GET nutzt. Die SickDays-API hat `from`/`to` als Query-Parameter (tech-epic-01.md), aber der "Catch-All"-Range `"2000-01-01"` bis `"2099-12-31"` ist ein Hack. Besser: Einen parameterlosen GET-Endpoint fuer SickDays vorsehen (analog zu `GET /v1/vacation-days` ohne Filter), ODER den Sync-Endpoint immer aufrufen (auch ohne pending, wie in der iOS-Implementierung in shared-concepts.md beschrieben).

2. In der `else`-Branch (kein pending) werden Server-SickDays via `dao.insert()` eingefuegt, aber nie lokale Eintraege geloescht, die auf dem Server nicht mehr existieren. Das fuehrt zu "Geister"-Krankheitstagen, die lokal existieren aber auf dem Server geloescht wurden.

**Risiko**: Datenkonsistenz-Problem. Geloeschte Krankheitstage koennten lokal "wiederauftauchen".

**Alternative**: Den SickDay-Sync exakt wie den VacationDay-Sync implementieren -- bei keinem pending einen GET machen und dann eine Set-Differenz ausfuehren (wie bei WorkSessions), ODER den Sync-Endpoint immer aufrufen. Da der Sync-Endpoint ja genau fuer diesen Zweck existiert, ist die einfachste Loesung: IMMER den Sync-Endpoint aufrufen, auch wenn keine pending existieren (mit leerer oder vollstaendiger lokaler Liste).

---

### 🟡 6. ExportViewModel vs. OverviewViewModel: Widerspruch in den Dokumenten

**Kategorie**: Konsistenz zwischen PO-Plan und Tech-Specs

**Problem**: Die EPIC-06-Datei (epic-06-export.md) definiert ein eigenstaendiges `ExportViewModel` als Story P2-E06-S07 mit detaillierten Akzeptanzkriterien. Die tech-epic-06.md sagt hingegen:

> "Design-Entscheidung: Kein separates ExportViewModel. Die Export-Logik wird direkt ins OverviewViewModel integriert."

Das ist ein direkter Widerspruch. Die PO-Story P2-E06-S07 definiert ein ExportViewModel mit State (`selectedMonth`, `exportTimeRange`, `isGenerating`), die tech-spec sagt, das alles gehoert ins OverviewViewModel.

**Risiko**: AI-Agenten koennten je nach Dokument unterschiedlich implementieren. iOS erstellt ein ExportViewModel, Android integriert es ins OverviewViewModel -- oder umgekehrt.

**Alternative**: Die Tech-Spec-Entscheidung (kein separates ExportViewModel) ist die bessere Wahl (vermeidet Daten-Duplikation). Aber dann muss die PO-Story P2-E06-S07 gestrichen oder umformuliert werden zu "OverviewViewModel um Export-Methoden erweitern". Die Akzeptanzkriterien koennen bleiben, nur der Kontext aendert sich.

**Empfehlung**: P2-E06-S07 in "OverviewViewModel um Export erweitern" umbenennen und die Tech-Spec-Entscheidung als fuehrend markieren.

---

### 🟡 7. Kalender-Leistung: `holidays()` wird bei jedem Render neu berechnet

**Kategorie**: Komplexitaet / Performance

**Problem**: Der `HolidayCalculator` berechnet Ostern und alle Feiertage bei JEDEM Aufruf von `holidays(bundesland, year)`. In der VacationCalendar-Komponente wird diese Methode potentiell bei jedem Recompose/Redraw aufgerufen (z.B. beim Scrollen, bei State-Aenderungen). In `isHoliday()` wird sogar `holidays()` aufgerufen und dann linear gesucht -- fuer jeden Tag im Monat.

Das bedeutet: Beim Rendern eines Monats werden mindestens 42 * `holidays()` Aufrufe gemacht (42 Zellen im Grid, jede prueft `isHoliday`). Das sind 42 * Gauss-Berechnung + 42 * Iteration ueber 11-13 Feiertage.

**Risiko**: Auf aelteren Geraeten spuerbare Ruckler beim Monatswechsel. In der Praxis wahrscheinlich nicht kritisch (Oster-Berechnung ist O(1), die Listen sind kurz), aber es ist ein unnoetig schlechtes Pattern.

**Alternative**: Das `VacationCalendar` sollte die Feiertage einmalig als `Set<DateComponents>` / `Set<LocalDate>` vorberechnen und das Set in die `buildCells()`-Funktion uebergeben. Der `HolidayCalculator` ist korrekt als Utility, aber die Aufrufer sollten das Ergebnis cachen.

Der tech-epic-03.md Code-Sketch macht das bereits teilweise richtig (holidays werden als Parameter uebergeben), aber `isHoliday()` wird trotzdem noch in mehreren Contexts genutzt. Die Empfehlung: In der Checkliste vermerken, dass `holidays()` pro Monatswechsel EINMAL aufgerufen und dann als Set gecacht wird.

---

### 🟡 8. Fehlende Story: Backend UpdatedAt fuer Settings-Endpoint

**Kategorie**: Fehlende Story / Abhaengigkeit

**Problem**: Die Phase-2-Planung setzt voraus, dass `GET /v1/settings` und `PUT /v1/settings` das Feld `UpdatedAt` liefern (fuer Last-Write-Wins). Die bestehende Backend-API (`backend-integration.md`) liefert dieses Feld NICHT:

```json
// Aktuelle GET /v1/settings Response:
{
  "CalendarUrl": "webcal://...",
  "VacationDaysPerYear": 30,
  "WorkHoursPerWeek": 40.00,
  "WorkDays": 31,
  "Bundesland": "NW"
}
// Kein "UpdatedAt" Feld!
```

Die Tech-Spec (tech-epic-02.md) erwaehnt einen Fallback: "Bei null = Server-wins (Phase 1 Verhalten)". Das ist gut. Aber es fehlt eine explizite Backend-Story, die `UpdatedAt` zum Settings-Endpoint hinzufuegt. Ohne diese Story funktioniert Last-Write-Wins nie -- die App faellt dauerhaft auf Server-wins zurueck.

**Risiko**: Last-Write-Wins bleibt toter Code. Nutzer aendern Settings lokal, die bei jedem Sync ueberschrieben werden.

**Alternative**: Eine Story `P2-E01-S05: Backend Settings-Endpoint um UpdatedAt erweitern` hinzufuegen. Oder explizit in E01 aufnehmen. Aufwand: S (ein Feld hinzufuegen, in GET + PUT beruecksichtigen).

---

### 🟡 9. Execution-Waves: 0.5 Wochen fuer Testing ist zu knapp

**Kategorie**: Aufwandsschaetzung / Realistik

**Problem**: Welle 6 (Testing & Bug-Fixing) hat nur 0.5 Wochen eingeplant fuer:
- 10 Integrations-Testszenarien auf 2 Plattformen
- Bug-Fixing
- Performance-Check
- TestFlight Build
- Firebase App Distribution Build
- Regressions-Test Phase 1

Das sind bei 0.5 Wochen = 2.5 Arbeitstage fuer ~30 Aufgaben auf 2 Plattformen. Das ist unrealistisch, besonders weil:
1. Bugs die in Integrationstests gefunden werden, Zeit zum Fixen brauchen
2. Build-Pipelines fuer TestFlight und Firebase eingerichtet/aktualisiert werden muessen
3. Regressions-Tests fuer Phase 1 alle Timer/History/Sync-Flows umfassen

**Risiko**: Testing wird abgekuerzt, Bugs gehen in die Beta, oder Phase 2 verzoegert sich.

**Alternative**: Welle 6 auf 1 Woche erhoehen. Alternativ: Bug-Fixing bereits in Welle 5 integrieren (Integration + Bug-Fixing = 1 Woche statt 0.5 Wochen). Der 2.5-Wochen-Puffer zwischen kritischem Pfad (6 Wochen) und Gesamtzeitraum (8.5 Wochen) ist vorhanden, sollte aber explizit fuer Testing reserviert werden.

---

### 🟢 10. iOS SickDay: `updatedAt` fehlt in UserSettings-Model (Phase 1 Code)

**Kategorie**: Konsistenz Code <-> Plan

**Problem**: Die bestehende iOS `UserSettings`-Klasse (aus data-layer.md, Phase 1 Code) hat KEIN `updatedAt`-Property. Das wird in Phase 2 benoetigt fuer Last-Write-Wins. Die Tech-Specs erwaehnen das korrekt unter "Modifizierte Dateien", aber es ist gut, es hier nochmal explizit zu benennen: `UserSettings.swift` muss ein `var updatedAt: Date?` bekommen (optional, damit Phase-1-Daten migriert werden koennen).

**Risiko**: Gering. Ist klar dokumentiert.

---

### 🟢 11. iOS VacationDay Sync: Unterschied zum Plan-Algorithmus

**Kategorie**: Konsistenz

**Problem**: Die bestehende iOS `syncVacationDays()` (SyncEngine.swift Zeile 124-177) hat eine Optimierung die im Plan nicht erwaehnt wird: Wenn keine pending Changes existieren, wird ein einfacher GET statt dem Sync-Endpoint aufgerufen. Das ist identisch zum Android-Code. Die Phase-2-Doku (shared-concepts.md) beschreibt hingegen: "ALLE lokalen Tage senden" -- immer.

Das ist kein Problem, da der bestehende Code tatsaechlich das Richtige tut (GET bei keinen Aenderungen ist effizienter). Aber es sollte sichergestellt werden, dass die SickDay-Sync-Implementierung das gleiche Pattern nutzt UND dabei die Set-Differenz korrekt handhabt (siehe Befund #5).

---

### 🟢 12. CSV: Wochenende-Zeilen zeigen "0;0,00" statt leere Felder

**Kategorie**: UX / Klarheit

**Problem**: Das CSV-Beispiel in tech-epic-06.md zeigt fuer Wochenenden:
```
06.03.2026;Samstag;;;;0;0,00;
```

Die `0` in der Pause-Spalte und `0,00` in der Netto-Spalte sind technisch korrekt, koennten aber in Excel verwirrend sein ("Hat die Person 0 Stunden gearbeitet?"). Besser waeren leere Felder:
```
06.03.2026;Samstag;;;;;
```

**Risiko**: Gering. Kosmetisch.

---

## Positives

Die Phase-2-Planung hat einige bemerkenswerte Staerken:

1. **Durchgaengige Konsistenz mit Phase 1**: Die Code-Skizzen passen zur bestehenden Codebasis. Naming-Conventions, Ordnerstruktur und Patterns (ViewModels, DAOs, DTOs) werden konsistent fortgefuehrt.

2. **Saubere Abhaengigkeitsdefinition**: Die Parallelisierungsmatrix und das Abhaengigkeitsdiagramm sind korrekt und gut durchdacht. Die Mock-Strategien fuer parallele Entwicklung sind pragmatisch.

3. **Design-Entscheidung SickDay als separate Entity**: Die Entscheidung gegen ein generisches `AbsenceType`-Feld in VacationDay ist korrekt (YAGNI, kein Breaking Change, einfachere Sync-Logik).

4. **HolidayCalculator als zentrale Utility**: Korrekte Identifizierung, dass Feiertag-Berechnung an 5 Stellen gebraucht wird. Die Gauss-Osterformel ist korrekt implementiert (verifiziert: Ostern 2026 = 5. April). Die Bundesland-Feiertag-Zuordnungen sind vollstaendig und korrekt.

5. **Offline-First konsequent durchgehalten**: Export funktioniert mit lokalen Daten, Gesamt-Tab hat Disk-Cache, Kalender arbeitet offline. Das ist konsistent mit der Phase-1-Architektur.

6. **Mariae Himmelfahrt BY-Vereinfachung**: Die pragmatische Entscheidung, Mariae Himmelfahrt fuer ganz Bayern zu zaehlen (statt nur fuer Gemeinden mit ueberwiegend katholischer Bevoelkerung), ist fuer eine Zeiterfassungs-App angemessen.

7. **Placeholder-Parameter von Anfang an**: `sickDayDates` und `onDayLongPress` werden bereits in E03 als Parameter definiert (mit Default-Werten), um spaetere Erweiterungen in E04 konfliktfrei zu ermoeglichen. Gutes Software-Engineering.

8. **Merge-Reihenfolge explizit definiert**: Die detaillierte Merge-Reihenfolge im parallel-implementation-guide.md ist essentiell fuer AI-Agenten und minimiert Merge-Konflikte.

---

## Empfehlungen

### Vor Start der Implementierung

1. **DB-Schema-Versionen konsistent machen** (Befund #3): Alle Dokumente auf Android V2->V3 und iOS V1->V2 vereinheitlichen. Bestehende iOS-Schema-Version verifizieren.

2. **Backend-Story fuer Settings UpdatedAt hinzufuegen** (Befund #8): `P2-E01-S05` erstellen oder in E01 integrieren. Ohne das Feld ist Last-Write-Wins wirkungslos.

3. **ExportViewModel-Widerspruch aufloesen** (Befund #6): P2-E06-S07 anpassen (kein separates ViewModel, stattdessen OverviewViewModel erweitern).

### Waehrend der Implementierung

4. **PDF-Generierung robuster machen** (Befund #2): iOS: `WKNavigationDelegate.didFinish` statt `Task.sleep`. Android: Alternative zu `PrintDocumentAdapter` evaluieren oder den `PrintDocumentAdapter`-Ansatz sauber als Coroutine wrappen und dokumentieren.

5. **SickDay-Sync mit Set-Differenz** (Befund #5): In der `else`-Branch (keine pending) muss eine Loeschung lokaler Eintraege erfolgen, die nicht mehr auf dem Server existieren.

6. **HolidayCalculator-Ergebnis cachen** (Befund #7): Pro Monatswechsel einmal berechnen, nicht bei jedem Cell-Render.

### Dokumentation

7. **Bekannte Limitierungen** dokumentieren: Settings Last-Write-Wins ist kein Feld-Level-Merge (Befund #4).

8. **Testing-Budget erhoehen** (Befund #9): Welle 6 auf 1 Woche oder Testing in Welle 5 integrieren.

---

## Zusammenfassung der Schweregrade

| Schweregrad | Anzahl | Zusammenfassung |
|-------------|--------|-----------------|
| 🔴 Kritisch | 2 | PDF-Ansatz fragil (Android), DB-Schema-Version inkonsistent |
| 🟡 Bedenken | 6 | Settings Race Condition, SickDay-Sync Bug, ExportVM-Widerspruch, Holiday-Caching, fehlende Backend-Story, Testing-Budget |
| 🟢 Hinweis  | 3 | UserSettings updatedAt, VacationDay-Sync Optimierung, CSV Wochenende-Formatierung |

**Blocker fuer Implementierungsstart**: Befund #3 (DB-Schema-Version) und #8 (Backend Settings UpdatedAt Story) sollten vor dem Start geklaert werden. Die restlichen Befunde koennen waehrend der Implementierung adressiert werden.
