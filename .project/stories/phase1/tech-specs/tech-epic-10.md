# Tech-Spec: EPIC 10 -- Offline-UX & Polish

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Shared/OfflineBanner.swift` | iOS | E10-S01 | Gelbes Banner, Slide-Animation |
| `ui/shared/OfflineBanner.kt` | Android | E10-S02 | AnimatedVisibility Banner |
| `Shared/SyncStatusView.swift` | iOS | E10-S03 | 4 Zustaende, Toolbar-Integration |
| `ui/shared/SyncStatusIndicator.kt` | Android | E10-S04 | Icon mit Rotation |
| `Shared/InitialSyncView.swift` | iOS | E10-S05 | Ladebildschirm nach Login |
| `ui/shared/InitialSyncScreen.kt` | Android | E10-S05 | Ladebildschirm nach Login |

**Modifizierte Dateien:**
- `ContentView.swift` (OfflineBanner einbetten)
- `MainScreen.kt` (OfflineBanner einbetten)
- `TimeTrackingView.swift` (SyncStatusView in Toolbar)
- `TimeTrackingScreen.kt` (SyncStatusIndicator in TopAppBar)
- `FakturusTrackApp.swift` (InitialSyncView als Zwischenschirm)
- `MainActivity.kt` (InitialSyncScreen als Zwischenschirm)
- Diverse Views/ViewModels (E10-S06: Error-Handling Polish)

---

## Code-Skizzen

### iOS: OfflineBanner.swift

```swift
struct OfflineBanner: View {
    @Environment(NetworkMonitor.self) private var networkMonitor

    var body: some View {
        if !networkMonitor.isConnected {
            HStack(spacing: 8) {
                Image(systemName: "wifi.slash")
                    .font(.caption)
                Text("Offline -- Aenderungen werden lokal gespeichert")
                    .font(.caption)
                Spacer()
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
            .background(Color("offline-banner"))
            .transition(.move(edge: .top).combined(with: .opacity))
        }
    }
}

// Integration in ContentView.swift:
// var body: some View {
//     VStack(spacing: 0) {
//         OfflineBanner()
//             .animation(.spring(), value: networkMonitor.isConnected)
//         TabView(selection: $selectedTab) { ... }
//     }
// }
```

### Android: OfflineBanner.kt

```kotlin
@Composable
fun OfflineBanner(networkMonitor: NetworkMonitor) {
    val isConnected by networkMonitor.isConnected.collectAsState()

    AnimatedVisibility(
        visible = !isConnected,
        enter = slideInVertically(initialOffsetY = { -it }) + fadeIn(),
        exit = slideOutVertically(targetOffsetY = { -it }) + fadeOut()
    ) {
        Surface(
            color = OfflineBannerColor,
            modifier = Modifier.fillMaxWidth()
        ) {
            Row(
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    Icons.Default.WifiOff,
                    contentDescription = null,
                    modifier = Modifier.size(16.dp)
                )
                Spacer(Modifier.width(8.dp))
                Text(
                    "Offline -- Aenderungen werden lokal gespeichert",
                    style = MaterialTheme.typography.bodySmall
                )
            }
        }
    }
}

// Integration in MainScreen.kt:
// Scaffold(...) { padding ->
//     Column(modifier = Modifier.padding(padding)) {
//         OfflineBanner(services.networkMonitor)
//         AppNavigation(...)
//     }
// }
```

### iOS: SyncStatusView.swift

```swift
enum SyncStatus {
    case idle
    case syncing
    case synced
    case pending(count: Int)
    case error(String)
}

struct SyncStatusView: View {
    let status: SyncStatus
    let onSync: () -> Void

    @State private var rotation: Double = 0
    @State private var showSynced = false

    var body: some View {
        Button(action: onSync) {
            switch status {
            case .idle:
                Image(systemName: "arrow.triangle.2.circlepath")
            case .syncing:
                Image(systemName: "arrow.triangle.2.circlepath")
                    .rotationEffect(.degrees(rotation))
                    .onAppear {
                        withAnimation(.linear(duration: 1).repeatForever(autoreverses: false)) {
                            rotation = 360
                        }
                    }
            case .synced:
                Image(systemName: "checkmark.icloud")
                    .foregroundStyle(Color("sync-done"))
            case .pending(let count):
                ZStack {
                    Image(systemName: "icloud.and.arrow.up")
                        .foregroundStyle(Color("sync-pending"))
                    if count > 0 {
                        Text("\(count)")
                            .font(.system(size: 8))
                            .padding(2)
                            .background(.red)
                            .clipShape(Circle())
                            .foregroundStyle(.white)
                            .offset(x: 8, y: -8)
                    }
                }
            case .error:
                Image(systemName: "exclamationmark.icloud")
                    .foregroundStyle(.red)
            }
        }
    }
}

