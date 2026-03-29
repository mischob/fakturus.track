# Tech-Spec: EPIC 02 -- Authentifizierung (Azure B2C)

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Services/Auth/AuthManager.swift` | iOS | E02-S01 | MSAL B2C, Token-Management, @Observable |
| `Features/Auth/LoginView.swift` | iOS | E02-S03 | Login-Screen mit 3 Buttons |
| `services/auth/AuthManager.kt` | Android | E02-S02 | MSAL SingleAccount, StateFlow |
| `features/auth/LoginScreen.kt` | Android | E02-S04 | Login-Screen Composable |

**Modifizierte Dateien:**
- `FakturusTrackApp.swift` (Auth-Check: LoginView vs ContentView)
- `MainActivity.kt` (Auth-Check: LoginScreen vs MainScreen)
- `ServiceContainer.swift/kt` (AuthManager-Referenz, onLogin-Trigger)

---

## API-Contracts

Auth nutzt keine eigene REST-API -- alles laeuft ueber MSAL SDK gegen Azure B2C. Relevante Konfiguration:

```
Authority URL: https://fakturus.b2clogin.com/tfp/fakturus.onmicrosoft.com/B2C_1_BetaSignInOnly
Client ID:     3fb35bc6-8825-495e-b0a2-18e00352f968
Scopes:        ["https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"]
iOS Redirect:  msauth.com.fakturus.track://auth
Android Redirect: msauth://com.fakturus.track/{signature-hash}
```

MSAL liefert nach Login:
- `accessToken: String` (JWT, ~60min gueltig)
- `account: MSALAccount / IAccount` (cached fuer Silent Renewal)
- Claims im ID Token: `oid` (User-ID), `name`, `emails`

---

## Code-Skizzen

### iOS: AuthManager.swift

```swift
import MSAL

enum LoginProvider {
    case apple, google, email

    var domainHint: String? {
        switch self {
        case .apple: return "apple.com"
        case .google: return "google.com"
        case .email: return nil
        }
    }
}

enum AuthError: Error {
    case notAuthenticated
    case tokenExpired
    case cancelled
    case failed(String)
}

@Observable
final class AuthManager {
    var isAuthenticated = false
    var currentAccount: MSALAccount?
    private(set) var accessToken: String?

    private var msalApp: MSALPublicClientApplication?

    init() {
        configureMSAL()
        // Beim Start: Silent Check
        Task { await checkExistingSession() }
    }

    private func configureMSAL() {
        guard let authority = try? MSALB2CAuthority(
            url: URL(string: Configuration.b2cAuthorityUrl)!
        ) else { return }

        let config = MSALPublicClientApplicationConfig(
            clientId: Configuration.b2cClientId,
            redirectUri: Configuration.b2cRedirectUri,
            authority: authority
        )
        config.knownAuthorities = [authority]
        msalApp = try? MSALPublicClientApplication(configuration: config)
    }

    private func checkExistingSession() async {
        guard let account = msalApp?.allAccounts()?.first else { return }
        do {
            let token = try await acquireTokenSilently()
            accessToken = token
            currentAccount = account
            isAuthenticated = true
        } catch {
            // Kein gueltiger Token, Login-Screen zeigen
        }
    }

    @MainActor
    func acquireTokenInteractively(provider: LoginProvider) async throws {
        guard let msalApp else { throw AuthError.failed("MSAL not configured") }

        let params = MSALInteractiveTokenParameters(
            scopes: Configuration.b2cScopes,
            webviewParameters: MSALWebviewParameters(
                authPresentationViewController: UIApplication.topViewController()
            )
        )
        if let hint = provider.domainHint {
            params.extraQueryParameters = ["domain_hint": hint]
        }

        do {
            let result = try await msalApp.acquireToken(with: params)
            accessToken = result.accessToken
            currentAccount = result.account
            isAuthenticated = true
        } catch let error as NSError {
            if error.domain == MSALErrorDomain,
               error.code == MSALError.userCanceled.rawValue {
                throw AuthError.cancelled
            }
            throw AuthError.failed(error.localizedDescription)
        }
    }

