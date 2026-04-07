import Foundation

extension Date {
    private static let germanLocale = Locale(identifier: "de_DE")

    func formatted(as format: String) -> String {
        let formatter = DateFormatter()
        formatter.locale = Self.germanLocale
        formatter.dateFormat = format
        return formatter.string(from: self)
    }

    /// "Maerz 2026" / "März 2026"
    var monthYearString: String {
        formatted(as: "MMMM yyyy")
    }

    /// "Fr"
    var weekdayShort: String {
        formatted(as: "EE")
    }

    /// "29.03.2026"
    var dateShort: String {
        formatted(as: "dd.MM.yyyy")
    }

    /// "08:30"
    var timeShort: String {
        formatted(as: "HH:mm")
    }

    /// "Freitag 29.03."
    var weekdayDateShort: String {
        formatted(as: "EEEE dd.MM.")
    }
}
