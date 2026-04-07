import WebKit
import Foundation

enum PDFReportGenerator {

    @MainActor
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

        await withCheckedContinuation { continuation in
            let delegate = NavigationDelegate {
                continuation.resume()
            }
            webView.navigationDelegate = delegate
            webView.loadHTMLString(html, baseURL: nil)
            // delegate must be retained until didFinish is called
            objc_setAssociatedObject(webView, "delegate", delegate, .OBJC_ASSOCIATION_RETAIN)
        }

        let config = WKPDFConfiguration()
        config.rect = CGRect(x: 0, y: 0, width: 595.2, height: 841.8) // A4

        return try? await webView.pdf(configuration: config)
    }

    /// Helper: WKNavigationDelegate that calls a closure on didFinish
    private class NavigationDelegate: NSObject, WKNavigationDelegate {
        let onFinish: () -> Void
        init(onFinish: @escaping () -> Void) { self.onFinish = onFinish }
        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            onFinish()
        }
    }

    // MARK: - HTML Building

    private static func buildHTML(
        month: Int, year: Int,
        sessions: [WorkSession],
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        holidays: [Holiday],
        settings: UserSettings,
        employeeName: String?
    ) -> String {
        let cal = Calendar.current
        let daysInMonth = cal.range(of: .day, in: .month,
            for: cal.date(from: DateComponents(year: year, month: month))!)!.count

        var rows = ""
        var totalWorkedMinutes = 0
        var vacationCount = 0
        var sickCount = 0
        var holidayCount = 0

        let weekdayFormatter = DateFormatter()
        weekdayFormatter.locale = Locale(identifier: "de_DE")
        weekdayFormatter.dateFormat = "EEEE"

        for day in 1...daysInMonth {
            let date = cal.date(from: DateComponents(year: year, month: month, day: day))!
            let weekday = cal.component(.weekday, from: date)
            let weekdayName = weekdayFormatter.string(from: date)

            let isVacation = vacationDays.contains { cal.isDate($0.date, inSameDayAs: date) }
            let isSick = sickDays.contains { cal.isDate($0.date, inSameDayAs: date) }
            let holiday = holidays.first { cal.isDate($0.date, inSameDayAs: date) }
            let isWeekend = !isWorkday(weekday: weekday, workDays: settings.workDays)
            let session = sessions.first { cal.isDate($0.date, inSameDayAs: date) }

            let cssClass: String
            let typ: String

            if isWeekend {
                cssClass = "weekend"
                typ = ""
            } else if isSick {
                cssClass = "sick"
                typ = "Krank"
                sickCount += 1
            } else if isVacation {
                cssClass = "vacation"
                typ = "Urlaub"
                vacationCount += 1
            } else if holiday != nil {
                cssClass = "holiday"
                typ = "Feiertag"
                holidayCount += 1
            } else {
                cssClass = ""
                typ = session != nil ? "Arbeit" : ""
            }

            rows += "<tr class=\"\(cssClass)\">"
            rows += "<td>\(String(format: "%02d.%02d.%d", day, month, year))</td>"
            rows += "<td>\(weekdayName)</td>"

            if let s = session {
                let netMinutes = s.netDurationMinutes
                totalWorkedMinutes += netMinutes
                rows += "<td>\(formatTime(s.startTime))</td>"
                rows += "<td>\(s.stopTime.map { formatTime($0) } ?? "")</td>"
                rows += "<td>\(s.pauseMinutes) min</td>"
                rows += "<td>\(formatDuration(minutes: netMinutes))</td>"
            } else {
                rows += "<td></td><td></td><td></td><td></td>"
            }
            rows += "<td>\(typ)</td></tr>\n"
        }

        // Calculate expected hours
        let expectedHours = calculateExpectedHours(
            month: month, year: year,
            vacationDays: vacationDays, sickDays: sickDays,
            holidays: holidays, settings: settings
        )
        let workedHours = Double(totalWorkedMinutes) / 60.0
        let overtime = workedHours - expectedHours
        let overtimeClass = overtime >= 0 ? "positive" : "negative"

        let monthName = monthNameGerman(month)

        return htmlTemplate
            .replacingOccurrences(of: "{{TABLE_ROWS}}", with: rows)
            .replacingOccurrences(of: "{{MONTH_NAME}}", with: monthName)
            .replacingOccurrences(of: "{{YEAR}}", with: "\(year)")
            .replacingOccurrences(of: "{{EMPLOYEE_NAME}}", with: employeeName ?? "")
            .replacingOccurrences(of: "{{EXPECTED_HOURS}}", with: formatDuration(minutes: Int(expectedHours * 60)))
            .replacingOccurrences(of: "{{WORKED_HOURS}}", with: formatDuration(minutes: totalWorkedMinutes))
            .replacingOccurrences(of: "{{OVERTIME}}", with: formatDurationSigned(minutes: Int(overtime * 60)))
            .replacingOccurrences(of: "{{OVERTIME_CLASS}}", with: overtimeClass)
            .replacingOccurrences(of: "{{VACATION_DAYS}}", with: "\(vacationCount)")
            .replacingOccurrences(of: "{{SICK_DAYS}}", with: "\(sickCount)")
            .replacingOccurrences(of: "{{HOLIDAYS}}", with: "\(holidayCount)")
            .replacingOccurrences(of: "{{GENERATED_DATE}}", with: Date().formatted(
                .dateTime.day(.twoDigits).month(.twoDigits).year()
                .locale(Locale(identifier: "de_DE"))
            ))
    }

    // MARK: - Helpers

    private static func isWorkday(weekday: Int, workDays: Int) -> Bool {
        // weekday: 1=So, 2=Mo, ..., 7=Sa
        let mondayBased = weekday == 1 ? 7 : weekday - 1
        return (workDays & (1 << (mondayBased - 1))) != 0
    }

    private static func calculateExpectedHours(
        month: Int, year: Int,
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        holidays: [Holiday],
        settings: UserSettings
    ) -> Double {
        let cal = Calendar.current
        let daysInMonth = cal.range(of: .day, in: .month,
            for: cal.date(from: DateComponents(year: year, month: month))!)!.count

        // Count work days in settings
        var workDayCount = 0
        for i in 0..<7 {
            if (settings.workDays & (1 << i)) != 0 { workDayCount += 1 }
        }
        guard workDayCount > 0 else { return 0 }

        let dailyHours = settings.workHoursPerWeek / Double(workDayCount)
        var expectedDays = 0

        for day in 1...daysInMonth {
            let date = cal.date(from: DateComponents(year: year, month: month, day: day))!
            let weekday = cal.component(.weekday, from: date)
            guard isWorkday(weekday: weekday, workDays: settings.workDays) else { continue }

            let isVacation = vacationDays.contains { cal.isDate($0.date, inSameDayAs: date) }
            let isSick = sickDays.contains { cal.isDate($0.date, inSameDayAs: date) }
            let isHoliday = holidays.contains { cal.isDate($0.date, inSameDayAs: date) }

            if !isVacation && !isSick && !isHoliday {
                expectedDays += 1
            }
        }

        return Double(expectedDays) * dailyHours
    }

    private static func formatTime(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm"
        return formatter.string(from: date)
    }

    private static func formatDuration(minutes: Int) -> String {
        let hours = minutes / 60
        let mins = minutes % 60
        return String(format: "%d:%02dh", hours, mins)
    }

    private static func formatDurationSigned(minutes: Int) -> String {
        let sign = minutes >= 0 ? "+" : "-"
        let abs = abs(minutes)
        let hours = abs / 60
        let mins = abs % 60
        return String(format: "%@%d:%02dh", sign, hours, mins)
    }

    private static func monthNameGerman(_ month: Int) -> String {
        let names = ["", "Januar", "Februar", "Maerz", "April", "Mai", "Juni",
                     "Juli", "August", "September", "Oktober", "November", "Dezember"]
        return month >= 1 && month <= 12 ? names[month] : ""
    }

    // MARK: - HTML Template

    private static let htmlTemplate = """
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
    """
}
