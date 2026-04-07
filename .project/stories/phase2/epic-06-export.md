# EPIC 06: Export (PDF/CSV + Share)

## Ziel

Nutzer koennen Monatsreports als PDF und Arbeitszeitdaten als CSV exportieren und ueber das System-Share-Sheet teilen (E-Mail, AirDrop, Messenger, Dateien-App). Die Export-Funktionalitaet wird im Gesamt-Tab integriert. Der Export erfolgt client-seitig (kein Backend noetig), was zum Offline-First-Ansatz passt.

## Abhaengigkeiten

- **E05**: Gesamt-Tab (Export-Sektion wird dort integriert)
- **Phase 1**: WorkSession-Daten lokal verfuegbar (History)
- **E03/E04**: Urlaubs- und Krankheitstage fuer vollstaendigen Report (optional -- Export kann auch ohne diese Daten starten)

## Design-Entscheidung

**Client-seitige PDF-Generierung** (nicht Backend):
- Passt zum Offline-First-Ansatz
- Keine Backend-Aenderung noetig
- Nutzer hat auch offline Zugriff auf seine Reports
- Siehe `marketing-integration-changelog.md` Entscheidung #4

---

## Stories

### P2-E06-S01: iOS PDF-Monatsreport generieren

**Als** Nutzer
**moechte ich** einen professionell formatierten PDF-Monatsreport erstellen koennen,
**damit** ich meinem Arbeitgeber oder Behoerden einen Arbeitszeitnachweis vorlegen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E05-S07 (Gesamt-Screen als Kontext), Phase 1 (WorkSession-Daten)
**Parallelisierbar mit**: P2-E06-S02 (Android PDF), P2-E06-S03/S04 (CSV)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `PDFReportGenerator.swift` in `Services/Export/`
- [ ] Methode: `generateMonthlyReport(month: Int, year: Int, sessions: [WorkSession], vacationDays: [VacationDay], sickDays: [SickDay], settings: UserSettings) -> Data`
- [ ] PDF-Inhalt:
  - Header: "fakturus.track" Logo/Text + "Arbeitszeitnachweis" + Monat/Jahr
  - Mitarbeiter-Name (aus B2C Claims, falls verfuegbar)
  - Tabelle mit Spalten: Datum | Wochentag | Start | Ende | Pause (min) | Netto (h) | Typ
  - Typ-Spalte: "Arbeit", "Urlaub", "Feiertag", "Krank"
  - Feiertage und Urlaubstage als eigene Zeilen (ohne Start/Ende, nur Datum und Typ)
  - Zusammenfassung am Ende:
    - Soll-Stunden
    - Ist-Stunden
    - Ueberstunden (+/-)
    - Urlaubstage genommen
    - Krankheitstage
  - Footer: Generiert am {Datum} mit fakturus.track
- [ ] Given Maerz 2026 mit 22 Arbeitstagen, 2 Urlaubstagen, 1 Krankheitstag
  When der PDF-Report generiert wird
  Then enthaelt das PDF 31 Zeilen (alle Tage des Monats, inkl. WE als leer/grau)
  And die Zusammenfassung zeigt korrekte Werte
- [ ] PDF ist A4-Format, Hochformat
- [ ] Saubere Formatierung (kein Text-Ueberlauf, korrekte Ausrichtung)

**Technische Hinweise**:
- `UIGraphicsPDFRenderer` fuer PDF-Generierung
- Oder: Erstelle HTML -> `WKWebView.createPDF()` (einfacher fuer Tabellen-Layout)
- Empfehlung: HTML-Ansatz ist schneller zu implementieren und leichter zu stylen
- A4-Seitengroesse: `CGSize(width: 595.2, height: 841.8)` (72 DPI)

---

### P2-E06-S02: Android PDF-Monatsreport generieren

**Als** Nutzer
**moechte ich** einen professionell formatierten PDF-Monatsreport erstellen koennen,
**damit** ich meinem Arbeitgeber oder Behoerden einen Arbeitszeitnachweis vorlegen kann.

**Plattform**: Android
**Abhaengigkeiten**: P2-E05-S08 (Gesamt-Screen als Kontext), Phase 1 (WorkSession-Daten)
**Parallelisierbar mit**: P2-E06-S01 (iOS PDF), P2-E06-S03/S04 (CSV)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `PDFReportGenerator.kt` in `services/export/`
- [ ] Gleicher PDF-Inhalt wie iOS (Tabelle, Zusammenfassung, Header, Footer)
- [ ] PDF ist A4-Format, Hochformat
- [ ] Given die gleichen Testdaten wie bei iOS
  When der PDF-Report generiert wird
  Then ist der Inhalt identisch (gleiche Spalten, gleiche Zusammenfassung)

