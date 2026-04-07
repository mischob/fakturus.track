import Foundation

struct Holiday {
    let date: Date
    let name: String
}

enum HolidayCalculator {
    // MARK: - Cache

    private static let cacheQueue = DispatchQueue(label: "com.fakturus.holidayCache")
    private nonisolated(unsafe) static var _cache: [String: [Holiday]] = [:]

    private static func cacheKey(bundesland: String, year: Int) -> String {
        "\(bundesland)_\(year)"
    }

    /// Alle Feiertage fuer ein Bundesland und Jahr
    static func holidays(bundesland: String, year: Int) -> [Holiday] {
        let key = cacheKey(bundesland: bundesland, year: year)
        if let cached = cacheQueue.sync(execute: { _cache[key] }) { return cached }

        var result = nationalHolidays(year: year)
        result.append(contentsOf: stateHolidays(bundesland: bundesland, year: year))
        result.sort { $0.date < $1.date }
        cacheQueue.sync { _cache[key] = result }
        return result
    }

    /// Anzahl Feiertage (fuer BundeslandPicker)
    static func holidayCount(bundesland: String, year: Int) -> Int {
        holidays(bundesland: bundesland, year: year).count
    }

    /// Set von DateComponents fuer schnellen Lookup im Kalender
    static func holidayDateComponents(bundesland: String, year: Int) -> Set<DateComponents> {
        let cal = Calendar.current
        return Set(holidays(bundesland: bundesland, year: year).map { holiday in
            cal.dateComponents([.year, .month, .day], from: holiday.date)
        })
    }

    /// Pruefen ob ein Datum ein Feiertag ist (fuer Kalender)
    static func isHoliday(_ date: Date, bundesland: String, year: Int) -> Bool {
        let cal = Calendar.current
        return holidays(bundesland: bundesland, year: year).contains {
            cal.isDate($0.date, inSameDayAs: date)
        }
    }

    /// Feiertag-Name fuer ein Datum (fuer Long-Press Info)
    static func holidayName(for date: Date, bundesland: String, year: Int) -> String? {
        let cal = Calendar.current
        return holidays(bundesland: bundesland, year: year).first {
            cal.isDate($0.date, inSameDayAs: date)
        }?.name
    }

    // MARK: - Oster-Berechnung (Gauss)

    static func easterSunday(year: Int) -> Date {
        let a = year % 19
        let b = year / 100
        let c = year % 100
        let d = b / 4
        let e = b % 4
        let f = (b + 8) / 25
        let g = (b - f + 1) / 3
        let h = (19 * a + b - d - g + 15) % 30
        let i = c / 4
        let k = c % 4
        let l = (32 + 2 * e + 2 * i - h - k) % 7
        let m = (a + 11 * h + 22 * l) / 451
        let month = (h + l - 7 * m + 114) / 31
        let day = ((h + l - 7 * m + 114) % 31) + 1

        return makeDate(year: year, month: month, day: day)
    }

    // MARK: - Bundesweite Feiertage (9 Stueck)

    private static func nationalHolidays(year: Int) -> [Holiday] {
        let easter = easterSunday(year: year)
        return [
            Holiday(date: makeDate(year: year, month: 1, day: 1), name: "Neujahr"),
            Holiday(date: addDays(easter, -2), name: "Karfreitag"),
            Holiday(date: addDays(easter, 1), name: "Ostermontag"),
            Holiday(date: makeDate(year: year, month: 5, day: 1), name: "Tag der Arbeit"),
            Holiday(date: addDays(easter, 39), name: "Christi Himmelfahrt"),
            Holiday(date: addDays(easter, 50), name: "Pfingstmontag"),
            Holiday(date: makeDate(year: year, month: 10, day: 3), name: "Tag der Deutschen Einheit"),
            Holiday(date: makeDate(year: year, month: 12, day: 25), name: "1. Weihnachtstag"),
            Holiday(date: makeDate(year: year, month: 12, day: 26), name: "2. Weihnachtstag"),
        ]
    }

    // MARK: - Landesspezifische Feiertage

    private static func stateHolidays(bundesland: String, year: Int) -> [Holiday] {
        let easter = easterSunday(year: year)
        var result: [Holiday] = []

        // Heilige Drei Koenige (06.01.): BW, BY, ST
        if ["BW", "BY", "ST"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 1, day: 6),
                                  name: "Heilige Drei Koenige"))
        }

        // Internationaler Frauentag (08.03.): BE
        if bundesland == "BE" {
            result.append(Holiday(date: makeDate(year: year, month: 3, day: 8),
                                  name: "Internationaler Frauentag"))
        }

        // Fronleichnam (Ostern + 60): BW, BY, HE, NW, RP, SL
        if ["BW", "BY", "HE", "NW", "RP", "SL"].contains(bundesland) {
            result.append(Holiday(date: addDays(easter, 60), name: "Fronleichnam"))
        }

        // Mariae Himmelfahrt (15.08.): BY, SL
        if ["BY", "SL"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 8, day: 15),
                                  name: "Mariae Himmelfahrt"))
        }

        // Weltkindertag (20.09.): TH
        if bundesland == "TH" {
            result.append(Holiday(date: makeDate(year: year, month: 9, day: 20),
                                  name: "Weltkindertag"))
        }

        // Reformationstag (31.10.): BB, HB, HH, MV, NI, SH, SN, ST, TH
        if ["BB", "HB", "HH", "MV", "NI", "SH", "SN", "ST", "TH"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 10, day: 31),
                                  name: "Reformationstag"))
        }

        // Allerheiligen (01.11.): BW, BY, NW, RP, SL
        if ["BW", "BY", "NW", "RP", "SL"].contains(bundesland) {
            result.append(Holiday(date: makeDate(year: year, month: 11, day: 1),
                                  name: "Allerheiligen"))
        }

        // Buss- und Bettag (Mittwoch vor 23. Nov): SN
        if bundesland == "SN" {
            result.append(Holiday(date: bussUndBettag(year: year),
                                  name: "Buss- und Bettag"))
        }

        return result
    }

    // MARK: - Buss- und Bettag

    private static func bussUndBettag(year: Int) -> Date {
        // Mittwoch vor dem 23. November
        let nov23 = makeDate(year: year, month: 11, day: 23)
        let cal = Calendar.current
        let weekday = cal.component(.weekday, from: nov23) // 1=So, 2=Mo, ..., 7=Sa
        // Mittwoch = weekday 4
        let daysBack = (weekday - 4 + 7) % 7
        let offset = daysBack == 0 ? 7 : daysBack
        return addDays(nov23, -offset)
    }

    // MARK: - Helpers

    static func makeDate(year: Int, month: Int, day: Int) -> Date {
        var components = DateComponents()
        components.year = year
        components.month = month
        components.day = day
        return Calendar.current.date(from: components)!
    }

    private static func addDays(_ date: Date, _ days: Int) -> Date {
        Calendar.current.date(byAdding: .day, value: days, to: date)!
    }
}
