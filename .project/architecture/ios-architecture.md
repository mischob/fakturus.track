# iOS-Architektur -- Fakturus Track

## Technologie-Stack

| Bereich | Technologie | Begruendung |
|---------|-------------|-------------|
| Sprache | Swift 6 | Strikte Concurrency, @Observable |
| UI | SwiftUI (iOS 17+) | Deklarativ, weniger Code als UIKit |
| Datenbank | SwiftData | Native SwiftUI-Integration, @Query |
| Auth | MSAL iOS | Bewaehrt in fakturus.poi |
| Netzwerk | URLSession + async/await | Kein Drittanbieter-Dependency noetig |
| Testing | Swift Testing + XCTest | Natives Framework |
| Package Manager | SPM | Standard fuer Swift |

## Projektstruktur

```
FakturusTrack/
  FakturusTrack.xcodeproj
  FakturusTrack/
    App/
      FakturusTrackApp.swift         -- @main Entry Point
      AppState.swift                 -- Globaler App-Zustand (@Observable)
      ServiceContainer.swift         -- Service-Initialisierung (Lektion aus POI)
      Configuration.swift            -- B2C-Config, API-URLs

    Services/
      Auth/
        AuthManager.swift            -- MSAL B2C (adaptiert von POI)
      API/
        APIClient.swift              -- HTTP Client (adaptiert von POI)
        APIError.swift               -- Fehler-Enum
      Sync/
        SyncEngine.swift             -- Orchestriert alle Syncs
      Network/
        NetworkMonitor.swift         -- NWPathMonitor Wrapper

    Features/
      TimeTracking/
        TimeTrackingView.swift       -- Tab-View (Active Session + History)
        TimeTrackingViewModel.swift  -- State + Logik
        ActiveSessionCard.swift      -- Laufende/Idle Session
        SessionRow.swift             -- Einzelne Session in History
        SessionDetailSheet.swift     -- Bearbeitung
        MonthGroup.swift             -- Auf-/zuklappbare Monatsgruppe

      Overtime/
        OvertimeView.swift           -- Tab-View
        OvertimeViewModel.swift      -- State + Logik

      Vacation/
        VacationView.swift           -- Tab-View
        VacationViewModel.swift      -- State + Logik
        VacationCalendar.swift       -- Custom Kalender-Komponente

      Settings/
        SettingsView.swift           -- Tab-View
        SettingsViewModel.swift      -- State + Logik

      Auth/
        LoginView.swift              -- Login Screen

    Models/
      WorkSession.swift              -- SwiftData @Model
      VacationDay.swift              -- SwiftData @Model
      UserSettings.swift             -- SwiftData @Model
      SchoolHolidayPeriod.swift      -- SwiftData @Model
      DTOs.swift                     -- API Response/Request Typen (eine Datei!)

    Shared/
      OfflineBanner.swift
      SyncStatusView.swift
      TimerDisplay.swift
      BottomTabBar.swift

    Extensions/
      Date+Formatting.swift          -- Deutsche Formatierung
      TimeInterval+Display.swift     -- Dauer-Anzeige

    Resources/
      Assets.xcassets
      Localizable.xcstrings

  FakturusTrackTests/
    TimeTrackingViewModelTests.swift
    SyncEngineTests.swift
    APIClientTests.swift
```

### Struktur-Entscheidungen

**Feature-basiert statt technisch-geschichtet**: Alles was zu "TimeTracking" gehoert liegt in einem Ordner. Ein AI-Agent muss nur diesen Ordner lesen um das Feature zu verstehen.

**DTOs in einer Datei**: Die API hat ~8 Endpunkte. Alle Request/Response-Typen passen in eine Datei. Keine 8 einzelnen DTO-Dateien.

**ServiceContainer statt App-Monster**: Anders als fakturus.poi (480 Zeilen App-Datei) lagern wir Service-Initialisierung aus.

**Keine Endpoints-Ordner**: APIClient macht direkte Calls. Keine eigene Datei pro Endpoint -- die Methoden leben im ViewModel oder als Extension auf APIClient.

---

## Kern-Patterns mit Code-Beispielen

### 1. App Entry Point (schlank gehalten)

