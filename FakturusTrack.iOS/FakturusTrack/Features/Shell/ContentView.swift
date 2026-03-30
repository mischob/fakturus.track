import SwiftUI

struct ContentView: View {
    @Environment(AppState.self) private var appState
    @Environment(AuthManager.self) private var authManager
    @Environment(NetworkMonitor.self) private var networkMonitor
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    var body: some View {
        @Bindable var appState = appState

        VStack(spacing: 0) {
            OfflineBanner()
                .animation(reduceMotion ? nil : .spring(), value: networkMonitor.isConnected)

            TabView(selection: $appState.selectedTab) {
                TimeTrackingView()
                    .tabItem { Label(String(localized: "times_tab_title"), systemImage: "clock") }
                    .tag(0)

                VacationScreen()
                    .tabItem { Label(String(localized: "vacation_tab_title"), systemImage: "sun.max") }
                    .tag(1)

                OverviewScreen()
                    .tabItem { Label(String(localized: "overview_tab_title"), systemImage: "chart.bar") }
                    .tag(2)

                SettingsView()
                    .tabItem { Label(String(localized: "settings_tab_title"), systemImage: "gearshape") }
                    .tag(3)
            }
        }
    }
}
