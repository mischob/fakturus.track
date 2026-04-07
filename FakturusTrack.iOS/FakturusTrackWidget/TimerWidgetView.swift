import SwiftUI
import WidgetKit

struct TimerWidgetView: View {
    @Environment(\.widgetFamily) var family
    let entry: TimerWidgetEntry

    var body: some View {
        switch family {
        case .systemSmall:
            smallWidget
        case .systemMedium:
            mediumWidget
        case .accessoryCircular:
            circularWidget
        case .accessoryRectangular:
            rectangularWidget
        default:
            smallWidget
        }
    }

    // MARK: - Small (2x2)

    private var smallWidget: some View {
        VStack(spacing: 8) {
            HStack {
                Circle()
                    .fill(entry.isRunning ? .green : .secondary)
                    .frame(width: 8, height: 8)
                Spacer()
                Image(systemName: "clock.fill")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            if entry.isRunning, let start = entry.startDate {
                Text(timerInterval: start...Date.distantFuture, countsDown: false)
                    .font(.system(.title, design: .monospaced))
                    .monospacedDigit()
                    .minimumScaleFactor(0.7)
            } else if entry.isPaused {
                Image(systemName: "pause.circle.fill")
                    .font(.title)
                    .foregroundStyle(.orange)
                Text(String(localized: "widget_paused"))
                    .font(.caption)
            } else {
                Text(String(localized: "widget_ready"))
                    .font(.headline)
                    .foregroundStyle(.secondary)
            }

            Spacer()
        }
        .padding()
    }

    // MARK: - Medium (4x2)

    private var mediumWidget: some View {
        HStack {
            // Left: Timer
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Circle()
                        .fill(entry.isRunning ? .green : .secondary)
                        .frame(width: 8, height: 8)
                    Text(statusText)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                if entry.isRunning, let start = entry.startDate {
                    Text(timerInterval: start...Date.distantFuture, countsDown: false)
                        .font(.system(.title, design: .monospaced))
                        .monospacedDigit()
                    Text("\(String(localized: "widget_since")) \(start.formatted(date: .omitted, time: .shortened))")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                } else {
                    Text("--:--:--")
                        .font(.system(.title, design: .monospaced))
                        .foregroundStyle(.secondary)
                }
            }

            Spacer()

            // Right: Day summary + buttons
            VStack(alignment: .trailing, spacing: 8) {
                VStack(alignment: .trailing) {
                    Text(String(localized: "widget_today"))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    let h = entry.todayTotalSeconds / 3600
                    let m = (entry.todayTotalSeconds % 3600) / 60
                    Text("\(h):\(String(format: "%02d", m))h")
                        .font(.title2)
                        .monospacedDigit()

                    if entry.pauseMinutes > 0 {
                        Text("\(String(localized: "widget_pause")): \(entry.pauseMinutes) Min")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                    }
                }

                // Interactive buttons (iOS 17+)
                if #available(iOSApplicationExtension 17.0, *) {
                    interactiveButtons
                }
            }
        }
        .padding()
    }

    @available(iOSApplicationExtension 17.0, *)
    @ViewBuilder
    private var interactiveButtons: some View {
        HStack(spacing: 8) {
            if entry.isRunning {
                Button(intent: PauseResumeTimerIntent()) {
                    Image(systemName: entry.isPaused ? "play.fill" : "pause.fill")
                }
                .buttonStyle(.bordered)

                Button(intent: StopTimerIntent()) {
                    Image(systemName: "stop.fill")
                }
                .buttonStyle(.bordered)
                .tint(.red)
            } else {
                Button(intent: StartTimerIntent()) {
                    Label(String(localized: "times_timer_start"), systemImage: "play.fill")
                }
                .buttonStyle(.borderedProminent)
                .tint(.green)
            }
        }
    }

    // MARK: - Lock Screen Circular

    private var circularWidget: some View {
        ZStack {
            AccessoryWidgetBackground()
            if entry.isRunning, let start = entry.startDate {
                // Text(timerInterval:) zählt live auf dem Lock Screen!
                VStack(spacing: 0) {
                    Image(systemName: "timer")
                        .font(.caption2)
                    Text(timerInterval: start...Date.distantFuture, countsDown: false)
                        .font(.system(.caption, design: .monospaced))
                        .monospacedDigit()
                        .multilineTextAlignment(.center)
                }
            } else if entry.isPaused {
                Image(systemName: "pause.circle")
                    .font(.title3)
            } else {
                Image(systemName: "clock.badge.checkmark")
                    .font(.title3)
            }
        }
    }

    // MARK: - Lock Screen Rectangular

    private var rectangularWidget: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                if entry.isRunning, let start = entry.startDate {
                    HStack(spacing: 4) {
                        Image(systemName: "record.circle")
                            .foregroundStyle(.green)
                            .font(.caption2)
                        Text("Fakturus Track")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                    }
                    Text(timerInterval: start...Date.distantFuture, countsDown: false)
                        .font(.system(.title3, design: .monospaced))
                        .monospacedDigit()
                } else if entry.isPaused {
                    HStack(spacing: 4) {
                        Image(systemName: "pause.circle")
                            .foregroundStyle(.orange)
                            .font(.caption2)
                        Text("Pausiert")
                            .font(.caption2)
                    }
                    Text("--:--:--")
                        .font(.system(.title3, design: .monospaced))
                        .foregroundStyle(.secondary)
                } else {
                    HStack(spacing: 4) {
                        Image(systemName: "clock.badge.checkmark")
                            .font(.caption2)
                        Text("Fakturus Track")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                    }
                    Text("Bereit")
                        .font(.headline)
                }
            }
            Spacer()
        }
    }

    private var statusText: String {
        if entry.isRunning && !entry.isPaused {
            return String(localized: "widget_status_running")
        } else if entry.isPaused {
            return String(localized: "widget_status_paused")
        } else {
            return String(localized: "widget_status_ready")
        }
    }
}
