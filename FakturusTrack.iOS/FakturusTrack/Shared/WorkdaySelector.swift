import SwiftUI

struct WorkdaySelector: View {
    @Binding var workDays: Int

    private let days: [(label: String, fullName: String, bit: Int)] = [
        ("Mo", "Montag", 1), ("Di", "Dienstag", 2), ("Mi", "Mittwoch", 4), ("Do", "Donnerstag", 8),
        ("Fr", "Freitag", 16), ("Sa", "Samstag", 32), ("So", "Sonntag", 64)
    ]

    var body: some View {
        HStack(spacing: 8) {
            ForEach(days, id: \.bit) { day in
                let isSelected = (workDays & day.bit) != 0
                Text(day.label)
                    .font(.caption.bold())
                    .frame(width: 36, height: 36)
                    .background(
                        Capsule()
                            .fill(isSelected ? Theme.primary : Color.clear)
                    )
                    .overlay(
                        Capsule()
                            .stroke(isSelected ? Theme.primary : Theme.gray200, lineWidth: 1)
                    )
                    .foregroundStyle(isSelected ? .white : Theme.textPrimary)
                    .onTapGesture {
                        if isSelected {
                            workDays &= ~day.bit
                        } else {
                            workDays |= day.bit
                        }
                    }
                    .accessibilityLabel(day.fullName)
                    .accessibilityValue(isSelected ? "aktiviert" : "deaktiviert")
                    .accessibilityAddTraits(.isButton)
            }
        }
    }
}
