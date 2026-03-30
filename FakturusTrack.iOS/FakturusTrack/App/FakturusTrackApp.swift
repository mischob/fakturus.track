import SwiftUI
import SwiftData

@main
struct FakturusTrackApp: App {
    @State private var appState = AppState()
    @State private var authManager = AuthManager()
    @State private var services = ServiceContainer()
    @Environment(\.scenePhase) private var scenePhase
    @AppStorage("appearance") private var appearance = "system"

    @State private var syncTimer: Timer?
    @State private var hasCompletedInitialSync = false

    var body: some Scene {
        WindowGroup {
            Group {
                if authManager.isAuthenticated {
                    if hasCompletedInitialSync {
                        ContentView()
                    } else {
                        InitialSyncView(
                            syncEngine: services.syncEngine,
                            onComplete: {
                                hasCompletedInitialSync = true
                            },
                            onSkip: {
                                hasCompletedInitialSync = true
                            }
                        )
                    }
                } else {
                    LoginView()
                }
            }
            .preferredColorScheme(colorSchemeFor(appearance))
            .environment(appState)
            .environment(authManager)
            .environment(services.networkMonitor)
            .environment(services)
            .onChange(of: authManager.isAuthenticated) { _, isAuthenticated in
                if !isAuthenticated {
                    // Reset initial sync state on logout
                    hasCompletedInitialSync = false
                }
            }
        }
        .modelContainer(PersistenceManager.container)
        .onChange(of: scenePhase) { _, newPhase in
            switch newPhase {
            case .active:
                startSyncTimer()
                Task { await services.syncEngine?.syncAll() }
            case .background:
                stopSyncTimer()
            default:
                break
            }
        }
    }

    // MARK: - Sync Timer

    private func startSyncTimer() {
        stopSyncTimer()
        syncTimer = Timer.scheduledTimer(withTimeInterval: 30, repeats: true) { _ in
            Task { await services.syncEngine?.syncAll() }
        }
    }

    private func stopSyncTimer() {
        syncTimer?.invalidate()
        syncTimer = nil
    }

    // MARK: - Appearance

    private func colorSchemeFor(_ appearance: String) -> ColorScheme? {
        switch appearance {
        case "light": return .light
        case "dark": return .dark
        default: return nil
        }
    }
}