// Integration in TimeTrackingView.swift:
// .toolbar {
//     ToolbarItem(placement: .topBarTrailing) {
//         SyncStatusView(status: syncStatus, onSync: { Task { await syncEngine.syncAll() } })
//     }
// }
```

### Android: SyncStatusIndicator.kt

```kotlin
@Composable
fun SyncStatusIndicator(
    isSyncing: Boolean,
    pendingCount: Int,
    lastError: String?,
    onSync: () -> Unit
) {
    val infiniteTransition = rememberInfiniteTransition(label = "sync")
    val rotationAngle by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(
            animation = tween(1000, easing = LinearEasing)
        ),
        label = "rotation"
    )

    IconButton(onClick = onSync) {
        when {
            isSyncing -> Icon(
                Icons.Default.Sync,
                contentDescription = "Synchronisiere...",
                modifier = Modifier.rotate(rotationAngle)
            )
            lastError != null -> Icon(
                Icons.Default.SyncProblem,
                contentDescription = "Sync fehlgeschlagen",
                tint = MaterialTheme.colorScheme.error
            )
            pendingCount > 0 -> BadgedBox(
                badge = {
                    Badge { Text("$pendingCount") }
                }
            ) {
                Icon(Icons.Default.CloudUpload, "Ausstehende Aenderungen", tint = SyncPending)
            }
            else -> Icon(Icons.Default.CloudDone, "Synchronisiert", tint = SyncDone)
        }
    }
}
```

### iOS: InitialSyncView.swift

```swift
struct InitialSyncView: View {
    let syncEngine: SyncEngine?
    let onComplete: () -> Void
    let onSkip: () -> Void

    @State private var isSyncing = true
    @State private var error: String?

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            if isSyncing {
                ProgressView()
                    .scaleEffect(1.5)
                Text("Daten werden geladen...")
                    .font(.headline)
            } else if let error {
                Image(systemName: "exclamationmark.triangle")
                    .font(.system(size: 48))
                    .foregroundStyle(.orange)
                Text("Synchronisation fehlgeschlagen")
                    .font(.headline)
                Text(error)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)

                Button("Erneut versuchen") { startSync() }
                    .buttonStyle(.borderedProminent)
                Button("Ohne Daten fortfahren") { onSkip() }
                    .buttonStyle(.bordered)
            }

            Spacer()
        }
        .padding()
        .task { startSync() }
    }

    private func startSync() {
        isSyncing = true
        error = nil
        Task {
            // Timeout nach 30 Sekunden
            let result = await withTaskGroup(of: Bool.self) { group in
                group.addTask {
                    await syncEngine?.syncAll()
                    return true
                }
                group.addTask {
                    try? await Task.sleep(for: .seconds(30))
                    return false
                }
                return await group.next() ?? false
            }

            isSyncing = false
            if result {
                onComplete()
            } else {
                error = "Zeitueberschreitung. Bitte pruefen Sie Ihre Internetverbindung."
            }
        }
    }
}
```

### Android: InitialSyncScreen.kt

```kotlin
@Composable
fun InitialSyncScreen(
    syncEngine: SyncEngine?,
    onComplete: () -> Unit,
    onSkip: () -> Unit
) {
    var isSyncing by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        try {
            withTimeout(30_000) {
                syncEngine?.syncAll()
            }
            onComplete()
        } catch (e: TimeoutCancellationException) {
            isSyncing = false
            error = "Zeitueberschreitung. Bitte pruefen Sie Ihre Internetverbindung."
        } catch (e: Exception) {
            isSyncing = false
            error = "Synchronisation fehlgeschlagen."
        }
    }

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        if (isSyncing) {
            CircularProgressIndicator()
            Spacer(Modifier.height(16.dp))
            Text("Daten werden geladen...", style = MaterialTheme.typography.headlineSmall)
        } else if (error != null) {
            Icon(Icons.Default.Warning, null, Modifier.size(48.dp), tint = Color(0xFFFFA000))
            Spacer(Modifier.height(16.dp))
            Text("Synchronisation fehlgeschlagen", style = MaterialTheme.typography.headlineSmall)
            Text(error!!, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(24.dp))
            FilledButton(onClick = {
                isSyncing = true
                error = null
                scope.launch {
                    try {
                        withTimeout(30_000) { syncEngine?.syncAll() }
                        onComplete()
                    } catch (e: Exception) {
                        isSyncing = false
                        error = "Synchronisation fehlgeschlagen."
                    }
                }
            }) { Text("Erneut versuchen") }
            Spacer(Modifier.height(8.dp))
            TextButton(onClick = onSkip) { Text("Ohne Daten fortfahren") }
        }
    }
}
```

### Error-Handling Polish (E10-S06)

Kein eigener Code -- stattdessen Anpassungen in bestehenden Dateien:

**Auth-Fehler (Session abgelaufen):**
```swift
// In APIClient, bei 401 nach Retry-Failure:
// -> Notification an App: "Session expired"
// -> App zeigt Alert: "Sitzung abgelaufen. Bitte melden Sie sich erneut an."
// -> Logout + Login-Screen
```

**Sync-Fehler:**
```swift
// SyncEngine setzt lastError, UI zeigt SyncStatusView mit .error
// Toast/Snackbar: "Synchronisation fehlgeschlagen. Daten sind lokal gespeichert."
```

**Validierungsfehler:**
```swift
// Bereits in SessionDetailSheet implementiert:
// "Endzeit muss nach Startzeit liegen" (inline unter Feld)
```

**Fehler-Texte (Deutsch):**
```swift
enum ErrorMessages {
    static let sessionExpired = "Sitzung abgelaufen. Bitte melden Sie sich erneut an."
    static let syncFailed = "Synchronisation fehlgeschlagen. Ihre Daten sind lokal gespeichert."
    static let serverError = "Serverfehler. Bitte versuchen Sie es spaeter erneut."
    static let endBeforeStart = "Endzeit muss nach Startzeit liegen"
    static let durationTooLong = "Dauer ueber 24 Stunden"
    static let networkError = "Keine Internetverbindung"
}
```

---

## Datenfluss

### OfflineBanner

```
NetworkMonitor.isConnected
    |
    | false -> OfflineBanner sichtbar (Slide-In)
    | true  -> OfflineBanner unsichtbar (Slide-Out)
