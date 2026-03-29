# Guide fuer parallele Implementierung -- Phase 1

## Uebersicht

Phase 1 wird von mehreren AI-Agenten parallel umgesetzt (mindestens iOS-Agent + Android-Agent). Dieser Guide definiert die Schnittstellen, Mock-Strategien und Merge-Reihenfolge, damit parallel entwickelte Stories sauber zusammenarbeiten.

---

## 1. Contracts die ZUERST definiert werden muessen

Bevor parallele Stories starten, muessen folgende "Contracts" feststehen. Diese werden in **Welle 1 (E01)** definiert und sind die Basis fuer alles Weitere.

### 1.1 Datenmodell-Contract (E03)

Die Feldnamen und Typen der Entities muessen zwischen iOS und Android identisch sein (semantisch, nicht syntaktisch). Quelle der Wahrheit: `data-layer.md`.

```
WorkSession:
  id: UUID/String
  userId: String
  date: Date/String (ISO "2026-03-29")
  startTime: Date/String (ISO "2026-03-29T08:00:00Z")
  stopTime: Date?/String? (nullable)
  pauseMinutes: Int
  calendarEventId: String? (lokal, nicht gesynct)
  createdAt, updatedAt, syncedAt: Date/String
  isPendingSync, isSynced, isFinished: Bool/Boolean

VacationDay:
  id, userId, date, createdAt, updatedAt, syncedAt, isPendingSync, isSynced

UserSettings:
  userId, calendarUrl?, vacationDaysPerYear, workHoursPerWeek, workDays, bundesland
```

### 1.2 DTO-Contract (E03-S03/S04)

Die DTOs muessen exakt dem Backend-API-Format entsprechen. Quelle: `data-layer.md` Abschnitt "DTOs" und `shared-concepts.md`.

**Wichtig:** PascalCase-Feldnamen im JSON (`"StartTime"`, `"PauseMinutes"`).

### 1.3 API-Endpoint-Contract (E04)

Basis-URL, Pfade und HTTP-Methoden sind fix (siehe `tech-epic-04.md`):

| Methode | Pfad | Funktion |
|---------|------|----------|
| GET | `/v1/work-sessions` | Alle Sessions laden |
| POST | `/v1/work-sessions/sync` | Pending hochladen + alle zurueck |
| DELETE | `/v1/work-sessions/{id}` | Session loeschen |
| POST | `/v1/vacation-days/sync` | Alle Tage senden + Merge |
| GET | `/v1/settings` | Settings laden |
| PUT | `/v1/settings` | Settings speichern |

### 1.4 ViewModel-Interface-Contract (E05)

Beide Plattformen implementieren die gleichen Methoden im ViewModel:

```
TimeTrackingViewModel:
  Properties:
    - activeSession (nullable)
    - isPaused
    - isLoading
    - error (nullable String)

  Methoden:
    - startSession()
    - stopSession()
    - finishSession()
    - pauseSession()
    - resumeSession()
    - updateSession(date, startTime, stopTime, pauseMinutes)
    - deleteSession(session)
```

### 1.5 Theme/Farben-Contract (E01-S03/S04)

Beide Plattformen definieren die gleichen Named Colors:

```
Primary (Fakturus-Blau), Secondary, Background, Surface
Error, Warning, Success
timer-running (Gruen), timer-paused (Gelb/Orange), timer-stopped (Orange)
pause (eigene Farbe)
sync-pending (Gelb), sync-done (Gruen)
offline-banner (Gelb/Orange)
```

---

## 2. Mock-Strategien

### 2.1 UI-Stories ohne fertige Datenbank

Stories in Welle 3 (Timer-UI, History) koennen mit Mock-Daten entwickelt werden, wenn die DB noch nicht fertig ist.

**iOS: SwiftUI Preview Mocks**

```swift
#Preview {
    let mockSession = WorkSession(
        date: Date(),
        startTime: Calendar.current.date(bySettingHour: 8, minute: 30, second: 0, of: Date())!,
        stopTime: Calendar.current.date(bySettingHour: 17, minute: 0, second: 0, of: Date())!,
        pauseMinutes: 30,
        isSynced: true,
        isFinished: true
    )

    SessionRow(
        session: mockSession,
        onTap: {},
        onDelete: {}
    )
}
```

**Android: Preview Mocks**

```kotlin
@Preview
@Composable
fun SessionRowPreview() {
    val mockSession = WorkSessionEntity(
        date = "2026-03-29",
        startTime = "2026-03-29T08:30:00Z",
        stopTime = "2026-03-29T17:00:00Z",
        pauseMinutes = 30,
        isSynced = true,
        isFinished = true
    )
    FakturusTrackTheme {
        SessionRow(session = mockSession, onClick = {}, onDelete = {})
    }
}
```

### 2.2 ViewModel-Stories ohne fertige DB

Das ViewModel kann mit einem In-Memory ModelContext (iOS) oder einer In-Memory Room-DB (Android) entwickelt und getestet werden:

**iOS:**
```swift
let container = try ModelContainer(
    for: WorkSession.self,
    configurations: .init(isStoredInMemoryOnly: true)
)
let vm = TimeTrackingViewModel(modelContext: ModelContext(container))
```

