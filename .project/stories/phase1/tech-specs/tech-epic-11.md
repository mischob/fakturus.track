# Tech-Spec: EPIC 11 -- Offline-Login & Session-Persistierung

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Services/Auth/OfflineSessionManager.swift` | iOS | E11-S01 | Keychain-basierte Session-Persistierung |
| `services/auth/OfflineSessionManager.kt` | Android | E11-S01 | EncryptedSharedPreferences Session |
| `Models/OfflineSession.swift` | iOS | E11-S01 | Session-Datenmodell |
| `models/OfflineSession.kt` | Android | E11-S01 | Session-Datenmodell |

**Modifizierte Dateien:**
- `Services/Auth/AuthManager.swift` (Session schreiben nach Login/Refresh, `resolveStartState()`, Offline-Modus)
- `services/auth/AuthManager.kt` (dto.)
- `Services/Network/NetworkMonitor.swift` (Hintergrund-Refresh bei offline->online Wechsel)
- `services/network/NetworkMonitor.kt` (dto.)
- `Features/Auth/LoginView.swift` (Offline-Hinweise, deaktivierte Buttons)
- `features/auth/LoginScreen.kt` (dto.)
- `FakturusTrackApp.swift` (resolveStartState() beim App-Start aufrufen)
- `MainActivity.kt` (dto.)

**Architektur-Entscheidung**: Kein separater `AppStartCoordinator`. Die Entscheidungslogik wird als `resolveStartState()` direkt auf dem AuthManager implementiert. Begruendung: ADR-002 (kein Clean Architecture) und ADR-006 (MVVM ohne UseCases) -- keine separaten Coordinator/Orchestrator-Klassen.

---

## Architektur-Uebersicht

```
App-Start
    |
    v
NetworkMonitor.isConnected (sofort, <500ms)
    |
    +-- Online --> MSAL Token Cache pruefen
    |                 |
    |                 +-- Cache Hit (gueltig) --> App oeffnen, Session aktualisieren
    |                 +-- Cache Miss --> acquireTokenSilently() (mit Netzwerk)
    |                                       |
    |                                       +-- Erfolg --> App oeffnen, Session aktualisieren
    |                                       +-- Fehler --> InteractiveLogin()
    |
    +-- Offline --> MSAL Token Cache pruefen (NUR lokal, KEIN Netzwerk!)
                      |
                      +-- Cache Hit (Access Token noch gueltig) --> App oeffnen (Offline-Modus)
                      +-- Cache Miss/Expired --> OfflineSession pruefen
                                                   |
                                                   +-- Gueltig (<14 Tage) --> App oeffnen (Offline-Modus)
                                                   +-- Abgelaufen/Fehlt --> Login-Screen + Offline-Hinweis
```

**WICHTIG**: Im Offline-Pfad darf KEIN MSAL-Netzwerkaufruf erfolgen. `acquireTokenSilently()` darf nur mit `forceRefresh = false` aufgerufen werden, sodass nur der lokale Cache geprueft wird. Wenn der Cache leer oder expired ist, wird direkt zur OfflineSession-Pruefung gesprungen. Begruendung: MSAL-interne Netzwerk-Timeouts koennen 30+ Sekunden dauern und die 1-Sekunden-Garantie brechen.

---

## Datenmodell

### OfflineSession (minimiert -- nur das Noetigste)

Die Session enthaelt absichtlich keine PII (kein displayName, keine email, kein loginProvider). Diese Daten koennen nach dem Offline-Start aus der lokalen SQLite-Datenbank geladen werden. Weniger PII im sicheren Speicher = kleinere Angriffsflaeche.

```swift
// iOS
struct OfflineSession: Codable {
    let userId: String          // MSAL Account OID
    let lastSuccessfulAuth: Date // Letzter erfolgreicher Token-Erhalt
    
    var isValid: Bool {
        let maxOfflineDays = 14.0
        let daysSinceAuth = Date().timeIntervalSince(lastSuccessfulAuth) / 86400
        return daysSinceAuth <= maxOfflineDays
    }
    
