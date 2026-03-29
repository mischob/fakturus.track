# Implementation-Checkliste -- Vor Story-Start lesen

Dieses Dokument muss jeder Entwickler / AI-Agent lesen, bevor er eine Story anfaengt. Es enthaelt die Konventionen, die fuer ALLE Stories in Phase 1 gelten.

---

## 1. Vor dem Start: Dependencies pruefen

### Pflicht-Lektuere pro Story

Bevor du mit einer Story beginnst, lies:

1. **Die Story selbst** (`.project/stories/phase1/epic-XX.md`)
2. **Die Tech-Spec** (`.project/stories/phase1/tech-specs/tech-epic-XX.md`)
3. **Die Architektur-Docs** die fuer deine Story relevant sind:
   - iOS: `.project/architecture/ios-architecture.md`
   - Android: `.project/architecture/android-architecture.md`
   - Sync: `.project/architecture/shared-concepts.md`
   - DB: `.project/architecture/data-layer.md`

### Existierende Dateien pruefen

Bevor du eine Datei erstellst, pruefe ob sie schon existiert (von einer vorherigen Story). Wenn ja:
- **Erweitern**, nicht neu erstellen
- Bestehende Funktionalitaet nicht brechen
- Bestehende Tests nicht loeschen

### Abhaengigkeiten der Story

Jede Story hat im EPIC-Dokument ein Feld `Abhaengigkeiten`. Pruefe:
- Sind diese Stories gemergt?
- Existieren die Dateien die du importierst?
- Falls eine Dependency noch nicht gemergt ist: Nutze Mocks (siehe `parallel-implementation-guide.md`)

---

## 2. Namenskonventionen

### Dateien

| Plattform | Konvention | Beispiel |
|-----------|------------|---------|
| iOS | PascalCase + .swift | `TimeTrackingViewModel.swift` |
| Android | PascalCase + .kt | `TimeTrackingViewModel.kt` |
| iOS Views | PascalCase + View/Sheet/Card | `SessionDetailSheet.swift` |
| Android Composables | PascalCase + Screen/Sheet/Card | `SessionDetailSheet.kt` |

### Klassen / Structs

| Plattform | Konvention | Beispiel |
|-----------|------------|---------|
| iOS Models | `@Model final class` | `WorkSession` |
| Android Entities | `data class ...Entity` | `WorkSessionEntity` |
| iOS ViewModels | `@Observable final class` | `TimeTrackingViewModel` |
| Android ViewModels | `class ... : ViewModel()` | `TimeTrackingViewModel` |
| iOS Views | `struct ... : View` | `ActiveSessionCard` |
| Android Composables | `@Composable fun ...` | `ActiveSessionCard(...)` |

### Properties / Variablen

| Plattform | Konvention | Beispiel |
|-----------|------------|---------|
| iOS | camelCase, `is...` fuer Booleans | `isAuthenticated`, `activeSession` |
| Android | camelCase, `is...` fuer Booleans | `isAuthenticated`, `activeSession` |
| iOS State | `@State private var` | `@State private var isExpanded = false` |
| Android State | `MutableStateFlow` + `StateFlow` | `private val _isLoading = MutableStateFlow(false)` |

### DTOs und API

| Element | Konvention | Beispiel |
|---------|------------|---------|
| DTO Suffix | `...DTO` fuer Responses, `...SyncItem` fuer Request-Items | `WorkSessionDTO`, `WorkSessionSyncItem` |
| Request Suffix | `...Request` | `SyncWorkSessionsRequest` |
| Response Suffix | `...Response` | `SyncVacationDaysResponse` |
| JSON-Keys | PascalCase via @SerialName (Android) / custom decoder (iOS) | `@SerialName("StartTime")` |

---

## 3. Ordnerstruktur (NICHT aendern)

### iOS

```
FakturusTrack/
  App/           -- Entry Point, AppState, ServiceContainer, Configuration
  Services/      -- Auth, API, Sync, Network (1 Ordner pro Service-Bereich)
  Features/      -- Auth, TimeTracking, Shell (1 Ordner pro Feature)
  Shared/        -- Plattformuebergreifende UI-Komponenten
  Models/        -- SwiftData Models + DTOs + PersistenceManager
  Extensions/    -- Date, TimeInterval Extensions
  Resources/     -- Assets, Localizable
```

### Android

```
com/fakturus/track/
  (Root)         -- App, Activity, ServiceContainer, Configuration
  services/      -- auth, api, sync, network
  features/      -- auth, timetracking, shell
  models/        -- Entities, AppDatabase, DTOs
  ui/            -- theme, navigation, shared
  util/          -- DateFormatting
```