**Android:**
```kotlin
val db = Room.inMemoryDatabaseBuilder(context, AppDatabase::class.java).build()
val vm = TimeTrackingViewModel(database = db)
```

### 2.3 Sync-Stories ohne fertigen APIClient

Die SyncEngine kann mit einem Mock-APIClient entwickelt werden:

**iOS:**
```swift
class MockAPIClient: APIClient {
    var mockSessions: [WorkSessionDTO] = []

    override func getWorkSessions() async throws -> [WorkSessionDTO] {
        return mockSessions
    }

    override func syncWorkSessions(_ request: SyncWorkSessionsRequest) async throws -> [WorkSessionDTO] {
        return mockSessions
    }
}
```

**Android:**
```kotlin
class MockAPIClient : APIClient(baseUrl = "", authManager = MockAuthManager()) {
    var mockSessions = listOf<WorkSessionDTO>()

    override suspend fun getWorkSessions(): List<WorkSessionDTO> = mockSessions
    override suspend fun syncWorkSessions(req: SyncWorkSessionsRequest): List<WorkSessionDTO> = mockSessions
}
```

**Hinweis:** Da wir keine Interfaces nutzen (ADR-006), sind die Mocks Subclasses. Das funktioniert fuer Tests, ist aber bewusst begrenzt -- keine Interface-Hierarchie fuer Testbarkeit.

### 2.4 UI-Stories ohne fertigen SyncEngine

Der Zeiten-Tab (E06-S07/S08) braucht keinen SyncEngine:
- Pull-to-Refresh: `.refreshable {}` mit leerer Closure
- Sync-Button: Placeholder Icon ohne Aktion
- Sync-Status: Nicht anzeigen bis E10

### 2.5 Auth ohne Azure Portal

Auth-Stories (E02) koennen ohne konfigurierte Redirect-URIs begonnen werden:
- MSAL-Konfiguration implementieren
- Login-Screen UI komplett bauen
- Erst fuer den **echten Login-Test** werden Redirect-URIs gebraucht (E02-S05)

---

## 3. Integrationspunkte

Hier treffen parallel entwickelte Teile aufeinander und muessen zusammengefuehrt werden:

### 3.1 ViewModel + SyncEngine (E07-S05)

**Wann:** Nachdem E05 (ViewModel) und E07 (SyncEngine) beide fertig sind.

**Was:** ViewModel erhaelt SyncEngine als Dependency.
- `finishSession()` triggert `syncEngine.syncAll()` am Ende
- `deleteSession()` ruft `apiClient.deleteWorkSession()` auf wenn online
- Pull-to-Refresh ruft `syncEngine.syncAll()` auf

**Wie integrieren:**
1. ViewModel-Konstruktor erweitern: `init(modelContext:, syncEngine:)`
2. `finishSession()` um Sync-Call ergaenzen
3. `deleteSession()` um API-Delete ergaenzen

### 3.2 App-Shell + Feature-Screens (E09 + E06)

**Wann:** Nachdem E06-S07/S08 (Zeiten-Screen) und E09 (App-Shell) beide fertig sind.

**Was:** ContentView/MainScreen bindet TimeTrackingView/Screen in Tab 0 ein.

**Wie integrieren:**
- iOS: `TimeTrackingView()` als Content von Tab 0 in ContentView
- Android: `TimeTrackingScreen(services)` in NavHost Route "zeiten"
- Wenig Konfliktpotential da klare Schnittstelle (keine Props, Environment/ServiceContainer)

### 3.3 OfflineBanner + ContentView/MainScreen (E10-S01/S02 + E09)

**Wann:** Nachdem E09 (App-Shell) und E10-S01/S02 (OfflineBanner) fertig sind.

**Was:** OfflineBanner ueber allen Tabs einbetten.

**Wie integrieren:**
- iOS: `VStack { OfflineBanner(); TabView {...} }` in ContentView
- Android: `Column { OfflineBanner(); AppNavigation() }` in MainScreen

### 3.4 SyncStatusView + Zeiten-Tab (E10-S03/S04 + E06-S07/S08)

**Wann:** Nachdem E06 (Zeiten-Screen) und E10-S03/S04 (SyncStatus) fertig sind.

**Was:** Sync-Status Icon in der Toolbar des Zeiten-Tabs.

**Wie integrieren:**
- iOS: `.toolbar { ToolbarItem(.topBarTrailing) { SyncStatusView(...) } }`
- Android: TopAppBar `actions = { SyncStatusIndicator(...) }`

### 3.5 Pause-UI + ActiveSessionCard (E08-S03/S04 + E05-S03/S04)

**Wann:** Nachdem E08-S01/S02 (ViewModel-Erweiterung) fertig ist.

**Was:** Paused-State in ActiveSessionCard, Pause-Button aktivieren.

**Wie integrieren:**
- Neuen `isPaused`-Branch in der Card-Logik
- Pause-Button `.disabled(true)` entfernen
- Neuen `pausedContent()` ViewBuilder hinzufuegen

---

## 4. Merge-Reihenfolge

