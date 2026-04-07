# Authentifizierungskonzept -- Azure AD B2C

## Ueberblick

Fakturus Track nutzt den gleichen Azure AD B2C Tenant wie fakturus.poi (`fakturus.onmicrosoft.com`), jedoch mit einer **eigenen App-Registration** und eigenem API-Scope.

### Bestehende Konfiguration (MAUI-App)

| Parameter | Wert |
|-----------|------|
| Tenant | `fakturus.onmicrosoft.com` |
| B2C Instance | `https://fakturus.b2clogin.com` |
| Client ID | `3fb35bc6-8825-495e-b0a2-18e00352f968` |
| Policy | `B2C_1_BetaSignInOnly` |
| API Scope | `https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access` |
| Redirect URI | `msal3fb35bc6-8825-495e-b0a2-18e00352f968://auth` |

### fakturus.poi Konfiguration (Referenz)

| Parameter | Wert |
|-----------|------|
| Client ID | `4f8fbb19-2b03-4684-ab9d-32f1448a08dd` |
| Policy | `B2C_1_OpenSignUpSignIn` |
| API Scope | `https://fakturus.onmicrosoft.com/39b192a3-ef40-422f-aaaf-4ed15a7170f4/Access` |
| Redirect URI | `msal4f8fbb19-2b03-4684-ab9d-32f1448a08dd://auth` |

---

## Konzept fuer Native Apps

### Variante A: Gleiche App-Registration beibehalten (empfohlen fuer Phase 1)

Die nativen Apps verwenden die **gleiche Client ID** wie die MAUI-App. Vorteile:
- Kein neuer B2C-Setup noetig
- Bestehende Nutzer koennen sich sofort anmelden
- Token-Kompatibilitaet mit dem Backend ist sichergestellt

**Aenderungen noetig:**
- Redirect URI pro Plattform hinzufuegen (in Azure Portal):
  - iOS: `msauth.com.fakturus.track://auth` (MSAL iOS Standard)
  - Android: `msauth://com.fakturus.track/{signature-hash}` (MSAL Android Standard)

### Variante B: Eigene App-Registration (empfohlen fuer Phase 2)

Separate App-Registration fuer Fakturus Track mit eigenem Client-ID. Vorteile:
- Saubere Trennung von fakturus.poi
- Eigene Login-Policy moeglich (z.B. `B2C_1_TrackSignIn`)
- Separate Analytics und Nutzungs-Statistiken in Azure

### Empfehlung

**Phase 1**: Variante A (bestehende App-Registration)
**Phase 2+**: Migration zu Variante B (eigene Registration, sobald stabil)

---

## Login-Flow

### Sequenz

```
User         Native App        MSAL SDK       Azure B2C        Backend API
  |               |                |              |                  |
  |-- Tap Login-->|                |              |                  |
  |               |-- acquireToken |              |                  |
  |               |    interactive |              |                  |
  |               |--------------->|              |                  |
  |               |                |-- Redirect-->|                  |
  |<----- B2C Login Page ----------|              |                  |
  |-- Credentials ----------------->              |                  |
  |               |                |<-- Token ----|                  |
  |               |<-- Result -----|              |                  |
  |               |  (accessToken, |              |                  |
  |               |   account)     |              |                  |
  |               |                |              |                  |
  |               |-- Store Token (Keychain/Keystore)               |
  |               |                |              |                  |
  |               |-- API Request mit Bearer Token ---------------->|
  |               |<-- User Data -----------------------------------|
  |               |                |              |                  |
  |<-- Dashboard--|                |              |                  |
```

### Social Login Provider

Basierend auf fakturus.poi erprobt:

| Provider | Domain Hint | Anmerkung |
|----------|-------------|-----------|
| Apple | `apple.com` | Pflicht fuer iOS App Store |
| Google | `google.com` | Beliebteste Option |
| Microsoft | `live.com` | Fuer Business-Nutzer |
| E-Mail | `null` | Fallback, ohne Domain Hint |

**Empfehlung fuer Track:** Apple + Google + E-Mail (reduziert auf das Wesentliche)

Amazon entfaellt (ist POI-spezifisch), Microsoft optional.

### Domain Hints

Domain Hints beschleunigen den Login-Flow, indem der B2C Identity Provider direkt aufgerufen wird (kein Provider-Auswahl-Screen):

```swift
// iOS
parameters.extraQueryParameters = ["domain_hint": "apple.com"]

// Android
parameters.extraQueryParameters = mapOf("domain_hint" to "apple.com")
```

---

## Token-Management

### Token-Lebenszyklus

| Token | Lebensdauer | Speicherort |
|-------|-------------|-------------|
| Access Token | ~60 Minuten (B2C Standard) | In-Memory + MSAL Cache |
| Refresh Token | 14 Tage (B2C Standard) | Keychain (iOS) / Keystore (Android) |
| ID Token | ~60 Minuten | In-Memory |

### Silent Token Renewal

```
App startet / API-Call noetig
      |
      v
  Token in Cache?
      |
   Ja |          Nein
      v            v
  Gueltig?     acquireTokenSilently()
      |              |
   Ja |  Nein   Erfolg?
      v    v       |
  Nutzen  Silent   Ja --> Token verwenden
           |       |
         Erfolg?  Nein --> InteractiveLogin()
           |
        Ja/Nein
```

### iOS-Implementation (adaptiert von fakturus.poi)

