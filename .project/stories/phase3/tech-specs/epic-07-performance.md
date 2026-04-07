# Tech-Spec: EPIC 07 -- Performance-Optimierung

## Uebersicht

Keine neuen Dateien. Profiling mit nativen Tools, dann gezielte Optimierungen in bestehenden Dateien. Hauptfokus: Cold Start, Scroll-Performance, Memory.

---

## S01/S02: Profiling

### iOS: Instruments-Workflow

1. **App Launch** (Cold Start):
   - Scheme auf Release setzen (Debug ist verfaelscht)
   - Instruments > App Launch Template
   - Messen: Time-to-First-Frame, Time-to-Interactive
   - Ziel: < 1.0s

2. **Core Animation** (Scroll):
   - History-Liste mit 100+ Sessions scrollen
   - Kalender-Monatswechsel
   - Ziel: 60fps konstant, keine Frame-Drops

3. **Time Profiler** (Main Thread):
   - Waehrend Sync: Main Thread < 16ms pro Frame
   - Bei Tab-Wechsel: Kein Blocking

4. **Leaks + Allocations** (Memory):
   - 10 Minuten Nutzung, alle Tabs durchgehen
   - Timer starten/stoppen, Sessions erstellen/loeschen
   - Ziel: 0 Leaks, < 50MB Peak

### Android: Profiler-Workflow

1. **Android Studio Profiler > CPU** (Cold Start):
   - App killen, cold start
   - Messen: Startup Tracing
   - Ziel: < 1.0s

2. **Compose Compiler Metrics**:
   ```kotlin
   // In app/build.gradle.kts:
   composeCompiler {
       metricsDestination = layout.buildDirectory.dir("compose-metrics")
       reportsDestination = layout.buildDirectory.dir("compose-reports")
   }
   ```
   - Restartable/Skippable Analyse
   - Instabile Parameter identifizieren

3. **Layout Inspector** (Recomposition):
   - Recomposition Counter fuer alle Composables
   - Ziel: Kein exzessives Recomposition (>10x pro Sekunde)

4. **Memory Profiler**:
   - Heap Dump nach 10 Min Nutzung
   - Ziel: < 80MB, keine Leaks

---

## S03: iOS Performance-Optimierung

### Cold Start Optimierung

```swift
// ServiceContainer.swift -- Lazy Init
// VORHER: Alles in init()
// NACHHER: Nur das Noetigste beim Start

@Observable @MainActor
final class ServiceContainer {
    let authManager = AuthManager()         // Sofort (Auth-Check noetig)
    let networkMonitor = NetworkMonitor()    // Sofort (Offline-Banner)

    // Diese werden erst bei Login erstellt -- kein Startup-Cost
    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?
}
```

### History-Scrolling: Lazy Loading mit Pagination

```swift
// TimeTrackingView.swift -- Nur sichtbare Monate laden
@Query(
    filter: #Predicate<WorkSession> { $0.isFinished },
    sort: \WorkSession.date,
    order: .reverse
) private var allSessions: [WorkSession]

// Pagination: Nur erste 50 anzeigen, bei Scroll mehr laden
@State private var visibleCount = 50

var body: some View {
    LazyVStack {
        ForEach(Array(allSessions.prefix(visibleCount)), id: \.id) { session in
            SessionRow(session: session)
                .onAppear {
                    if session.id == allSessions.prefix(visibleCount).last?.id {
                        visibleCount += 50
                    }
                }
        }
    }
}
```

### SwiftData Background Queries

```swift
// Fuer schwere Operationen (Export, Aggregation):
// NICHT auf Main Thread!
func calculateTodayTotal() async -> Int {
    let context = ModelContext(PersistenceManager.container)
    let today = Calendar.current.startOfDay(for: Date())
    let tomorrow = Calendar.current.date(byAdding: .day, value: 1, to: today)!

    let descriptor = FetchDescriptor<WorkSession>(
        predicate: #Predicate { $0.date >= today && $0.date < tomorrow && $0.isFinished }
    )
    let sessions = (try? context.fetch(descriptor)) ?? []
    return sessions.reduce(0) { $0 + $1.netDurationMinutes }
}
```

