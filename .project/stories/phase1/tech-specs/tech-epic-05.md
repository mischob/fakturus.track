# Tech-Spec: EPIC 05 -- Zeiterfassungs-UI (Timer-Screen)

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Features/TimeTracking/TimerDisplay.swift` | iOS | E05-S01 | HH:MM:SS mit TimelineView |
| `features/timetracking/TimerDisplay.kt` | Android | E05-S02 | LaunchedEffect-basiert |
| `Features/TimeTracking/ActiveSessionCard.swift` | iOS | E05-S03 | 3 Zustaende: Idle/Running/Stopped |
| `features/timetracking/ActiveSessionCard.kt` | Android | E05-S04 | 3 Zustaende |
| `Features/TimeTracking/TimeTrackingViewModel.swift` | iOS | E05-S05 | @Observable, CRUD |
| `features/timetracking/TimeTrackingViewModel.kt` | Android | E05-S06 | StateFlow, CoroutineScope |
| `features/timetracking/TimeTrackingViewModelFactory.kt` | Android | E05-S06 | ViewModelProvider.Factory |

---

## Code-Skizzen

### iOS: TimerDisplay.swift

```swift
enum TimerSize {
    case large, medium, small

    var fontSize: CGFloat {
        switch self {
        case .large: return 48
        case .medium: return 28
        case .small: return 17
        }
    }
}

struct TimerDisplay: View {
    let startTime: Date
    var pauseOffset: TimeInterval = 0   // Abzug fuer bisherige Pausen
    var isRunning: Bool = true
    var size: TimerSize = .large

    var body: some View {
        HStack(spacing: 8) {
            if isRunning {
                // Pulsierender gruener Punkt
                Circle()
                    .fill(.green)
                    .frame(width: 10, height: 10)
                    .opacity(pulseOpacity)
                    .animation(.easeInOut(duration: 1).repeatForever(autoreverses: true),
                               value: pulseOpacity)
                    .onAppear { pulseOpacity = 0.3 }
            }

            if isRunning {
                TimelineView(.periodic(from: .now, by: 1)) { context in
                    let elapsed = context.date.timeIntervalSince(startTime) - pauseOffset
                    Text(max(0, elapsed).formattedHHMMSS)
                        .font(.system(size: size.fontSize, design: .monospaced))
                        .monospacedDigit()
                }
            } else {
                // Statische Anzeige
                let elapsed = (Date().timeIntervalSince(startTime)) - pauseOffset
                Text(max(0, elapsed).formattedHHMMSS)
                    .font(.system(size: size.fontSize, design: .monospaced))
                    .monospacedDigit()
            }
        }
    }

    @State private var pulseOpacity: Double = 1.0
}
```

### Android: TimerDisplay.kt

```kotlin
enum class TimerSize(val fontSize: TextUnit) {
    LARGE(48.sp), MEDIUM(28.sp), SMALL(16.sp)
}

@Composable
fun TimerDisplay(
    startTime: Instant,
    pauseOffsetMillis: Long = 0,
    isRunning: Boolean = true,
    size: TimerSize = TimerSize.LARGE
) {
    var elapsedMillis by remember { mutableLongStateOf(0L) }

    LaunchedEffect(isRunning, startTime) {
        if (isRunning) {
            while (true) {
                elapsedMillis = Duration.between(startTime, Instant.now()).toMillis() - pauseOffsetMillis
                delay(1000)
            }
        } else {
            elapsedMillis = Duration.between(startTime, Instant.now()).toMillis() - pauseOffsetMillis
        }
    }

    Row(verticalAlignment = Alignment.CenterVertically) {
        if (isRunning) {
            PulsingDot(color = TimerRunning)
            Spacer(Modifier.width(8.dp))
        }
        Text(
            text = DateFormatting.formatDurationHHMMSS(maxOf(0, elapsedMillis)),
            fontFamily = FontFamily.Monospace,
            fontSize = size.fontSize
        )
    }
}

@Composable
private fun PulsingDot(color: Color) {
    val infiniteTransition = rememberInfiniteTransition(label = "pulse")
    val alpha by infiniteTransition.animateFloat(
        initialValue = 1f, targetValue = 0.3f,
        animationSpec = infiniteRepeatable(
            animation = tween(1000), repeatMode = RepeatMode.Reverse
        ), label = "pulseAlpha"
    )
    Box(
        modifier = Modifier.size(10.dp)
            .clip(CircleShape)
            .background(color.copy(alpha = alpha))
    )
}
```

### iOS: ActiveSessionCard.swift (Grundstruktur, Pause-Placeholder)

```swift
struct ActiveSessionCard: View {
    let session: WorkSession?
    let isPaused: Bool
    let onStart: () -> Void
    let onStop: () -> Void
    let onFinish: () -> Void
    let onPause: () -> Void       // Placeholder, wird in E08 aktiviert
    let onResume: () -> Void      // Placeholder
    let onSave: (Date, Date, Date?, Int) -> Void  // date, startTime, stopTime, pauseMinutes
    let onDelete: () -> Void

