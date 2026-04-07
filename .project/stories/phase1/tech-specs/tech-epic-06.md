# Tech-Spec: EPIC 06 -- History & Session-Verwaltung

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Features/TimeTracking/SessionRow.swift` | iOS | E06-S01 | Kompakte Zeile in History |
| `features/timetracking/SessionRow.kt` | Android | E06-S02 | ListItem mit SwipeToDismiss |
| `Features/TimeTracking/MonthGroup.swift` | iOS | E06-S03 | Expand/Collapse Monatsgruppe |
| `features/timetracking/MonthGroup.kt` | Android | E06-S04 | AnimatedVisibility |
| `Features/TimeTracking/SessionDetailSheet.swift` | iOS | E06-S05 | Half-Sheet, Bearbeitung |
| `features/timetracking/SessionDetailSheet.kt` | Android | E06-S06 | ModalBottomSheet |
| `Features/TimeTracking/TimeTrackingView.swift` | iOS | E06-S07 | Zeiten-Tab Zusammenbau |
| `features/timetracking/TimeTrackingScreen.kt` | Android | E06-S08 | LazyColumn Zusammenbau |

---

## Code-Skizzen

### iOS: SessionRow.swift

```swift
struct SessionRow: View {
    let session: WorkSession
    let onTap: () -> Void
    let onDelete: () -> Void

    var body: some View {
        Button(action: onTap) {
            HStack(spacing: 12) {
                // Sync-Status Icon
                syncIcon
                    .font(.caption)
                    .frame(width: 20)

                // Wochentag + Datum
                VStack(alignment: .leading, spacing: 2) {
                    Text(session.date.weekdayShort + " " + session.date.formatted(as: "dd.MM."))
                        .font(.subheadline)
                    Text("\(session.startTime.timeShort) - \(session.stopTime?.timeShort ?? "--:--")")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                // Pause (wenn > 0)
                if session.pauseMinutes > 0 {
                    Text("P\(session.pauseMinutes)")
                        .font(.caption2)
                        .foregroundStyle(Color("pause"))
                        .padding(.horizontal, 4)
                        .background(Color("pause").opacity(0.1), in: RoundedRectangle(cornerRadius: 4))
                }

                // Netto-Dauer
                Text(session.netDuration.formattedHHMM)
                    .font(.subheadline.bold())
                    .monospacedDigit()
            }
        }
        .buttonStyle(.plain)
        .swipeActions(edge: .trailing) {
            Button(role: .destructive) {
                onDelete()
            } label: {
                Label("Loeschen", systemImage: "trash")
            }
        }
    }

    @ViewBuilder
    private var syncIcon: some View {
        if session.isSynced {
            Image(systemName: "cloud.fill")
                .foregroundStyle(Color("sync-done"))
        } else if session.isPendingSync {
            Image(systemName: "icloud.and.arrow.up")
                .foregroundStyle(Color("sync-pending"))
        }
    }
}
```

### iOS: MonthGroup.swift

```swift
struct MonthGroup: View {
    let monthName: String
    let sessions: [WorkSession]
    let onDeleteSession: (WorkSession) -> Void
    let onSelectSession: (WorkSession) -> Void

    @State private var isExpanded: Bool

    init(
        monthName: String,
        sessions: [WorkSession],
        isCurrentMonth: Bool = false,
        onDeleteSession: @escaping (WorkSession) -> Void,
        onSelectSession: @escaping (WorkSession) -> Void
    ) {
        self.monthName = monthName
        self.sessions = sessions
        self.onDeleteSession = onDeleteSession
        self.onSelectSession = onSelectSession
        self._isExpanded = State(initialValue: isCurrentMonth)
    }

    private var totalNetDuration: TimeInterval {
        sessions.reduce(0) { $0 + $1.netDuration }
    }

