import SwiftUI

struct ContentView: View {
    @Environment(AppState.self) private var appState
    @Environment(AuthManager.self) private var authManager
    @Environment(NetworkMonitor.self) private var networkMonitor

    var body: some View {
        @Bindable var appState = appState

        VStack(spacing: 0) {
            OfflineBanner()
                .animation(.spring(), value: networkMonitor.isConnected)

            TabView(selection: $appState.selectedTab) {
                TimeTrackingView()
                    .tabItem { Label("Zeiten", systemImage: "clock") }
                    .tag(0)

                VacationScreen()
                    .tabItem { Label("Urlaub", systemImage: "sun.max") }
                    .tag(1)

                OverviewScreen()
                    .tabItem { Label("Gesamt", systemImage: "chart.bar") }
                    .tag(2)

                SettingsView()
                    .tabItem { Label("Einstellungen", systemImage: "gearshape") }
                    .tag(3)
            }
        }
    }
}
