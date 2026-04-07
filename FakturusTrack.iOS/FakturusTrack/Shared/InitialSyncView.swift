import SwiftUI

struct InitialSyncView: View {
    let syncEngine: SyncEngine?
    let onComplete: () -> Void
    let onSkip: () -> Void

    @State private var isSyncing = true
    @State private var error: String?

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            if isSyncing {
                ProgressView()
                    .scaleEffect(1.5)
                Text("Daten werden geladen...")
                    .font(.headline)
            } else if let error {
                Image(systemName: "exclamationmark.triangle")
                    .font(.system(size: 48))
                    .foregroundStyle(.orange)
                Text("Synchronisation fehlgeschlagen")
                    .font(.headline)
                Text(error)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)

                Button("Erneut versuchen") {
                    startSync()
                }
                .buttonStyle(.borderedProminent)

                Button("Ohne Daten fortfahren") {
                    onSkip()
                }
                .buttonStyle(.bordered)
            }

            Spacer()
        }
        .padding()
        .task {
            startSync()
        }
    }

    private func startSync() {
        isSyncing = true
        error = nil
        Task {
            let result = await withTaskGroup(of: Bool.self) { group in
                group.addTask {
                    await syncEngine?.syncAll()
                    return true
                }
                group.addTask {
                    try? await Task.sleep(for: .seconds(30))
                    return false
                }
                // First to finish wins
                return await group.next() ?? false
            }

            isSyncing = false
            if result {
                onComplete()
            } else {
                error = "Zeitueberschreitung. Bitte pruefen Sie Ihre Internetverbindung."
            }
        }
    }
}

// MARK: - Preview

#Preview("Loading") {
    InitialSyncView(
        syncEngine: nil,
        onComplete: {},
        onSkip: {}
    )
}
