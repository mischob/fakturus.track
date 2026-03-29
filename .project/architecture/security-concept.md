# Sicherheitskonzept -- Fakturus Track

## 1. Token Storage

### iOS: Keychain

MSAL iOS speichert Tokens automatisch in der iOS Keychain. Kein manueller Code noetig.

| Token | Speicherort | Verwaltung |
|-------|-------------|-----------|
| Access Token | MSAL In-Memory Cache + Keychain | MSAL automatisch |
| Refresh Token | iOS Keychain | MSAL automatisch |
| ID Token | MSAL In-Memory Cache | MSAL automatisch |

**Keychain Access Group:** `com.fakturus.track`

```swift
// MSAL konfiguriert Keychain automatisch
let config = MSALPublicClientApplicationConfig(
    clientId: Configuration.b2cClientId,
    redirectUri: Configuration.b2cRedirectUri,
    authority: authority
)
// Keychain Sharing wird ueber Entitlements konfiguriert
```

### Android: Keystore / EncryptedSharedPreferences

MSAL Android nutzt den Android Keystore fuer Token-Verschluesselung.

| Token | Speicherort | Verwaltung |
|-------|-------------|-----------|
| Access Token | MSAL SharedAccountCredentialCache | MSAL automatisch |
| Refresh Token | Android Keystore (verschluesselt) | MSAL automatisch |
| ID Token | MSAL Cache | MSAL automatisch |

Kein manueller Keystore-Code noetig. MSAL verwaltet alles intern.

### Sicherheitsregeln

- **Kein Token-Logging**: In Release-Builds werden Tokens nie geloggt
- **Kein Token in UserDefaults/SharedPreferences**: Nur ueber MSAL-Cache
- **Kein Token in Klartext**: MSAL verschluesselt automatisch
- **Logout bereinigt alles**: `signOut()` loescht MSAL-Cache komplett

---

## 2. Certificate Pinning

### Phase 1: Kein Certificate Pinning

Begruendung:
- Azure B2C nutzt Microsoft-verwaltete Zertifikate (regelmaessige Rotation)
- Pinning gegen rotierende Zertifikate fuehrt zu App-Ausfall
- Das Backend nutzt Azure-verwaltete TLS-Zertifikate
- Die App verarbeitet keine hochsensiblen Finanzdaten

### Phase 3 (optional): Public Key Pinning

Falls spaeter gewuenscht, dann **Public Key Pinning** (nicht Certificate Pinning), da Public Keys bei Zertifikatsrotation stabil bleiben koennen.

**iOS (Network Security):**
```xml
<!-- Info.plist -->
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSPinnedDomains</key>
    <dict>
        <key>api.track.fakturus.com</key>
        <dict>
            <key>NSIncludesSubdomains</key>
            <true/>
            <key>NSPinnedLeafIdentities</key>
            <array>
                <dict>
                    <key>SPKI-SHA256-BASE64</key>
                    <string>base64-encoded-hash</string>
                </dict>
            </array>
        </dict>
    </dict>
</dict>
```

**Android (Network Security Config):**
```xml
<!-- res/xml/network_security_config.xml -->
<network-security-config>
    <domain-config>
        <domain includeSubdomains="true">api.track.fakturus.com</domain>
        <pin-set>
            <pin digest="SHA-256">base64-encoded-hash</pin>
            <pin digest="SHA-256">backup-pin-hash</pin>
        </pin-set>
    </domain-config>
</network-security-config>
```

---

## 3. Biometrische Authentifizierung

### Phase 3 Feature

Biometrie dient als **Convenience-Feature** (Schnellzugriff), nicht als primaere Authentifizierung. Der B2C-Login bleibt die einzige echte Authentifizierung.

### Flow

```
App-Start
    |
    +-- B2C Token im Cache (gueltig)?
          |
          +-- Ja + Biometrie aktiviert:
          |     -> Face ID / Fingerprint Abfrage
          |     -> Erfolg: App oeffnen (kein neuer Token-Fetch)
          |     -> Fehlschlag: Login-Screen (B2C)
          |
          +-- Ja + Biometrie nicht aktiviert:
          |     -> Direkt zur App
          |
          +-- Nein:
                -> Login-Screen (B2C)
```

### iOS (LocalAuthentication)

```swift
import LocalAuthentication

func authenticateWithBiometrics() async -> Bool {
    let context = LAContext()
    var error: NSError?

    guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error) else {
        return false
    }

    do {
        return try await context.evaluatePolicy(
            .deviceOwnerAuthenticationWithBiometrics,
            localizedReason: "Zugriff auf Fakturus Track"
        )
    } catch {
        return false
    }
}
```

### Android (BiometricPrompt)

```kotlin
val promptInfo = BiometricPrompt.PromptInfo.Builder()
    .setTitle("Fakturus Track")
    .setSubtitle("Bitte authentifizieren Sie sich")
    .setNegativeButtonText("Abbrechen")
    .setAllowedAuthenticators(BiometricManager.Authenticators.BIOMETRIC_STRONG)
    .build()

biometricPrompt.authenticate(promptInfo)
```

---

## 4. Datenverschluesselung

### Lokale Datenbank

| Plattform | Verschluesselung | Details |
|-----------|-----------------|---------|
| iOS | File Protection (Complete) | iOS verschluesselt die App-Sandbox automatisch |
| Android | Keine DB-Verschluesselung | Room/SQLite sind nicht verschluesselt |

### Begruendung: Keine explizite DB-Verschluesselung