### Timer: Kein Main Thread Blocking

```swift
// TimerDisplay.swift -- TimelineView statt Timer.publish
// TimelineView ist bereits in Verwendung (aus Phase 1).
// Sicherstellen: Keine schwere Berechnung im TimelineView-Body.

struct TimerDisplay: View {
    let startTime: Date

    var body: some View {
        TimelineView(.animation(minimumInterval: 1.0)) { timeline in
            // NUR String-Formatierung, KEINE DB-Queries hier
            let elapsed = timeline.date.timeIntervalSince(startTime)
            Text(formatDuration(elapsed))
                .monospacedDigit()
        }
    }
}
```

---

## S04: Android Performance-Optimierung

### Baseline Profile

```kotlin
// Neues Test-Modul oder im bestehenden androidTest:
@ExperimentalBaselineProfilesApi
class BaselineProfileGenerator {
    @get:Rule
    val rule = BaselineProfileRule()

    @Test
    fun generate() {
        rule.collect("com.fakturus.track") {
            pressHome()
            startActivityAndWait()
            // Haupt-Flows durchgehen
            device.wait(Until.hasObject(By.text("Zeiten")), 5000)
        }
    }
}
```

### Compose Recomposition vermeiden

```kotlin
// TimeTrackingViewModel.kt -- Stabile Keys
LazyColumn {
    items(sessions, key = { it.id }) { session ->
        // key = id verhindert unnoetige Recomposition bei Listenänderungen
        SessionRow(session = session)
    }
}

// Teure Berechnungen cachen:
val groupedSessions by remember(sessions) {
    derivedStateOf {
        sessions.groupBy { it.monthKey }
    }
}
```

### Room: IO-Dispatcher sicherstellen

```kotlin
// SyncEngine.kt -- Bereits korrekt: suspend fun nutzt IO-Dispatcher automatisch bei Room
// Pruefen: Kein runBlocking() auf Main Thread!

// Falls doch Main-Thread-Zugriff gefunden:
viewModelScope.launch(Dispatchers.IO) {
    val sessions = dao.getAllSessions()
    withContext(Dispatchers.Main) {
        _sessions.value = sessions
    }
}
```

### ProGuard/R8 Konfiguration pruefen

```proguard
# Sicherstellen dass MSAL, Ktor, Room Entities nicht gestrippt werden
# (sollte bereits aus Phase 1 existieren, aber verifizieren)
-keep class com.fakturus.track.models.** { *; }
-keep class com.fakturus.track.services.auth.** { *; }
```

---

## Performance-Ziele (Verifizierung)

| Metrik | Ziel | Messmethode |
|--------|------|------------|
| Cold Start (iOS) | < 1.0s | Instruments App Launch, Release Build, iPhone 12 |
| Cold Start (Android) | < 1.0s | Android Studio Profiler, Release Build, Pixel 6a |
| Tab-Wechsel | < 200ms | Gefuehlte Latenz, kein Jank |
| History Scroll (200 Items) | 60fps | Core Animation / GPU Inspector |
| Kalender Monatswechsel | < 100ms | Keine sichtbare Verzoegerung |
| Memory (iOS) | < 50MB | Instruments Allocations |
| Memory (Android) | < 80MB | Memory Profiler |
| Memory Leaks | 0 | Instruments Leaks / Memory Profiler |

---

## Ergebnis-Dokumentation

Nach Profiling und Optimierung eine `performance-baseline.md` erstellen mit:
- Gemessene Werte (vorher/nachher)
- Geraete auf denen gemessen wurde
- Identifizierte Bottlenecks und Loesungen
- Verbleibende Risiken
