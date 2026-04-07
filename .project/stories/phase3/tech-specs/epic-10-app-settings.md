# Tech-Spec: EPIC 10 -- App-Einstellungen & Rechtliches

## Uebersicht

Erweiterung des bestehenden Settings-Screens um eine "APP"-Sektion. Neue Properties: Erscheinungsbild, Benachrichtigungen, Personalnummer, Version, Rechtliches. Keine neuen Screen-Dateien -- alles in bestehende SettingsView/SettingsScreen integriert.

---

## S01: iOS App-Einstellungen

### SettingsView.swift -- Neue Sektion

```swift
// In SettingsView.swift -- Neue Section unterhalb der bestehenden
Section("APP") {
    // Erscheinungsbild
    Picker(String(localized: "settings_appearance"), selection: $viewModel.appearance) {
        Text(String(localized: "settings_appearance_system")).tag("system")
        Text(String(localized: "settings_appearance_light")).tag("light")
        Text(String(localized: "settings_appearance_dark")).tag("dark")
    }

    // Benachrichtigungen (ArbZG-Hinweise)
    Toggle(String(localized: "settings_notifications"), isOn: $viewModel.notificationsEnabled)

    // Personalnummer (fuer DATEV-Export)
    HStack {
        Text(String(localized: "settings_personal_number"))
        Spacer()
        TextField("12345", text: $viewModel.personalNumber)
            .keyboardType(.numberPad)
            .multilineTextAlignment(.trailing)
            .frame(maxWidth: 120)
    }
}

Section("INFO") {
    // Version
    HStack {
        Text(String(localized: "settings_version"))
        Spacer()
        Text(viewModel.appVersion)
            .foregroundStyle(.secondary)
    }

    // Datenschutz
    Link(String(localized: "settings_privacy"),
         destination: URL(string: "https://track.fakturus.com/privacy")!)

    // Impressum
    Link(String(localized: "settings_imprint"),
         destination: URL(string: "https://track.fakturus.com/imprint")!)

    // Lizenzen
    NavigationLink(String(localized: "settings_licenses")) {
        LicensesView()
    }
}
```

### SettingsViewModel.swift -- Neue Properties

```swift
// Neue Properties in SettingsViewModel:

// Lokal gespeichert (nicht Backend-synced)
@AppStorage("appearance") var appearance: String = "system"
@AppStorage("notificationsEnabled") var notificationsEnabled: Bool = true

// In UserSettings gespeichert (Backend-synced via SyncEngine)
var personalNumber: String {
    get { settings?.personalNumber ?? "" }
    set {
        settings?.personalNumber = newValue
        settings?.updatedAt = Date()
        try? modelContext?.save()
        debouncedSync()
    }
}

var appVersion: String {
    let version = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "?"
    let build = Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "?"
    return "\(version) (\(build))"
}
```

### FakturusTrackApp.swift -- Appearance Anwendung

```swift
// Root-Level:
@AppStorage("appearance") private var appearance = "system"

var body: some Scene {
    WindowGroup {
        Group {
            // ... bestehender Auth/Sync Code ...
        }
        .preferredColorScheme(colorSchemeFor(appearance))
    }
}

private func colorSchemeFor(_ appearance: String) -> ColorScheme? {
    switch appearance {
    case "light": return .light
    case "dark": return .dark
    default: return nil  // System folgen
    }
}
```

---

## S02: Android App-Einstellungen

### SettingsScreen.kt -- Neue Sektion

```kotlin
// In SettingsScreen.kt -- Neue Section:

// APP Sektion
item {
    Text(
        text = "APP",
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(start = 16.dp, top = 24.dp, bottom = 8.dp)
    )
}

// Erscheinungsbild
item {
    var expanded by remember { mutableStateOf(false) }
    ListItem(
        headlineContent = { Text(stringResource(R.string.settings_appearance)) },
        trailingContent = {
            TextButton(onClick = { expanded = true }) {
                Text(when (appearance) {
                    "light" -> stringResource(R.string.settings_appearance_light)
                    "dark" -> stringResource(R.string.settings_appearance_dark)
                    else -> stringResource(R.string.settings_appearance_system)
                })
            }
            DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                DropdownMenuItem(
                    text = { Text(stringResource(R.string.settings_appearance_system)) },
                    onClick = { viewModel.setAppearance("system"); expanded = false }
                )
                DropdownMenuItem(
                    text = { Text(stringResource(R.string.settings_appearance_light)) },
                    onClick = { viewModel.setAppearance("light"); expanded = false }
                )
                DropdownMenuItem(
                    text = { Text(stringResource(R.string.settings_appearance_dark)) },
                    onClick = { viewModel.setAppearance("dark"); expanded = false }
                )
            }
        }
    )
}

// Benachrichtigungen Toggle
item {
    ListItem(
        headlineContent = { Text(stringResource(R.string.settings_notifications)) },
        trailingContent = {
            Switch(checked = notificationsEnabled, onCheckedChange = { viewModel.setNotifications(it) })
        }
    )
}

// Personalnummer
item {
    ListItem(
        headlineContent = { Text(stringResource(R.string.settings_personal_number)) },
        trailingContent = {
            OutlinedTextField(
                value = personalNumber,
                onValueChange = { viewModel.setPersonalNumber(it) },
                placeholder = { Text("12345") },
                singleLine = true,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                modifier = Modifier.width(120.dp)
            )
        }
    )
}

// INFO Sektion
item {
    Text(
        text = "INFO",
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(start = 16.dp, top = 24.dp, bottom = 8.dp)
    )
}

// Version
item {
    ListItem(
        headlineContent = { Text(stringResource(R.string.settings_version)) },
        trailingContent = {
            Text(
                text = viewModel.appVersion,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    )
}

// Datenschutz + Impressum + Lizenzen (als Clickable Items mit CustomTabs)
item {
    val uriHandler = LocalUriHandler.current
    ListItem(
        headlineContent = { Text(stringResource(R.string.settings_privacy)) },
        trailingContent = { Icon(Icons.AutoMirrored.Default.OpenInNew, null) },
        modifier = Modifier.clickable {
            uriHandler.openUri("https://track.fakturus.com/privacy")
        }
    )
}
```