**Technische Hinweise**:
- `android.graphics.pdf.PdfDocument` fuer native PDF-Generierung
- Oder: HTML + `WebView.createPrintDocumentAdapter()` (einfacher, analog zu iOS)
- Empfehlung: HTML-Ansatz fuer konsistentes Layout auf beiden Plattformen
- A4-Seitengroesse: `PrintAttributes.MediaSize.ISO_A4`

---

### P2-E06-S03: iOS CSV-Export

**Als** Nutzer
**moechte ich** meine Arbeitszeitdaten als CSV exportieren koennen,
**damit** ich sie in Excel oder Google Sheets weiterverarbeiten kann.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 1 (WorkSession-Daten)
**Parallelisierbar mit**: P2-E06-S04 (Android CSV), P2-E06-S01 (PDF)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `CSVExporter.swift` in `Services/Export/`
- [ ] Methode: `generateCSV(sessions: [WorkSession], vacationDays: [VacationDay], sickDays: [SickDay], holidays: [(Date, String)], from: Date, to: Date) -> String`
- [ ] CSV-Spalten (Semikolon-getrennt, deutscher Standard):
  ```
  Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ
  01.03.2026;Montag;08:30;17:00;30;8,00;Arbeit
  02.03.2026;Dienstag;09:00;17:30;30;8,00;Arbeit
  03.03.2026;Mittwoch;;;;0;0,00;Krank
  ...
  ```
- [ ] Typ-Spalte: "Arbeit", "Urlaub", "Feiertag", "Krank", "Wochenende" (oder leer fuer WE)
- [ ] Dezimalformat: Komma als Trennzeichen (8,00 statt 8.00) -- deutscher Standard
- [ ] Zeitraumauswahl: Monat, Quartal oder Jahr
- [ ] Given Maerz 2026 wird als CSV exportiert
  When die Datei in Excel geoeffnet wird
  Then werden alle Spalten korrekt erkannt (Semikolon-Trennung)
  And Dezimalzahlen werden als Zahlen erkannt (Komma-Format)
- [ ] UTF-8 mit BOM (damit Excel die Umlaute korrekt darstellt)

**Technische Hinweise**:
- CSV-Datei: `String` zusammenbauen, als `.csv` Temp-Datei schreiben
- Semikolon-Trennung (NICHT Komma!) weil deutsches Excel Semikolon erwartet
- UTF-8 BOM: `"\u{FEFF}"` als erstes Zeichen
- Dezimalformat: `String(format: "%.2f", hours).replacingOccurrences(of: ".", with: ",")`

---

### P2-E06-S04: Android CSV-Export

**Als** Nutzer
**moechte ich** meine Arbeitszeitdaten als CSV exportieren koennen,
**damit** ich sie in Excel oder Google Sheets weiterverarbeiten kann.

**Plattform**: Android
**Abhaengigkeiten**: Phase 1 (WorkSession-Daten)
**Parallelisierbar mit**: P2-E06-S03 (iOS CSV), P2-E06-S02 (PDF)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `CSVExporter.kt` in `services/export/`
- [ ] Gleiche CSV-Struktur wie iOS (Semikolon, Komma-Dezimal, UTF-8 BOM)
- [ ] Zeitraumauswahl: Monat, Quartal, Jahr
- [ ] Gleiche Testszenarien wie iOS

**Technische Hinweise**:
- `StringBuilder` fuer CSV-String
- BOM: `"\uFEFF"` voranstellen
- Decimal-Format: `String.format(Locale.GERMAN, "%.2f", hours)`

---

### P2-E06-S05: iOS Export-UI + Share-Sheet

**Als** Nutzer
**moechte ich** den generierten Report direkt teilen oder speichern koennen,
**damit** ich ihn per E-Mail, AirDrop oder in meine Dateien-App senden kann.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E06-S01 (PDF), P2-E06-S03 (CSV), P2-E05-S07 (Gesamt-Screen)
**Parallelisierbar mit**: P2-E06-S06 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Export-Sektion im Gesamt-Screen (unterhalb der Monatstabelle):
  - Sektions-Header "EXPORT"
  - "PDF-Monatsreport" Button
  - "CSV-Export" Button
  - Monatsauswahl: "← Maerz 2026 →" Picker
- [ ] Tap auf "PDF-Monatsreport":
  1. Monatsauswahl (Picker mit aktuellem Monat als Default)
  2. Loading-Indikator waehrend PDF-Generierung
  3. PDF-Vorschau (optional, nice-to-have)
  4. System-Share-Sheet oeffnet sich mit PDF-Datei
  - Given Maerz 2026 ausgewaehlt
    When "PDF-Monatsreport" getippt wird
    Then wird das PDF generiert
    And das Share-Sheet oeffnet sich mit "Arbeitszeitnachweis_2026-03.pdf"
- [ ] Tap auf "CSV-Export":
  1. Zeitraum-Auswahl: Monat / Quartal / Jahr (SegmentedControl)
  2. Bei Monat: Monatsauswahl
  3. Share-Sheet oeffnet sich mit CSV-Datei
  - Given Zeitraum "Quartal" (Q1 2026) gewaehlt
    When "CSV-Export" getippt wird
    Then wird eine CSV mit Jan-Maerz 2026 generiert
    And der Dateiname ist "Arbeitszeiten_2026-Q1.csv"
