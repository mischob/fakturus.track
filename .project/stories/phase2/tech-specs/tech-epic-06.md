# Tech-Spec: EPIC 06 -- Export (PDF/CSV + Share)

## Dateien

### Neue Dateien

| Datei | Plattform | Beschreibung |
|-------|-----------|-------------|
| `Services/Export/PDFReportGenerator.swift` | iOS | HTML -> WKWebView PDF |
| `Services/Export/CSVExporter.swift` | iOS | Semikolon-CSV mit UTF-8 BOM |
| `services/export/PDFReportGenerator.kt` | Android | HTML -> WebView PrintAdapter |
| `services/export/CSVExporter.kt` | Android | Semikolon-CSV mit UTF-8 BOM |

### Modifizierte Dateien

| Datei | Plattform | Aenderung |
|-------|-----------|-----------|
| `Features/Overview/OverviewScreen.swift` | iOS | +Export-Sektion unterhalb Tabelle |
| `features/overview/OverviewScreen.kt` | Android | +Export-Sektion unterhalb Tabelle |
| `Features/Overview/OverviewViewModel.swift` | iOS | +Export-Methoden (generatePDF, generateCSV) |
| `features/overview/OverviewViewModel.kt` | Android | +Export-Methoden |

**Design-Entscheidung**: Kein separates ExportViewModel. Die Export-Logik wird direkt ins OverviewViewModel integriert. Begruendung: Export braucht genau die gleichen Daten (Sessions, VacationDays, SickDays, Settings, Holidays) die das OverviewViewModel bereits hat oder einfach laden kann. Ein separates ViewModel wuerde nur Daten-Duplikation erzeugen.

---

## PDF-Generierung: HTML-Ansatz

Beide Plattformen generieren HTML und konvertieren zu PDF. Das ist einfacher als native PDF-APIs und liefert konsistentes Layout.

### HTML-Template (identisch fuer iOS + Android)

```html
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
  body { font-family: -apple-system, 'Segoe UI', sans-serif; font-size: 10pt; margin: 40px; }
  h1 { font-size: 16pt; margin-bottom: 4px; }
  h2 { font-size: 12pt; color: #666; margin-top: 0; }
  table { width: 100%; border-collapse: collapse; margin-top: 16px; }
  th { background: #f5f5f5; text-align: left; padding: 6px 8px; border-bottom: 2px solid #333; font-size: 9pt; }
  td { padding: 4px 8px; border-bottom: 1px solid #eee; font-size: 9pt; }
  tr.weekend { background: #fafafa; color: #999; }
  tr.holiday { background: #f0e6ff; }
  tr.vacation { background: #e0f7fa; }
  tr.sick { background: #ffebee; }
  .summary { margin-top: 20px; }
  .summary td { font-weight: bold; border-top: 2px solid #333; }
  .footer { margin-top: 30px; font-size: 8pt; color: #999; text-align: center; }
  .positive { color: #2e7d32; }
  .negative { color: #c62828; }
</style>
</head>
<body>
  <h1>fakturus.track &mdash; Arbeitszeitnachweis</h1>
  <h2>{{MONTH_NAME}} {{YEAR}} &middot; {{EMPLOYEE_NAME}}</h2>

  <table>
    <thead>
      <tr>
        <th>Datum</th>
        <th>Wochentag</th>
        <th>Start</th>
        <th>Ende</th>
        <th>Pause</th>
        <th>Netto</th>
        <th>Typ</th>
      </tr>
    </thead>
    <tbody>
      {{TABLE_ROWS}}
    </tbody>
  </table>

  <table class="summary">
    <tr><td>Soll-Stunden</td><td>{{EXPECTED_HOURS}}</td></tr>
    <tr><td>Ist-Stunden</td><td>{{WORKED_HOURS}}</td></tr>
    <tr><td>Ueberstunden</td><td class="{{OVERTIME_CLASS}}">{{OVERTIME}}</td></tr>
    <tr><td>Urlaubstage</td><td>{{VACATION_DAYS}}</td></tr>
    <tr><td>Krankheitstage</td><td>{{SICK_DAYS}}</td></tr>
    <tr><td>Feiertage</td><td>{{HOLIDAYS}}</td></tr>
  </table>

  <div class="footer">
    Generiert am {{GENERATED_DATE}} mit fakturus.track
  </div>
</body>
</html>
```