```

### SyncStatus

```
SyncEngine.isSyncing + lastError + pendingCount
    |
    v
SyncStatusView / SyncStatusIndicator
    |
    +-- isSyncing=true    -> Rotierende Pfeile
    +-- lastError!=nil    -> Rotes Fehler-Icon
    +-- pendingCount > 0  -> Gelber Upload-Pfeil + Badge
    +-- alles synced      -> Gruener Haken (3s, dann idle)
```

### InitialSync

```
Login erfolgreich
    |
    v
InitialSyncView / InitialSyncScreen
    |
    | syncEngine.syncAll() + 30s Timeout
    |
    +-- Erfolg -> onComplete() -> ContentView/MainScreen
    +-- Timeout -> Fehler + "Erneut" / "Ohne Daten" Buttons
    +-- "Ohne Daten" -> onSkip() -> ContentView/MainScreen (leere DB)
```

---

## Testbare Kriterien

- [ ] iOS: OfflineBanner erscheint bei isConnected=false
- [ ] iOS: OfflineBanner verschwindet animiert bei isConnected=true
- [ ] iOS: SyncStatusView zeigt korrekte Icons fuer alle 4 Zustaende
- [ ] iOS: Sync-Button in Toolbar triggert manuellen Sync
- [ ] iOS: InitialSyncView zeigt Spinner waehrend Sync
- [ ] iOS: InitialSyncView zeigt Fehler nach Timeout
- [ ] iOS: "Ohne Daten fortfahren" navigiert zur App
- [ ] Android: OfflineBanner AnimatedVisibility korrekt
- [ ] Android: SyncStatusIndicator Rotation Animation
- [ ] Android: InitialSyncScreen 30s Timeout funktioniert
- [ ] Beide: Fehler-Texte sind deutsch, keine technischen Details

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| NetworkMonitor liefert false-positive "offline" | Niedrig | Geraete-Netzwerk-Status ist zuverlaessig, ggf. Ping-Test als Verifikation |
| InitialSync Timeout zu kurz bei langsamer Verbindung | Mittel | 30s sollte reichen, "Ohne Daten fortfahren" als Escape |
| SyncStatusView flickert zwischen Zustaenden | Niedrig | Debounce: "Synced" 3s anzeigen bevor zu idle wechseln |
| Error-Handling: Auth-Alert blockiert Interaktion | Niedrig | Alert mit "Anmelden" Button, Logout automatisch |