    var daysUntilExpiry: Int {
        let daysSinceAuth = Date().timeIntervalSince(lastSuccessfulAuth) / 86400
        return max(0, Int(14.0 - daysSinceAuth))
    }
}
```

```kotlin
// Android
@Serializable
data class OfflineSession(
    val userId: String,
    val lastSuccessfulAuthEpochMillis: Long
) {
    val isValid: Boolean
        get() {
            val maxOfflineDays = 14
            val daysSinceAuth = (System.currentTimeMillis() - lastSuccessfulAuthEpochMillis) / 86_400_000
            return daysSinceAuth <= maxOfflineDays
        }
    
    val daysUntilExpiry: Int
        get() {
            val daysSinceAuth = (System.currentTimeMillis() - lastSuccessfulAuthEpochMillis) / 86_400_000
            return maxOf(0, (14 - daysSinceAuth).toInt())
        }
}
```

---

## Code-Skizzen

### iOS: OfflineSessionManager.swift

```swift
import Foundation
import Security

final class OfflineSessionManager {
    private let service = "com.fakturus.track.offline-session"
    private let account = "current-session"
    
    func save(_ session: OfflineSession) throws {
        let data = try JSONEncoder().encode(session)
        
        // Erst loeschen falls vorhanden
        let deleteQuery: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account
        ]
        SecItemDelete(deleteQuery as CFDictionary)
        
        // Neu anlegen
        let addQuery: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleAfterFirstUnlock
        ]
        
        let status = SecItemAdd(addQuery as CFDictionary, nil)
        guard status == errSecSuccess else {
            throw OfflineSessionError.saveFailed(status)
        }
    }
    
    func load() -> OfflineSession? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ]
        
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        
        guard status == errSecSuccess, let data = result as? Data else {
            return nil
        }
        
        return try? JSONDecoder().decode(OfflineSession.self, from: data)
    }
    
    func delete() {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account
        ]
        SecItemDelete(query as CFDictionary)
    }
}

enum OfflineSessionError: Error {
    case saveFailed(OSStatus)
}
```

### Android: OfflineSessionManager.kt

```kotlin
import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class OfflineSessionManager(context: Context) {
    private val masterKey = MasterKey.Builder(context)
        .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
        .build()
    
    private val prefs = EncryptedSharedPreferences.create(
        context,
        "offline_session_prefs",
        masterKey,
        EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
        EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
    )
    
    fun save(session: OfflineSession) {
        val json = Json.encodeToString(session)
        prefs.edit().putString(KEY_SESSION, json).apply()
    }
    
    fun load(): OfflineSession? {
        val json = prefs.getString(KEY_SESSION, null) ?: return null
        return try {
            Json.decodeFromString<OfflineSession>(json)
        } catch (_: Exception) {
            null
        }
    }
    
    fun delete() {
        prefs.edit().remove(KEY_SESSION).apply()
    }
    
    companion object {
        private const val KEY_SESSION = "current_session"
    }
}
```

### AuthManager-Erweiterung: resolveStartState()

Die Entscheidungslogik wird direkt als Methode auf dem AuthManager implementiert (kein separater Coordinator).

```swift
// iOS: AuthManager.swift -- Ergaenzungen

enum AppStartResult {
    case authenticated          // Online, Token gueltig
    case offlineWithSession     // Offline, lokale Session gueltig
    case loginRequired          // Kein Token, keine Session, oder Session abgelaufen
    case loginRequiredNoNetwork // Login noetig, aber kein Netz
}

enum LoginContext {
    case normal            // Standard-Login (erstes Mal oder regulaer)
    case sessionExpired    // Session abgelaufen, muss neu einloggen
    case firstLogin        // Allererstes Mal, noch nie eingeloggt
}

extension AuthManager {
    // Neue Properties
    var isOfflineMode: Bool = false
    var offlineUserId: String?
    
    func resolveStartState(
        networkMonitor: NetworkMonitor,
        sessionManager: OfflineSessionManager
    ) async -> (result: AppStartResult, context: LoginContext) {
        let isOnline = networkMonitor.isConnected
        
        if isOnline {
            // Online-Pfad: MSAL darf Netzwerk nutzen
            if let account = msalApp?.allAccounts()?.first {
                do {
                    let _ = try await acquireTokenSilently()
                    updateOfflineSession(sessionManager: sessionManager)
                    isAuthenticated = true
                    return (.authenticated, .normal)
                } catch {
                    // Silent Refresh fehlgeschlagen -> Interactive Login
                    return (.loginRequired, .normal)
                }
            }
            return (.loginRequired, .normal)
        }
        
        // Offline-Pfad: KEIN MSAL-Netzwerkaufruf!
        // Nur lokalen Cache pruefen (Access Token noch gueltig?)
        if let _ = accessToken, let expiry = tokenExpiry, expiry > Date() {
            // Cached Access Token noch gueltig
            isAuthenticated = true
            isOfflineMode = true
            return (.offlineWithSession, .normal)
        }
        
        // Access Token abgelaufen/nicht vorhanden -> Offline-Session pruefen
        if let session = sessionManager.load() {
            if session.isValid {
                isAuthenticated = true
                isOfflineMode = true
                offlineUserId = session.userId
                return (.offlineWithSession, .normal)
            } else {
                return (.loginRequiredNoNetwork, .sessionExpired)
            }
        }
        
        // Noch nie eingeloggt
        return (.loginRequiredNoNetwork, .firstLogin)
    }
    
