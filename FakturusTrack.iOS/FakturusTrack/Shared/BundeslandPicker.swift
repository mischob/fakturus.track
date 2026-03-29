import SwiftUI

struct BundeslandPicker: View {
    @Binding var selectedBundesland: String

    static let bundeslaender: [(code: String, name: String)] = [
        ("BW", "Baden-Wuerttemberg"),
        ("BY", "Bayern"),
        ("BE", "Berlin"),
        ("BB", "Brandenburg"),
        ("HB", "Bremen"),
        ("HH", "Hamburg"),
        ("HE", "Hessen"),
        ("MV", "Mecklenburg-Vorpommern"),
        ("NI", "Niedersachsen"),
        ("NW", "Nordrhein-Westfalen"),
        ("RP", "Rheinland-Pfalz"),
        ("SL", "Saarland"),
        ("SN", "Sachsen"),
        ("ST", "Sachsen-Anhalt"),
        ("SH", "Schleswig-Holstein"),
        ("TH", "Thueringen"),
    ]

    var body: some View {
        let currentYear = Calendar.current.component(.year, from: Date())
        Picker("Bundesland", selection: $selectedBundesland) {
            ForEach(Self.bundeslaender, id: \.code) { land in
                let count = HolidayCalculator.holidayCount(bundesland: land.code, year: currentYear)
                Text("\(land.name) (\(count) Feiertage)")
                    .tag(land.code)
            }
        }
    }
}