    var body: some View {
        VStack(spacing: 0) {
            // Header
            Button {
                withAnimation(.spring()) { isExpanded.toggle() }
            } label: {
                HStack {
                    Text(monthName).font(.headline)
                    Spacer()
                    Text("\(sessions.count) Eintr.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(totalNetDuration.formattedHHMM)
                        .font(.subheadline.bold())
                        .monospacedDigit()
                    Image(systemName: "chevron.right")
                        .rotationEffect(.degrees(isExpanded ? 90 : 0))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                .padding(.vertical, 8)
            }
            .buttonStyle(.plain)

            // Content
            if isExpanded {
                Divider()
                ForEach(sessions) { session in
                    SessionRow(
                        session: session,
                        onTap: { onSelectSession(session) },
                        onDelete: { onDeleteSession(session) }
                    )
                    if session.id != sessions.last?.id {
                        Divider().padding(.leading, 32)
                    }
                }
            }
        }
    }
}
```

### iOS: SessionDetailSheet.swift

```swift
struct SessionDetailSheet: View {
    let session: WorkSession
    let onSave: (Date, Date, Date?, Int) -> Void
    let onDelete: () -> Void
    @Environment(\.dismiss) private var dismiss

    @State private var editDate: Date
    @State private var editStartTime: Date
    @State private var editStopTime: Date
    @State private var editPauseMinutes: String
    @State private var showDeleteConfirmation = false
    @State private var validationError: String?

    init(session: WorkSession, onSave: @escaping (Date, Date, Date?, Int) -> Void, onDelete: @escaping () -> Void) {
        self.session = session
        self.onSave = onSave
        self.onDelete = onDelete
        _editDate = State(initialValue: session.date)
        _editStartTime = State(initialValue: session.startTime)
        _editStopTime = State(initialValue: session.stopTime ?? Date())
        _editPauseMinutes = State(initialValue: String(session.pauseMinutes))
    }

    private var pauseMinutes: Int { Int(editPauseMinutes) ?? 0 }

    private var bruttoDuration: TimeInterval {
        editStopTime.timeIntervalSince(editStartTime)
    }

    private var nettoDuration: TimeInterval {
        max(0, bruttoDuration - Double(pauseMinutes * 60))
    }

    private var isValid: Bool {
        editStopTime > editStartTime && bruttoDuration <= 86400 // max 24h
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Zeitraum") {
                    DatePicker("Datum", selection: $editDate, displayedComponents: .date)
                    DatePicker("Start", selection: $editStartTime, displayedComponents: .hourAndMinute)
                    DatePicker("Ende", selection: $editStopTime, displayedComponents: .hourAndMinute)
                    HStack {
                        Text("Pause (min)")
                        TextField("0", text: $editPauseMinutes)
                            .keyboardType(.numberPad)
                            .multilineTextAlignment(.trailing)
                    }
                }

                Section("Berechnung") {
                    HStack {
                        Text("Brutto")
                        Spacer()
                        Text(bruttoDuration.formattedHHMM).monospacedDigit()
                    }
                    HStack {
                        Text("Netto")
                        Spacer()
                        Text(nettoDuration.formattedHHMM).bold().monospacedDigit()
                    }
                }

                if let error = validationError {
                    Section {
                        Text(error).foregroundStyle(.red).font(.caption)
                    }
                }

                Section {
                    Button("Loeschen", role: .destructive) {
                        showDeleteConfirmation = true
                    }
                }
            }
            .navigationTitle("Session bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Speichern") {
                        onSave(editDate, editStartTime, editStopTime, pauseMinutes)
                        dismiss()
                    }
                    .disabled(!isValid)
                }
            }
            .confirmationDialog("Session loeschen?", isPresented: $showDeleteConfirmation) {
                Button("Loeschen", role: .destructive) {
                    onDelete()
                    dismiss()
                }
            }
            .onChange(of: editStopTime) { _, _ in validate() }
            .onChange(of: editStartTime) { _, _ in validate() }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }

    private func validate() {
        if editStopTime <= editStartTime {
            validationError = "Endzeit muss nach Startzeit liegen"
        } else if bruttoDuration > 86400 {
            validationError = "Dauer ueber 24 Stunden"
        } else {
            validationError = nil
        }
    }
}
```

### iOS: TimeTrackingView.swift (Zusammenbau)

```swift
struct TimeTrackingView: View {
    @Environment(\.modelContext) private var modelContext
    @Query(sort: \WorkSession.date, order: .reverse) private var sessions: [WorkSession]
    @State private var viewModel: TimeTrackingViewModel?
    @State private var selectedSession: WorkSession?

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 16) {
                    // Active Session Card
                    if let vm = viewModel {
                        ActiveSessionCard(
                            session: vm.activeSession,
                            isPaused: vm.isPaused,
                            onStart: { vm.startSession() },
                            onStop: { vm.stopSession() },
                            onFinish: { vm.finishSession() },
                            onPause: { vm.pauseSession() },
                            onResume: { vm.resumeSession() },
                            onSave: { d, s, e, p in vm.updateSession(date: d, startTime: s, stopTime: e, pauseMinutes: p) },
                            onDelete: {
                                if let s = vm.activeSession { vm.deleteSession(s) }
                            }
                        )
                    }

                    // History
                    if finishedSessions.isEmpty {
                        emptyState
                    } else {
                        ForEach(groupedByMonth, id: \.key) { month, monthSessions in
                            MonthGroup(
                                monthName: month,
                                sessions: monthSessions,
                                isCurrentMonth: month == Date().monthYearString,
                                onDeleteSession: { viewModel?.deleteSession($0) },
                                onSelectSession: { selectedSession = $0 }
                            )
                        }
                    }
                }
                .padding()
            }
            .navigationTitle("Zeiten")
            .refreshable {
                // Sync in E07
            }
            .sheet(item: $selectedSession) { session in
                SessionDetailSheet(
                    session: session,
                    onSave: { d, s, e, p in
                        viewModel?.updateSession(date: d, startTime: s, stopTime: e, pauseMinutes: p)
                    },
                    onDelete: { viewModel?.deleteSession(session) }
                )
            }
        }
        .onAppear {
            if viewModel == nil {
                viewModel = TimeTrackingViewModel(modelContext: modelContext)
            }
        }
    }

    private var finishedSessions: [WorkSession] {
        sessions.filter { $0.isFinished }
    }

    private var groupedByMonth: [(key: String, value: [WorkSession])] {
        Dictionary(grouping: finishedSessions) { $0.monthKey }
            .sorted { $0.key > $1.key }
    }

    @ViewBuilder
    private var emptyState: some View {
        VStack(spacing: 12) {
            Image(systemName: "clock")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)
            Text("Noch keine Eintraege")
                .font(.headline)
            Text("Starten Sie Ihre erste Arbeitssitzung!")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .padding(.vertical, 48)
    }
}
```

### Android: TimeTrackingScreen.kt (Zusammenbau, analog iOS)

```kotlin
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TimeTrackingScreen(services: ServiceContainer) {
    val viewModel: TimeTrackingViewModel = viewModel(
        factory = TimeTrackingViewModelFactory(services)
    )
    val activeSession by viewModel.activeSession.collectAsState()
    val sessions by viewModel.sessions.collectAsState(initial = emptyList())
    val isPaused by viewModel.isPaused.collectAsState()

    var selectedSession by remember { mutableStateOf<WorkSessionEntity?>(null) }
    val sheetState = rememberModalBottomSheetState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Zeiten") },
                actions = {
                    // Sync-Button Placeholder (E10)
                    IconButton(onClick = { /* E07 */ }) {
                        Icon(Icons.Default.Sync, contentDescription = "Sync")
                    }
                }
            )
        }
    ) { padding ->
        val finishedSessions = sessions.filter { it.isFinished }
        val grouped = finishedSessions.groupBy { it.monthKey }

        LazyColumn(
            modifier = Modifier.padding(padding),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            item {
                ActiveSessionCard(
                    session = activeSession,
                    isPaused = isPaused,
                    onStart = viewModel::startSession,
                    onStop = viewModel::stopSession,
                    onFinish = viewModel::finishSession,
                    onPause = { /* E08 */ },
                    onResume = { /* E08 */ }
                )
            }

            if (finishedSessions.isEmpty()) {
                item { EmptyState() }
            } else {
                grouped.forEach { (month, monthSessions) ->
                    item {
                        MonthGroup(
                            monthName = month,
                            sessions = monthSessions,
                            isCurrentMonth = month == DateFormatting.formatMonthYear(LocalDate.now()),
                            onDeleteSession = { viewModel.deleteSession(it) },
                            onSelectSession = { selectedSession = it }
                        )
                    }
                }
            }
        }
    }

    // Detail Sheet
    selectedSession?.let { session ->
        SessionDetailSheet(
            session = session,
            onDismiss = { selectedSession = null },
            onSave = { d, s, e, p ->
                viewModel.updateSession(d, s, e, p)
                selectedSession = null
            },
            onDelete = {
                viewModel.deleteSession(session)
                selectedSession = null
            }
        )
    }
}
```

---

## Datenfluss

```
@Query / Flow<List<WorkSessionEntity>>
    |
    v
