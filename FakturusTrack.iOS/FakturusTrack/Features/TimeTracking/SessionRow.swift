import SwiftUI

struct SessionRow: View {
    let session: WorkSession
    let onTap: () -> Void
    let onDelete: () -> Void

    var body: some View {
        Button(action: onTap) {
            HStack(spacing: 12) {
                syncIcon
                    .font(.caption)
                    .frame(width: 20)

                VStack(alignment: .leading, spacing: 2) {
                    Text(session.date.weekdayShort + " " + session.date.formatted(as: "dd.MM."))
                        .font(.subheadline)
                    Text("\(session.startTime.timeShort) - \(session.stopTime?.timeShort ?? "--:--")")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                if session.pauseMinutes > 0 {
                    Text("P\(session.pauseMinutes)")
                        .font(.caption2)
                        .foregroundStyle(Theme.pause)
                        .padding(.horizontal, 4)
                        .background(
                            Theme.pause.opacity(0.1),
                            in: RoundedRectangle(cornerRadius: 4)
                        )
                }

                Text(session.netDuration.formattedHHMM)
                    .font(.subheadline.bold())
                    .monospacedDigit()
            }
        }
        .buttonStyle(.plain)
        .accessibilityElement(children: .combine)
        .accessibilityLabel(accessibilityDescription)
        .accessibilityHint(String(localized: "a11y_session_detail_hint"))
        .transition(.asymmetric(
            insertion: .move(edge: .top).combined(with: .opacity),
            removal: .move(edge: .trailing).combined(with: .opacity)
        ))
        .swipeActions(edge: .trailing) {
            Button(role: .destructive) {
                onDelete()
            } label: {
                Label(String(localized: "times_session_delete"), systemImage: "trash")
            }
        }
    }

    // MARK: - Accessibility

    private var accessibilityDescription: String {
        let weekday = session.date.weekdayDateShort
        let start = session.startTime.timeShort
        let end = session.stopTime?.timeShort ?? "--:--"
        let net = session.netDuration.formattedHHMM
        let syncState = session.isSynced
            ? String(localized: "a11y_sync_done")
            : String(localized: "a11y_sync_pending")
        return "\(weekday), \(start) bis \(end), \(net) Nettoarbeitszeit, \(syncState)"
    }

    @ViewBuilder
    private var syncIcon: some View {
        if session.isSynced {
            Image(systemName: "cloud.fill")
                .foregroundStyle(Theme.syncDone)
                .accessibilityLabel(String(localized: "a11y_sync_done"))
        } else if session.isPendingSync {
            Image(systemName: "icloud.and.arrow.up")
                .foregroundStyle(Theme.syncPending)
                .accessibilityLabel(String(localized: "a11y_sync_pending"))
        }
    }
}

// MARK: - Preview

#Preview {
    List {
        SessionRow(
            session: WorkSession(
                date: Date(),
                startTime: Calendar.current.date(bySettingHour: 8, minute: 0, second: 0, of: Date())!,
                stopTime: Calendar.current.date(bySettingHour: 17, minute: 0, second: 0, of: Date())!,
                pauseMinutes: 30,
                isPendingSync: false,
                isSynced: true,
                isFinished: true
            ),
            onTap: {},
            onDelete: {}
        )

        SessionRow(
            session: WorkSession(
                date: Date().addingTimeInterval(-86400),
                startTime: Calendar.current.date(bySettingHour: 9, minute: 15, second: 0, of: Date().addingTimeInterval(-86400))!,
                stopTime: Calendar.current.date(bySettingHour: 16, minute: 45, second: 0, of: Date().addingTimeInterval(-86400))!,
                pauseMinutes: 0,
                isPendingSync: true,
                isSynced: false,
                isFinished: true
            ),
            onTap: {},
            onDelete: {}
        )
    }
}