    /// Setzt den Offline-Modus basierend auf lokaler Session
    func setOfflineMode(userId: String) {
        isAuthenticated = true
        isOfflineMode = true
        offlineUserId = userId
    }
    
    private func updateOfflineSession(sessionManager: OfflineSessionManager) {
        guard let account = currentAccount else { return }
        let session = OfflineSession(
            userId: account.identifier ?? "",
            lastSuccessfulAuth: Date()
        )
        try? sessionManager.save(session)
    }
}
```

```kotlin
// Android: AuthManager.kt -- Ergaenzungen

enum class AppStartResult {
    AUTHENTICATED,
    OFFLINE_WITH_SESSION,
    LOGIN_REQUIRED,
    LOGIN_REQUIRED_NO_NETWORK
}

enum class LoginContext {
    NORMAL, SESSION_EXPIRED, FIRST_LOGIN
}

// Neue Properties in AuthManager
private val _isOfflineMode = MutableStateFlow(false)
val isOfflineMode: StateFlow<Boolean> = _isOfflineMode.asStateFlow()
private var offlineUserId: String? = null

suspend fun resolveStartState(
    networkMonitor: NetworkMonitor,
    sessionManager: OfflineSessionManager
): Pair<AppStartResult, LoginContext> {
    val isOnline = networkMonitor.isConnected.value
    
    if (isOnline) {
        val account = msalApp.getAccount()
        if (account != null) {
            return try {
                acquireTokenSilently()
                updateOfflineSession(sessionManager)
                _isAuthenticated.value = true
                AppStartResult.AUTHENTICATED to LoginContext.NORMAL
            } catch (_: Exception) {
                AppStartResult.LOGIN_REQUIRED to LoginContext.NORMAL
            }
        }
        return AppStartResult.LOGIN_REQUIRED to LoginContext.NORMAL
    }
    
    // Offline-Pfad: KEIN MSAL-Netzwerkaufruf!
    // Nur pruefen ob cached Access Token noch gueltig
    if (cachedAccessToken != null && tokenExpiry?.let { it > System.currentTimeMillis() } == true) {
        _isAuthenticated.value = true
        _isOfflineMode.value = true
        return AppStartResult.OFFLINE_WITH_SESSION to LoginContext.NORMAL
    }
    
    // Offline-Session pruefen
    val session = sessionManager.load()
    if (session != null) {
        return if (session.isValid) {
            _isAuthenticated.value = true
            _isOfflineMode.value = true
            offlineUserId = session.userId
            AppStartResult.OFFLINE_WITH_SESSION to LoginContext.NORMAL
        } else {
            AppStartResult.LOGIN_REQUIRED_NO_NETWORK to LoginContext.SESSION_EXPIRED
        }
    }
    
    return AppStartResult.LOGIN_REQUIRED_NO_NETWORK to LoginContext.FIRST_LOGIN
}

fun setOfflineMode(userId: String) {
    _isAuthenticated.value = true
    _isOfflineMode.value = true
    offlineUserId = userId
}

private fun updateOfflineSession(sessionManager: OfflineSessionManager) {
    val account = msalApp.getAccount() ?: return
    val session = OfflineSession(
        userId = account.id ?: "",
        lastSuccessfulAuthEpochMillis = System.currentTimeMillis()
    )
    sessionManager.save(session)
}
```

### Login-Screen Erweiterung (Auszug)

```swift
// iOS: LoginView.swift -- Ergaenzungen

@Environment(NetworkMonitor.self) private var networkMonitor

// Im body, ueber den Login-Buttons:
if !networkMonitor.isConnected {
    InfoBox(
        icon: "wifi.slash",
        text: loginContext == .firstLogin
            ? "Fuer die erste Anmeldung wird eine Internetverbindung benoetigt."
            : "Ihre Sitzung ist abgelaufen. Bitte stellen Sie eine Internetverbindung her.",
        style: .warning
    )
    .transition(.opacity.combined(with: .scale))
}

