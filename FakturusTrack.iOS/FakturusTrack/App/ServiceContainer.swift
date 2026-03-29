import Foundation
import Observation
import SwiftData

@Observable @MainActor
final class ServiceContainer {
    let appState = AppState()
    let authManager = AuthManager()
    let networkMonitor = NetworkMonitor()

    // Lazy-initialized after login
    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?

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
