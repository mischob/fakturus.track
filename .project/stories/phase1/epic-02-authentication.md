# EPIC 02: Authentifizierung (Azure B2C)

## Ziel

Nutzer koennen sich ueber Azure AD B2C mit Apple, Google oder E-Mail anmelden. Der Access Token wird sicher gespeichert und automatisch erneuert. Nach dem Login werden die App-Services initialisiert.

## Abhaengigkeiten

- **E01**: Projekt-Setup muss abgeschlossen sein (MSAL SDK, Configuration)

## Voraussetzungen (Azure Portal)

Vor dem ersten Login-Test muessen die Redirect-URIs in der Azure B2C App-Registration konfiguriert sein:
- iOS: `msauth.com.fakturus.track://auth` (Plattform: iOS/macOS, Bundle ID: `com.fakturus.track`)
- Android: `msauth://com.fakturus.track/{signature-hash}` (Plattform: Android)
- Bestehende MAUI-URI NICHT entfernen!

---

## Stories

### P1-E02-S01: iOS AuthManager (MSAL Integration)

**Als** Nutzer
**moechte ich** mich sicher ueber Azure B2C anmelden koennen,
**damit** meine Zeiterfassungsdaten meinem Konto zugeordnet werden.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S01
**Parallelisierbar mit**: P1-E02-S02 (Android AuthManager), P1-E03-S01 (iOS DB)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `AuthManager.swift` als `@Observable` Klasse implementiert
- [ ] MSAL `MSALPublicClientApplication` korrekt konfiguriert mit B2C-Authority
- [ ] `isAuthenticated: Bool` Property, reactive (UI reagiert auf Aenderungen)
- [ ] `currentAccount: MSALAccount?` Property
- [ ] `accessToken: String?` Property (in-memory, nicht persistiert)
- [ ] `acquireTokenInteractively(provider:)` Methode:
  - Parameter `provider`: `.apple`, `.google`, `.email`
  - Setzt `domain_hint` entsprechend: `"apple.com"`, `"google.com"`, `nil`
  - Oeffnet System-WebView fuer B2C Login
  - Bei Erfolg: Token speichern, `isAuthenticated = true`
  - Bei Abbruch: Kein Fehler, zurueck zum Login-Screen
  - Bei Fehler: Fehlermeldung zurueckgeben
- [ ] `acquireTokenSilently()` async Methode:
  - Prueft Cache, dann Silent Renewal via MSAL
  - Gibt Access Token zurueck
  - Wirft `AuthError.notAuthenticated` wenn kein Account
  - Wirft `AuthError.tokenExpired` wenn Silent Refresh fehlschlaegt
- [ ] `logout()` Methode:
  - MSAL Account entfernen
  - Token-Cache leeren
  - `isAuthenticated = false`
- [ ] Token-Speicherung via MSAL-eigenem Keychain-Management
- [ ] Beim App-Start: Automatischer Silent-Token-Check (wenn Account im Cache)

**Technische Hinweise**:
- Adaptieren von fakturus.poi `AuthManager.swift`
- B2C Authority URL: `https://fakturus.b2clogin.com/tfp/fakturus.onmicrosoft.com/B2C_1_BetaSignInOnly`
- Scopes: `["https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"]`
- Redirect URI: `msauth.com.fakturus.track://auth`
- MSAL verwaltet Keychain automatisch

---

### P1-E02-S02: Android AuthManager (MSAL Integration)

**Als** Nutzer
**moechte ich** mich sicher ueber Azure B2C anmelden koennen,
**damit** meine Zeiterfassungsdaten meinem Konto zugeordnet werden.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S02
**Parallelisierbar mit**: P1-E02-S01 (iOS AuthManager), P1-E03-S02 (Android DB)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `AuthManager.kt` Klasse implementiert
- [ ] MSAL `SingleAccountPublicClientApplication` korrekt konfiguriert
- [ ] `isAuthenticated: StateFlow<Boolean>` (reactive, Compose-kompatibel)
- [ ] `currentAccount: IAccount?` Property
- [ ] `acquireTokenInteractively(activity, provider)` suspend Methode:
  - Parameter `provider`: `LoginProvider.APPLE`, `.GOOGLE`, `.EMAIL`
  - Setzt `domain_hint` in `extraQueryParameters`
  - Oeffnet CustomTab fuer B2C Login
  - Analog zu iOS Erfolg/Abbruch/Fehler-Handling
- [ ] `acquireTokenSilently()` suspend Methode:
  - Prueft Cache, dann Silent Renewal
  - Gibt Access Token als String zurueck
  - Wirft `AuthException.NotAuthenticated` / `AuthException.TokenExpired`
- [ ] `logout()` suspend Methode:
  - MSAL Account entfernen, Cache leeren
  - `isAuthenticated` auf `false`
- [ ] Token-Speicherung via MSAL (Android Keystore / SharedPreferences)
- [ ] `auth_config.json` in `res/raw/` korrekt konfiguriert
- [ ] Beim App-Start: Automatischer Silent-Token-Check

**Technische Hinweise**:
- Adaptieren von fakturus.poi Android `AuthManager.kt`
- `auth_config.json` Struktur:
  ```json
  {
    "client_id": "3fb35bc6-8825-495e-b0a2-18e00352f968",
    "authorization_user_agent": "BROWSER",
    "redirect_uri": "msauth://com.fakturus.track/{hash}",
    "authorities": [{
      "type": "B2C",
      "authority_url": "https://fakturus.b2clogin.com/tfp/fakturus.onmicrosoft.com/B2C_1_BetaSignInOnly/"
    }]
  }
  ```
- Signature Hash generieren: `keytool -exportcert -alias androiddebugkey -keystore ~/.android/debug.keystore | openssl sha1 -binary | openssl base64`
- AndroidManifest: `BrowserTabActivity` mit `msal` scheme

