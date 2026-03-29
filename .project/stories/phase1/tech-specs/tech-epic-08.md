# Tech-Spec: EPIC 08 -- Pausenerfassung

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Shared/ArbZGBanner.swift` | iOS | E08-S05 | 6h/9h/10h Hinweis-Banner |
| `ui/shared/ArbZGBanner.kt` | Android | E08-S06 | 6h/9h/10h Hinweis-Banner |

**Modifizierte Dateien:**
- `TimeTrackingViewModel.swift` (E08-S01: Pause-State + pauseSession/resumeSession)
- `TimeTrackingViewModel.kt` (E08-S02: analog)
- `ActiveSessionCard.swift` (E08-S03: Paused-State, Pause-Button aktivieren)
- `ActiveSessionCard.kt` (E08-S04: analog)
- `SessionRow.swift/kt` (E08-S07: Netto-Dauer bereits korrekt, MonthGroup Summe pruefen)
- `MonthGroup.swift/kt` (E08-S07: Gesamtdauer = Summe Netto)

---

## Code-Skizzen

### iOS: ViewModel-Erweiterung (E08-S01)

```swift
// Erweiterung von TimeTrackingViewModel (bereits in E05-S05 angelegt)

// Neue Properties (schon als Placeholder definiert):
// var isPaused = false
// var currentPauseStart: Date?
// var accumulatedPauseMinutes: Int = 0

func pauseSession() {
    guard let session = activeSession, session.isRunning, !isPaused else { return }
    isPaused = true
    currentPauseStart = Date()
}

func resumeSession() {
    guard isPaused, let pauseStart = currentPauseStart else { return }
    let pauseDuration = Date().timeIntervalSince(pauseStart)
    let pauseMinutes = Int(ceil(pauseDuration / 60.0))  // Aufrunden auf volle Minuten
    accumulatedPauseMinutes += pauseMinutes

    if let session = activeSession {
        session.pauseMinutes = accumulatedPauseMinutes
        session.updatedAt = Date()
        try? modelContext.save()
    }

    isPaused = false
    currentPauseStart = nil
}

// finishSession() Erweiterung (ueberschreibt bestehende Methode):
func finishSession() {
    guard let session = activeSession else { return }

    // Falls noch pausiert, Pause beenden und aufaddieren
    if isPaused { resumeSession() }

    if session.stopTime == nil { session.stopTime = Date() }
    session.isFinished = true
    session.isPendingSync = true
    session.updatedAt = Date()
    try? modelContext.save()

    activeSession = nil
    isPaused = false
    accumulatedPauseMinutes = 0
    currentPauseStart = nil
}
```

### iOS: ActiveSessionCard Paused-State (E08-S03)

```swift
// Neuer Zustand in ActiveSessionCard (zwischen Running und Stopped)

@ViewBuilder
private func pausedContent(_ session: WorkSession) -> some View {
    VStack(spacing: 16) {
        // Gelber Status
        HStack {
            Circle().fill(Color("timer-paused")).frame(width: 8, height: 8)
            Text("Pausiert").font(.subheadline).foregroundStyle(.secondary)
            Spacer()
        }

        // Pause-Timer (wie lange die aktuelle Pause dauert)
        if let pauseStart = currentPauseStart {
            TimerDisplay(startTime: pauseStart, isRunning: true, size: .medium)
                .foregroundStyle(Color("timer-paused"))
        }

        // Bisherige Gesamtpause
        if accumulatedPauseMinutes > 0 {
            Text("Pause bisher: \(accumulatedPauseMinutes) min")
                .font(.caption)
                .foregroundStyle(Color("pause"))
        }

        // Buttons
        HStack(spacing: 12) {
            Button("Weiter", systemImage: "play.fill") {
                UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                onResume()
            }
            .buttonStyle(.borderedProminent)

            Button("Fertig", systemImage: "checkmark.circle.fill") {
                UIImpactFeedbackGenerator(style: .medium).impactOccurred()
                onFinish()
            }
            .buttonStyle(.borderedProminent)
            .tint(.secondary)
        }
    }
    .padding()
}

// Running-State: Pause-Button aktivieren (war vorher disabled)
Button("Pause", systemImage: "pause.fill") {
    UIImpactFeedbackGenerator(style: .medium).impactOccurred()
    onPause()
}
.buttonStyle(.bordered)
.tint(Color("timer-paused"))
// .disabled(true) ENTFERNT -- jetzt aktiv!
```

### iOS: ArbZGBanner.swift (E08-S05)

```swift
struct ArbZGBanner: View {
    let netWorkMinutes: Int   // Netto-Arbeitsminuten (ohne Pause)
    let pauseMinutes: Int

    @State private var hasShown6h = false
    @State private var hasShown9h = false
    @State private var hasShown10h = false
    @State private var currentBanner: ArbZGHint?

    enum ArbZGHint: Identifiable {
        case sixHours, nineHours, tenHours

        var id: Self { self }

        var message: String {
            switch self {
            case .sixHours:
                return "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
            case .nineHours:
                return "Erinnerung: Nach 9 Stunden Arbeit betraegt die Mindestpause 45 Minuten."
            case .tenHours:
                return "Hinweis: Sie arbeiten seit 10 Stunden. Die gesetzliche Hoechstarbeitszeit betraegt 10 Stunden."
            }
        }
    }