```swift
@main
struct FakturusTrackApp: App {
    @State private var services = ServiceContainer()

    var body: some Scene {
        WindowGroup {
            if services.authManager.isAuthenticated {
                ContentView()
            } else {
                LoginView()
            }
            .environment(services.appState)
            .environment(services.authManager)
            .environment(services.syncEngine)
            .onChange(of: services.authManager.isAuthenticated) { _, isAuth in
                if isAuth {
                    services.onLogin()
                } else {
                    services.onLogout()
                }
            }
        }
        .modelContainer(for: [
            WorkSession.self,
            VacationDay.self,
            UserSettings.self,
            SchoolHolidayPeriod.self
        ])
    }
}
```

### 2. ServiceContainer (Lektion aus fakturus.poi)

```swift
@Observable
final class ServiceContainer {
    let authManager = AuthManager()
    let appState = AppState()

    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?
    private(set) var networkMonitor = NetworkMonitor()

    func onLogin() {
        let client = APIClient(authManager: authManager)
        apiClient = client
        syncEngine = SyncEngine(
            apiClient: client,
            networkMonitor: networkMonitor
        )
        appState.isAuthenticated = true

        // Initial sync
        Task { await syncEngine?.syncAll() }
    }

    func onLogout() {
        apiClient = nil
        syncEngine = nil
        appState.isAuthenticated = false
    }
}
```

### 3. AppState (minimal -- nur was global sichtbar sein muss)

```swift
@Observable
final class AppState {
    var isAuthenticated = false
    var isOnline = true
    var isSyncing = false
    var lastSyncDate: Date?
    var syncError: String?

    // Aktive Session (global sichtbar fuer Widget, Timer-Badge etc.)
    var activeSession: WorkSession?
}
```

### 4. ViewModel-Pattern (direkt und explizit)

```swift
@Observable
final class TimeTrackingViewModel {
    // MARK: - State
    var activeSession: WorkSession?
    var isLoading = false
    var error: String?
    var selectedSession: WorkSession?
    var expandedMonths: Set<String> = []

    // MARK: - Dependencies (direkt, keine Protocols)
    private let modelContext: ModelContext
    private let apiClient: APIClient
    private let syncEngine: SyncEngine

    init(modelContext: ModelContext, apiClient: APIClient, syncEngine: SyncEngine) {
        self.modelContext = modelContext
        self.apiClient = apiClient
        self.syncEngine = syncEngine
    }

    // MARK: - Actions

    func startSession() {
        let session = WorkSession(
            id: UUID(),
            date: Date(),
            startTime: Date(),
            isPendingSync: true,
            isSynced: false,
            isFinished: false
        )
        modelContext.insert(session)
        activeSession = session
    }

    func stopSession() {
        guard let session = activeSession else { return }
        session.stopTime = Date()
        try? modelContext.save()
    }

    func finishSession() {
        guard let session = activeSession else { return }
        session.isFinished = true
        session.isPendingSync = true
        try? modelContext.save()
        activeSession = nil

        // Trigger sync
        Task { await syncEngine.syncWorkSessions() }
    }

    func deleteSession(_ session: WorkSession) {
        modelContext.delete(session)
        try? modelContext.save()

        // If synced, also delete on server
        if session.isSynced {
            Task {
                try? await apiClient.delete("/v1/work-sessions/\(session.id)")
            }
        }
    }
}
```

### 5. View-Pattern (SwiftUI-nativ)