    func acquireTokenSilently(forceRefresh: Bool = false) async throws -> String {
        guard let msalApp, let account = currentAccount ?? msalApp.allAccounts()?.first else {
            throw AuthError.notAuthenticated
        }

        let params = MSALSilentTokenParameters(scopes: Configuration.b2cScopes, account: account)
        params.forceRefresh = forceRefresh

        do {
            let result = try await msalApp.acquireTokenSilent(with: params)
            accessToken = result.accessToken
            return result.accessToken
        } catch {
            throw AuthError.tokenExpired
        }
    }

    @MainActor
    func logout() {
        guard let msalApp, let account = currentAccount else { return }
        try? msalApp.remove(account)
        accessToken = nil
        currentAccount = nil
        isAuthenticated = false
    }
}

// Helper: Top ViewController fuer MSAL Presentation
extension UIApplication {
    static func topViewController() -> UIViewController {
        let scene = shared.connectedScenes.first as? UIWindowScene
        return scene?.windows.first?.rootViewController ?? UIViewController()
    }
}
```

### iOS: LoginView.swift

```swift
struct LoginView: View {
    @Environment(AuthManager.self) private var authManager
    @State private var isLoading = false
    @State private var errorMessage: String?

    var body: some View {
        VStack(spacing: 32) {
            Spacer()

            // Logo
            Image(systemName: "clock.badge.checkmark")
                .font(.system(size: 72))
                .foregroundStyle(.accent)

            VStack(spacing: 8) {
                Text("Fakturus Track")
                    .font(.largeTitle.bold())
                Text("Arbeitszeit erfassen. Einfach. Ueberall.")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            // Login Buttons
            VStack(spacing: 12) {
                SignInWithAppleButton(.signIn) { request in
                    // ASAuthorizationAppleIDButton styling
                } onCompletion: { _ in }
                // MSAL uebernimmt den echten Flow:
                .frame(height: 50)
                .overlay {
                    Button { login(provider: .apple) } label: { Color.clear }
                }

                loginButton("Mit Google anmelden", icon: "g.circle.fill", provider: .google)
                loginButton("Mit E-Mail anmelden", icon: "envelope.fill", provider: .email)
            }
            .disabled(isLoading)

            if isLoading {
                ProgressView()
            }

            if let error = errorMessage {
                Text(error)
                    .font(.caption)
                    .foregroundStyle(.red)
                    .multilineTextAlignment(.center)
            }

            Spacer()
        }
        .padding(.horizontal, 32)
    }

    private func loginButton(_ title: String, icon: String, provider: LoginProvider) -> some View {
        Button {
            login(provider: provider)
        } label: {
            Label(title, systemImage: icon)
                .frame(maxWidth: .infinity)
                .padding(.vertical, 12)
        }
        .buttonStyle(.bordered)
    }

    private func login(provider: LoginProvider) {
        isLoading = true
        errorMessage = nil
        Task {
            do {
                try await authManager.acquireTokenInteractively(provider: provider)
            } catch AuthError.cancelled {
                // User hat abgebrochen, kein Fehler
            } catch {
                errorMessage = "Anmeldung fehlgeschlagen. Bitte versuchen Sie es erneut."
            }
            isLoading = false
        }
    }
}
```

### Android: AuthManager.kt

```kotlin
enum class LoginProvider { APPLE, GOOGLE, EMAIL }

sealed class AuthException : Exception() {
    object NotAuthenticated : AuthException()
    object TokenExpired : AuthException()
    object Cancelled : AuthException()
    data class Failed(override val message: String) : AuthException()
}

class AuthManager(private val context: Context) {
    private val _isAuthenticated = MutableStateFlow(false)
    val isAuthenticated: StateFlow<Boolean> = _isAuthenticated.asStateFlow()

    private var msalApp: ISingleAccountPublicClientApplication? = null
    private var currentAccount: IAccount? = null

