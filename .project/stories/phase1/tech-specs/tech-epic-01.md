# Tech-Spec: EPIC 01 -- Projekt-Setup & Infrastruktur

## Dateien die erstellt werden

### iOS (E01-S01: Xcode-Projekt)

| Datei | Zweck |
|-------|-------|
| `FakturusTrack/App/FakturusTrackApp.swift` | @main Entry Point, Auth-Check, Environment-Bindung |
| `FakturusTrack/App/AppState.swift` | Globaler App-Zustand |
| `FakturusTrack/App/ServiceContainer.swift` | Service-Lifecycle (onLogin/onLogout) |
| `FakturusTrack/App/Configuration.swift` | B2C-Config, API-URLs |
| `FakturusTrack.entitlements` | Keychain Sharing, Background Modes |

### iOS (E01-S03: Theme & Farben)

| Datei | Zweck |
|-------|-------|
| `FakturusTrack/Resources/Assets.xcassets` | Farbdefinitionen (Light+Dark), App-Icon Placeholder |
| `FakturusTrack/Extensions/Date+Formatting.swift` | Deutsche Datumsformate |
| `FakturusTrack/Extensions/TimeInterval+Display.swift` | Dauer-Anzeige (HH:MM:SS, HH:MMh) |

### Android (E01-S02: Android-Studio-Projekt)

| Datei | Zweck |
|-------|-------|
| `app/src/main/java/com/fakturus/track/FakturusTrackApp.kt` | Application-Klasse |
| `app/src/main/java/com/fakturus/track/MainActivity.kt` | Single Activity, Compose Host |
| `app/src/main/java/com/fakturus/track/ServiceContainer.kt` | Service-Lifecycle |
| `app/src/main/java/com/fakturus/track/Configuration.kt` | B2C-Config, API-URLs |
| `app/src/main/res/raw/auth_config.json` | MSAL B2C JSON-Konfiguration |
| `gradle/libs.versions.toml` | Version Catalog |

### Android (E01-S04: Theme & Farben)

| Datei | Zweck |
|-------|-------|
| `app/src/main/java/com/fakturus/track/ui/theme/Theme.kt` | Material 3 Color Scheme |
| `app/src/main/java/com/fakturus/track/ui/theme/Color.kt` | Named Colors |
| `app/src/main/java/com/fakturus/track/ui/theme/Type.kt` | Typography |
| `app/src/main/java/com/fakturus/track/util/DateFormatting.kt` | Deutsche Formate |

---

## Code-Skizzen

### iOS: Configuration.swift

```swift
enum Configuration {
    // API
    static let apiBaseUrl: String = {
        #if DEBUG
        return "https://localhost:7001"
        #else
        return "https://api.track.fakturus.com"
        #endif
    }()

    // Azure AD B2C
    static let b2cTenant = "fakturus.onmicrosoft.com"
    static let b2cClientId = "3fb35bc6-8825-495e-b0a2-18e00352f968"
    static let b2cPolicy = "B2C_1_BetaSignInOnly"
    static let b2cRedirectUri = "msauth.com.fakturus.track://auth"
    static let b2cScopes = [
        "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
    ]
    static let b2cAuthorityUrl =
        "https://fakturus.b2clogin.com/tfp/fakturus.onmicrosoft.com/B2C_1_BetaSignInOnly"
}
```

### iOS: ServiceContainer.swift

```swift
@Observable
final class ServiceContainer {
    let authManager = AuthManager()
    let networkMonitor = NetworkMonitor()

    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?

    func onLogin() {
        let client = APIClient(authManager: authManager)
        apiClient = client
        // SyncEngine wird in E07 erstellt, hier erstmal nil
        // syncEngine = SyncEngine(apiClient: client, networkMonitor: networkMonitor)
    }

    func onLogout() {
        apiClient = nil
        syncEngine = nil
    }
}
```

### iOS: FakturusTrackApp.swift (Grundgeruest)

```swift
@main
struct FakturusTrackApp: App {
    @State private var services = ServiceContainer()

    var body: some Scene {
        WindowGroup {
            // Auth-Check wird in E02 implementiert, erstmal leerer Screen
            Text("Fakturus Track")
                .environment(services.authManager)
                .environment(services.networkMonitor)
        }
    }
}
```

### iOS: Date+Formatting.swift

```swift
extension Date {
    func formatted(as format: String) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "de_DE")
        formatter.dateFormat = format
        return formatter.string(from: self)
    }

    var monthYearString: String {
        formatted(as: "MMMM yyyy")   // "Maerz 2026"
    }

    var weekdayShort: String {
        formatted(as: "EE")           // "Fr"
    }

    var dateShort: String {
        formatted(as: "dd.MM.yyyy")   // "29.03.2026"
    }

    var timeShort: String {
        formatted(as: "HH:mm")        // "08:30"
    }
}
```

### iOS: TimeInterval+Display.swift

