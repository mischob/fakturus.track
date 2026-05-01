import SwiftUI

struct SessionDetailSheet: View {
    let session: WorkSession
    let isCreateMode: Bool
    let onSave: (Date, Date, Date?, Int) -> Void
    let onDelete: () -> Void

    @Environment(\.dismiss) private var dismiss

    // Two full timestamps. We deliberately drop the previous `editDate` field —
    // mixing a single date with two time-only pickers caused multi-day sessions
    // to collapse onto one day on save. With combined date+time pickers, what
    // the user sees is exactly what gets stored.
    @State private var editStartTime: Date
    @State private var editStopTime: Date
    @State private var editPauseMinutes: String
    @State private var showDeleteConfirmation = false

    /// Allow up to 72h to cover "forgot to stop the timer" cases without
    /// silently accepting nonsense (100h+).
    private static let maxDurationSeconds: TimeInterval = 72 * 3600

    init(
        session: WorkSession,
        isCreateMode: Bool = false,
        onSave: @escaping (Date, Date, Date?, Int) -> Void,
        onDelete: @escaping () -> Void
    ) {
        self.session = session
        self.isCreateMode = isCreateMode
        self.onSave = onSave
        self.onDelete = onDelete
        _editStartTime = State(initialValue: session.startTime)
        _editStopTime = State(initialValue: session.stopTime ?? session.startTime.addingTimeInterval(3600))
        _editPauseMinutes = State(initialValue: String(session.pauseMinutes))
    }

    private var pauseMinutes: Int { Int(editPauseMinutes) ?? 0 }

    private var bruttoDuration: TimeInterval {
        editStopTime.timeIntervalSince(editStartTime)
    }

    private var nettoDuration: TimeInterval {
        max(0, bruttoDuration - Double(pauseMinutes * 60))
    }

    private var validationError: String? {
        if editStopTime <= editStartTime {
            return "Endzeit muss nach Startzeit liegen"
        }
        if bruttoDuration > Self.maxDurationSeconds {
            return "Dauer ueber 72 Stunden — bitte pruefen"
        }
        return nil
    }

    private var isValid: Bool { validationError == nil }

    var body: some View {
        NavigationStack {
            Form {
                Section("Zeitraum") {
                    DatePicker(
                        "Start",
                        selection: $editStartTime,
                        displayedComponents: [.date, .hourAndMinute]
                    )
                    .environment(\.locale, Locale(identifier: "de_DE"))

                    DatePicker(
                        "Ende",
                        selection: $editStopTime,
                        displayedComponents: [.date, .hourAndMinute]
                    )
                    .environment(\.locale, Locale(identifier: "de_DE"))

                    HStack {
                        Text("Pause (min)")
                        Spacer()
                        TextField("0", text: $editPauseMinutes)
                            .keyboardType(.numberPad)
                            .multilineTextAlignment(.trailing)
                            .frame(width: 60)
                    }
                }

                Section("Berechnung") {
                    HStack {
                        Text("Brutto")
                        Spacer()
                        Text(bruttoDuration.formattedHHMM)
                            .monospacedDigit()
                    }
                    HStack {
                        Text("Netto")
                        Spacer()
                        Text(nettoDuration.formattedHHMM)
                            .bold()
                            .monospacedDigit()
                    }
                }

                if let error = validationError {
                    Section {
                        Text(error)
                            .foregroundStyle(Theme.danger)
                            .font(.caption)
                    }
                }

                if !isCreateMode {
                    Section {
                        Button("Loeschen", role: .destructive) {
                            showDeleteConfirmation = true
                        }
                    }
                }
            }
            .navigationTitle(isCreateMode ? "Neue Sitzung" : "Session bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        if isCreateMode {
                            onDelete()
                        }
                        dismiss()
                    }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Speichern") {
                        commitPendingPickerEdits()
                        save()
                    }
                    .disabled(!isValid)
                }
            }
            .toolbar {
                ToolbarItemGroup(placement: .keyboard) {
                    Spacer()
                    Button("Fertig") {
                        commitPendingPickerEdits()
                    }
                }
            }
            .confirmationDialog("Session loeschen?", isPresented: $showDeleteConfirmation) {
                Button("Loeschen", role: .destructive) {
                    onDelete()
                    dismiss()
                }
            }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }

    /// Drops focus from any open keyboard / picker overlay so that pending
    /// edits flush into their bindings before we read them.
    private func commitPendingPickerEdits() {
        UIApplication.shared.sendAction(
            #selector(UIResponder.resignFirstResponder),
            to: nil, from: nil, for: nil
        )
    }

    private func save() {
        guard isValid else { return }
        // session.date is derived from startTime so the row's group / sort key
        // always matches the actual start day, even after multi-day edits.
        let derivedDate = Calendar.current.startOfDay(for: editStartTime)
        onSave(derivedDate, editStartTime, editStopTime, pauseMinutes)
        dismiss()
    }
}

// MARK: - Preview

#Preview("Edit") {
    SessionDetailSheet(
        session: WorkSession(
            date: Date(),
            startTime: Calendar.current.date(bySettingHour: 8, minute: 0, second: 0, of: Date())!,
            stopTime: Calendar.current.date(bySettingHour: 17, minute: 0, second: 0, of: Date())!,
            pauseMinutes: 30,
            isPendingSync: true,
            isSynced: false,
            isFinished: true
        ),
        onSave: { _, _, _, _ in },
        onDelete: {}
    )
}

#Preview("Create") {
    SessionDetailSheet(
        session: WorkSession(
            date: Date(),
            startTime: Date().addingTimeInterval(-3600),
            stopTime: Date(),
            pauseMinutes: 0,
            isPendingSync: true,
            isSynced: false,
            isFinished: false
        ),
        isCreateMode: true,
        onSave: { _, _, _, _ in },
        onDelete: {}
    )
}