```swift
struct TimeTrackingView: View {
    @Environment(\.modelContext) private var modelContext
    @Environment(SyncEngine.self) private var syncEngine
    @Query(sort: \WorkSession.date, order: .reverse) private var sessions: [WorkSession]
    @State private var viewModel: TimeTrackingViewModel?

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 16) {
                    // Active Session oder Idle State
                    ActiveSessionCard(
                        session: viewModel?.activeSession,
                        onStart: { viewModel?.startSession() },
                        onStop: { viewModel?.stopSession() },
                        onFinish: { viewModel?.finishSession() }
                    )

                    // History nach Monaten gruppiert
                    ForEach(groupedByMonth, id: \.key) { month, sessions in
                        MonthGroup(
                            monthName: month,
                            sessions: sessions,
                            onDelete: { session in viewModel?.deleteSession(session) },
                            onSelect: { session in viewModel?.selectedSession = session }
                        )
                    }
                }
                .padding()
            }
            .navigationTitle("Zeiten")
            .refreshable {
                await syncEngine.syncAll()
            }
            .sheet(item: Binding(
                get: { viewModel?.selectedSession },
                set: { viewModel?.selectedSession = $0 }
            )) { session in
                SessionDetailSheet(session: session)
            }
        }
        .onAppear {
            if viewModel == nil {
                viewModel = TimeTrackingViewModel(
                    modelContext: modelContext,
                    apiClient: /* from environment */,
                    syncEngine: syncEngine
                )
            }
        }
    }

    private var groupedByMonth: [(key: String, value: [WorkSession])] {
        Dictionary(grouping: sessions) { session in
            session.date.formatted(.dateTime.month(.wide).year())
        }
        .sorted { $0.key > $1.key }
    }
}
```

### 6. ActiveSessionCard (Zustandsbasiert)

```swift
struct ActiveSessionCard: View {
    let session: WorkSession?
    let onStart: () -> Void
    let onStop: () -> Void
    let onFinish: () -> Void

    var body: some View {
        GroupBox {
            if let session {
                runningOrStoppedContent(session)
            } else {
                idleContent
            }
        }
    }

    @ViewBuilder
    private var idleContent: some View {
        VStack(spacing: 16) {
            Text("Bereit fuer den naechsten Eintrag")
                .font(.headline)
                .foregroundStyle(.secondary)

            Button(action: onStart) {
                Label("Starten", systemImage: "play.fill")
                    .font(.headline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
        }
        .padding()
    }

    @ViewBuilder
    private func runningOrStoppedContent(_ session: WorkSession) -> some View {
        VStack(spacing: 16) {
            // Status-Indikator
            HStack {
                Circle()
                    .fill(session.isRunning ? .green : .orange)
                    .frame(width: 8, height: 8)
                Text(session.isRunning ? "Laufende Sitzung" : "Gestoppt")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                Spacer()
            }

            // Timer
            if session.isRunning {
                TimerDisplay(startTime: session.startTime, size: .large)
            } else if let stop = session.stopTime {
                Text(session.duration.formatted())
                    .font(.system(.largeTitle, design: .monospaced))
                    .monospacedDigit()
            }

            // Zeiten
            HStack {
                VStack(alignment: .leading) {
                    Text("Start").font(.caption).foregroundStyle(.secondary)
                    Text(session.startTime.formatted(date: .omitted, time: .shortened))
                }
                Spacer()
                VStack(alignment: .trailing) {
                    Text("Ende").font(.caption).foregroundStyle(.secondary)
                    Text(session.stopTime?.formatted(date: .omitted, time: .shortened) ?? "--:--")
                }
            }

            // Buttons
            HStack(spacing: 12) {
                if session.isRunning {
                    Button("Stop", systemImage: "stop.fill", action: onStop)
                        .buttonStyle(.bordered)
                    Button("Fertig", systemImage: "checkmark.circle.fill", action: onFinish)
                        .buttonStyle(.borderedProminent)
                } else {
                    Button("Fertig", systemImage: "checkmark.circle.fill", action: onFinish)
                        .buttonStyle(.borderedProminent)
                        .frame(maxWidth: .infinity)
                }
            }
        }
        .padding()
    }
}
```

### 7. APIClient-Erweiterungen (keine eigenen Endpoint-Dateien)

```swift
// Direkt als Extension auf APIClient -- kein eigenes File pro Endpoint
extension APIClient {
    // Work Sessions
    func getWorkSessions() async throws -> [WorkSessionDTO] {
        try await get("/v1/work-sessions")
    }

    func syncWorkSessions(_ request: SyncWorkSessionsRequest) async throws -> [WorkSessionDTO] {
        try await post("/v1/work-sessions/sync", body: request)
    }

    // Vacation Days
    func getVacationDays() async throws -> [VacationDayDTO] {
        try await get("/v1/vacation-days")
    }

    func syncVacationDays(_ request: SyncVacationDaysRequest) async throws -> SyncVacationDaysResponse {
        try await post("/v1/vacation-days/sync", body: request)
    }

    // Settings
    func getUserSettings() async throws -> UserSettingsDTO {
        try await get("/v1/settings")
    }

    func updateUserSettings(_ settings: UserSettingsDTO) async throws {
        try await putNoResponse("/v1/settings", body: settings)
    }

    // Overtime
    func getOvertimeSummary(year: Int) async throws -> OvertimeSummaryDTO {
        try await get("/v1/overtime-summary", queryItems: [
            URLQueryItem(name: "year", value: String(year))
        ])
    }
}
```

