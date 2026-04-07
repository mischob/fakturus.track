import SwiftUI

enum TimerSize {
    case large, medium, small

    var textStyle: Font.TextStyle {
        switch self {
        case .large: return .largeTitle
        case .medium: return .title2
        case .small: return .body
        }
    }
}

struct TimerDisplay: View {
    let startTime: Date
    var pauseOffset: TimeInterval = 0
    var isRunning: Bool = true
    var size: TimerSize = .large

    @State private var pulseOpacity: Double = 1.0
    @ScaledMetric(relativeTo: .body) private var indicatorSize: CGFloat = 10

    var body: some View {
        HStack(spacing: 8) {
            if isRunning {
                Circle()
                    .fill(Theme.timerActive)
                    .frame(width: indicatorSize, height: indicatorSize)
                    .opacity(pulseOpacity)
                    .animation(
                        .easeInOut(duration: 1).repeatForever(autoreverses: true),
                        value: pulseOpacity
                    )
                    .onAppear { pulseOpacity = 0.3 }
                    .accessibilityHidden(true)
            }

            if isRunning {
                TimelineView(.periodic(from: .now, by: 1)) { context in
                    let elapsed = context.date.timeIntervalSince(startTime) - pauseOffset
                    let safeElapsed = max(0, elapsed)
                    Text(safeElapsed.formattedHHMMSS)
                        .font(.system(size.textStyle, design: .monospaced))
                        .monospacedDigit()
                        .minimumScaleFactor(0.5)
                        .lineLimit(1)
                        .contentTransition(.numericText())
                        .accessibilityLabel(accessibleTimerText(elapsed: safeElapsed))
                        .accessibilityAddTraits(.updatesFrequently)
                }
            } else {
                let elapsed = Date().timeIntervalSince(startTime) - pauseOffset
                let safeElapsed = max(0, elapsed)
                Text(safeElapsed.formattedHHMMSS)
                    .font(.system(size.textStyle, design: .monospaced))
                    .monospacedDigit()
                    .minimumScaleFactor(0.5)
                    .lineLimit(1)
                    .accessibilityLabel(accessibleTimerText(elapsed: safeElapsed))
            }
        }
    }

    // MARK: - Accessibility

    private func accessibleTimerText(elapsed: TimeInterval) -> String {
        let totalSeconds = Int(elapsed)
        let h = totalSeconds / 3600
        let m = (totalSeconds % 3600) / 60
        if h > 0 {
            return String(localized: "a11y_timer_hours_minutes", defaultValue: "\(h) Stunden \(m) Minuten Arbeitszeit")
                .replacingOccurrences(of: "%lld", with: "") // fallback; localized format handles it
        }
        return String(localized: "a11y_timer_minutes", defaultValue: "\(m) Minuten Arbeitszeit")
            .replacingOccurrences(of: "%lld", with: "")
    }
}

// MARK: - Preview

#Preview("Running") {
    TimerDisplay(
        startTime: Date().addingTimeInterval(-3723),
        isRunning: true,
        size: .large
    )
    .padding()
}

#Preview("Stopped") {
    TimerDisplay(
        startTime: Date().addingTimeInterval(-7200),
        isRunning: false,
        size: .medium
    )
    .padding()
}

#Preview("Small") {
    TimerDisplay(
        startTime: Date().addingTimeInterval(-1800),
        pauseOffset: 600,
        isRunning: true,
        size: .small
    )
    .padding()
}