- [ ] Dateinamen-Konvention:
  - PDF: `Arbeitszeitnachweis_{YYYY}-{MM}.pdf`
  - CSV: `Arbeitszeiten_{YYYY}-{MM}.csv` (Monat), `..._Q{N}.csv` (Quartal), `..._YYYY.csv` (Jahr)

**Technische Hinweise**:
- Share-Sheet: `ShareLink(item: url)` (SwiftUI) oder `UIActivityViewController` (UIKit-Wrapper)
- PDF als Temp-Datei: `FileManager.default.temporaryDirectory.appendingPathComponent("...")`
- Monatsauswahl: `Picker` oder eigene Navigation (← Monat →)

---

### P2-E06-S06: Android Export-UI + Share-Intent

**Als** Nutzer
**moechte ich** den generierten Report direkt teilen oder speichern koennen,
**damit** ich ihn per E-Mail, Messenger oder Download-Ordner senden kann.

**Plattform**: Android
**Abhaengigkeiten**: P2-E06-S02 (PDF), P2-E06-S04 (CSV), P2-E05-S08 (Gesamt-Screen)
**Parallelisierbar mit**: P2-E06-S05 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Export-Sektion im Gesamt-Screen (gleiche Position wie iOS)
- [ ] Gleiche Funktionalitaet: PDF-Report, CSV-Export, Zeitraumauswahl
- [ ] Share via `Intent.ACTION_SEND` mit FileProvider
- [ ] Gleiche Dateinamen-Konvention wie iOS

**Technische Hinweise**:
- FileProvider in `AndroidManifest.xml` konfigurieren fuer externe File-Sharing
- `Intent(Intent.ACTION_SEND).apply { type = "application/pdf"; putExtra(Intent.EXTRA_STREAM, uri) }`
- Temp-Datei: `File(context.cacheDir, "Arbeitszeitnachweis_2026-03.pdf")`
- `FileProvider.getUriForFile()` fuer sichere URI-Generierung

---

### P2-E06-S07: OverviewViewModel um Export-Methoden erweitern (Beide Plattformen)

**Als** Entwickler
**moechte ich** die Export-Logik im bestehenden OverviewViewModel integrieren,
**damit** kein separates ViewModel noetig ist und die bereits geladenen Daten (Sessions, VacationDays, SickDays, Settings, Holidays) direkt wiederverwendet werden.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P2-E06-S01/S02 (PDF Generator), P2-E06-S03/S04 (CSV Exporter), P2-E05-S05/S06 (OverviewViewModel existiert)
**Parallelisierbar mit**: P2-E06-S05/S06 (Export-UI)
**Geschaetzter Aufwand**: M

**Design-Entscheidung**: Kein separates ExportViewModel. Export braucht genau die gleichen Daten die das OverviewViewModel bereits hat oder einfach laden kann. Ein separates ViewModel wuerde nur Daten-Duplikation erzeugen. Siehe auch `tech-specs/tech-epic-06.md`.

**Akzeptanzkriterien**:
- [ ] iOS: `OverviewViewModel.swift` um Export-Methoden und -State erweitern
- [ ] Android: `OverviewViewModel.kt` um Export-Methoden und -State erweitern
- [ ] Neuer State im OverviewViewModel:
  - `selectedExportMonth: Int`, `selectedExportYear: Int`
  - `exportTimeRange: ExportTimeRange` (Monat / Quartal / Jahr)
  - `isGenerating: Bool`
  - `exportError: String?`
- [ ] Neue Methoden im OverviewViewModel:
  - `generatePDFReport() -> URL/Uri` (gibt Pfad zur generierten PDF zurueck)
  - `generateCSVExport() -> URL/Uri` (gibt Pfad zur generierten CSV zurueck)
- [ ] Nutzt die bereits im OverviewViewModel vorhandenen Daten (Sessions, VacationDays, SickDays, Holidays, Settings)
- [ ] Given der Nutzer generiert einen PDF-Report fuer Maerz 2026
  When die Methode aufgerufen wird
  Then werden WorkSessions, VacationDays und SickDays fuer Maerz 2026 geladen
  And Feiertage fuer das Bundesland werden berechnet
  And der PDFReportGenerator wird mit allen Daten aufgerufen
  And der Pfad zur generierten PDF wird zurueckgegeben

**Technische Hinweise**:
- Export-Methoden als Erweiterung des bestehenden OverviewViewModels, KEIN neues ViewModel erstellen
- ExportTimeRange Enum: `.month`, `.quarter`, `.year`
- Bei Quartal: Monat auf Q-Start mappen (Q1=Jan-Maerz, Q2=Apr-Jun, ...)