```swift
extension TimeInterval {
    /// "03:42:18" (fuer laufenden Timer)
    var formattedHHMMSS: String {
        let total = Int(self)
        let h = total / 3600
        let m = (total % 3600) / 60
        let s = total % 60
        return String(format: "%02d:%02d:%02d", h, m, s)
    }

    /// "8:30h" (fuer Dauer-Anzeige)
    var formattedHHMM: String {
        let total = Int(self) / 60  // Minuten
        let h = total / 60
        let m = total % 60
        return m > 0 ? "\(h):\(String(format: "%02d", m))h" : "\(h)h"
    }
}
```

### Android: Configuration.kt

```kotlin
object Configuration {
    val apiBaseUrl: String
        get() = if (BuildConfig.DEBUG) "https://10.0.2.2:7001"
                else "https://api.track.fakturus.com"

    const val B2C_TENANT = "fakturus"
    const val B2C_CLIENT_ID = "3fb35bc6-8825-495e-b0a2-18e00352f968"
    const val B2C_POLICY = "B2C_1_BetaSignInOnly"
    val B2C_SCOPES = listOf(
        "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
    )
    val B2C_AUTHORITY_URL: String
        get() = "https://$B2C_TENANT.b2clogin.com/$B2C_TENANT.onmicrosoft.com/$B2C_POLICY"
}
```

### Android: ServiceContainer.kt

```kotlin
class ServiceContainer(private val context: Context) {
    val authManager = AuthManager(context)
    val networkMonitor = NetworkMonitor(context)

    // Lazy nach Login
    var apiClient: APIClient? = null
        private set
    var syncEngine: SyncEngine? = null
        private set

    // Database wird sofort erstellt (fuer Offline-First)
    val database: AppDatabase by lazy {
        Room.databaseBuilder(context, AppDatabase::class.java, "fakturus_track.db").build()
    }

    fun onLogin() {
        val client = APIClient(baseUrl = Configuration.apiBaseUrl, authManager = authManager)
        apiClient = client
        // SyncEngine in E07
    }

    fun onLogout() {
        apiClient = null
        syncEngine = null
    }
}
```

### Android: DateFormatting.kt

```kotlin
object DateFormatting {
    private val germanLocale = Locale.GERMAN

    fun formatDate(date: LocalDate): String =
        date.format(DateTimeFormatter.ofPattern("dd.MM.yyyy", germanLocale))

    fun formatTime(instant: Instant): String =
        instant.atZone(ZoneId.systemDefault())
            .format(DateTimeFormatter.ofPattern("HH:mm", germanLocale))

    fun formatMonthYear(date: LocalDate): String =
        date.format(DateTimeFormatter.ofPattern("MMMM yyyy", germanLocale))

    fun formatWeekdayShort(date: LocalDate): String =
        date.format(DateTimeFormatter.ofPattern("EE", germanLocale))

    fun formatDurationHHMMSS(durationMillis: Long): String {
        val total = durationMillis / 1000
        val h = total / 3600
        val m = (total % 3600) / 60
        val s = total % 60
        return "%02d:%02d:%02d".format(h, m, s)
    }

    fun formatDurationHHMM(durationMinutes: Long): String {
        val h = durationMinutes / 60
        val m = durationMinutes % 60
        return if (m > 0) "$h:%02dh".format(m) else "${h}h"
    }
}
```

---

## Datenfluss

E01 hat keinen echten Datenfluss -- es geht nur um Projektstruktur und Utilities. Die Dateien dieses EPICs sind Grundlage fuer alles Weitere.

---

## Testbare Kriterien

- [ ] iOS: `FakturusTrack.xcodeproj` oeffnet in Xcode ohne Fehler
- [ ] iOS: Build + Run auf Simulator zeigt leeren Screen
- [ ] iOS: MSAL SPM Package ist aufgeloest
- [ ] iOS: `Date().monthYearString` gibt z.B. "Maerz 2026" zurueck
- [ ] iOS: `TimeInterval(3723).formattedHHMMSS` gibt "01:02:03" zurueck
- [ ] Android: Projekt oeffnet in Android Studio ohne Gradle-Fehler
- [ ] Android: Build + Run auf Emulator zeigt leeren Compose-Screen
- [ ] Android: Alle Dependencies in Version Catalog aufgeloest
- [ ] Android: `DateFormatting.formatDate(LocalDate.now())` gibt "29.03.2026" zurueck
- [ ] Android: `DateFormatting.formatDurationHHMMSS(3723000)` gibt "01:02:03" zurueck

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| MSAL SPM-Paket hat Build-Fehler mit Xcode 16 | Niedrig | Spezifische MSAL-Version pinnen, Release Notes pruefen |
| Gradle-Dependency-Konflikte (Compose BOM vs. Room) | Niedrig | Version Catalog Versionen anpassen, BOM-Override |
| Swift 6 Strict Concurrency Warnungen in MSAL | Mittel | `@preconcurrency import` fuer MSAL, Warnings ignorieren |
| Android Emulator Performance | Niedrig | Hardware-Beschleunigung (HAXM/Hypervisor) sicherstellen |
