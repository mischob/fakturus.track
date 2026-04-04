import Foundation
import Observation
import SwiftData

@Observable @MainActor
final class ServiceContainer {
    let appState = AppState()
    let authManager = AuthManager()
    let networkMonitor = NetworkMonitor()

    // Phase 4: Subscription (initialisiert bei App-Start, NICHT bei Login)
    let subscriptionManager = SubscriptionManager()
    let storeKitManager = StoreKitManager()
    let consentManager = ConsentManager()

    // Lazy-initialized after login
    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?

    // Offline-Login State
    var loginContext: LoginContext = .normal
    var isResolvingStartState = true

    init() {
        // StoreKit bei App-Start initialisieren (NICHT erst bei Login!)
        // Grund: Abo-Status und Produkte muessen sofort verfuegbar sein,
        // auch bevor der User eingeloggt ist (z.B. Paywall in Onboarding).
        Task {
            await storeKitManager.configure(subscriptionManager: subscriptionManager)
        }

        // Hintergrund-Token-Refresh bei Netzwerkwechsel
        networkMonitor.setOnBecameOnline { [weak self] in
            guard let self else { return }
            Task { @MainActor in
                await self.authManager.attemptBackgroundTokenRefresh()
                // Wenn Refresh erfolgreich und Sync-Engine vorhanden, synchronisieren
                if !self.authManager.isOfflineMode, let engine = self.syncEngine {
                    await engine.syncAll()
                }
            }
        }
    }

    func resolveStartState() async {
        isResolvingStartState = true
        let (result, context) = await authManager.resolveStartState(networkMonitor: networkMonitor)
        loginContext = context

        switch result {
        case .authenticated:
            onLogin()
            Task { await syncEngine?.syncAll() }
        case .offlineWithSession:
            onLogin()
        case .loginRequired, .loginRequiredNoNetwork:
            break
        }
        isResolvingStartState = false
    }

    func onLogin() {
        appState.isAuthenticated = true

        // Initialize APIClient
        let client = APIClient(authManager: authManager)
        apiClient = client

        // Configure ConsentManager
        consentManager.configure(apiClient: client)
        consentManager.checkConsent()
        Task { await consentManager.syncPendingConsent() }
        Task { await consentManager.checkForVersionUpdates() }

        // Initialize SyncEngine
        let engine = SyncEngine(modelContainer: PersistenceManager.container)
        Task {
            await engine.configure(apiClient: client, networkMonitor: networkMonitor)
        }
        syncEngine = engine
    }

    func onLogout() {
        appState.isAuthenticated = false

        // Tear down
        syncEngine = nil
        apiClient = nil
        consentManager.configure(apiClient: nil)
    }
}
