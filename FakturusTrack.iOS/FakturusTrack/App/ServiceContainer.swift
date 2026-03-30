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

    // Lazy-initialized after login
    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?

    init() {
        // StoreKit bei App-Start initialisieren (NICHT erst bei Login!)
        // Grund: Abo-Status und Produkte muessen sofort verfuegbar sein,
        // auch bevor der User eingeloggt ist (z.B. Paywall in Onboarding).
        Task {
            await storeKitManager.configure(subscriptionManager: subscriptionManager)
        }
    }

    func onLogin() {
        appState.isAuthenticated = true

        // Initialize APIClient
        let client = APIClient(authManager: authManager)
        apiClient = client

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
    }
}
