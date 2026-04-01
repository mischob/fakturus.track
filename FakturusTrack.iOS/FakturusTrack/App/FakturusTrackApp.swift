import SwiftUI
import SwiftData

@main
struct FakturusTrackApp: App {
    @State private var services = ServiceContainer()
    @Environment(\.scenePhase) private var scenePhase
    @AppStorage("appearance") private var appearance = "system"

    @State private var syncTimer: Timer?

    var body: some Scene {
        WindowGroup {
            Group {
                if services.authManager.isAuthenticated {
                    if services.consentManager.hasRequiredConsents {
                        ContentView()
                    } else {
                        ConsentView(
                            onConsented: {
                                services.consentManager.recordConsent(termsVersion: 1)
                            },
                            onDeclined: {
                                services.authManager.signOut()
                            }
                        )
                    }
                } else {
                    LoginView()
                }
            }
            .preferredColorScheme(colorSchemeFor(appearance))
            .environment(services.appState)
            .environment(services.authManager)
            .environment(services.networkMonitor)
            .environment(services)
            .environment(services.subscriptionManager)
            .environment(services.storeKitManager)
            .task {
                await services.storeKitManager.listenForTransactions()
            }
            .onChange(of: services.authManager.isAuthenticated) { _, isAuthenticated in
                if isAuthenticated {
                    print("[App] Login detected, initializing services...")
                    services.onLogin()
                    Task { await services.syncEngine?.syncAll() }
                } else {
                    print("[App] Logout detected, tearing down services...")
                    services.onLogout()
                }
            }
        }
        .modelContainer(PersistenceManager.container)
        .onChange(of: scenePhase) { _, newPhase in
            switch newPhase {
            case .active:
                startSyncTimer()
                if services.authManager.isAuthenticated {
                    Task { await services.syncEngine?.syncAll() }
                }
            case .background:
                stopSyncTimer()
            default:
                break
            }
        }
    }

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

    private func colorSchemeFor(_ appearance: String) -> ColorScheme? {
        switch appearance {
        case "light": return .light
        case "dark": return .dark
        default: return nil
        }
    }
}