### SettingsViewModel.kt -- Neue Properties

```kotlin
// Neue Properties:

// Lokal in DataStore (nicht Backend-synced)
private val _appearance = MutableStateFlow("system")
val appearance: StateFlow<String> = _appearance.asStateFlow()

private val _notificationsEnabled = MutableStateFlow(true)
val notificationsEnabled: StateFlow<Boolean> = _notificationsEnabled.asStateFlow()

fun setAppearance(value: String) {
    _appearance.value = value
    viewModelScope.launch {
        // In DataStore speichern
        context.settingsDataStore.edit { it[stringPreferencesKey("appearance")] = value }
    }
}

fun setNotifications(enabled: Boolean) {
    _notificationsEnabled.value = enabled
    viewModelScope.launch {
        context.settingsDataStore.edit { it[booleanPreferencesKey("notifications")] = enabled }
    }
}

fun setPersonalNumber(number: String) {
    viewModelScope.launch {
        val settings = database.userSettingsDao().getSettingsOnce() ?: return@launch
        val updated = settings.copy(
            personalNumber = number,
            updatedAt = Instant.now().toString()
        )
        database.userSettingsDao().update(updated)
    }
}

val appVersion: String
    get() = "${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})"
```

### Theme.kt -- Appearance aus DataStore lesen

```kotlin
// In MainActivity.kt oder Theme.kt:
@Composable
fun FakturusTrackThemeWrapper(content: @Composable () -> Unit) {
    val context = LocalContext.current
    val appearance by context.settingsDataStore.data
        .map { it[stringPreferencesKey("appearance")] ?: "system" }
        .collectAsState(initial = "system")

    FakturusTrackTheme(overrideAppearance = appearance) {
        content()
    }
}
```

---

## S03: Datenschutzerklaerung & Impressum

### Web-Inhalte (nicht Teil der App-Codebasis)

Die URLs muessen vor Store-Einreichung erreichbar sein:
- `https://track.fakturus.com/privacy` -- Datenschutzerklaerung
- `https://track.fakturus.com/imprint` -- Impressum

### Offline-Fallback (optional)

Falls die URLs noch nicht bereit sind, koennen Markdown-Dateien gebundled werden:

```swift
// iOS: Als Bundle-Resource
if let url = Bundle.main.url(forResource: "privacy", withExtension: "md") {
    // In WebView oder als formatierten Text anzeigen
}
```

```kotlin
// Android: Als raw Resource
val privacyText = context.resources.openRawResource(R.raw.privacy)
    .bufferedReader().readText()
```

### Lizenzen-Screen (Minimal)

```swift
// iOS: Einfache Liste
struct LicensesView: View {
    var body: some View {
        List {
            Section("Microsoft Authentication Library (MSAL)") {
                Text("MIT License")
                Text("Copyright (c) Microsoft Corporation")
            }
        }
        .navigationTitle(String(localized: "settings_licenses"))
    }
}
```

```kotlin
// Android: Einfache LazyColumn oder oss-licenses-plugin
// Fuer MVP reicht eine einfache Liste der verwendeten Bibliotheken
```

---

## Zusammenfassung modifizierter Dateien

| Datei | Aenderung |
|-------|-----------|
| `SettingsView.swift` | +APP-Sektion, +INFO-Sektion |
| `SettingsViewModel.swift` | +appearance, +notificationsEnabled, +personalNumber, +appVersion |
| `FakturusTrackApp.swift` | +preferredColorScheme(appearance) |
| `UserSettings.swift` | +personalNumber: String? |
| `SettingsScreen.kt` | +APP-Sektion, +INFO-Sektion |
| `SettingsViewModel.kt` | +appearance, +notificationsEnabled, +personalNumber, +appVersion |
| `Theme.kt` / `MainActivity.kt` | +FakturusTrackThemeWrapper mit DataStore |
| `UserSettingsEntity` | +personalNumber: String? |
| `ServiceContainer.kt` | +MIGRATION_4_5 |