Die Reihenfolge in der Feature-Branches in den Entwicklungs-Branch gemergt werden sollen:

### Phase A: Foundation (keine Konflikte erwartet)

```
1. E01-S01 (iOS Setup)     \
2. E01-S02 (Android Setup)  > Parallel, getrennte Dateien
3. E01-S03 (iOS Theme)      |
4. E01-S04 (Android Theme)  /
```

### Phase B: Infrastruktur (leichte Konflikte moeglich)

```
5. E03-S01 (iOS DB) + E03-S03 (iOS DTOs)      -- Models muessen vor ViewModels gemergt werden
6. E03-S02 (Android DB) + E03-S04 (Android DTOs)
7. E02-S01 (iOS Auth)                           -- unabhaengig von DB
8. E02-S02 (Android Auth)
9. E09-S01 (iOS Shell)                          -- braucht Auth fuer Login-Check
10. E09-S02 (Android Shell)
11. E02-S03 (iOS Login) + E02-S04 (Android Login)
```

### Phase C: Features (abhaengig von B)

```
12. E05-S01 (iOS Timer)  + E05-S02 (Android Timer)    -- nur Theme-Dependency
13. E05-S05 (iOS VM)     + E05-S06 (Android VM)       -- braucht DB
14. E05-S03 (iOS Card)   + E05-S04 (Android Card)     -- braucht Timer + VM
15. E06-S01/S03/S05 (iOS History-Komponenten)
16. E06-S02/S04/S06 (Android History-Komponenten)
17. E06-S07 (iOS Zusammenbau)                          -- ALLE vorherigen iOS-Stories
18. E06-S08 (Android Zusammenbau)                      -- ALLE vorherigen Android-Stories
```

### Phase D: Backend-Integration (abhaengig von Auth + DB)

```
19. E04-S03 (iOS NetworkMonitor)  + E04-S04 (Android)  -- keine Konflikte
20. E04-S01 (iOS APIClient)       + E04-S02 (Android)  -- braucht Auth
21. E07-S01 (iOS SyncEngine)      + E07-S02 (Android)  -- braucht API + DB
22. E07-S03 (iOS Sync-Trigger)    + E07-S04 (Android)
23. E07-S05 (Sync in ViewModel)                         -- MODIFIZIERT bestehende ViewModels!
```

**Achtung bei Step 23:** E07-S05 aendert `TimeTrackingViewModel.swift/kt`. Wenn Pausen-Stories (E08) parallel laufen, gibt es Merge-Konflikte im ViewModel.

### Phase E: Erweiterungen (abhaengig von C + D)

```
24. E08-S01 (iOS Pause-VM)   + E08-S02 (Android)  -- MODIFIZIERT ViewModel
25. E08-S03 (iOS Pause-UI)   + E08-S04 (Android)  -- MODIFIZIERT ActiveSessionCard
26. E08-S05 (iOS ArbZG)      + E08-S06 (Android)  -- neue Dateien, kein Konflikt
27. E08-S07 (Pause in History)                     -- MODIFIZIERT SessionRow, MonthGroup
```

### Phase F: Polish (abhaengig von D)

```
28. E10-S01 (iOS Banner)     + E10-S02 (Android)  -- neue Dateien
29. E10-S03 (iOS SyncStatus) + E10-S04 (Android)  -- neue Dateien
30. E10-S05 (InitialSync)                          -- MODIFIZIERT App Entry Point
31. E10-S06 (Error Polish)                         -- MODIFIZIERT diverse Dateien
```

---

## 5. Konfliktvermeidung

### Regel 1: Eine Datei = Ein Verantwortlicher

Wenn mehrere Stories die gleiche Datei aendern, bestimme vorher wer zuerst mergt. Kritische Dateien:

| Datei | Aendernde Stories | Merge-Reihenfolge |
|-------|-------------------|-------------------|
| `TimeTrackingViewModel.swift/kt` | E05-S05, E07-S05, E08-S01 | E05 -> E07 -> E08 |
| `ActiveSessionCard.swift/kt` | E05-S03, E08-S03 | E05 -> E08 |
| `ServiceContainer.swift/kt` | E01-S01, E02, E04, E07 | E01 -> E04 -> E07 |
| `FakturusTrackApp.swift` | E01-S01, E02, E07-S03, E09, E10-S05 | E01 -> E02/E09 -> E07 -> E10 |

### Regel 2: Feature-Branches kurz halten

Jede Story = ein Branch. Nicht mehrere Stories in einem Branch sammeln. Kuerzere Branches = weniger Merge-Konflikte.

### Regel 3: Placeholder-Pattern

Wenn eine Story eine Datei erstellt die spaeter erweitert wird:
- Explizite `// TODO: E08` oder `// Placeholder fuer E08` Kommentare setzen
- Placeholder-Parameter mit Default-Werten (`isPaused: Bool = false`)
- Disabled Buttons statt fehlender Buttons (`Button("Pause").disabled(true)`)

### Regel 4: iOS und Android parallel = kein Konflikt

iOS und Android aendern NIEMALS die gleichen Dateien. Sie koennen immer parallel und in beliebiger Reihenfolge gemergt werden.