```swift
func acquireTokenSilently() async throws -> String {
    // 1. Pruefe Cache
    if let token = accessToken, let expiry = tokenExpiry, expiry > Date() {
        return token
    }

    // 2. Silent Renewal via MSAL
    guard let app = msalApplication, let account = currentAccount else {
        throw AuthError.notAuthenticated
    }

    let params = MSALSilentTokenParameters(scopes: Configuration.b2cScopes, account: account)
    let result = try await app.acquireTokenSilent(with: params)
    self.accessToken = result.accessToken
    self.tokenExpiry = result.expiresOn
    return result.accessToken
}
```

### Android-Implementation

```kotlin
suspend fun acquireTokenSilently(): String {
    val account = msalApp.getAccount() ?: throw AuthException.NotAuthenticated

    return try {
        val result = msalApp.acquireTokenSilentAsync(
            AcquireTokenSilentParameters.Builder(scopes, account)
                .forceRefresh(false)
                .build()
        ).await()
        result.accessToken
    } catch (e: MsalUiRequiredException) {
        throw AuthException.TokenExpired
    }
}
```

---

## API-Request Authorization

### iOS (wie fakturus.poi APIClient)

```swift
private func buildRequest(path: String, method: String) async throws -> URLRequest {
    var request = URLRequest(url: baseURL.appendingPathComponent(path))
    request.httpMethod = method
    let token = try await authManager.acquireTokenSilently()
    request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
    return request
}
```

### Android (OkHttp Interceptor)

```kotlin
class AuthInterceptor(
    private val authManager: AuthManager
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val token = runBlocking { authManager.acquireTokenSilently() }
        val request = chain.request().newBuilder()
            .header("Authorization", "Bearer $token")
            .build()
        return chain.proceed(request)
    }
}
```

---

## Anonymer Modus

Die bestehende MAUI-App unterstuetzt einen anonymen Modus (Offline-Nutzung ohne Login). Fuer die nativen Apps:

**Empfehlung: Login-Pflicht**

Gruende:
- Vereinfacht die Architektur erheblich (kein Dual-Pfad)
- Sync funktioniert nur mit Authentifizierung
- Anonyme Daten gehen bei App-Neuinstallation verloren
- BAG-Urteil erfordert personenbezogene Zeiterfassung

**Fallback:** Wenn kein Netzwerk verfuegbar: App zeigt Login-Screen, aber mit "Offline nutzen"-Hinweis. Einmal eingeloggt, funktioniert die App auch offline (Token wird lokal gespeichert).

---

## B2C Policy-Ueberlegungen

### Aktuelle Policy: B2C_1_BetaSignInOnly
- Nur Anmeldung, keine Registrierung
- Geeignet fuer geschlossene Beta

### Empfehlung fuer Produktion: B2C_1_TrackSignUpSignIn
- Selbstregistrierung ermoeglichen
- Social Login Provider konfigurieren (Apple, Google)
- Custom Page Branding (Fakturus-Design)
- MFA optional (fuer sicherheitsbewusste Nutzer)

### Beta-Flag
Die MAUI-App prueft `extension_BetaSupport` Custom Claim. Fuer die nativen Apps:
- Phase 1: Beta-Check beibehalten (gleiche Nutzergruppe)
- Phase 2: Beta-Check entfernen (oeffentlicher Launch)

---

## Azure Portal Redirect-URI Checkliste

**WICHTIG:** Vor dem ersten Login-Test auf einer nativen Plattform muessen die Redirect-URIs im Azure Portal konfiguriert sein. Fehlende URIs fuehren zu einem kryptischen Fehler ("redirect_uri mismatch").

**App Registration:** `Fakturus Track` (Client ID: `3fb35bc6-8825-495e-b0a2-18e00352f968`)

Im Azure Portal unter **Authentication > Platform configurations** muessen folgende Redirect-URIs eingetragen sein:

### Bestehend (NICHT entfernen!)
- [ ] `msal3fb35bc6-8825-495e-b0a2-18e00352f968://auth` -- MAUI-App (bestehend, muss erhalten bleiben)

### Neu hinzufuegen fuer native Apps
- [ ] **iOS:** `msauth.com.fakturus.track://auth` -- MSAL iOS Standard-Format (Plattform: "iOS / macOS", Bundle ID: `com.fakturus.track`)
- [ ] **Android:** `msauth://com.fakturus.track/{signature-hash}` -- MSAL Android Standard-Format (Plattform: "Android", Package: `com.fakturus.track`, Signature Hash: aus `keytool -exportcert` generieren)

### Validierung
- [ ] iOS Login getestet (Simulator + Device)
- [ ] Android Login getestet (Emulator + Device)
- [ ] MAUI-App Login funktioniert weiterhin (Regression-Test!)

---

## Sicherheits-Checkliste

- [ ] Tokens nur in Keychain (iOS) / Keystore (Android) speichern
- [ ] Kein Token-Logging in Produktion
- [ ] Certificate Pinning fuer API-Calls (optional, aber empfohlen)
- [ ] Biometrische Entsperrung fuer App-Zugriff (Phase 3)
- [ ] Automatischer Logout nach 14 Tagen Inaktivitaet (Token-Verfall)
- [ ] MSAL Cache bei Logout vollstaendig bereinigen
- [ ] Redirect URIs in Azure Portal korrekt konfiguriert