**Neue Dateien IMMER in den richtigen Ordner legen.** Keine neuen Top-Level-Ordner erstellen.

---

## 4. Code-Stil

### Allgemein

- **Deutsch fuer User-facing Strings**: "Bereit fuer den naechsten Eintrag", "Loeschen", "Pause"
- **Englisch fuer Code**: Variablen, Methoden, Kommentare in Englisch
- **Keine Abkuerzungen** in oeffentlichen APIs: `deleteSession()` statt `delSess()`
- **Keine unnuetzen Kommentare**: Kein `// This function starts a session` ueber `func startSession()`
- **Hilfreiche Kommentare**: `// ACHTUNG: ALLE lokalen VacationDays senden, nicht nur pending!`

### iOS-spezifisch

- `@Observable` fuer alle ViewModels und Services (kein `ObservableObject` + `@Published`)
- `@Environment` fuer globale Services (AuthManager, NetworkMonitor, SyncEngine)
- `@Query` fuer DB-Abfragen direkt in Views (SwiftData-nativ)
- `@State private var viewModel` fuer Feature-ViewModels in Views
- Keine `Combine`-Imports (ausser Timer.publish wo noetig)
- `async/await` statt Completion-Handler
- `actor` fuer thread-safe Klassen (SyncEngine)

### Android-spezifisch

- `StateFlow` / `Flow` fuer reaktiven State
- `viewModelScope.launch {}` fuer ViewModel-Coroutines
- `collectAsState()` in Compose-Funktionen
- `@Serializable` + `@SerialName("PascalCase")` fuer DTOs
- Kein `Hilt` / `@Inject` / `@HiltViewModel`
- Kein `LiveData` (nur StateFlow)
- `ServiceContainer` fuer manuelle DI (kein Dagger)

### Error Handling

- **ViewModels**: try/catch um DB-Operationen, Fehler in `error: String?` State setzen
- **SyncEngine**: try/catch um alles, Fehler loggen aber NICHT werfen
- **APIClient**: Wirft `APIError` / `APIError` (sealed class), ViewModel faengt
- **User-facing**: Deutsche Fehlertexte, keine technischen Details

---

## 5. Testing

### Was MUSS getestet werden

| Bereich | Test-Typ | Pflicht |
|---------|----------|---------|
| ViewModel Methoden (start/stop/finish/pause) | Unit Test | Ja |
| SyncEngine Algorithmus | Unit Test mit Mock-APIClient | Ja |
| Netto-Dauer Berechnung | Unit Test | Ja |
| DTO Serialisierung/Deserialisierung | Unit Test | Ja |

### Was KANN getestet werden (nice-to-have)

| Bereich | Test-Typ | Pflicht |
|---------|----------|---------|
| APIClient PascalCase Konvertierung | Unit Test | Optional |
| View Rendering (Previews) | SwiftUI Preview / Compose Preview | Optional |
| Room DAO Queries | Instrumented Test | Optional |

### Test-Konventionen

**iOS (Swift Testing):**
```swift
@Test func startSession_createsNewSession() async {
    let container = PersistenceManager.testContainer()
    let context = ModelContext(container)
    let vm = TimeTrackingViewModel(modelContext: context)

    vm.startSession()

    #expect(vm.activeSession != nil)
    #expect(vm.activeSession?.isFinished == false)
    #expect(vm.activeSession?.isPendingSync == true)
}
```

**Android (JUnit 5):**
```kotlin
@Test
fun `startSession creates new pending session`() = runTest {
    val db = Room.inMemoryDatabaseBuilder(context, AppDatabase::class.java)
        .allowMainThreadQueries().build()
    val vm = TimeTrackingViewModel(database = db)

    vm.startSession()

    val session = vm.activeSession.first()
    assertNotNull(session)
    assertFalse(session!!.isFinished)
    assertTrue(session.isPendingSync)
}
```

---

## 6. Git-Workflow

### Branch-Naming

```
feat/P1-E01-S01-ios-project-setup
feat/P1-E02-S02-android-auth
feat/P1-E05-S03-ios-active-session-card
```

Format: `feat/P1-{EPIC}-{STORY}-{plattform}-{kurzbeschreibung}`

### Commit-Messages

```
feat(ios): add Xcode project with MSAL dependency

- Bundle ID: com.fakturus.track
- Swift 6, iOS 17 minimum
- SPM dependency on MSAL
- Configuration.swift with B2C config
```

Format: `feat({plattform}): {was geaendert}`

