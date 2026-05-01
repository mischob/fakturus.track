import SwiftUI

struct ActiveSessionCard: View {
    let session: WorkSession?
    let isPaused: Bool
    let currentPauseStart: Date?
    let accumulatedPauseMinutes: Int
    let onStart: () -> Void
    let onStop: () -> Void
    let onFinish: () -> Void
    let onPause: () -> Void
    let onResume: () -> Void
    let onSave: (Date, Date, Date?, Int) -> Void
    let onDelete: () -> Void

    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    init(
        session: WorkSession?,
        isPaused: Bool,
        currentPauseStart: Date? = nil,
        accumulatedPauseMinutes: Int = 0,
        onStart: @escaping () -> Void,
        onStop: @escaping () -> Void,
        onFinish: @escaping () -> Void,
        onPause: @escaping () -> Void,
        onResume: @escaping () -> Void,
        onSave: @escaping (Date, Date, Date?, Int) -> Void,
        onDelete: @escaping () -> Void
    ) {
        self.session = session
        self.isPaused = isPaused
        self.currentPauseStart = currentPauseStart
        self.accumulatedPauseMinutes = accumulatedPauseMinutes
        self.onStart = onStart
        self.onStop = onStop
        self.onFinish = onFinish
        self.onPause = onPause
        self.onResume = onResume
        self.onSave = onSave
        self.onDelete = onDelete
    }

    var body: some View {
        GroupBox {
            if let session {
                if isPaused {
                    pausedContent(session)
                } else if session.isRunning {
                    runningContent(session)
                } else if !session.isFinished {
                    stoppedContent(session)
                }
            } else {
                idleContent
            }
        }
        .animation(reduceMotion ? nil : .spring(response: 0.3, dampingFraction: 0.7), value: session?.isRunning)
        .animation(reduceMotion ? nil : .spring(response: 0.3, dampingFraction: 0.7), value: isPaused)
    }

    // MARK: - Idle State