    init {
        PublicClientApplication.createSingleAccountPublicClientApplication(
            context,
            R.raw.auth_config,
            object : IPublicClientApplication.ISingleAccountApplicationCreatedListener {
                override fun onCreated(app: ISingleAccountPublicClientApplication) {
                    msalApp = app
                    checkExistingSession()
                }
                override fun onError(exception: MsalException) {
                    Log.e("AuthManager", "MSAL init failed", exception)
                }
            }
        )
    }

    private fun checkExistingSession() {
        msalApp?.getCurrentAccountAsync(object :
            ISingleAccountPublicClientApplication.CurrentAccountCallback {
            override fun onAccountLoaded(account: IAccount?) {
                if (account != null) {
                    currentAccount = account
                    // Versuche silent token
                    CoroutineScope(Dispatchers.IO).launch {
                        try {
                            acquireTokenSilently()
                            _isAuthenticated.value = true
                        } catch (_: Exception) { /* Login-Screen zeigen */ }
                    }
                }
            }
            override fun onAccountChanged(prev: IAccount?, current: IAccount?) {}
            override fun onError(exception: MsalException) {}
        })
    }

    suspend fun acquireTokenInteractively(
        activity: Activity,
        provider: LoginProvider
    ) = suspendCancellableCoroutine { continuation ->
        val app = msalApp ?: run {
            continuation.resumeWithException(AuthException.Failed("MSAL not initialized"))
            return@suspendCancellableCoroutine
        }
        val params = AcquireTokenParameters.Builder()
            .startAuthorizationFromActivity(activity)
            .withScopes(Configuration.B2C_SCOPES)
            .apply {
                val hint = when (provider) {
                    LoginProvider.APPLE -> "apple.com"
                    LoginProvider.GOOGLE -> "google.com"
                    LoginProvider.EMAIL -> null
                }
                if (hint != null) {
                    withAuthorizationQueryStringParameters(listOf(Pair("domain_hint", hint)))
                }
            }
            .withCallback(object : AuthenticationCallback {
                override fun onSuccess(result: IAuthenticationResult) {
                    currentAccount = result.account
                    _isAuthenticated.value = true
                    continuation.resume(Unit)
                }
                override fun onError(exception: MsalException) {
                    continuation.resumeWithException(
                        AuthException.Failed(exception.message ?: "Login failed")
                    )
                }
                override fun onCancel() {
                    continuation.resumeWithException(AuthException.Cancelled)
                }
            })
            .build()

        app.acquireToken(params)
    }

    suspend fun acquireTokenSilently(): String {
        val app = msalApp ?: throw AuthException.NotAuthenticated
        val account = currentAccount ?: throw AuthException.NotAuthenticated
        val params = AcquireTokenSilentParameters.Builder()
            .forAccount(account)
            .fromAuthority(Configuration.B2C_AUTHORITY_URL)
            .withScopes(Configuration.B2C_SCOPES)
            .build()
        return try {
            val result = app.acquireTokenSilent(params)
            result.accessToken
        } catch (e: Exception) {
            throw AuthException.TokenExpired
        }
    }

