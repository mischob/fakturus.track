# Tech-Spec: EPIC 01 -- UI/UX Polish

## Uebersicht

Dark Mode, Haptics, Animationen, Loading States, Error Handling. Rein client-seitig, keine neuen Dateien fuer Dark Mode (bestehende Theme.swift/Theme.kt modifizieren), wenige neue Dateien fuer Haptics und Loading.

---

## S01/S02: Dark Mode (iOS + Android)

### iOS: Theme.swift ist bereits Dark-Mode-ready

Die bestehende `Theme.swift` nutzt bereits `Color(light:dark:)` fuer die meisten Farben. Hauptarbeit:

1. **Farb-Audit**: Alle Views durchgehen, hardcodierte Farben durch Theme-Referenzen ersetzen
2. **Color Assets**: Fehlende semantische Farben in Theme.swift ergaenzen
3. **App-weites Override**: `preferredColorScheme()` auf Root-View

```swift
// FakturusTrackApp.swift -- Ergaenzung
@AppStorage("appearance") private var appearance = "system"

var body: some Scene {
    WindowGroup {
        Group { /* bestehender Code */ }
            .preferredColorScheme(colorSchemeFor(appearance))
    }
}

private func colorSchemeFor(_ appearance: String) -> ColorScheme? {
    switch appearance {
    case "light": return .light
    case "dark": return .dark
    default: return nil
    }
}
```

### Android: Theme.kt erweitern

```kotlin
// Theme.kt -- Erweiterung
@Composable
fun FakturusTrackTheme(
    overrideAppearance: String = "system", // NEU: aus DataStore
    content: @Composable () -> Unit
) {
    val darkTheme = when (overrideAppearance) {
        "light" -> false
        "dark" -> true
        else -> isSystemInDarkTheme()
    }

    val colorScheme = if (darkTheme) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            dynamicDarkColorScheme(LocalContext.current)
        } else { DarkColorScheme }
    } else {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            dynamicLightColorScheme(LocalContext.current)
        } else { LightColorScheme }
    }

    MaterialTheme(colorScheme = colorScheme, typography = Typography) {
        content()
    }
}
```

---

## S03/S04: Haptic Feedback

### iOS: HapticManager.swift (NEU)

```swift
// Shared/HapticManager.swift
import UIKit

enum HapticManager {
    private static let impactLight = UIImpactFeedbackGenerator(style: .light)
    private static let impactMedium = UIImpactFeedbackGenerator(style: .medium)
    private static let impactHeavy = UIImpactFeedbackGenerator(style: .heavy)
    private static let selection = UISelectionFeedbackGenerator()
    private static let notification = UINotificationFeedbackGenerator()

    static func timerStart() {
        impactMedium.prepare()
        impactMedium.impactOccurred()
    }

    static func timerStop() {
        impactHeavy.prepare()
        impactHeavy.impactOccurred()
    }

    static func timerPauseResume() {
        impactLight.prepare()
        impactLight.impactOccurred()
    }

    static func sessionFinished() {
        notification.prepare()
        notification.notificationOccurred(.success)
    }

    static func sessionDeleted() {
        notification.prepare()
        notification.notificationOccurred(.warning)
    }

    static func toggle() {
        selection.prepare()
        selection.selectionChanged()
    }

    static func error() {
        notification.prepare()
        notification.notificationOccurred(.error)
    }
}
```

**Integration**: Aufrufe in `TimeTrackingViewModel.startSession()`, `.stopSession()` etc. und in Views bei Button-Actions.

### Android: HapticManager.kt (NEU)

```kotlin
// util/HapticManager.kt
import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import android.view.HapticFeedbackConstants
import android.view.View

object HapticManager {
    fun timerStart(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.CONFIRM)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }

    fun timerStop(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.REJECT)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }

    fun toggle(view: View) {
        view.performHapticFeedback(HapticFeedbackConstants.CLOCK_TICK)
    }

    fun error(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.REJECT)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }
}
```

**Integration in Compose**: `LocalView.current` fuer View-Referenz.

```kotlin
val view = LocalView.current
Button(onClick = {
    HapticManager.timerStart(view)
    viewModel.startSession()
}) { /* ... */ }
```

---

## S05/S06: Animationen

### iOS: Modifikation bestehender Views

Keine neuen Dateien. Animationen werden in bestehende Views integriert:

```swift
// ActiveSessionCard.swift -- Ergaenzung
// Timer-Start Animation
withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
    viewModel.startSession()
}

// Timer-Zaehler mit numericText Transition (iOS 17+)
Text(timerText)
    .contentTransition(.numericText())

// MonthGroup.swift -- Expand/Collapse
DisclosureGroup(isExpanded: $isExpanded) { /* content */ }
    .animation(.spring(response: 0.25), value: isExpanded)

// SessionRow.swift -- Swipe-to-Delete ist bereits nativ
// History-Liste mit Transition
.transition(.asymmetric(
    insertion: .move(edge: .top).combined(with: .opacity),
    removal: .move(edge: .trailing).combined(with: .opacity)
))
```

