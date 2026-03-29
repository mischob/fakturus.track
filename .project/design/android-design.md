# Android Design -- Material Design 3 Konform

## Navigation

### Bottom Navigation Bar (Material 3)

```
┌─────────────────────────────────────────────┐
│                                             │
│              [Content Area]                 │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  🕐 Zeiten   ☀️ Urlaub   📊 Gesamt   ⚙️ Settings │
│  ━━━━━━━━                                   │
│  (Indicator)                                │
└─────────────────────────────────────────────┘
```

Material 3 NavigationBar:
- 80dp Hoehe
- Active Indicator (Pill-Shape) unter aktivem Icon
- Filled Icon fuer aktiven Tab, Outlined fuer inaktiv
- Label immer sichtbar (nicht nur aktiv)
- Dynamic Color unterstuetzung (Material You)

### Top App Bar

- Medium Top App Bar (collapsed -> expanded)
- Titel linksbündig
- Keine Navigation-Icons auf Top-Level
- Optional: Action Icons rechts (Sync, Filter)

---

## Material 3 Patterns

### 1. Cards

Material 3 ElevatedCard fuer Daten-Container:

```kotlin
ElevatedCard(
    modifier = Modifier.fillMaxWidth(),
    elevation = CardDefaults.elevatedCardElevation(defaultElevation = 1.dp)
) {
    // Content
}
```

### 2. Bottom Sheet

Fuer Session-Bearbeitung:

```kotlin
ModalBottomSheet(
    onDismissRequest = { /* ... */ },
    sheetState = sheetState,
    dragHandle = { BottomSheetDefaults.DragHandle() }
) {
    SessionDetailContent(session = session)
}
```

### 3. Swipe to Dismiss

```kotlin
SwipeToDismissBox(
    state = dismissState,
    backgroundContent = {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.errorContainer),
            contentAlignment = Alignment.CenterEnd
        ) {
            Icon(Icons.Default.Delete, "Loeschen",
                modifier = Modifier.padding(16.dp))
        }
    }
) {
    SessionRow(session = session)
}
```

### 4. Pull to Refresh

```kotlin
val pullRefreshState = rememberPullToRefreshState()
PullToRefreshBox(
    state = pullRefreshState,
    isRefreshing = isSyncing,
    onRefresh = { viewModel.sync() }
) {
    LazyColumn { /* Session List */ }
}
```

### 5. Snackbar mit Undo

```kotlin
val snackbarHostState = remember { SnackbarHostState() }
Scaffold(snackbarHost = { SnackbarHost(snackbarHostState) }) {
    // Bei Loeschung:
    val result = snackbarHostState.showSnackbar(
        message = "Session geloescht",
        actionLabel = "Rueckgaengig",
        duration = SnackbarDuration.Short
    )
    if (result == SnackbarResult.ActionPerformed) {
        viewModel.undoDelete()
    }
}
```

---

## Komponenten-Spezifikation (Android)

### Active Session Card

```
┌────────────────────────────────────────┐
│  ● Laufende Sitzung                   │
│                                        │
│           03:42:18                     │
│                                        │
│  Start    08:30                        │
│  Ende     --:--                        │
│  Datum    29.03.2026                   │
│                                        │
│  ┌──────────┐  ┌──────────┐           │
│  │  ■ Stop  │  │ ✓ Fertig │           │
│  └──────────┘  └──────────┘           │
│  (FilledTonalButton) (FilledButton)    │
└────────────────────────────────────────┘
```

- ElevatedCard mit Material 3 Elevation
- Timer in `displayMedium` Typography (monospaced)
- Gruener Indikator-Punkt (animiert)
- Buttons: FilledButton (Fertig) und FilledTonalButton (Stop)
- Minimum Touch Target: 48dp (Material Standard)

### Idle State

```
┌────────────────────────────────────────┐
│                                        │
│  Bereit fuer den naechsten Eintrag     │
│                                        │
│    ┌────────────────────────────┐      │
│    │    ▶ Starten               │      │
│    │    (ExtendedFAB Style)     │      │
│    └────────────────────────────┘      │
│                                        │
└────────────────────────────────────────┘
```

- ExtendedFloatingActionButton oder grosser FilledButton
- Primary Container Color
- Play-Icon + Text

### Month Group (Sticky Header)

```
┌ Maerz 2026 ──────────── 42:18h ──── ⌄ ┐
├────────────────────────────────────────┤
│  Fr 29.03.  08:30 - 17:00   8:30h   │
│  Do 28.03.  09:00 - 17:30   8:30h   │
│  Mi 27.03.  08:00 - 16:15   8:15h   │
└────────────────────────────────────────┘
```

- LazyColumn mit `stickyHeader { }` (experimentell) oder Card-basiert
- Material 3 ListItem fuer einzelne Sessions
- Expandable: AnimatedVisibility

### Session Row (ListItem)

```kotlin
ListItem(
    headlineContent = { Text("08:30 - 17:00") },
    supportingContent = { Text("Fr 29.03.2026") },
    trailingContent = { Text("8:30h", fontWeight = FontWeight.Bold) },
    leadingContent = {
        if (session.isSynced) {
            Icon(Icons.Default.CloudDone, "Synchronisiert")
        } else {
            Icon(Icons.Default.CloudUpload, "Ausstehend")
        }
    }
)
```

