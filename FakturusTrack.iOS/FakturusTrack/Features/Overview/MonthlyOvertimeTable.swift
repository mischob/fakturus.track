import SwiftUI

struct MonthlyOvertimeTable: View {
    let months: [MonthlyOvertimeDTO]

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Text(String(localized: "overview_month"))
                    .frame(maxWidth: .infinity, alignment: .leading)
                Text(String(localized: "overview_worked"))
                    .frame(width: 80, alignment: .trailing)
                Text(String(localized: "overview_expected"))
                    .frame(width: 80, alignment: .trailing)
                Text("+/-")
                    .frame(width: 70, alignment: .trailing)
            }
            .font(.caption.bold())
            .foregroundStyle(.secondary)
            .padding(.horizontal)
            .padding(.vertical, 8)
            .accessibilityHidden(true)

            Divider()

            // Rows
            ForEach(months, id: \.month) { entry in
                HStack {
                    Text(entry.monthName)
                        .frame(maxWidth: .infinity, alignment: .leading)
                    Text(formatWorked(entry.workedHours))
                        .frame(width: 80, alignment: .trailing)
                    Text(formatWorked(entry.expectedHours))
                        .frame(width: 80, alignment: .trailing)
                    Text(OverviewViewModel.formatHours(entry.overtimeHours))
                        .frame(width: 70, alignment: .trailing)
                        .foregroundStyle(entry.overtimeHours >= 0 ? Theme.success : Theme.danger)
                }
                .font(.caption)
                .padding(.horizontal)
                .padding(.vertical, 6)
                .accessibilityElement(children: .combine)
                .accessibilityLabel("\(entry.monthName): \(formatWorked(entry.workedHours)) gearbeitet, \(formatWorked(entry.expectedHours)) erwartet, \(OverviewViewModel.formatHours(entry.overtimeHours)) Differenz")

                if entry.month != months.last?.month {
                    Divider().padding(.horizontal)
                }
            }

            // Footer / Total
            if !months.isEmpty {
                Divider()

                let totalWorked = months.reduce(0.0) { $0 + $1.workedHours }
                let totalExpected = months.reduce(0.0) { $0 + $1.expectedHours }
                let totalOvertime = months.reduce(0.0) { $0 + $1.overtimeHours }

                HStack {
                    Text(String(localized: "overview_total"))
                        .fontWeight(.bold)
                        .frame(maxWidth: .infinity, alignment: .leading)
                    Text(formatWorked(totalWorked))
                        .frame(width: 80, alignment: .trailing)
                    Text(formatWorked(totalExpected))
                        .frame(width: 80, alignment: .trailing)
                    Text(OverviewViewModel.formatHours(totalOvertime))
                        .frame(width: 70, alignment: .trailing)
                        .foregroundStyle(totalOvertime >= 0 ? Theme.success : Theme.danger)
                }
                .font(.caption.bold())
                .padding(.horizontal)
                .padding(.vertical, 8)
                .accessibilityElement(children: .combine)
                .accessibilityLabel("Gesamt: \(formatWorked(totalWorked)) gearbeitet, \(formatWorked(totalExpected)) erwartet, \(OverviewViewModel.formatHours(totalOvertime)) Differenz")
            }
        }
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 12))
    }

    private func formatWorked(_ hours: Double) -> String {
        let h = Int(hours)
        let m = Int((hours - Double(h)) * 60)
        return "\(h):\(String(format: "%02d", m))h"
    }
}