    @ViewBuilder
    private var idleContent: some View {
        VStack(spacing: 16) {
            Text(String(localized: "times_session_idle"))
                .font(.headline)
                .foregroundStyle(.secondary)

            Button(action: {
                HapticManager.timerStart()
                onStart()
            }) {
                Label(String(localized: "times_timer_start"), systemImage: "play.fill")
                    .font(.headline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .accessibilityHint(String(localized: "a11y_start_hint"))
        }
        .padding()
    }

    // MARK: - Running State

    @ViewBuilder
    private func runningContent(_ session: WorkSession) -> some View {
        VStack(spacing: 16) {
            HStack {
                Circle()
                    .fill(Theme.timerActive)
                    .frame(width: 8, height: 8)
                    .accessibilityHidden(true)
                Text(String(localized: "times_session_running"))
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                Spacer()
            }

            TimerDisplay(
                startTime: session.startTime,
                pauseOffset: Double(accumulatedPauseMinutes * 60),
                isRunning: true,
                size: .large
            )

            HStack {
                VStack(alignment: .leading) {
                    Text(String(localized: "times_start"))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(session.startTime.timeShort)
                }
                Spacer()
                if accumulatedPauseMinutes > 0 {
                    Text("Pause: \(accumulatedPauseMinutes) min")
                        .font(.caption)
                        .foregroundStyle(Theme.pause)
                }
            }

            HStack(spacing: 12) {
                Button(String(localized: "times_timer_pause"), systemImage: "pause.fill") {
                    HapticManager.timerPauseResume()
                    onPause()
                }
                .buttonStyle(.bordered)
                .tint(Theme.timerPaused)
                .accessibilityHint(String(localized: "a11y_pause_hint"))

                Button("Stop", systemImage: "stop.fill") {
                    HapticManager.timerStop()
                    onStop()
                }
                .buttonStyle(.bordered)
                .accessibilityLabel(String(localized: "times_timer_stop"))
                .accessibilityHint(String(localized: "a11y_stop_hint"))

                Button(String(localized: "times_timer_finish"), systemImage: "checkmark.circle.fill") {
                    HapticManager.sessionFinished()
                    onFinish()
                }
                .buttonStyle(.borderedProminent)
                .accessibilityHint(String(localized: "a11y_finish_hint"))
            }
        }
        .padding()
    }

    // MARK: - Paused State

    @ViewBuilder
    private func pausedContent(_ session: WorkSession) -> some View {
        VStack(spacing: 16) {
            HStack {
                Circle()
                    .fill(Theme.timerPaused)
                    .frame(width: 8, height: 8)
                    .accessibilityHidden(true)
                Text(String(localized: "times_session_paused"))
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                Spacer()
            }

            // Pause timer showing how long the current pause has been
            if let pauseStart = currentPauseStart {
                TimerDisplay(
                    startTime: pauseStart,
                    isRunning: true,
                    size: .medium
                )
                .foregroundStyle(Theme.timerPaused)
            }

            // Accumulated pause so far
            if accumulatedPauseMinutes > 0 {
                Text("Pause bisher: \(accumulatedPauseMinutes) min")
                    .font(.caption)
                    .foregroundStyle(Theme.pause)
            }

            HStack {
                VStack(alignment: .leading) {
                    Text(String(localized: "times_start"))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(session.startTime.timeShort)
                }
                Spacer()
            }

            HStack(spacing: 12) {
                Button(String(localized: "times_timer_resume"), systemImage: "play.fill") {
                    HapticManager.timerPauseResume()
                    onResume()
                }
                .buttonStyle(.borderedProminent)
                .accessibilityHint(String(localized: "a11y_resume_hint"))

                Button(String(localized: "times_timer_finish"), systemImage: "checkmark.circle.fill") {
                    HapticManager.sessionFinished()
                    onFinish()
                }
                .buttonStyle(.borderedProminent)
                .tint(.secondary)
                .accessibilityHint(String(localized: "a11y_finish_hint"))
            }
        }
        .padding()
    }

    // MARK: - Stopped State

    @ViewBuilder
    private func stoppedContent(_ session: WorkSession) -> some View {
        StoppedSessionEditor(
            session: session,
            onSave: onSave,
            onFinish: onFinish,
            onDelete: onDelete
        )
    }
}

// MARK: - StoppedSessionEditor

private struct StoppedSessionEditor: View {
    let session: WorkSession
    let onSave: (Date, Date, Date?, Int) -> Void
    let onFinish: () -> Void
    let onDelete: () -> Void

    // Two full timestamps — see SessionDetailSheet for rationale. A single
    // `editDate` combined with time-only pickers caused multi-day sessions to
    // be silently truncated when the user tapped finish.
    @State private var editStartTime: Date
    @State private var editStopTime: Date
    @State private var editPauseMinutes: String

    private static let maxDurationSeconds: TimeInterval = 72 * 3600

    init(
        session: WorkSession,
        onSave: @escaping (Date, Date, Date?, Int) -> Void,
        onFinish: @escaping () -> Void,
        onDelete: @escaping () -> Void
    ) {
        self.session = session
        self.onSave = onSave
        self.onFinish = onFinish
        self.onDelete = onDelete
        _editStartTime = State(initialValue: session.startTime)
        _editStopTime = State(initialValue: session.stopTime ?? Date())
        _editPauseMinutes = State(initialValue: String(session.pauseMinutes))
    }

    private var pauseMinutes: Int { Int(editPauseMinutes) ?? 0 }

    private var bruttoDuration: TimeInterval {
        editStopTime.timeIntervalSince(editStartTime)
    }

    private var nettoDuration: TimeInterval {
        max(0, bruttoDuration - Double(pauseMinutes * 60))
    }

    private var isValid: Bool {
        editStopTime > editStartTime && bruttoDuration <= Self.maxDurationSeconds
    }

    var body: some View {
        VStack(spacing: 16) {
            HStack {
                Image(systemName: "pencil.circle.fill")
                    .foregroundStyle(Theme.warning)
                    .accessibilityHidden(true)
                Text(String(localized: "times_session_edit"))
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                Spacer()
            }

            VStack(spacing: 12) {
                DatePicker(
                    String(localized: "times_start"),
                    selection: $editStartTime,
                    displayedComponents: [.date, .hourAndMinute]
                )
                .environment(\.locale, Locale(identifier: "de_DE"))
                DatePicker(
                    String(localized: "times_end"),
                    selection: $editStopTime,
                    displayedComponents: [.date, .hourAndMinute]
                )
                .environment(\.locale, Locale(identifier: "de_DE"))
                HStack {
                    Text(String(localized: "times_pause_min"))
                    Spacer()
                    TextField("0", text: $editPauseMinutes)
                        .keyboardType(.numberPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                }
            }

            Divider()

            HStack {
                Text(String(localized: "times_gross"))
                    .foregroundStyle(.secondary)
                Spacer()
                Text(bruttoDuration.formattedHHMM)
                    .monospacedDigit()
            }
            .font(.subheadline)

            HStack {
                Text(String(localized: "times_net"))
                    .foregroundStyle(.secondary)
                Spacer()
                Text(nettoDuration.formattedHHMM)
                    .bold()
                    .monospacedDigit()
            }
            .font(.subheadline)

            if !isValid {
                Text(String(localized: "times_end_after_start_error"))
                    .font(.caption)
                    .foregroundStyle(Theme.danger)
            }

            HStack(spacing: 12) {
                Button(role: .destructive) {
                    onDelete()
                } label: {
                    Text(String(localized: "times_session_discard"))
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.bordered)

                Button {
                    HapticManager.sessionFinished()
                    UIApplication.shared.sendAction(
                        #selector(UIResponder.resignFirstResponder),
                        to: nil, from: nil, for: nil
                    )
                    let derivedDate = Calendar.current.startOfDay(for: editStartTime)
                    onSave(derivedDate, editStartTime, editStopTime, pauseMinutes)
                    onFinish()
                } label: {
                    Text(String(localized: "times_timer_finish"))
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.borderedProminent)
                .disabled(!isValid)
            }
        }
        .padding()
    }
}

// MARK: - Previews

#Preview("Idle") {
    ActiveSessionCard(
        session: nil,
        isPaused: false,
        onStart: {},
        onStop: {},
        onFinish: {},
        onPause: {},
        onResume: {},
        onSave: { _, _, _, _ in },
        onDelete: {}
    )
    .padding()
}

#Preview("Running") {
    let session = WorkSession(
        date: Date(),
        startTime: Date().addingTimeInterval(-3600),
        pauseMinutes: 0,
        isPendingSync: true,
        isSynced: false,
        isFinished: false
    )
    ActiveSessionCard(
        session: session,
        isPaused: false,
        onStart: {},
        onStop: {},
        onFinish: {},
        onPause: {},
        onResume: {},
        onSave: { _, _, _, _ in },
        onDelete: {}
    )
    .padding()
}

#Preview("Paused") {
    let session = WorkSession(
        date: Date(),
        startTime: Date().addingTimeInterval(-3600),
        pauseMinutes: 15,
        isPendingSync: true,
        isSynced: false,
        isFinished: false
    )
    ActiveSessionCard(
        session: session,
        isPaused: true,
        currentPauseStart: Date().addingTimeInterval(-300),
        accumulatedPauseMinutes: 15,
        onStart: {},
        onStop: {},
        onFinish: {},
        onPause: {},
        onResume: {},
        onSave: { _, _, _, _ in },
        onDelete: {}
    )
    .padding()
}

#Preview("Stopped") {
    let session = WorkSession(
        date: Date(),
        startTime: Date().addingTimeInterval(-7200),
        stopTime: Date(),
        pauseMinutes: 30,
        isPendingSync: true,
        isSynced: false,
        isFinished: false
    )
    ActiveSessionCard(
        session: session,
        isPaused: false,
        onStart: {},
        onStop: {},
        onFinish: {},
        onPause: {},
        onResume: {},
        onSave: { _, _, _, _ in },
        onDelete: {}
    )
    .padding()
}
