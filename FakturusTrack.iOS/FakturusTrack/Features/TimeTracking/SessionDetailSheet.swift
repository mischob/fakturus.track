import SwiftUI

struct SessionDetailSheet: View {
    let session: WorkSession
    let isCreateMode: Bool
    let onSave: (Date, Date, Date?, Int) -> Void
    let onDelete: () -> Void

    @Environment(\.dismiss) private var dismiss

    @State private var editDate: Date
    @State private var editStartTime: Date
    @State private var editStopTime: Date
    @State private var editPauseMinutes: String
    @State private var hasStopTime: Bool
    @State private var showDeleteConfirmation = false
    @State private var validationError: String?

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
        _editDate = State(initialValue: session.date)
        _editStartTime = State(initialValue: session.startTime)
        _editStopTime = State(initialValue: session.stopTime ?? session.startTime.addingTimeInterval(3600))
        _editPauseMinutes = State(initialValue: String(session.pauseMinutes))
        _hasStopTime = State(initialValue: session.stopTime != nil)
    }

    private var pauseMinutes: Int { Int(editPauseMinutes) ?? 0 }

    private var bruttoDuration: TimeInterval {
        effectiveStopTime.timeIntervalSince(effectiveStartTime)
    }

    private var nettoDuration: TimeInterval {
        max(0, bruttoDuration - Double(pauseMinutes * 60))
    }

    /// Combine editDate with time-only pickers to get comparable dates
    private var effectiveStartTime: Date {
        let cal = Calendar.current
        let startComps = cal.dateComponents([.hour, .minute], from: editStartTime)
        return cal.date(bySettingHour: startComps.hour ?? 0, minute: startComps.minute ?? 0, second: 0, of: editDate) ?? editStartTime
    }

    private var effectiveStopTime: Date {
        let cal = Calendar.current
        let stopComps = cal.dateComponents([.hour, .minute], from: editStopTime)
        return cal.date(bySettingHour: stopComps.hour ?? 0, minute: stopComps.minute ?? 0, second: 0, of: editDate) ?? editStopTime
    }

    private var isValid: Bool {
        if hasStopTime {
            return effectiveStopTime > effectiveStartTime && effectiveStopTime.timeIntervalSince(effectiveStartTime) <= 86400
        }
        return true
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Zeitraum") {
                    DatePicker("Datum", selection: $editDate, displayedComponents: .date)
                        .environment(\.locale, Locale(identifier: "de_DE"))
                    DatePicker("Start", selection: $editStartTime, displayedComponents: .hourAndMinute)
                    if hasStopTime {
                        DatePicker("Ende", selection: $editStopTime, displayedComponents: .hourAndMinute)
                    } else {
                        Button {
                            hasStopTime = true
                            editStopTime = Date()
                        } label: {
                            HStack {
                                Text("Ende")
                                    .foregroundStyle(.primary)
                                Spacer()
                                Text("Endzeit hinzufuegen")
                                    .foregroundStyle(.blue)
                            }
                        }
                    }
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
                        onSave(editDate, effectiveStartTime, hasStopTime ? effectiveStopTime : nil, pauseMinutes)
                        dismiss()
                    }
                    .disabled(!isValid)
                }
            }
            .confirmationDialog("Session loeschen?", isPresented: $showDeleteConfirmation) {
                Button("Loeschen", role: .destructive) {
                    onDelete()
                    dismiss()
                }
            }
            .onChange(of: editStopTime) { _, _ in validate() }
            .onChange(of: editStartTime) { _, _ in validate() }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }

    private func validate() {
        guard hasStopTime else { validationError = nil; return }
        if effectiveStopTime <= effectiveStartTime {
            validationError = "Endzeit muss nach Startzeit liegen"
        } else if bruttoDuration > 86400 {
            validationError = "Dauer ueber 24 Stunden"
        } else {
            validationError = nil
        }
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