// Buttons:
.disabled(isLoading || !networkMonitor.isConnected)
```

```kotlin
// Android: LoginScreen.kt -- Ergaenzungen

val isConnected by networkMonitor.isConnected.collectAsState()

// Ueber den Buttons:
AnimatedVisibility(visible = !isConnected) {
    InfoBox(
        text = if (loginContext == LoginContext.FIRST_LOGIN)
            "Fuer die erste Anmeldung wird eine Internetverbindung benoetigt."
        else
            "Ihre Sitzung ist abgelaufen. Bitte stellen Sie eine Internetverbindung her.",
        style = InfoBoxStyle.WARNING
    )
}

// Buttons:
enabled = !isLoading && isConnected
```

### Hintergrund-Token-Refresh (NetworkMonitor-Erweiterung)

```swift
// iOS: NetworkMonitor.swift -- Ergaenzung

// Callback fuer Netzwerkwechsel
private var onBecameOnline: (() -> Void)?

func setOnBecameOnline(_ handler: @escaping () -> Void) {
    self.onBecameOnline = handler
}

// Im pathUpdateHandler:
let wasOffline = !self.isConnected
self.isConnected = path.status == .satisfied
if wasOffline && self.isConnected {
    // Debounce: 2 Sekunden warten
    Task {
        try? await Task.sleep(nanoseconds: 2_000_000_000)
        if self.isConnected { // Immer noch online?
            self.onBecameOnline?()
        }
    }
}
```

```kotlin
// Android: NetworkMonitor.kt -- Ergaenzung

private var onBecameOnline: (() -> Unit)? = null
private val debounceJob = AtomicReference<Job?>(null)

fun setOnBecameOnline(handler: () -> Unit) {
    onBecameOnline = handler
}

// Im NetworkCallback.onAvailable():
override fun onAvailable(network: Network) {
    val wasOffline = !_isConnected.value
    _isConnected.value = true
    if (wasOffline) {
        debounceJob.getAndSet(
            CoroutineScope(Dispatchers.IO).launch {
                delay(2000) // 2s Debounce
                if (_isConnected.value) {
                    onBecameOnline?.invoke()
                }
            }
        )?.cancel()
    }
}
```

---

## Sicherheitsbetrachtung

| Aspekt | Massnahme |
|--------|-----------|
| Session-Daten im Klartext | Nein: iOS Keychain / Android EncryptedSharedPreferences |
| PII in Session | Minimiert: Nur userId + Zeitstempel, kein Name/Email |
| Unbegrenzter Offline-Zugang | 14-Tage-Limit, analog zu B2C Refresh-Token-Lebensdauer |
| Geraetediebstahl | Kein Access Token gespeichert, nur Session-Metadaten. Geraetesperre (PIN/Biometrie) ist erste Verteidigung |
| Nutzer-Account deaktiviert | Erst beim naechsten Online-Login erkannt. Akzeptables Risiko fuer max. 14 Tage Offline-Fenster |
| Lokale Daten nach Session-Ablauf | Bleiben erhalten, werden nach erneutem Login synchronisiert |
| Man-in-the-Middle | Kein Token wird ueber unsichere Kanaele gesendet. Offline-Session enthaelt kein Secret |

**Bewusste Entscheidung**: Die lokale Session enthaelt KEIN Token, KEIN Passwort und KEINE PII. Sie ist ein minimaler Nachweis (User-ID + Zeitstempel), dass der Nutzer sich einmal erfolgreich authentifiziert hat. Der eigentliche Zugangsschutz ist die Geraetesperre.

---

## Sequenzdiagramme

### Szenario: App-Start Offline, gueltige Session

```
User            App               NetworkMonitor    SessionManager    AuthManager
 |               |                     |                 |                |
 | App oeffnen   |                     |                 |                |
 |-------------->|                     |                 |                |
 |               | isConnected?        |                 |                |
 |               |-------------------->|                 |                |
 |               | false (<500ms)      |                 |                |
 |               |<--------------------|                 |                |
 |               | resolveStartState() |                 |                |
 |               |---------------------------------------------->------->|
 |               | Cached Token? Nein/Expired             |                |
 |               | load()              |                 |                |
 |               |-------------------------------->----->|                |
 |               | OfflineSession (gueltig, 5 Tage alt)  |                |
 |               |<--------------------------------|-----|                |
 |               | setOfflineMode()    |                 |                |
 |               | isAuthenticated = true                |                |
 |               |<----------------------------------------------|-------|
 | Hauptscreen   |                     |                 |                |
 |<--------------|                     |                 |                |
 | (+ OfflineBanner)                   |                 |                |