TimeTrackingView / TimeTrackingScreen
    |
    | sessions.filter { isFinished }.groupBy { monthKey }
    v
MonthGroup (pro Monat)
    |
    | sessions im Monat
    v
SessionRow (pro Session) -- Tap -> selectedSession
    |                         |
    v                         v
Swipe-Delete              SessionDetailSheet
    |                         |
    v                         | onSave(date, startTime, stopTime, pauseMinutes)
ViewModel.deleteSession()     |
                              v
                         ViewModel.updateSession()
                              |
                              v
                         DB Update -> @Query/Flow emittiert neu -> UI aktualisiert
```

---

## Testbare Kriterien

- [ ] iOS: SessionRow zeigt korrektes Layout (Sync-Icon, Datum, Zeitraum, Netto-Dauer)
- [ ] iOS: SessionRow Netto-Berechnung: 9h Brutto - 30min Pause = 8:30h
- [ ] iOS: MonthGroup collapsed by default (ausser aktueller Monat)
- [ ] iOS: MonthGroup Gesamtdauer ist Summe der Netto-Dauern
- [ ] iOS: SessionDetailSheet validiert Endzeit > Startzeit
- [ ] iOS: TimeTrackingView zeigt leeren State wenn keine Sessions
- [ ] iOS: Swipe-Delete loescht Session
- [ ] Android: SessionRow mit SwipeToDismiss
- [ ] Android: MonthGroup AnimatedVisibility funktioniert
- [ ] Android: SessionDetailSheet ModalBottomSheet oeffnet/schliesst korrekt
- [ ] Android: LazyColumn mit ActiveSessionCard + MonthGroups scrollt fluessig

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| iOS: `sheet(item:)` feuert nicht bei WorkSession (Identifiable?) | Niedrig | WorkSession conformt zu Identifiable via `id: UUID` |
| Android: Nested LazyColumn (MonthGroup mit items) | Hoch | MonthGroup als regulaere Column, NICHT als LazyColumn -- items inline in aeussere LazyColumn |
| SwiftData @Query Performance bei 500+ Sessions | Niedrig | FetchLimit/Pagination bei Bedarf |
| iOS DisclosureGroup Styling limitiert | Mittel | Custom Toggle mit VStack statt DisclosureGroup |