### Android: Compose Animationen

```kotlin
// ActiveSessionCard.kt -- Ergaenzung
AnimatedContent(
    targetState = activeSession,
    transitionSpec = {
        fadeIn(tween(300)) + slideInVertically { -it / 4 } togetherWith
            fadeOut(tween(200)) + slideOutVertically { it / 4 }
    }
) { session -> /* ... */ }

// MonthGroup.kt -- AnimatedVisibility ist bereits vorhanden, spring() hinzufuegen
AnimatedVisibility(
    visible = isExpanded,
    enter = expandVertically(animationSpec = spring(dampingRatio = 0.7f)),
    exit = shrinkVertically()
)
```

---

## S07/S08: Loading States & Skeletons

### iOS: ShimmerModifier.swift (NEU)

```swift
// Shared/ShimmerModifier.swift
import SwiftUI

struct ShimmerModifier: ViewModifier {
    @State private var phase: CGFloat = 0

    func body(content: Content) -> some View {
        content
            .redacted(reason: .placeholder)
            .overlay(
                LinearGradient(
                    colors: [.clear, .white.opacity(0.3), .clear],
                    startPoint: .leading,
                    endPoint: .trailing
                )
                .rotationEffect(.degrees(20))
                .offset(x: phase)
            )
            .clipped()
            .onAppear {
                withAnimation(.linear(duration: 1.5).repeatForever(autoreverses: false)) {
                    phase = 300
                }
            }
    }
}

extension View {
    func shimmer() -> some View {
        modifier(ShimmerModifier())
    }
}
```

**Verwendung**: `OverviewScreen` und `VacationScreen` bei `isLoading`:
```swift
if viewModel.isLoading && viewModel.summary == nil {
    OvertimeCard(title: "Placeholder", value: "---")
        .shimmer()
} else { /* echte Daten */ }
```

### Android: Shimmer Composable

```kotlin
// Kein Accompanist noetig -- eigene Shimmer-Implementierung
@Composable
fun ShimmerBox(modifier: Modifier = Modifier) {
    val shimmerColors = listOf(
        MaterialTheme.colorScheme.surfaceVariant,
        MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
        MaterialTheme.colorScheme.surfaceVariant
    )
    val transition = rememberInfiniteTransition(label = "shimmer")
    val translateAnim by transition.animateFloat(
        initialValue = 0f, targetValue = 1000f,
        animationSpec = infiniteRepeatable(tween(1200, easing = LinearEasing)),
        label = "shimmer"
    )
    val brush = Brush.linearGradient(
        colors = shimmerColors,
        start = Offset(translateAnim - 200f, 0f),
        end = Offset(translateAnim, 0f)
    )
    Box(modifier = modifier.background(brush, RoundedCornerShape(8.dp)))
}
```

---

## S09: Error Handling Konsistenz

### iOS: ErrorBanner.swift (NEU)

```swift
// Shared/ErrorBanner.swift
import SwiftUI

struct ErrorBanner: View {
    let message: String
    let action: (() -> Void)?
    let actionLabel: String?

    init(_ message: String, action: (() -> Void)? = nil, actionLabel: String? = nil) {
        self.message = message
        self.action = action
        self.actionLabel = actionLabel
    }

    var body: some View {
        HStack {
            Image(systemName: "exclamationmark.triangle.fill")
                .foregroundStyle(Theme.danger)
            Text(message)
                .font(.subheadline)
            Spacer()
            if let action, let label = actionLabel {
                Button(label, action: action)
                    .font(.subheadline.bold())
            }
        }
        .padding(12)
        .background(Theme.danger.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}
```

### Android: Snackbar-basiert (kein neues File)

Error-Handling nutzt `SnackbarHostState` im bestehenden Scaffold. Zentrales Mapping:

```kotlin
// In jedem Screen, der Errors anzeigt:
val snackbarHostState = remember { SnackbarHostState() }

LaunchedEffect(error) {
    error?.let {
        snackbarHostState.showSnackbar(
            message = it,
            actionLabel = "Erneut versuchen",
            duration = SnackbarDuration.Long
        )
    }
}
```

---

## Modifizierte bestehende Dateien (Zusammenfassung)

| Datei | Aenderung |
|-------|-----------|
| `Theme.swift` | Fehlende Dark-Varianten ergaenzen, Farb-Audit |
| `Theme.kt` / `Color.kt` | Dark-Varianten, dynamicColorScheme |
| `ActiveSessionCard.swift/kt` | +Animationen, +Haptics |
| `SessionRow.swift/kt` | +Transition Animationen |
| `MonthGroup.swift/kt` | +Spring Animation fuer Expand/Collapse |
| `TimeTrackingView.swift` | +ErrorBanner Integration |
| `TimeTrackingScreen.kt` | +Snackbar Error Handling |
| `OverviewScreen.swift/kt` | +Shimmer Loading State |
| `VacationScreen.swift/kt` | +Shimmer Loading State |
