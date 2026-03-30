import SwiftUI

struct MonthGroup: View {
    let monthName: String
    let sessions: [WorkSession]
    let onDeleteSession: (WorkSession) -> Void
    let onSelectSession: (WorkSession) -> Void

    @State private var isExpanded: Bool
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    init(
        monthName: String,
        sessions: [WorkSession],
        isCurrentMonth: Bool = false,
        onDeleteSession: @escaping (WorkSession) -> Void,
        onSelectSession: @escaping (WorkSession) -> Void
    ) {
        self.monthName = monthName
        self.sessions = sessions
        self.onDeleteSession = onDeleteSession
        self.onSelectSession = onSelectSession
        _isExpanded = State(initialValue: isCurrentMonth)
    }

    private var totalNetDuration: TimeInterval {
        sessions.reduce(0) { $0 + $1.netDuration }
    }

    var body: some View {
        VStack(spacing: 0) {
            // Header
            Button {
                if reduceMotion {
                    isExpanded.toggle()
                } else {
                    withAnimation(.spring(response: 0.25)) { isExpanded.toggle() }
                }
            } label: {
                HStack {
                    Text(monthName)
                        .font(.headline)
                    Spacer()
                    Text("\(sessions.count) Eintr.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(totalNetDuration.formattedHHMM)
                        .font(.subheadline.bold())
                        .monospacedDigit()
                    Image(systemName: "chevron.right")
                        .rotationEffect(.degrees(isExpanded ? 90 : 0))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                .padding(.vertical, 8)
            }
            .buttonStyle(.plain)
            .accessibilityElement(children: .combine)
            .accessibilityLabel("\(monthName), \(sessions.count) Eintraege, \(totalNetDuration.formattedHHMM)")
            .accessibilityAddTraits(.isHeader)
            .accessibilityHint(isExpanded
                ? String(localized: "a11y_collapse_hint")
                : String(localized: "a11y_expand_hint"))

            // Content
            if isExpanded {
                Divider()
                ForEach(sessions) { session in
                    SessionRow(
                        session: session,
                        onTap: { onSelectSession(session) },
                        onDelete: { onDeleteSession(session) }
                    )
                    .padding(.vertical, 4)

                    if session.id != sessions.last?.id {
                        Divider()
                            .padding(.leading, 32)
                    }
                }
            }
        }
    }
}

// MARK: - Preview

#Preview {
    let sessions = [
        WorkSession(
            date: Date(),
            startTime: Calendar.current.date(bySettingHour: 8, minute: 0, second: 0, of: Date())!,
            stopTime: Calendar.current.date(bySettingHour: 17, minute: 0, second: 0, of: Date())!,
            pauseMinutes: 30,
            isPendingSync: false,
            isSynced: true,
            isFinished: true
        ),
        WorkSession(
            date: Date().addingTimeInterval(-86400),
            startTime: Calendar.current.date(bySettingHour: 9, minute: 0, second: 0, of: Date().addingTimeInterval(-86400))!,
            stopTime: Calendar.current.date(bySettingHour: 16, minute: 30, second: 0, of: Date().addingTimeInterval(-86400))!,
            pauseMinutes: 0,
            isPendingSync: true,
            isSynced: false,
            isFinished: true
        ),
    ]

    ScrollView {
        MonthGroup(
            monthName: "Maerz 2026",
            sessions: sessions,
            isCurrentMonth: true,
            onDeleteSession: { _ in },
            onSelectSession: { _ in }
        )
        .padding()
    }
}
