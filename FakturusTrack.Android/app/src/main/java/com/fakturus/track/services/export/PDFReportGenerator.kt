package com.fakturus.track.services.export

import android.content.Context
import android.graphics.pdf.PdfDocument
import android.print.PrintAttributes
import android.view.View
import android.webkit.WebView
import android.webkit.WebViewClient
import com.fakturus.track.models.SickDayEntity
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.models.VacationDayEntity
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.ui.shared.isWorkday
import com.fakturus.track.util.Holiday
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import java.io.File
import java.io.FileOutputStream
import java.time.Instant
import java.time.LocalDate
import java.time.Month
import java.time.YearMonth
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.TextStyle
import java.util.Locale
import kotlin.coroutines.resume

class PDFReportGenerator(private val context: Context) {

    suspend fun generateMonthlyReport(
        month: Int,
        year: Int,
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        holidays: List<Holiday>,
        settings: UserSettingsEntity,
        employeeName: String?
    ): File? {
        val html = buildHTML(month, year, sessions, vacationDays, sickDays, holidays, settings, employeeName)
        return htmlToPDF(html, "Arbeitszeitnachweis_${year}-${"%02d".format(month)}.pdf")
    }

    private suspend fun htmlToPDF(html: String, fileName: String): File? {
        return withContext(Dispatchers.Main) {
            val webView = WebView(context)
            webView.settings.javaScriptEnabled = false

            // A4 at 72 dpi: 595 x 842
            val pageWidth = 595
            val pageHeight = 842

            suspendCancellableCoroutine<Unit> { cont ->
                webView.webViewClient = object : WebViewClient() {
                    override fun onPageFinished(view: WebView?, url: String?) {
                        if (cont.isActive) cont.resume(Unit)
                    }
                }
                webView.loadDataWithBaseURL(null, html, "text/html", "UTF-8", null)
            }

            // Measure the WebView content
            webView.measure(
                View.MeasureSpec.makeMeasureSpec(pageWidth, View.MeasureSpec.EXACTLY),
                View.MeasureSpec.makeMeasureSpec(0, View.MeasureSpec.UNSPECIFIED)
            )
            val contentHeight = webView.measuredHeight
            webView.layout(0, 0, pageWidth, contentHeight)

            try {
                val document = PdfDocument()
                // Calculate pages needed
                val totalPages = ((contentHeight + pageHeight - 1) / pageHeight).coerceAtLeast(1)

                for (pageNum in 0 until totalPages) {
                    val pageInfo = PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pageNum + 1).create()
                    val page = document.startPage(pageInfo)
                    val canvas = page.canvas

                    // Translate to draw the correct portion
                    canvas.translate(0f, -(pageNum * pageHeight).toFloat())
                    webView.draw(canvas)

                    document.finishPage(page)
                }

                val file = File(context.cacheDir, fileName)
                FileOutputStream(file).use { fos ->
                    document.writeTo(fos)
                }
                document.close()
                file
            } catch (e: Exception) {
                null
            }
        }
    }

    private fun buildHTML(
        month: Int,
        year: Int,
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        holidays: List<Holiday>,
        settings: UserSettingsEntity,
        employeeName: String?
    ): String {
        val yearMonth = YearMonth.of(year, month)
        val daysInMonth = yearMonth.lengthOfMonth()
        val holidayMap = holidays.associateBy { it.date }

        var rows = ""
        var workedMinutes = 0L
        var vacCount = 0
        var sickCount = 0
        var holidayCount = 0
        val hoursPerDay = settings.workHoursPerWeek / 5.0

        for (day in 1..daysInMonth) {
            val date = LocalDate.of(year, month, day)
            val weekdayName = date.format(DateTimeFormatter.ofPattern("EEEE", Locale.GERMAN))
            val dateStr = "%02d.%02d.%d".format(day, month, year)
            val dateIso = date.toString()

            val isVacation = vacationDays.any { it.date == dateIso }
            val isSick = sickDays.any { it.date == dateIso }
            val holiday = holidayMap[date]
            val isWeekend = !isWorkday(date.dayOfWeek, settings.workDays)
            val session = sessions.firstOrNull { it.date == dateIso }

            val cssClass = when {
                isWeekend -> "weekend"
                isSick -> "sick"
                isVacation -> "vacation"
                holiday != null -> "holiday"
                else -> ""
            }

            val typ = when {
                isSick -> { sickCount++; "Krank" }
                isVacation -> { vacCount++; "Urlaub" }
                holiday != null -> { if (!isWeekend) holidayCount++; "Feiertag" }
                isWeekend -> ""
                else -> "Arbeit"
            }

            rows += "<tr class=\"$cssClass\">"
            rows += "<td>$dateStr</td>"
            rows += "<td>$weekdayName</td>"

            if (session != null) {
                val startStr = formatTime(session.startTime)
                val endStr = session.stopTime?.let { formatTime(it) } ?: ""
                rows += "<td>$startStr</td>"
                rows += "<td>$endStr</td>"
                rows += "<td>${session.pauseMinutes} min</td>"
                val netHours = session.netDurationMinutes / 60.0
                rows += "<td>${String.format(Locale.GERMAN, "%.2f", netHours)}h</td>"
                workedMinutes += session.netDurationMinutes
            } else {
                rows += "<td></td><td></td><td></td><td></td>"
            }
            rows += "<td>$typ</td></tr>\n"
        }

        // Calculate expected hours
        var workdayCount = 0
        for (day in 1..daysInMonth) {
            val date = LocalDate.of(year, month, day)
            val dateIso = date.toString()
            val isWeekend = !isWorkday(date.dayOfWeek, settings.workDays)
            val isHoliday = holidayMap.containsKey(date)
            val isVacation = vacationDays.any { it.date == dateIso }
            val isSick = sickDays.any { it.date == dateIso }
            if (!isWeekend && !isHoliday && !isVacation && !isSick) {
                workdayCount++
            }
        }
        val expectedHours = workdayCount * hoursPerDay
        val workedHours = workedMinutes / 60.0
        val overtime = workedHours - expectedHours
        val overtimeClass = if (overtime >= 0) "positive" else "negative"

        val monthName = Month.of(month).getDisplayName(TextStyle.FULL, Locale.GERMAN)
        val generatedDate = LocalDate.now().format(DateTimeFormatter.ofPattern("dd.MM.yyyy"))

        return HTML_TEMPLATE
            .replace("{{TABLE_ROWS}}", rows)
            .replace("{{MONTH_NAME}}", monthName)
            .replace("{{YEAR}}", year.toString())
            .replace("{{EMPLOYEE_NAME}}", employeeName ?: "")
            .replace("{{EXPECTED_HOURS}}", String.format(Locale.GERMAN, "%.2f h", expectedHours))
            .replace("{{WORKED_HOURS}}", String.format(Locale.GERMAN, "%.2f h", workedHours))
            .replace("{{OVERTIME}}", String.format(Locale.GERMAN, "%+.2f h", overtime))
            .replace("{{OVERTIME_CLASS}}", overtimeClass)
            .replace("{{VACATION_DAYS}}", vacCount.toString())
            .replace("{{SICK_DAYS}}", sickCount.toString())
            .replace("{{HOLIDAYS}}", holidayCount.toString())
            .replace("{{GENERATED_DATE}}", generatedDate)
    }

    private fun formatTime(isoInstant: String): String {
        return try {
            val instant = Instant.parse(isoInstant)
            val localTime = instant.atZone(ZoneId.systemDefault()).toLocalTime()
            localTime.format(DateTimeFormatter.ofPattern("HH:mm"))
        } catch (e: Exception) {
            ""
        }
    }

    companion object {
        private val HTML_TEMPLATE = """
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
        """.trimIndent()
    }
}