    var body: some View {
        if let banner = currentBanner {
            HStack {
                Image(systemName: "exclamationmark.triangle.fill")
                    .foregroundStyle(.orange)
                Text(banner.message)
                    .font(.caption)
                Spacer()
                Button("OK") {
                    withAnimation { currentBanner = nil }
                }
                .font(.caption.bold())
            }
            .padding(12)
            .background(Color.orange.opacity(0.1))
            .clipShape(RoundedRectangle(cornerRadius: 8))
            .transition(.move(edge: .top).combined(with: .opacity))
        }
    }

    // Pruefe bei jedem Timer-Update
    func checkThresholds() {
        let netHours = Double(netWorkMinutes) / 60.0

        if netHours >= 10 && !hasShown10h {
            hasShown10h = true
            withAnimation { currentBanner = .tenHours }
        } else if netHours >= 9 && pauseMinutes < 45 && !hasShown9h {
            hasShown9h = true
            withAnimation { currentBanner = .nineHours }
        } else if netHours >= 6 && pauseMinutes < 30 && !hasShown6h {
            hasShown6h = true
            withAnimation { currentBanner = .sixHours }
        }
    }
}
```

### Android: ArbZGBanner.kt

```kotlin
@Composable
fun ArbZGBanner(
    netWorkMinutes: Long,
    pauseMinutes: Int,
    hasShown6h: Boolean,
    hasShown9h: Boolean,
    hasShown10h: Boolean,
    onDismiss: () -> Unit
) {
    val netHours = netWorkMinutes / 60.0
    val message = when {
        netHours >= 10 && !hasShown10h ->
            "Hinweis: Sie arbeiten seit 10 Stunden. Die gesetzliche Hoechstarbeitszeit betraegt 10 Stunden."
        netHours >= 9 && pauseMinutes < 45 && !hasShown9h ->
            "Erinnerung: Nach 9 Stunden Arbeit betraegt die Mindestpause 45 Minuten."
        netHours >= 6 && pauseMinutes < 30 && !hasShown6h ->
            "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
        else -> null
    }

    AnimatedVisibility(
        visible = message != null,
        enter = slideInVertically() + fadeIn(),
        exit = slideOutVertically() + fadeOut()
    ) {
        message?.let {
            Card(
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.3f)),
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp)
            ) {
                Row(
                    modifier = Modifier.padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(Icons.Default.Warning, null, tint = Color(0xFFFFA000))
                    Spacer(Modifier.width(8.dp))
                    Text(it, style = MaterialTheme.typography.bodySmall, modifier = Modifier.weight(1f))
                    TextButton(onClick = onDismiss) { Text("OK") }
                }
            }
        }
    }
}
```

---

## Datenfluss

```
Timer laeuft (TimelineView / LaunchedEffect)
    |
    v
Jede Sekunde: Netto-Arbeitsminuten berechnen
    = (now - startTime - aktuelle Pause - bisherige Pausen) / 60
    |
    v
ArbZGBanner.checkThresholds(netWorkMinutes, pauseMinutes)
    |
    +-- >= 6h && Pause < 30min -> Banner "30min Pause"
    +-- >= 9h && Pause < 45min -> Banner "45min Pause"
    +-- >= 10h                 -> Banner "Hoechstarbeitszeit"
    |
    v
Banner einmalig pro Session (hasShown-Flags)

---

User Tap "Pause"
    |
    v
ViewModel.pauseSession()
    +-- isPaused = true
    +-- currentPauseStart = Date.now
    |
    v
ActiveSessionCard wechselt zu Paused-State
    +-- Gelber Indikator
    +-- Pause-Timer laeuft
    +-- "Weiter" Button sichtbar

User Tap "Weiter"
    |
    v
ViewModel.resumeSession()
    +-- Pausendauer = now - currentPauseStart (aufgerundet)
    +-- accumulatedPauseMinutes += Pausendauer
    +-- session.pauseMinutes = accumulatedPauseMinutes
    +-- isPaused = false
    |
    v
ActiveSessionCard wechselt zurueck zu Running-State
    +-- Arbeits-Timer zeigt Netto-Zeit
```

---

## Testbare Kriterien

- [ ] iOS: pauseSession() setzt isPaused=true, currentPauseStart
- [ ] iOS: resumeSession() berechnet korrekte Pausenminuten (aufgerundet)
- [ ] iOS: Mehrere Pausen: 20min + 15min = 35min total
- [ ] iOS: finishSession() beendet offene Pause automatisch
- [ ] iOS: ArbZGBanner erscheint bei 6h mit < 30min Pause
- [ ] iOS: ArbZGBanner erscheint nur einmal pro Session
- [ ] iOS: Netto-Berechnung: 9h Brutto - 45min Pause = 8:15h
- [ ] Android: pauseSession/resumeSession analog korrekt
- [ ] Android: ArbZGBanner AnimatedVisibility funktioniert
- [ ] Beide: MonthGroup Gesamtdauer ist Summe der Netto-Dauern
- [ ] Beide: SessionRow zeigt "P30" wenn pauseMinutes=30

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| App wird waehrend Pause gekillt -- currentPauseStart verloren | Mittel | Nice-to-have: pauseStart in DB persistieren, bei App-Start wiederherstellen |
| Pause-Timer Drift (Minuten-Aufrundung bei kurzen Pausen) | Niedrig | Akzeptabel, dokumentiertes Verhalten |
| ArbZG-Hinweis nervt bei taeglicher 8h-Arbeit (6h Schwelle) | Niedrig | Banner ist dezent, einmalig, "OK" schliesst sofort |
| Pause-Button versehentlich getippt | Niedrig | Pause kann sofort mit "Weiter" beendet werden (0min aufgerundet auf 1min) |