### Overtime Summary Cards

Horizontal Scrollable Row mit Material 3 Cards:

```kotlin
LazyRow(
    horizontalArrangement = Arrangement.spacedBy(12.dp),
    contentPadding = PaddingValues(horizontal = 16.dp)
) {
    item { OvertimeCard(title = "Ueberstunden", value = "+12:30h", color = colorSuccess) }
    item { OvertimeCard(title = "Urlaub", value = "5 / 30", subtitle = "25 uebrig") }
    item { OvertimeCard(title = "Feiertage", value = "11", subtitle = "in 2026") }
}
```

---

## Material 3 Theme

### Color Scheme

```kotlin
private val LightColorScheme = lightColorScheme(
    primary = Color(0xFF1A5CFF),
    onPrimary = Color.White,
    primaryContainer = Color(0xFFEBF0FF),
    onPrimaryContainer = Color(0xFF0D1F4A),
    secondary = Color(0xFF6B7280),
    onSecondary = Color.White,
    secondaryContainer = Color(0xFFF3F4F6),
    surface = Color.White,
    surfaceVariant = Color(0xFFF3F4F6),
    background = Color(0xFFF9FAFB),
    error = Color(0xFFE5383B),
    errorContainer = Color(0xFFFDECEC),
)

private val DarkColorScheme = darkColorScheme(
    primary = Color(0xFF4D8AFF),
    primaryContainer = Color(0xFF1A2B5A),
    surface = Color(0xFF1A1D27),
    background = Color(0xFF0F1117),
    // ...
)
```

### Dynamic Color (Material You)

```kotlin
@Composable
fun FakturusTrackTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    dynamicColor: Boolean = true,
    content: @Composable () -> Unit
) {
    val colorScheme = when {
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            if (darkTheme) dynamicDarkColorScheme(LocalContext.current)
            else dynamicLightColorScheme(LocalContext.current)
        }
        darkTheme -> DarkColorScheme
        else -> LightColorScheme
    }
    MaterialTheme(colorScheme = colorScheme, content = content)
}
```

### Typography

```kotlin
val TrackTypography = Typography(
    displayMedium = TextStyle(
        fontFamily = FontFamily.Monospace,
        fontWeight = FontWeight.Bold,
        fontSize = 48.sp,
        // Fuer Timer
    ),
    titleLarge = TextStyle(
        fontWeight = FontWeight.Bold,
        fontSize = 22.sp,
    ),
    bodyLarge = TextStyle(
        fontSize = 16.sp,
    ),
    bodyMedium = TextStyle(
        fontSize = 14.sp,
    ),
    labelSmall = TextStyle(
        fontSize = 12.sp,
        color = Color(0xFF6B7280),
    ),
)
```

---

## Android-spezifische Features

### Home Screen Widget (Phase 3)

Glance AppWidget:

```kotlin
class TrackWidget : GlanceAppWidget() {
    override suspend fun provideGlance(context: Context, id: GlanceId) {
        provideContent {
            Column(modifier = GlanceModifier.fillMaxSize().padding(16.dp)) {
                Text("Fakturus Track", style = TextStyle(fontWeight = FontWeight.Bold))
                Text("Heute: 6:30h")
                if (isRunning) {
                    Text("🟢 03:42:18", style = TextStyle(color = ColorProvider(Color.Green)))
                }
                Button(text = if (isRunning) "Stop" else "Starten",
                    onClick = actionRunCallback<ToggleSessionAction>())
            }
        }
    }
}
```

### App Shortcuts (Phase 3)

```xml
<shortcuts xmlns:android="http://schemas.android.com/apk/res/android">
    <shortcut android:shortcutId="start_session"
              android:shortcutShortLabel="@string/start_session"
              android:icon="@drawable/ic_play">
        <intent android:action="com.fakturus.track.START_SESSION"
                android:targetPackage="com.fakturus.track"
                android:targetClass="com.fakturus.track.MainActivity" />
    </shortcut>
</shortcuts>
```

### Edge-to-Edge

```kotlin
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        setContent {
            FakturusTrackTheme {
                Scaffold(
                    modifier = Modifier.fillMaxSize(),
                    contentWindowInsets = ScaffoldDefaults.contentWindowInsets
                ) { innerPadding ->
                    // Content mit innerPadding
                }
            }
        }
    }
}
```

---

## Accessibility (Android)

### TalkBack
- Alle Composables haben `contentDescription` via `semantics { }`
- Timer: `"Laufende Sitzung: drei Stunden, zweiundvierzig Minuten"`
- Custom Actions fuer Swipe-Elemente

### Font Scale
- Alle Texte verwenden `sp` (skalierbar)
- Layout mit `fillMaxWidth` statt festen Breiten
- Testen mit 200% Font Scale

### Kontraste
- Material 3 Color Roles garantieren WCAG AA
- Custom Colors muessen manuell geprueft werden

### Beispiel
```kotlin
Text(
    text = "03:42:18",
    style = MaterialTheme.typography.displayMedium,
    modifier = Modifier.semantics {
        contentDescription = "Laufende Sitzung: drei Stunden und zweiundvierzig Minuten"
        liveRegion = LiveRegionMode.Polite
    }
)
```