```

### Szenario: Offline -> Online Wechsel

```
User            App               NetworkMonitor    AuthManager       SyncEngine
 |               |                     |                |                |
 | (arbeitet offline)                  |                |                |
 |               |                     |                |                |
 |               | WiFi verfuegbar     |                |                |
 |               |<--------------------|                |                |
 |               | (2s Debounce)       |                |                |
 |               | acquireTokenSilently()               |                |
 |               |------------------------------------->|                |
 |               | Token refreshed     |                |                |
 |               |<------------------------------------|                |
 |               | updateOfflineSession()               |                |
 |               |----->                |                |                |
 |               | syncAll()           |                |                |
 |               |---------------------------------------------->------>|
 |               | Sync complete       |                |                |
 |               |<----------------------------------------------|-----|
 | OfflineBanner verschwindet          |                |                |
 |<--------------|                     |                |                |
```

---

## Testbare Kriterien

- [ ] OfflineSessionManager.save() + load() Round-Trip: gespeicherte Session kann geladen werden
- [ ] OfflineSession.isValid: true fuer Session von vor 13 Tagen
- [ ] OfflineSession.isValid: false fuer Session von vor 15 Tagen
- [ ] OfflineSession.daysUntilExpiry: korrekte Berechnung
- [ ] AuthManager.resolveStartState(): Online + gueltiger Token -> .authenticated
- [ ] AuthManager.resolveStartState(): Offline + gueltige Session -> .offlineWithSession
- [ ] AuthManager.resolveStartState(): Offline + abgelaufene Session -> .loginRequiredNoNetwork
- [ ] AuthManager.resolveStartState(): Offline + keine Session -> .loginRequiredNoNetwork (firstLogin)
- [ ] AuthManager.resolveStartState(): Offline-Pfad macht KEINEN Netzwerkaufruf (< 1 Sekunde)
- [ ] Login-Screen: Buttons deaktiviert wenn offline
- [ ] Login-Screen: Buttons werden aktiviert wenn Netz zurueckkehrt
- [ ] Login-Screen: Kontextabhaengige Meldung (firstLogin vs. sessionExpired)
- [ ] Hintergrund-Refresh: Token wird refreshed bei offline->online Wechsel
- [ ] Hintergrund-Refresh: Debounce verhindert mehrfache Requests
- [ ] Logout: OfflineSession wird geloescht

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| Keychain/EncryptedSharedPrefs nicht verfuegbar (z.B. nach OS-Update) | Niedrig | Graceful Degradation: Login-Screen zeigen, kein Crash |
| MSAL Cache und OfflineSession inkonsistent (unterschiedliche User) | Niedrig | Bei User-ID-Mismatch: OfflineSession loeschen, Login-Screen |
| NWPathMonitor/ConnectivityManager meldet falsches Ergebnis | Mittel | Fallback auf Token-Cache-Check. Wenn Token gueltig, rein lassen |
| 14-Tage-Limit zu kurz fuer manche Nutzer | Mittel | 14 Tage ist analog zur B2C Refresh-Token-Lebensdauer. Eine Aenderung ergibt nur Sinn wenn die B2C-Policy geaendert wird. |
| Race Condition: Netzwerk wechselt waehrend Entscheidungslogik | Niedrig | Entscheidung basiert auf Snapshot, kein Re-Check waehrend Resolve |
| EncryptedSharedPreferences Performance auf alten Android-Geraeten | Niedrig | Session ist klein (<1KB), Performance kein Problem |

---

## Abgrenzung / Nicht im Scope

- **Biometrische Absicherung** (Face ID / Fingerprint fuer Offline-Login): Separates Feature, kann als eigene Story nachgezogen werden
- **PIN-Code als Alternative**: Nicht geplant, Geraetesperre reicht
- **Offline-Login fuer mehrere Accounts auf einem Geraet**: Nicht unterstuetzt, Single-Account-Modell (MSAL SingleAccount)
- **Remote Session Invalidierung** (z.B. Admin loescht User): Erst beim naechsten Online-Login erkannt, kein Push-Mechanismus
- **Ablauf-Warnung** ("Ihre Session laeuft in X Tagen ab"): Bewusst nicht umgesetzt (YAGNI). S03 zeigt bei tatsaechlichem Ablauf den korrekten Hinweis auf dem Login-Screen.