### Zeile pro Tag

Fuer jeden Tag im Monat wird eine Zeile generiert:

```
| 01.03.2026 | Montag    | 08:30 | 17:00 | 30 min | 8:00h  | Arbeit   |
| 02.03.2026 | Dienstag  | 09:00 | 17:30 | 30 min | 8:00h  | Arbeit   |
| 03.03.2026 | Mittwoch  |       |       |        |        | Krank    |
| 04.03.2026 | Donnerstag|       |       |        |        | Urlaub   |
| 05.03.2026 | Freitag   |       |       |        |        | Feiertag |
| 06.03.2026 | Samstag   |       |       |        |        |          |  (WE)
| 07.03.2026 | Sonntag   |       |       |        |        |          |  (WE)
```

---

## PDF-Generierung

### Swift: PDFReportGenerator.swift

```swift
import WebKit

enum PDFReportGenerator {

    static func generateMonthlyReport(
        month: Int, year: Int,
        sessions: [WorkSession],
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        holidays: [Holiday],
        settings: UserSettings,
        employeeName: String?
    ) async -> Data? {
        let html = buildHTML(
            month: month, year: year,
            sessions: sessions, vacationDays: vacationDays,
            sickDays: sickDays, holidays: holidays,
            settings: settings, employeeName: employeeName
        )
        return await htmlToPDF(html: html)
    }

    @MainActor
    private static func htmlToPDF(html: String) async -> Data? {
        let webView = WKWebView(frame: CGRect(x: 0, y: 0, width: 595, height: 842))

        // Warten bis WKWebView geladen hat via WKNavigationDelegate + Continuation
        // NICHT Task.sleep verwenden -- unzuverlaessig und langsam!
        await withCheckedContinuation { continuation in
            let delegate = NavigationDelegate {
                continuation.resume()
            }
            webView.navigationDelegate = delegate
            webView.loadHTMLString(html, baseURL: nil)
            // delegate muss retained bleiben bis didFinish aufgerufen wird
            objc_setAssociatedObject(webView, "delegate", delegate, .OBJC_ASSOCIATION_RETAIN)
        }

        let config = WKPDFConfiguration()
        config.rect = CGRect(x: 0, y: 0, width: 595.2, height: 841.8) // A4

        return try? await webView.pdf(configuration: config)
    }

    /// Helper: WKNavigationDelegate der bei didFinish ein Closure aufruft
    private class NavigationDelegate: NSObject, WKNavigationDelegate {
        let onFinish: () -> Void
        init(onFinish: @escaping () -> Void) { self.onFinish = onFinish }
        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            onFinish()
        }
    }

    private static func buildHTML(/* params */) -> String {
        // HTML-Template mit Platzhaltern fuellen
        var rows = ""
        let cal = Calendar.current

        let daysInMonth = cal.range(of: .day, in: .month,
            for: cal.date(from: DateComponents(year: year, month: month))!)!.count

        for day in 1...daysInMonth {
            let date = cal.date(from: DateComponents(year: year, month: month, day: day))!
            let weekday = cal.component(.weekday, from: date)
            let weekdayName = date.formatted(.dateTime.weekday(.wide).locale(Locale(identifier: "de_DE")))

            // Typ bestimmen
            let isVacation = vacationDays.contains { cal.isDate($0.date, inSameDayAs: date) }
            let isSick = sickDays.contains { cal.isDate($0.date, inSameDayAs: date) }
            let holiday = holidays.first { cal.isDate($0.date, inSameDayAs: date) }
            let isWeekend = !isWorkday(weekday: weekday, workDays: settings.workDays)

            let session = sessions.first { cal.isDate($0.date, inSameDayAs: date) }

            // Zeile bauen
            let cssClass = isWeekend ? "weekend" : isSick ? "sick" :
                           isVacation ? "vacation" : holiday != nil ? "holiday" : ""
            let typ = isSick ? "Krank" : isVacation ? "Urlaub" :
                      holiday != nil ? "Feiertag" : isWeekend ? "" : "Arbeit"

            rows += "<tr class=\"\(cssClass)\">"
            rows += "<td>\(String(format: "%02d.%02d.%d", day, month, year))</td>"
            rows += "<td>\(weekdayName)</td>"
            // Start/Ende/Pause/Netto nur bei Arbeitstagen mit Session
            if let s = session {
                rows += "<td>\(formatTime(s.startTime))</td>"
                rows += "<td>\(s.stopTime.map(formatTime) ?? "")</td>"
                rows += "<td>\(s.pauseMinutes) min</td>"
                rows += "<td>\(formatDuration(s.netDuration))</td>"
            } else {
                rows += "<td></td><td></td><td></td><td></td>"
            }
            rows += "<td>\(typ)</td></tr>\n"
        }

        return htmlTemplate
            .replacingOccurrences(of: "{{TABLE_ROWS}}", with: rows)
            // ... weitere Platzhalter
    }
}
```

