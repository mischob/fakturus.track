import Foundation

enum CSVExporter {

    enum TimeRange {
        case month(month: Int, year: Int)
        case quarter(quarter: Int, year: Int)
        case year(year: Int)

        var from: Date {
            let cal = Calendar.current
            switch self {
            case .month(let month, let year):
                return cal.date(from: DateComponents(year: year, month: month, day: 1))!
            case .quarter(let quarter, let year):
                let month = (quarter - 1) * 3 + 1
                return cal.date(from: DateComponents(year: year, month: month, day: 1))!
            case .year(let year):
                return cal.date(from: DateComponents(year: year, month: 1, day: 1))!
            }
        }

        var to: Date {
            let cal = Calendar.current
            switch self {
            case .month(let month, let year):
                let start = cal.date(from: DateComponents(year: year, month: month, day: 1))!
                return cal.date(byAdding: DateComponents(month: 1, day: -1), to: start)!
            case .quarter(let quarter, let year):
                let month = (quarter - 1) * 3 + 1
                let start = cal.date(from: DateComponents(year: year, month: month, day: 1))!
                return cal.date(byAdding: DateComponents(month: 3, day: -1), to: start)!
            case .year(let year):
                return cal.date(from: DateComponents(year: year, month: 12, day: 31))!
            }
        }

        var fileName: String {
            switch self {
            case .month(let month, let year):
                return "Arbeitszeiten_\(year)-\(String(format: "%02d", month)).csv"
            case .quarter(let quarter, let year):
                return "Arbeitszeiten_\(year)-Q\(quarter).csv"
            case .year(let year):
                return "Arbeitszeiten_\(year).csv"
            }
        }
    }

    static func generateCSV(
        sessions: [WorkSession],
        vacationDays: [VacationDay],
        sickDays: [SickDay],
        holidays: [Holiday],
        settings: UserSettings,
        timeRange: TimeRange
    ) -> String {
        let from = timeRange.from
        let to = timeRange.to
        var csv = "\u{FEFF}" // UTF-8 BOM
        csv += "Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ\n"

        let cal = Calendar.current
        let weekdayFormatter = DateFormatter()
        weekdayFormatter.locale = Locale(identifier: "de_DE")
        weekdayFormatter.dateFormat = "EEEE"

        let dateFormatter = DateFormatter()
        dateFormatter.locale = Locale(identifier: "de_DE")
        dateFormatter.dateFormat = "dd.MM.yyyy"

        let timeFormatter = DateFormatter()
        timeFormatter.dateFormat = "HH:mm"

        var current = from
        while current <= to {
            let weekdayName = weekdayFormatter.string(from: current)
            let dateStr = dateFormatter.string(from: current)

            let isVacation = vacationDays.contains { cal.isDate($0.date, inSameDayAs: current) }
            let isSick = sickDays.contains { cal.isDate($0.date, inSameDayAs: current) }
            let holiday = holidays.first { cal.isDate($0.date, inSameDayAs: current) }
            let session = sessions.first { cal.isDate($0.date, inSameDayAs: current) }

            let typ: String
            if isSick {
                typ = "Krank"
            } else if isVacation {
                typ = "Urlaub"
            } else if holiday != nil {
                typ = "Feiertag"
            } else if session != nil {
                typ = "Arbeit"
            } else {
                typ = ""
            }

            if let s = session {
                let netHours = Double(s.netDurationMinutes) / 60.0
                let netStr = String(format: "%.2f", netHours)
                    .replacingOccurrences(of: ".", with: ",")
                let startStr = timeFormatter.string(from: s.startTime)
                let endStr = s.stopTime.map { timeFormatter.string(from: $0) } ?? ""
                csv += "\(dateStr);\(weekdayName);\(startStr);\(endStr);\(s.pauseMinutes);\(netStr);\(typ)\n"
            } else {
                csv += "\(dateStr);\(weekdayName);;;;0;0,00;\(typ)\n"
            }

            current = cal.date(byAdding: .day, value: 1, to: current)!
        }

        return csv
    }
}