    var body: some View {
        GroupBox {
            if let session {
                if session.isRunning {
                    runningContent(session)
                } else if !session.isFinished {
                    stoppedContent(session)
                }
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
            Button(action: {
                UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                onStart()
            }) {
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
    private func runningContent(_ session: WorkSession) -> some View {
        VStack(spacing: 16) {
            // Status
            HStack {
                Circle().fill(Color("timer-running")).frame(width: 8, height: 8)
                Text("Laufende Sitzung").font(.subheadline).foregroundStyle(.secondary)
                Spacer()
            }

            // Timer
            TimerDisplay(startTime: session.startTime, isRunning: true, size: .large)

            // Info
            HStack {
                VStack(alignment: .leading) {
                    Text("Start").font(.caption).foregroundStyle(.secondary)
                    Text(session.startTime.timeShort)
                }
                Spacer()
                if session.pauseMinutes > 0 {
                    Text("Pause: \(session.pauseMinutes) min")
                        .font(.caption)
                        .foregroundStyle(Color("pause"))
                }
            }

            // Buttons
            HStack(spacing: 12) {
                // Pause-Button (E08 aktiviert dies)
                Button("Pause", systemImage: "pause.fill") { onPause() }
                    .buttonStyle(.bordered)
                    .disabled(true) // Wird in E08 aktiviert

                Button("Stop", systemImage: "stop.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onStop()
                }
                .buttonStyle(.bordered)

                Button("Fertig", systemImage: "checkmark.circle.fill") {
                    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                    onFinish()
                }
                .buttonStyle(.borderedProminent)
            }
        }
        .padding()
    }

    @ViewBuilder
    private func stoppedContent(_ session: WorkSession) -> some View {
        // Editierbare Felder fuer Datum, Start, Ende, Pause
        // DatePicker und TimePicker
        // Fertig + Verwerfen Buttons
        // Brutto/Netto Berechnung live
        StoppedSessionEditor(
            session: session,
            onSave: onSave,
            onFinish: onFinish,
            onDelete: onDelete
        )
    }
}
```

### iOS: TimeTrackingViewModel.swift

```swift
@Observable
final class TimeTrackingViewModel {
    // MARK: - State
    var activeSession: WorkSession?
    var isLoading = false
    var error: String?

    // Pause-State (wird in E08 erweitert)
    var isPaused = false
    var currentPauseStart: Date?
    var accumulatedPauseMinutes: Int = 0

    // MARK: - Dependencies
    private let modelContext: ModelContext

    init(modelContext: ModelContext) {
        self.modelContext = modelContext
        loadActiveSession()
    }

    private func loadActiveSession() {
        let descriptor = FetchDescriptor<WorkSession>(
            predicate: #Predicate { !$0.isFinished }
        )
        activeSession = try? modelContext.fetch(descriptor).first
    }

    // MARK: - Actions

    func startSession() {
        let session = WorkSession(
            date: Date(),
            startTime: Date(),
            isPendingSync: true,
            isSynced: false,
            isFinished: false
        )
        modelContext.insert(session)
        try? modelContext.save()
        activeSession = session
        accumulatedPauseMinutes = 0
    }

    func stopSession() {
        guard let session = activeSession, session.isRunning else { return }
        // Falls pausiert, Pause beenden
        if isPaused { resumeSession() }
        session.stopTime = Date()
        session.updatedAt = Date()
        try? modelContext.save()
    }

    func finishSession() {
        guard let session = activeSession else { return }
        if isPaused { resumeSession() }
        if session.stopTime == nil { session.stopTime = Date() }
        session.isFinished = true
        session.isPendingSync = true
        session.updatedAt = Date()
        try? modelContext.save()
        activeSession = nil
        isPaused = false
        accumulatedPauseMinutes = 0

        // Sync-Trigger (wird in E07-S05 implementiert)
        // Task { await syncEngine?.syncAll() }
    }

    func updateSession(date: Date, startTime: Date, stopTime: Date?, pauseMinutes: Int) {
        guard let session = activeSession else { return }
        // Validierung
        if let stop = stopTime, stop <= startTime { return }

        session.date = date
        session.startTime = startTime
        session.stopTime = stopTime
        session.pauseMinutes = pauseMinutes
        session.updatedAt = Date()
        session.isPendingSync = true
        try? modelContext.save()
    }

    func deleteSession(_ session: WorkSession) {
        modelContext.delete(session)
        try? modelContext.save()
        if session.id == activeSession?.id {
            activeSession = nil
        }
        // API DELETE wenn synced (wird in E07-S05 implementiert)
    }

    // Pause-Methoden (Grundgeruest fuer E08)
    func pauseSession() { /* E08-S01 */ }
    func resumeSession() { /* E08-S01 */ }
}
```

### Android: TimeTrackingViewModel.kt

```kotlin
class TimeTrackingViewModel(
    private val database: AppDatabase
) : ViewModel() {

    private val dao = database.workSessionDao()

    private val _activeSession = MutableStateFlow<WorkSessionEntity?>(null)
    val activeSession: StateFlow<WorkSessionEntity?> = _activeSession.asStateFlow()

    val sessions: Flow<List<WorkSessionEntity>> = dao.getAllOrderedByDate()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private val _error = MutableStateFlow<String?>(null)
    val error: StateFlow<String?> = _error.asStateFlow()

    // Pause-State (E08)
    private val _isPaused = MutableStateFlow(false)
    val isPaused: StateFlow<Boolean> = _isPaused.asStateFlow()

    init {
        viewModelScope.launch {
            _activeSession.value = dao.getActiveSession()
        }
    }

    fun startSession() {
        viewModelScope.launch {
            val session = WorkSessionEntity(
                date = LocalDate.now().toString(),
                startTime = Instant.now().toString(),
                isPendingSync = true,
                isSynced = false,
                isFinished = false
            )
            dao.insert(session)
            _activeSession.value = session
        }
    }

    fun stopSession() {
        viewModelScope.launch {
            val session = _activeSession.value ?: return@launch
            val updated = session.copy(
                stopTime = Instant.now().toString(),
                updatedAt = Instant.now().toString()
            )
            dao.update(updated)
            _activeSession.value = updated
        }
    }

    fun finishSession() {
        viewModelScope.launch {
            val session = _activeSession.value ?: return@launch
            val updated = session.copy(
                stopTime = session.stopTime ?: Instant.now().toString(),
                isFinished = true,
                isPendingSync = true,
                updatedAt = Instant.now().toString()
            )
            dao.update(updated)
            _activeSession.value = null
            // Sync-Trigger in E07-S05
        }
    }

    fun updateSession(date: String, startTime: String, stopTime: String?, pauseMinutes: Int) {
        viewModelScope.launch {
            val session = _activeSession.value ?: return@launch
            val updated = session.copy(
                date = date, startTime = startTime, stopTime = stopTime,
                pauseMinutes = pauseMinutes, isPendingSync = true,
                updatedAt = Instant.now().toString()
            )
            dao.update(updated)
            _activeSession.value = updated
        }
    }

    fun deleteSession(session: WorkSessionEntity) {
        viewModelScope.launch {
            dao.delete(session)
            if (session.id == _activeSession.value?.id) {
                _activeSession.value = null
            }
        }
    }
}
```

---

## Datenfluss

```
User Tap "Starten"
    |
    v
ActiveSessionCard.onStart()
    |
    v
TimeTrackingViewModel.startSession()
    |
    +-- Erstellt WorkSession (id, date, startTime, isPendingSync=true)
    +-- Speichert in lokale DB (ModelContext.insert / DAO.insert)
    +-- Setzt activeSession = neue Session
    |
    v
ActiveSessionCard: session != nil, isRunning -> Running State
    |
    v
TimerDisplay: startTime = session.startTime, isRunning = true
    |
    | (TimelineView/LaunchedEffect aktualisiert jede Sekunde)
    v
User sieht "01:23:45" und steigend

User Tap "Stop"
    |
    v
ViewModel.stopSession() -> session.stopTime = now
    |
    v
ActiveSessionCard: !isRunning, !isFinished -> Stopped State
    |
    v
User sieht editierbare Felder (Datum, Start, Ende, Pause)

User Tap "Fertig"
    |
    v
ViewModel.finishSession() -> session.isFinished = true, isPendingSync = true
    |
    v
activeSession = nil -> ActiveSessionCard: Idle State
    |
    v
Session erscheint in History (via @Query / Flow)
```

---

## Testbare Kriterien

- [ ] iOS: TimerDisplay zeigt korrekte Zeit fuer `startTime = 1h ago`
- [ ] iOS: Pulsierender Punkt animiert wenn `isRunning == true`
- [ ] iOS: ActiveSessionCard zeigt Idle-State wenn session == nil
- [ ] iOS: startSession() erstellt WorkSession mit korrekten Default-Werten
- [ ] iOS: stopSession() setzt stopTime auf aktuelle Zeit
- [ ] iOS: finishSession() setzt isFinished=true, isPendingSync=true, activeSession=nil
- [ ] Android: TimerDisplay aktualisiert jede Sekunde
- [ ] Android: ActiveSessionCard rendert alle 3 States korrekt
- [ ] Android: ViewModel startSession/stopSession/finishSession Lifecycle korrekt
- [ ] Beide: Haptic Feedback bei Start/Stop/Finish

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| TimelineView Performance bei langer Nutzung | Niedrig | TimelineView ist optimiert, kein Recompose-Problem |
| Android LaunchedEffect Drift (Sekunden-Ungenauigkeit) | Niedrig | System.currentTimeMillis() statt akkumuliertem Counter |
| Stopped-State: DatePicker/TimePicker UX-Probleme | Mittel | Eigene TimePickerSheet als Alternative |
| SwiftData `@Query` + ViewModel active session doppelt | Mittel | activeSession nur im ViewModel, @Query nur fuer History |
