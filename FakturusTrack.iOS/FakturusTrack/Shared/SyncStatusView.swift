import SwiftUI

enum SyncStatus {
    case idle
    case syncing
    case synced
    case pending(count: Int)
    case error(String)
}

struct SyncStatusView: View {
    let status: SyncStatus
    let onSync: () -> Void

    @State private var rotation: Double = 0
    @State private var showSynced = false

    var body: some View {
        Button(action: onSync) {
            switch status {
            case .idle:
                Image(systemName: "arrow.triangle.2.circlepath")
                    .foregroundStyle(.secondary)

            case .syncing:
                Image(systemName: "arrow.triangle.2.circlepath")
                    .rotationEffect(.degrees(rotation))
                    .onAppear {
                        rotation = 0
                        withAnimation(.linear(duration: 1).repeatForever(autoreverses: false)) {
                            rotation = 360
                        }
                    }
                    .onDisappear {
                        rotation = 0
                    }

            case .synced:
                Image(systemName: "checkmark.icloud")
                    .foregroundStyle(Theme.syncDone)

            case .pending(let count):
                ZStack {
                    Image(systemName: "icloud.and.arrow.up")
                        .foregroundStyle(Theme.syncPending)
                    if count > 0 {
                        Text("\(count)")
                            .font(.system(size: 8, weight: .bold))
                            .padding(2)
                            .background(.red)
                            .clipShape(Circle())
                            .foregroundStyle(.white)
                            .offset(x: 8, y: -8)
                    }
                }

            case .error:
                Image(systemName: "exclamationmark.icloud")
                    .foregroundStyle(Theme.danger)
            }
        }
    }
}

// MARK: - Preview

#Preview("All States") {
    HStack(spacing: 24) {
        SyncStatusView(status: .idle, onSync: {})
        SyncStatusView(status: .syncing, onSync: {})
        SyncStatusView(status: .synced, onSync: {})
        SyncStatusView(status: .pending(count: 3), onSync: {})
        SyncStatusView(status: .error("Test"), onSync: {})
    }
    .padding()
}
