import SwiftUI

struct WorkdaySelector: View {
    @Binding var workDays: Int

    private let days: [(label: String, bit: Int)] = [
        ("Mo", 1), ("Di", 2), ("Mi", 4), ("Do", 8),
        ("Fr", 16), ("Sa", 32), ("So", 64)
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
            }
        }
    }
}