---

## State Management Konzept

### Drei Ebenen von State

| Ebene | Mechanismus | Beispiel |
|-------|------------|---------|
| **Global** | `AppState` via `@Environment` | isAuthenticated, isOnline, activeSession |
| **Feature** | `@State` ViewModel im View | expandedMonths, selectedSession, isLoading |
| **Persistiert** | SwiftData `@Query` | WorkSessions, VacationDays, Settings |

### Kein Combine, kein Redux

SwiftUI + @Observable + SwiftData deckt alle State-Beduerfnisse ab:
- `@Observable` ViewModel fuer transienten UI-State
- `@Query` fuer persistierte Daten (automatische UI-Updates bei DB-Aenderungen)
- `@Environment` fuer globale Services und State

---

## SPM Dependencies

```swift
// Package.swift Dependencies
dependencies: [
    // MSAL fuer Azure B2C Auth
    .package(url: "https://github.com/AzureAD/microsoft-authentication-library-for-objc", from: "1.3.0"),
]
```

**Bewusst minimale Dependencies:**
- Kein Alamofire (URLSession reicht)
- Kein SwiftyJSON (Codable reicht)
- Kein SnapKit (SwiftUI-native Layouts)
- Kein Realm/GRDB (SwiftData reicht)
- Nur MSAL als externe Dependency

---

## Testing-Strategie

### Was testen?

| Bereich | Test-Typ | Prioritaet |
|---------|----------|-----------|
| ViewModels | Unit Tests | Hoch |
| SyncEngine | Unit Tests mit Mock-APIClient | Hoch |
| APIClient | Integration Tests (optionaler Live-Server) | Mittel |
| Views | Snapshot/Preview Tests | Niedrig |

### ViewModel-Tests (Beispiel)

```swift
@Test func startSession_createsNewSession() async {
    // Arrange
    let container = try ModelContainer(for: WorkSession.self, configurations: .init(isStoredInMemoryOnly: true))
    let context = ModelContext(container)
    let vm = TimeTrackingViewModel(
        modelContext: context,
        apiClient: MockAPIClient(),
        syncEngine: MockSyncEngine()
    )

    // Act
    vm.startSession()

    // Assert
    #expect(vm.activeSession != nil)
    #expect(vm.activeSession?.isFinished == false)
    #expect(vm.activeSession?.isPendingSync == true)
}
```

### Mock-Strategie

Kein Interface-Overhead fuer Testbarkeit. Stattdessen:
- **APIClient**: `MockAPIClient` Subclass (oder Protocol nur hier)
- **ModelContext**: In-Memory SwiftData Container
- **SyncEngine**: `MockSyncEngine` die nichts tut
- **AuthManager**: `MockAuthManager` (bereits in fakturus.poi vorhanden)

---

## Xcode-Projekt Konfiguration

### Targets

| Target | Typ | Minimum iOS |
|--------|-----|-------------|
| FakturusTrack | App | iOS 17.0 |
| FakturusTrackTests | Unit Tests | iOS 17.0 |
| FakturusTrackWidget | Widget Extension | iOS 17.0 (Phase 3) |
| FakturusTrackWatch | watchOS App | watchOS 10.0 (Phase 3) |

### Signing & Capabilities

- Bundle ID: `com.fakturus.track`
- Team: Fakturus
- Capabilities: Keychain Sharing, Background Modes (fetch, remote-notifications)
- Keychain Access Group: `com.fakturus.track`

### Build Configurations

| Config | API Base URL | Logging |
|--------|-------------|---------|
| Debug | `https://localhost:7001` | Verbose |
| Release | `https://api.track.fakturus.com` | Error only |
