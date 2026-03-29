import SwiftUI

enum TimerSize {
    case large, medium, small

    var fontSize: CGFloat {
        switch self {
        case .large: return 48
        case .medium: return 28
        case .small: return 17
        }
    }
}

struct TimerDisplay: View {
    let startTime: Date
    var pauseOffset: TimeInterval = 0
    var isRunning: Bool = true
    var size: TimerSize = .large

    @State private var pulseOpacity: Double = 1.0

    var body: some View {
        HStack(spacing: 8) {
            if isRunning {
                Circle()
                    .fill(Theme.timerActive)
                    .frame(width: 10, height: 10)
                    .opacity(pulseOpacity)
                    .animation(
                        .easeInOut(duration: 1).repeatForever(autoreverses: true),
                        value: pulseOpacity
                    )
                    .onAppear { pulseOpacity = 0.3 }
            }

            if isRunning {
                TimelineView(.periodic(from: .now, by: 1)) { context in
                    let elapsed = context.date.timeIntervalSince(startTime) - pauseOffset
                    Text(max(0, elapsed).formattedHHMMSS)
                        .font(.system(size: size.fontSize, design: .monospaced))
                        .monospacedDigit()
                }
            } else {
                let elapsed = Date().timeIntervalSince(startTime) - pauseOffset
                Text(max(0, elapsed).formattedHHMMSS)
                    .font(.system(size: size.fontSize, design: .monospaced))
                    .monospacedDigit()
            }
        }
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