### Kotlin: PDFReportGenerator.kt

```kotlin
class PDFReportGenerator(private val context: Context) {

    suspend fun generateMonthlyReport(
        month: Int, year: Int,
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        holidays: List<Holiday>,
        settings: UserSettingsEntity,
        employeeName: String?
    ): File? {
        val html = buildHTML(month, year, sessions, vacationDays, sickDays,
                            holidays, settings, employeeName)
        return htmlToPDF(html, "Arbeitszeitnachweis_${year}-${"%02d".format(month)}.pdf")
    }

    private suspend fun htmlToPDF(html: String, fileName: String): File? {
        return withContext(Dispatchers.Main) {
            val webView = WebView(context)

            // Warten bis WebView geladen hat via WebViewClient + Continuation
            // NICHT delay() verwenden -- unzuverlaessig!
            suspendCancellableCoroutine<Unit> { cont ->
                webView.webViewClient = object : android.webkit.WebViewClient() {
                    override fun onPageFinished(view: WebView?, url: String?) {
                        if (cont.isActive) cont.resume(Unit)
                    }
                }
                webView.loadDataWithBaseURL(null, html, "text/html", "UTF-8", null)
            }

            val printAdapter = webView.createPrintDocumentAdapter(fileName)
            val file = File(context.cacheDir, fileName)

            // PrintDocumentAdapter -> PDF-Datei via Coroutine-Wrapper
            printAdapterToFile(printAdapter, file)
        }
    }

    /** Wraps PrintDocumentAdapter Callbacks in eine suspend-Funktion */
    private suspend fun printAdapterToFile(
        adapter: android.print.PrintDocumentAdapter,
        outputFile: File
    ): File? = suspendCancellableCoroutine { cont ->
        val attrs = android.print.PrintAttributes.Builder()
            .setMediaSize(android.print.PrintAttributes.MediaSize.ISO_A4)
            .setResolution(android.print.PrintAttributes.Resolution("pdf", "pdf", 300, 300))
            .setMinMargins(android.print.PrintAttributes.Margins.NO_MARGINS)
            .build()

        adapter.onLayout(null, attrs, null, object : android.print.PrintDocumentAdapter.LayoutResultCallback() {
            override fun onLayoutFinished(info: android.print.PrintDocumentInfo?, changed: Boolean) {
                val fd = android.os.ParcelFileDescriptor.open(
                    outputFile,
                    android.os.ParcelFileDescriptor.MODE_WRITE_ONLY or
                        android.os.ParcelFileDescriptor.MODE_CREATE or
                        android.os.ParcelFileDescriptor.MODE_TRUNCATE
                )
                val pages = arrayOf(android.print.PageRange.ALL_PAGES)
                adapter.onWrite(pages, fd, null, object : android.print.PrintDocumentAdapter.WriteResultCallback() {
                    override fun onWriteFinished(pages: Array<out android.print.PageRange>?) {
                        fd.close()
                        if (cont.isActive) cont.resume(outputFile)
                    }
                    override fun onWriteFailed(error: CharSequence?) {
                        fd.close()
                        if (cont.isActive) cont.resume(null)
                    }
                })
            }
            override fun onLayoutFailed(error: CharSequence?) {
                if (cont.isActive) cont.resume(null)
            }
        }, null)
    }
}
```