    suspend fun logout() {
        msalApp?.signOut(object : ISingleAccountPublicClientApplication.SignOutCallback {
            override fun onSignOut() { _isAuthenticated.value = false }
            override fun onError(exception: MsalException) {
                _isAuthenticated.value = false
            }
        })
        currentAccount = null
    }
}
```

### Android: LoginScreen.kt

```kotlin
@Composable
fun LoginScreen(authManager: AuthManager) {
    val context = LocalContext.current
    val activity = context as ComponentActivity
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        // Logo Placeholder
        Icon(
            imageVector = Icons.Default.Schedule,
            contentDescription = null,
            modifier = Modifier.size(72.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(Modifier.height(16.dp))
        Text("Fakturus Track", style = MaterialTheme.typography.headlineLarge)
        Text(
            "Arbeitszeit erfassen. Einfach. Ueberall.",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(Modifier.height(48.dp))

        // Login Buttons
        LoginButton("Mit Apple anmelden", Icons.Default.Apple, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.APPLE,
                      onLoading = { isLoading = it }, onError = { errorMessage = it })
        }
        Spacer(Modifier.height(12.dp))
        LoginButton("Mit Google anmelden", Icons.Default.Email, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.GOOGLE,
                      onLoading = { isLoading = it }, onError = { errorMessage = it })
        }
        Spacer(Modifier.height(12.dp))
        LoginButton("Mit E-Mail anmelden", Icons.Default.Email, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.EMAIL,
                      onLoading = { isLoading = it }, onError = { errorMessage = it })
        }

        if (isLoading) {
            Spacer(Modifier.height(16.dp))
            CircularProgressIndicator()
        }
        errorMessage?.let { error ->
            Spacer(Modifier.height(16.dp))
            Text(error, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
        }
    }
}

private fun loginWith(
    scope: CoroutineScope,
    authManager: AuthManager,
    activity: Activity,
    provider: LoginProvider,
    onLoading: (Boolean) -> Unit,
    onError: (String?) -> Unit
) {
    scope.launch {
        onLoading(true)
        onError(null)
        try {
            authManager.acquireTokenInteractively(activity, provider)
        } catch (_: AuthException.Cancelled) {
            // Abbruch ist kein Fehler
        } catch (e: Exception) {
            onError("Anmeldung fehlgeschlagen. Bitte versuchen Sie es erneut.")
        }
        onLoading(false)
    }
}

@Composable
private fun LoginButton(
    text: String,
    icon: ImageVector,
    isLoading: Boolean,
    onClick: () -> Unit
) {
    OutlinedButton(
        onClick = onClick,
        enabled = !isLoading,
        modifier = Modifier.fillMaxWidth().height(50.dp)
    ) {
        Icon(icon, contentDescription = null, modifier = Modifier.size(20.dp))
        Spacer(Modifier.width(8.dp))
        Text(text)
    }
}
```

---

## Datenfluss

```
LoginView/LoginScreen
    |
    | (Tap auf Login-Button)
    v
AuthManager.acquireTokenInteractively(provider)
    |
    | (MSAL oeffnet System-WebView -> Azure B2C)
    v
B2C Login Flow (Apple/Google/E-Mail)
    |
    | (Callback mit Token)
    v
AuthManager: isAuthenticated = true
    |
    | (Reaktiv: SwiftUI / Compose beobachtet isAuthenticated)
    v
FakturusTrackApp/MainActivity: wechselt zu ContentView/MainScreen
    |
    | (onChange/collectAsState)
    v
ServiceContainer.onLogin() -> initialisiert APIClient etc.
```

---

## Testbare Kriterien

- [ ] iOS: AuthManager.configureMSAL() loest keinen Crash aus
- [ ] iOS: LoginView rendert korrekt mit 3 Buttons
- [ ] iOS: Loading-State deaktiviert Buttons
- [ ] iOS: Nach Mock-Login wechselt App zu ContentView
- [ ] Android: AuthManager initialisiert MSAL asynchron
- [ ] Android: LoginScreen zeigt 3 Buttons, zentriert
- [ ] Android: Loading-State zeigt CircularProgressIndicator
- [ ] **Echter Login-Test**: Redirect-URI im Azure Portal konfiguriert, Login auf Simulator/Emulator erfolgreich

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| MSAL iOS Redirect-URI funktioniert nicht | Mittel | Info.plist CFBundleURLSchemes pruefen, Entitlements pruefen |
| MSAL Android Signature Hash falsch | Hoch | Debug UND Release Hash generieren, in Azure Portal eintragen |
| B2C Policy aendert sich | Niedrig | Policy-Name in Configuration.swift/kt anpassen |
| domain_hint funktioniert nicht (Apple) | Niedrig | Ohne domain_hint testen, B2C zeigt dann Auswahl-Screen |
| Swift 6 Concurrency Warnings in MSAL Callbacks | Mittel | `@preconcurrency import MSAL`, `nonisolated(unsafe)` wo noetig |
| MSAL Android Callback-basiert statt Coroutine | Erwartet | `suspendCancellableCoroutine` Wrapper um Callback-API |