---

### P1-E02-S03: iOS Login-Screen

**Als** Nutzer
**moechte ich** einen uebersichtlichen Login-Screen sehen,
**damit** ich die fuer mich passende Anmeldemethode waehlen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E02-S01, P1-E01-S03
**Parallelisierbar mit**: P1-E02-S04 (Android Login-Screen)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `LoginView.swift` implementiert
- [ ] Layout (zentriert, viel Weissraum):
  - App-Icon/Logo oben (SF Symbol `clock.badge.checkmark` als Placeholder)
  - App-Name "Fakturus Track"
  - Tagline "Arbeitszeit erfassen. Einfach. Ueberall."
  - 3 Login-Buttons mit Abstand
- [ ] "Mit Apple anmelden" Button:
  - Nutzt `ASAuthorizationAppleIDButton` (Apple HIG Pflicht)
  - Tap -> `authManager.acquireTokenInteractively(provider: .apple)`
- [ ] "Mit Google anmelden" Button:
  - Google-Branding (G-Logo + Text)
  - Tap -> `authManager.acquireTokenInteractively(provider: .google)`
- [ ] "Mit E-Mail anmelden" Button:
  - Envelope-Icon + Text
  - Tap -> `authManager.acquireTokenInteractively(provider: .email)`
- [ ] Loading-State waehrend Login (Buttons deaktiviert, Spinner)
- [ ] Fehler-Anzeige bei fehlgeschlagenem Login (Text unter Buttons, rot)
- [ ] Bei Abbruch durch Nutzer: Kein Fehler, Buttons wieder aktiv
- [ ] Nach erfolgreichem Login: Automatische Navigation zum Hauptscreen
  - Given Nutzer ist auf dem Login-Screen
  - When Login erfolgreich abgeschlossen
  - Then wird der Zeiten-Tab (Hauptscreen) angezeigt

**Technische Hinweise**:
- `@Environment(AuthManager.self)` fuer Zugriff auf Auth-State
- Navigation ueber `authManager.isAuthenticated` (in App.swift: if/else)
- Kein NavigationStack noetig -- Login ist Root-View oder Content-View

---

### P1-E02-S04: Android Login-Screen

**Als** Nutzer
**moechte ich** einen uebersichtlichen Login-Screen sehen,
**damit** ich die fuer mich passende Anmeldemethode waehlen kann.

**Plattform**: Android
**Abhaengigkeiten**: P1-E02-S02, P1-E01-S04
**Parallelisierbar mit**: P1-E02-S03 (iOS Login-Screen)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `LoginScreen.kt` als Composable implementiert
- [ ] Layout (zentriert, Material 3):
  - App-Icon oben (Placeholder)
  - App-Name "Fakturus Track" (headlineLarge)
  - Tagline (bodyMedium, secondary color)
  - 3 Login-Buttons (FilledTonalButton oder OutlinedButton)
- [ ] "Mit Apple anmelden" Button:
  - Apple-Logo Icon + Text
  - Tap -> `authManager.acquireTokenInteractively(activity, LoginProvider.APPLE)`
- [ ] "Mit Google anmelden" Button:
  - Google-Logo Icon + Text
  - Tap -> `authManager.acquireTokenInteractively(activity, LoginProvider.GOOGLE)`
- [ ] "Mit E-Mail anmelden" Button:
  - Mail-Icon + Text
  - Tap -> `authManager.acquireTokenInteractively(activity, LoginProvider.EMAIL)`
- [ ] Loading-State: CircularProgressIndicator, Buttons disabled
- [ ] Fehler-Anzeige: Text(color = MaterialTheme.colorScheme.error)
- [ ] Bei Abbruch: Kein Fehler
- [ ] Nach erfolgreichem Login: Navigation zum Hauptscreen
  - Given Nutzer ist auf dem Login-Screen
  - When Login erfolgreich
  - Then wird der Zeiten-Tab angezeigt

**Technische Hinweise**:
- `LocalContext.current as ComponentActivity` fuer MSAL Activity-Parameter
- Navigation ueber `isAuthenticated.collectAsState()` in MainActivity
- Google Sign-In Branding Guidelines beachten (Logo, Farben)

---

### P1-E02-S05: Azure Portal Redirect-URI Konfiguration

**Als** Entwickler
**moechte ich** die Redirect-URIs im Azure Portal korrekt konfiguriert haben,
**damit** der Login auf beiden Plattformen funktioniert.

**Plattform**: Beide (Azure Portal)
**Abhaengigkeiten**: P1-E01-S01, P1-E01-S02 (fuer Signature Hash)
**Parallelisierbar mit**: P1-E02-S01, P1-E02-S02
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] iOS Redirect URI hinzugefuegt: `msauth.com.fakturus.track://auth`
  - Plattform: "iOS / macOS"
  - Bundle ID: `com.fakturus.track`
- [ ] Android Redirect URI hinzugefuegt: `msauth://com.fakturus.track/{signature-hash}`
  - Plattform: "Android"
  - Package: `com.fakturus.track`
  - Signature Hash: aus Debug-Keystore generiert
- [ ] Bestehende MAUI-URI unveraendert: `msal3fb35bc6-8825-495e-b0a2-18e00352f968://auth`
- [ ] MAUI-App Login funktioniert weiterhin (Regressions-Test)
- [ ] iOS Login auf Simulator getestet
- [ ] Android Login auf Emulator getestet

**Technische Hinweise**:
- Azure Portal: App Registrations > Fakturus Track > Authentication > Platform configurations
- Debug und Release Signature Hashes koennen unterschiedlich sein!
- Fuer Release spaeter: separaten Signature Hash hinzufuegen