1. **iOS**: File Protection `NSFileProtectionComplete` ist Standard. Die gesamte App-Sandbox ist verschluesselt, wenn das Geraet gesperrt ist.
2. **Android**: SQLCipher waere moeglich, aber:
   - Performance-Overhead
   - Die Daten sind Arbeitszeiten, keine Finanz- oder Gesundheitsdaten
   - Android-Geraeteverschluesselung (FDE/FBE) schuetzt die Daten auf Geraeteebene
   - Bei Jailbreak/Root ist ohnehin alles kompromittiert

### Netzwerk-Verschluesselung

- **TLS 1.2+** fuer alle API-Calls (Azure erzwingt dies)
- **Kein HTTP**: iOS App Transport Security und Android Cleartext-Verbot
- **Token** werden nur ueber HTTPS uebertragen

---

## 5. DSGVO-Konformitaet

### Verarbeitete personenbezogene Daten

| Datum | Zweck | Speicherort | Rechtsgrundlage |
|-------|-------|-------------|-----------------|
| E-Mail | Login/Identifizierung | Azure B2C | Vertragserfuellung (Art. 6 (1) b) |
| Name | Profilanzeige | Azure B2C Claims | Vertragserfuellung |
| Arbeitszeiten | Kernfunktion der App | Backend (PostgreSQL) + Lokal (SQLite/SwiftData) | Vertragserfuellung |
| Urlaubstage | Kernfunktion der App | Backend + Lokal | Vertragserfuellung |
| Einstellungen | Personalisierung | Backend + Lokal | Vertragserfuellung |

### Was wir NICHT verarbeiten

- **Keine Standortdaten** (kein GPS-Tracking)
- **Keine Geraete-IDs** (keine Analytics)
- **Keine Werbe-IDs** (kein AdMob/AdTracking)
- **Keine Crash-Reports an Dritte** (kein Firebase/Sentry in V1)
- **Keine Kontaktdaten** (kein Adressbuch-Zugriff)
- **Keine Kameradaten** (kein Kamera-Zugriff)

### Datenspeicherung

| Ort | Region | Anbieter |
|-----|--------|----------|
| Backend (API + DB) | Azure Germany (Frankfurt/Berlin) | Microsoft Azure |
| Azure B2C | EU (konfiguriert) | Microsoft Azure |
| Lokale App-Daten | Auf dem Geraet des Nutzers | -- |

### Nutzerrechte (Art. 15-22 DSGVO)

| Recht | Umsetzung |
|-------|-----------|
| **Auskunft** (Art. 15) | Alle Daten sind in der App sichtbar |
| **Berichtigung** (Art. 16) | User kann alle Daten in der App aendern |
| **Loeschung** (Art. 17) | Account-Loeschung via E-Mail (manueller Prozess in V1) |
| **Datenportabilitaet** (Art. 20) | CSV-Export in Phase 3 |
| **Widerspruch** (Art. 21) | Abmeldung und Account-Loeschung moeglich |

### Privacy Policy

Die Privacy Policy wird als Webseite gehostet und in der App verlinkt:
- URL: `https://track.fakturus.com/privacy` (DE)
- URL: `https://track.fakturus.com/privacy/en` (EN)
- Erreichbar via Settings > Datenschutz
- Pflichtangabe fuer App Store und Play Store

### App-Tracking Transparency (iOS)

**Nicht erforderlich.** ATT wird nur benoetigt wenn:
- Tracking-SDKs verwendet werden
- Daten mit Drittanbietern geteilt werden
- Werbe-IDs ausgelesen werden

Nichts davon trifft auf Fakturus Track zu.

---

## 6. Sicherheits-Checkliste fuer Launch

### Vor Phase 1 Beta

- [ ] MSAL-Tokens werden nur im Keychain/Keystore gespeichert (kein SharedPreferences/UserDefaults)
- [ ] Kein Token-Logging in Release-Builds
- [ ] HTTPS fuer alle API-Calls erzwungen
- [ ] Redirect URIs korrekt in Azure Portal konfiguriert
- [ ] MSAL-Cache wird bei Logout vollstaendig bereinigt
- [ ] Kein sensitiver Data-Leak in Logcat/Console (Release)
- [ ] iOS: `NSAppTransportSecurity` nicht deaktiviert (kein `NSAllowsArbitraryLoads`)
- [ ] Android: `android:usesCleartextTraffic="false"` in Manifest

### Vor Phase 4 Store Launch

- [ ] Privacy Policy online und aktuell
- [ ] App Store Privacy Labels korrekt ausgefuellt
- [ ] Google Play Data Safety Section korrekt ausgefuellt
- [ ] Keine Debug-Konfigurationen in Release-Build
- [ ] ProGuard/R8 Rules korrekt (keine Secrets im APK)
- [ ] iOS: Entitlements nur die noetigsten
- [ ] Penetration Test (optional, empfohlen)

---

## 7. Threat Model (vereinfacht)

| Bedrohung | Risiko | Mitigation |
|-----------|--------|-----------|
| Token-Diebstahl via Netzwerk | Niedrig | TLS 1.2+, kein HTTP |
| Token-Diebstahl via Geraet | Niedrig | Keychain/Keystore, Geraeteverschluesselung |
| Man-in-the-Middle | Niedrig | TLS, Optional: Certificate Pinning (Phase 3) |
| Brute-Force Login | Niedrig | B2C Smart Lockout (Microsoft-verwaltet) |
| SQL Injection (lokal) | Sehr niedrig | SwiftData/@Query bzw. Room/@Query (parametrisiert) |
| Datenleck bei App-Deinstallation | Niedrig | Daten liegen im Backend, lokal wird geloescht |
| Jailbreak/Root Access | Mittel | Akzeptiertes Risiko (kein Jailbreak-Detection) |