**Hinweis zur Android-PDF-Generierung**: Die WebView-basierte PDF-Generierung auf Android ist etwas umstaendlicher als auf iOS. Alternative: `PdfDocument` API direkt nutzen. Der HTML-Ansatz hat den Vorteil der Layout-Konsistenz. Falls die WebView-Loesung Probleme macht, kann auf native `PdfDocument` mit `Canvas.drawText()` zurueckgefallen werden.

---

## CSV-Export

### Swift: CSVExporter.swift

```swift
enum CSVExporter {
    static func generateCSV(
        sessions: [WorkSession],
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        holidays: [Holiday],
        settings: UserSettings,
        from: Date, to: Date
    ) -> String {
        var csv = "\u{FEFF}"  // UTF-8 BOM
        csv += "Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ\n"

        let cal = Calendar.current
        var current = from
        while current <= to {
            let weekdayName = current.formatted(.dateTime.weekday(.wide)
                .locale(Locale(identifier: "de_DE")))
            let dateStr = current.formatted(.dateTime.day(.twoDigits).month(.twoDigits)
                .year().locale(Locale(identifier: "de_DE")))

            let isVacation = vacationDays.contains { cal.isDate($0.date, inSameDayAs: current) }
            let isSick = sickDays.contains { cal.isDate($0.date, inSameDayAs: current) }
            let holiday = holidays.first { cal.isDate($0.date, inSameDayAs: current) }
            let session = sessions.first { cal.isDate($0.date, inSameDayAs: current) }

            let typ = isSick ? "Krank" : isVacation ? "Urlaub" :
                      holiday != nil ? "Feiertag" : session != nil ? "Arbeit" : ""

            if let s = session {
                let netHours = Double(s.netDurationMinutes) / 60.0
                let netStr = String(format: "%.2f", netHours)
                    .replacingOccurrences(of: ".", with: ",")
                csv += "\(dateStr);\(weekdayName);\(formatTime(s.startTime));"
                csv += "\(s.stopTime.map(formatTime) ?? "");\(s.pauseMinutes);\(netStr);\(typ)\n"
            } else {
                csv += "\(dateStr);\(weekdayName);;;;0;0,00;\(typ)\n"
            }

            current = cal.date(byAdding: .day, value: 1, to: current)!
        }

        return csv
    }
}
```

### Kotlin: CSVExporter.kt

```kotlin
object CSVExporter {
    fun generateCSV(
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        holidays: List<Holiday>,
        settings: UserSettingsEntity,
        from: LocalDate, to: LocalDate
    ): String {
        val sb = StringBuilder()
        sb.append("\uFEFF")  // UTF-8 BOM
        sb.appendLine("Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ")

        var current = from
        val formatter = DateTimeFormatter.ofPattern("dd.MM.yyyy")
        val weekdayFormatter = DateTimeFormatter.ofPattern("EEEE", Locale.GERMAN)

        while (!current.isAfter(to)) {
            val dateStr = current.format(formatter)
            val weekday = current.format(weekdayFormatter)
            val dateIso = current.toString()

            val isVacation = vacationDays.any { it.date == dateIso }
            val isSick = sickDays.any { it.date == dateIso }
            val holiday = holidays.firstOrNull { it.date == current }
            val session = sessions.firstOrNull { it.date == dateIso }

            val typ = when {
                isSick -> "Krank"
                isVacation -> "Urlaub"
                holiday != null -> "Feiertag"
                session != null -> "Arbeit"
                else -> ""
            }

            if (session != null) {
                val netHours = session.netDurationMinutes / 60.0
                val netStr = String.format(Locale.GERMAN, "%.2f", netHours)
                sb.appendLine("$dateStr;$weekday;${formatTime(session.startTime)};" +
                    "${session.stopTime?.let(::formatTime) ?: ""};${session.pauseMinutes};$netStr;$typ")
            } else {
                sb.appendLine("$dateStr;$weekday;;;;0;0,00;$typ")
            }

            current = current.plusDays(1)
        }

        return sb.toString()
    }
}
```

### CSV-Beispiel-Output

