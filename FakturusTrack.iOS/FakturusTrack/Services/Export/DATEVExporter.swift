import Foundation

enum DATEVExporter {

    /// Generates a DATEV Lodas compatible CSV export for a given month.
    ///
    /// Format: Semikolon-separated, dot decimal, CRLF line endings, UTF-8.
    /// Lohnarten: 200 = Arbeit, 400 = Urlaub, 500 = Krankheit.
    static func generateExport(
        month: Int,
        year: Int,
        sessions: [WorkSession],
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        settings: UserSettings,
        personalNumber: String
    ) -> String {
        let cal = Calendar.current

        // Month boundaries
        let startOfMonth = cal.date(from: DateComponents(year: year, month: month, day: 1))!

        let dateFormatter = DateFormatter()
        dateFormatter.locale = Locale(identifier: "de_DE")
        dateFormatter.dateFormat = "dd.MM.yyyy"

        let timeFormatter = DateFormatter()
        timeFormatter.dateFormat = "HH:mm"

        // Daily target hours = weekly hours / work days per week
        let workDaysPerWeek = max(1, countWorkDays(settings.workDays))
        let dailyHours = settings.workHoursPerWeek / Double(workDaysPerWeek)

        var lines: [String] = []
        lines.append("Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause")

        let pn = personalNumber.isEmpty ? "00000" : personalNumber

        // Work sessions (Lohnart 200)
        let monthSessions = sessions.filter { session in
            cal.isDate(session.date, equalTo: startOfMonth, toGranularity: .month)
        }
        for session in monthSessions.sorted(by: { $0.date < $1.date }) {
            let dateStr = dateFormatter.string(from: session.date)
            let netHours = Double(session.netDurationMinutes) / 60.0
            let hoursStr = String(format: "%.2f", netHours)
            let startStr = timeFormatter.string(from: session.startTime)
            let endStr = session.stopTime.map { timeFormatter.string(from: $0) } ?? ""
            let pauseStr = "\(session.pauseMinutes)"

            lines.append("\(pn);\(dateStr);200;\(hoursStr);\(startStr);\(endStr);\(pauseStr)")
        }

        // Vacation days (Lohnart 400)
        let monthVacation = vacationDays.filter { day in
            cal.isDate(day.date, equalTo: startOfMonth, toGranularity: .month)
        }
        for day in monthVacation.sorted(by: { $0.date < $1.date }) {
            let dateStr = dateFormatter.string(from: day.date)
            let hoursStr = String(format: "%.2f", dailyHours)
            lines.append("\(pn);\(dateStr);400;\(hoursStr);;;")
        }

        // Sick days (Lohnart 500)
        let monthSick = sickDays.filter { day in
            cal.isDate(day.date, equalTo: startOfMonth, toGranularity: .month)
        }
        for day in monthSick.sorted(by: { $0.date < $1.date }) {
            let dateStr = dateFormatter.string(from: day.date)
            let hoursStr = String(format: "%.2f", dailyHours)
            lines.append("\(pn);\(dateStr);500;\(hoursStr);;;")
        }

        return lines.joined(separator: "\r\n") + "\r\n"
    }

    static func fileName(month: Int, year: Int) -> String {
        "DATEV_Lohn_\(year)-\(String(format: "%02d", month)).csv"
    }

    private static func countWorkDays(_ bitmask: Int) -> Int {
        var count = 0
        for i in 0..<7 {
            if bitmask & (1 << i) != 0 { count += 1 }
        }
        return count
    }
}
