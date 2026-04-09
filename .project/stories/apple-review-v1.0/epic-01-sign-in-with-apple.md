# EPIC 01: Sign in with Apple Fix

## Ziel

Der Apple Reviewer erhielt einen Fehler beim Login mit "Sign in with Apple". Dies ist ein Blocker fuer die Store-Freigabe (Guideline 2.1(a) - App Completeness).

## Abhaengigkeiten

- Azure B2C Konfiguration (Apple als Identity Provider)
- Apple Developer Account (Service ID, Key fuer Sign in with Apple)

---

## Analyse

### Aktuelle Implementierung

Die App nutzt **Azure B2C** (MSAL SDK) fuer alle Login-Provider. "Sign in with Apple" wird NICHT nativ ueber `ASAuthorization` abgewickelt, sondern ueber Azure B2C mit `domain_hint: "apple.com"`.

**Relevante Dateien:**
- `FakturusTrack.iOS/FakturusTrack/Features/Auth/LoginView.swift` -- UI mit `SignInWithAppleButton` Overlay
- `FakturusTrack.iOS/FakturusTrack/Services/Auth/AuthManager.swift` -- MSAL Token-Akquisition
- `FakturusTrack.iOS/FakturusTrack/App/Configuration.swift` -- B2C Endpoints

**Login-Flow:**
1. User tippt "Sign in with Apple" Button
2. `login(provider: .apple)` wird aufgerufen
3. `AuthManager.acquireTokenInteractively()` oeffnet MSAL WebView
4. MSAL leitet an Azure B2C weiter mit `domain_hint=apple.com`
5. B2C leitet an Apple Sign-In weiter
6. Nach Apple-Auth leitet B2C zurueck zur App

### Moegliche Fehlerquellen

1. **Azure B2C Apple Federation**: Apple als Identity Provider in B2C nicht korrekt konfiguriert (Service ID, Private Key, Team ID)
2. **Apple Developer Portal**: "Sign in with Apple" Service ID nicht fuer B2C Return-URL registriert
3. **B2C Custom Policy**: Apple-Flow in der B2C Policy fehlerhaft oder fehlend
4. **Redirect URI**: B2C Redirect nach Apple-Auth stimmt nicht mit registrierter URI ueberein
5. **Review-Umgebung**: Der Reviewer nutzt einen Apple-Account der in der B2C-Sandbox nicht funktioniert

---

## Stories

### ARV1-E01-S01: Sign in with Apple Fehler reproduzieren

**Als** Entwickler
**moechte ich** den Sign-in-with-Apple Fehler auf einem echten Geraet reproduzieren,
**damit** ich die genaue Fehlermeldung und Ursache identifizieren kann.

**Plattform**: iOS
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] App auf physischem iPhone installiert (Release-Build, nicht Debug)
- [ ] "Sign in with Apple" Button angetippt
- [ ] Fehler reproduziert und exakte Fehlermeldung dokumentiert
- [ ] MSAL-Fehlerlogs erfasst (Domain, Code, Description)
- [ ] Azure B2C Logs im Azure Portal geprueft (Sign-in Logs unter B2C Tenant > Monitoring)
- [ ] Falls nicht reproduzierbar: Test mit frischem Apple-Account (noch nie bei B2C angemeldet)

**Technische Hinweise**:
- AuthManager loggt bereits: `[AuthManager] Login error: {domain} code={code} {description}`
- Azure B2C Sign-in Logs: Azure Portal > B2C Tenant > Monitoring > Sign-in logs
- Pruefen ob der Apple Identity Provider im B2C User Flow aktiv ist

---

### ARV1-E01-S02: Azure B2C Apple Federation pruefen und fixen

**Als** Entwickler
**moechte ich** die Azure B2C Apple-Federation-Konfiguration validieren,
**damit** Sign in with Apple zuverlaessig funktioniert.

**Plattform**: Azure Portal + Apple Developer Portal
**Abhaengigkeiten**: ARV1-E01-S01 (Fehler identifiziert)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Apple Developer Portal** geprueft:
  - Service ID fuer "Sign in with Apple" existiert
  - Return URL enthaelt die B2C-Redirect-URI (z.B. `https://{tenant}.b2clogin.com/{tenant}.onmicrosoft.com/oauth2/authresp`)
  - Domains and Subdomains enthaelt die B2C-Domain
  - Private Key ist gueltig (nicht abgelaufen)
- [ ] **Azure B2C** geprueft:
  - Apple Identity Provider konfiguriert mit korrektem Service ID, Team ID und Private Key
  - User Flow / Custom Policy enthaelt Apple als Option
  - Claim Mapping korrekt (Apple liefert `sub`, `email`, `name`)
- [ ] **Redirect URI** korrekt:
  - B2C erwartet die gleiche Redirect URI die in Apple registriert ist
  - Kein Mismatch zwischen Staging/Production URIs
- [ ] Fix implementiert basierend auf gefundener Ursache
- [ ] Given ein Apple Reviewer (oder Testnutzer) tippt "Sign in with Apple"
  When die Apple-Authentifizierung erfolgreich ist
  Then wird der User in die App eingeloggt ohne Fehlermeldung

**Technische Hinweise**:
- Apple Key hat 6 Monate Gueltigkeit -- pruefen ob abgelaufen
- B2C Custom Policy Referenz: `<ClaimsProvider>` mit `<Domain>apple.com</Domain>`
- Haeufiges Problem: Apple liefert `email` nur beim ERSTEN Login -- B2C muss damit umgehen
- Apple verlangt seit 2024 dass die Return URL in "Domains and Subdomains" UND "Return URLs" eingetragen ist

---

### ARV1-E01-S03: Sign in with Apple E2E-Test

**Als** QA-Tester
**moechte ich** den kompletten Apple-Sign-In-Flow testen,
**damit** wir sicher sind dass der Reviewer keinen Fehler mehr bekommt.

**Plattform**: iOS (physisches Geraet)
**Abhaengigkeiten**: ARV1-E01-S02 (Fix implementiert)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Release-Build (kein Debug) auf physischem iPhone
- [ ] Erster Login mit Apple-Account: Erfolgreich, User landet in der App
- [ ] Zweiter Login (nach Logout) mit gleichem Apple-Account: Erfolgreich
- [ ] Login mit "Hide My Email" Option: Erfolgreich
- [ ] Login nach App-Deinstallation und Neuinstallation: Erfolgreich
- [ ] Login auf einem Geraet das noch nie mit dieser App genutzt wurde: Erfolgreich
- [ ] Fehlermeldung im Error-Fall ist benutzerfreundlich (kein technischer Stack Trace)
- [ ] Test-Ergebnisse in "Notes for Reviewers" dokumentiert (App Store Connect)