```
Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ
01.03.2026;Montag;08:30;17:00;30;8,00;Arbeit
02.03.2026;Dienstag;09:00;17:30;30;8,00;Arbeit
03.03.2026;Mittwoch;;;;0;0,00;Krank
04.03.2026;Donnerstag;;;;0;0,00;Urlaub
05.03.2026;Freitag;;;;0;0,00;Feiertag
06.03.2026;Samstag;;;;0;0,00;
07.03.2026;Sonntag;;;;0;0,00;
```

---

## Share/Teilen

### iOS: ShareLink

```swift
// In OverviewScreen, nach PDF-Generierung:
if let pdfURL = generatedPDFURL {
    ShareLink(item: pdfURL) {
        Label("PDF teilen", systemImage: "square.and.arrow.up")
    }
}
```

Oder via UIActivityViewController:

```swift
func sharePDF(data: Data, fileName: String) {
    let tempURL = FileManager.default.temporaryDirectory.appendingPathComponent(fileName)
    try? data.write(to: tempURL)

    let activityVC = UIActivityViewController(activityItems: [tempURL], applicationActivities: nil)
    // Present
}
```

### Android: Intent.ACTION_SEND mit FileProvider

```kotlin
fun shareFile(context: Context, file: File, mimeType: String) {
    val uri = FileProvider.getUriForFile(context,
        "${context.packageName}.fileprovider", file)

    val intent = Intent(Intent.ACTION_SEND).apply {
        type = mimeType
        putExtra(Intent.EXTRA_STREAM, uri)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    context.startActivity(Intent.createChooser(intent, "Teilen"))
}
```

**AndroidManifest.xml** (FileProvider-Konfiguration):
```xml
<provider
    android:name="androidx.core.content.FileProvider"
    android:authorities="${applicationId}.fileprovider"
    android:exported="false"
    android:grantUriPermissions="true">
    <meta-data
        android:name="android.support.FILE_PROVIDER_PATHS"
        android:resource="@xml/file_paths" />
</provider>
```

**res/xml/file_paths.xml**:
```xml
<paths>
    <cache-path name="exports" path="." />
</paths>
```

---

## Dateinamen-Konvention

| Typ | Format | Beispiel |
|-----|--------|---------|
| PDF Monat | `Arbeitszeitnachweis_{YYYY}-{MM}.pdf` | `Arbeitszeitnachweis_2026-03.pdf` |
| CSV Monat | `Arbeitszeiten_{YYYY}-{MM}.csv` | `Arbeitszeiten_2026-03.csv` |
| CSV Quartal | `Arbeitszeiten_{YYYY}-Q{N}.csv` | `Arbeitszeiten_2026-Q1.csv` |
| CSV Jahr | `Arbeitszeiten_{YYYY}.csv` | `Arbeitszeiten_2026.csv` |

---

## Testbare Kriterien

1. PDF enthaelt korrekte Tabelle mit allen Tagen des Monats
2. PDF: Feiertage, Urlaubstage, Krankheitstage als eigene Zeilen ohne Start/Ende
3. PDF: Zusammenfassung zeigt korrekte Soll/Ist/Ueberstunden
4. PDF: A4-Format, kein Text-Ueberlauf
5. CSV: Semikolon-Trennung (oeffnet korrekt in deutschem Excel)
6. CSV: Komma als Dezimal-Trennzeichen (8,00 nicht 8.00)
7. CSV: UTF-8 BOM vorhanden (Umlaute korrekt in Excel)
8. Share-Sheet oeffnet sich mit korrektem Dateityp
9. Dateinamen folgen der Konvention
10. Export funktioniert offline (nutzt lokale Daten)

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| WebView-basierte PDF-Generierung ist fragil (Timing) | Mittel | iOS: UIGraphicsPDFRenderer als Fallback; Android: PdfDocument API |
| PDF-Layout sieht auf verschiedenen Geraeten anders aus | Niedrig | Feste Breiten in CSS, kein responsive Design |
| Android FileProvider-Konfiguration fehlt | Niedrig | Frueher testen; ohne FileProvider funktioniert Share nicht |
| Grosse Monate (31 Tage) passen nicht auf eine A4-Seite | Mittel | Font-Groesse 9pt, kompaktere Zeilenhoehe; ggf. Seitenumbruch |