Prefixes:
- `feat`: Neue Funktionalitaet
- `fix`: Bugfix
- `refactor`: Umstrukturierung ohne Funktionsaenderung
- `test`: Tests hinzugefuegt/geaendert
- `chore`: Build, Dependencies, Config

### Merge-Strategie

- Jede Story = ein Branch von `main` (oder `develop`)
- **Squash Merge** bevorzugt (saubere History)
- Vor dem Merge: Pruefe ob Tests bestehen
- Nach dem Merge: Naechste Story kann auf dem neuen Stand starten

---

## 7. Definition of Done (pro Story)

Eine Story ist "Done" wenn:

- [ ] Alle Akzeptanzkriterien aus dem EPIC-Dokument erfuellt
- [ ] Code kompiliert ohne Warnungen (Swift 6 Strict Concurrency beachten!)
- [ ] App laeuft auf Simulator/Emulator
- [ ] Tests fuer ViewModel-Logik geschrieben und bestanden (wo gefordert)
- [ ] SwiftUI Previews / Compose Previews funktionieren
- [ ] Kein `// TODO: fix this` ohne Story-Referenz (`// TODO: E08` ist OK)
- [ ] Keine hartkodierten Strings in der UI (ausser fuer Placeholder-Screens)
- [ ] Dateien im richtigen Ordner
- [ ] Branch hat aussagekraeftigen Namen

---

## 8. Haeufige Fehler (vermeide diese)

### iOS

- **Vergessen `@MainActor` fuer UI-Updates**: Alle `@Observable` Properties die die UI aendern muessen auf dem Main Thread aktualisiert werden.
- **SwiftData ModelContext auf falschem Thread**: ModelContext immer auf dem gleichen Actor/Thread nutzen. Fuer SyncEngine: `@ModelActor`.
- **MSAL UIViewController nicht gefunden**: `UIApplication.topViewController()` Helper nutzen.
- **DatePicker im Sheet**: `.presentationDetents([.medium, .large])` setzen, sonst zu klein.

### Android

- **Room DB auf Main Thread**: Room-Operationen sind `suspend` und muessen in einer Coroutine laufen.
- **Nested LazyColumn**: NIE eine LazyColumn in einer LazyColumn. MonthGroup als regulaere `Column`, items in die aeussere LazyColumn.
- **MSAL Callback vs Coroutine**: MSAL Android nutzt Callbacks. `suspendCancellableCoroutine` nutzen fuer Coroutine-Wrapper.
- **Vergessen `collectAsState()`**: StateFlow muss in Compose mit `.collectAsState()` beobachtet werden.
- **Navigation State bei Recomposition**: `rememberNavController()` und `currentBackStackEntryAsState()` nutzen.

### Beide

- **ISO 8601 Date Parsing**: Backend schickt teils mit, teils ohne Millisekunden. IMMER Fallback-Parsing implementieren.
- **PascalCase vergessen**: Backend erwartet `"StartTime"`, nicht `"startTime"`.
- **Sync-Flag nicht gesetzt**: Nach jeder lokalen Aenderung `isPendingSync = true` und `updatedAt = Date()` setzen.
- **VacationDay Sync**: ALLE lokalen Tage senden, NICHT nur pending. Das ist ein bewusster Unterschied zu WorkSessions.

---

## 9. Schnellreferenz: Wo finde ich was?

| Frage | Dokument |
|-------|----------|
| Was sind die Akzeptanzkriterien der Story? | `.project/stories/phase1/epic-XX.md` |
| Welche Dateien erstelle ich? | `.project/stories/phase1/tech-specs/tech-epic-XX.md` |
| Wie sieht der Code ungefaehr aus? | `.project/stories/phase1/tech-specs/tech-epic-XX.md` (Code-Skizzen) |
| Wie ist die Ordnerstruktur? | `.project/architecture/ios-architecture.md` / `android-architecture.md` |
| Wie funktioniert Sync? | `.project/architecture/shared-concepts.md` |
| Wie sehen die DB-Models aus? | `.project/architecture/data-layer.md` |
| Wie funktioniert Auth (MSAL)? | `.project/architecture/shared-concepts.md` Abschnitt 3 |
| Was sind die API-Endpoints? | `.project/stories/phase1/tech-specs/tech-epic-04.md` |
| Wie merged man parallel? | `.project/stories/phase1/parallel-implementation-guide.md` |
| Welche Welle ist meine Story? | `.project/stories/phase1/execution-waves.md` |
| Was ist die Gesamtstruktur am Ende? | `.project/stories/phase1/tech-blueprint.md` |
