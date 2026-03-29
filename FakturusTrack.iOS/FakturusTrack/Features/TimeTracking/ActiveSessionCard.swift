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
    }

    // MARK: - Idle State

    @ViewBuilder
    private var idleContent: some View {
        VStack(spacing: 16) {
            Text("Bereit fuer den naechsten Eintrag")
                .font(.headline)
                .foregroundStyle(.secondary)

            Button(action: {
                UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                onStart()
            }) {
                Label("Starten", systemImage: "play.fill")
                    .font(.headline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
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
                Text("Laufende Sitzung")
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
                    Text("Start")
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
                Button("Pause", systemImage: "pause.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onPause()
                }
                .buttonStyle(.bordered)
                .tint(Theme.timerPaused)

                Button("Stop", systemImage: "stop.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onStop()
                }
                .buttonStyle(.bordered)

                Button("Fertig", systemImage: "checkmark.circle.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onFinish()
                }
                .buttonStyle(.borderedProminent)
            }
        }
        .padding()
    }

    // MARK: - Paused State (E08-S03)

    @ViewBuilder
    private func pausedContent(_ session: WorkSession) -> some View {
        VStack(spacing: 16) {
            HStack {
                Circle()
                    .fill(Theme.timerPaused)
                    .frame(width: 8, height: 8)
                Text("Pausiert")
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
                    Text("Start")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(session.startTime.timeShort)
                }
                Spacer()
            }

            HStack(spacing: 12) {
                Button("Weiter", systemImage: "play.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onResume()
                }
                .buttonStyle(.borderedProminent)

                Button("Fertig", systemImage: "checkmark.circle.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onFinish()
                }
                .buttonStyle(.borderedProminent)
                .tint(.secondary)
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

    @State private var editDate: Date
    @State private var editStartTime: Date
    @State private var editStopTime: Date
    @State private var editPauseMinutes: String

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
        _editDate = State(initialValue: session.date)
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
        editStopTime > editStartTime && bruttoDuration <= 86400
    }

    var body: some View {
        VStack(spacing: 16) {
            HStack {
                Image(systemName: "pencil.circle.fill")
                    .foregroundStyle(Theme.warning)
                Text("Sitzung bearbeiten")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                Spacer()
            }

            VStack(spacing: 12) {
                DatePicker("Datum", selection: $editDate, displayedComponents: .date)
                    .environment(\.locale, Locale(identifier: "de_DE"))
                DatePicker("Start", selection: $editStartTime, displayedComponents: .hourAndMinute)
                DatePicker("Ende", selection: $editStopTime, displayedComponents: .hourAndMinute)
                HStack {
                    Text("Pause (min)")
                    Spacer()
                    TextField("0", text: $editPauseMinutes)
                        .keyboardType(.numberPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                }
            }

            Divider()

            HStack {
                Text("Brutto")
                    .foregroundStyle(.secondary)
                Spacer()
                Text(bruttoDuration.formattedHHMM)
                    .monospacedDigit()
            }
            .font(.subheadline)

            HStack {
                Text("Netto")
                    .foregroundStyle(.secondary)
                Spacer()
                Text(nettoDuration.formattedHHMM)
                    .bold()
                    .monospacedDigit()
            }
            .font(.subheadline)

            if !isValid {
                Text("Endzeit muss nach Startzeit liegen")
                    .font(.caption)
                    .foregroundStyle(Theme.danger)
            }

            HStack(spacing: 12) {
                Button(role: .destructive) {
                    onDelete()
                } label: {
                    Text("Verwerfen")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.bordered)

                Button {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onSave(editDate, editStartTime, editStopTime, pauseMinutes)
                    onFinish()
                } label: {
                    Text("Fertig")
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
