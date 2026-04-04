# EPIC 11: Offline-Login & Session-Persistierung

## Ziel

Nutzer koennen die App auch ohne Netzwerkverbindung oeffnen und sofort nutzen, sofern sie sich zuvor mindestens einmal erfolgreich eingeloggt haben. Die App erkennt den Netzwerkstatus sofort (keine Wartezeit) und entscheidet automatisch, ob ein Offline-Zugang moeglich ist oder ein Online-Login erforderlich ist.

## Abhaengigkeiten

- **E02**: Authentifizierung (AuthManager, MSAL Integration)
- **E04**: API-Client & Netzwerk (NetworkMonitor)
- **E10**: Offline-UX (OfflineBanner, Fehlerbehandlung)

## Konzept-Zusammenfassung

**Kernidee**: Lokale Session-Persistierung mit zeitlich begrenztem Offline-Zugang.

Die App speichert nach jedem erfolgreichen Login/Token-Refresh eine lokale Session (User-ID, Name, letzter erfolgreicher Auth-Zeitpunkt). Beim App-Start wird sofort der Netzwerkstatus geprueft. Ist kein Netz vorhanden und existiert eine gueltige lokale Session (nicht aelter als 14 Tage), wird der Nutzer direkt in die App gelassen. Alle Daten werden lokal gespeichert und beim naechsten Online-Zugang synchronisiert.

**Sicherheitsmodell**: Der Offline-Zugang ist auf 14 Tage begrenzt (analog zur Refresh-Token-Lebensdauer). Danach muss sich der Nutzer online anmelden. Die lokale Session wird bei Logout explizit geloescht. Biometrische Absicherung (Face ID / Fingerprint) ist als optionales Feature vorgesehen, wird aber als separate Story geplant (nicht in diesem Epic).

---

## Stories

### P1-E11-S01: Lokale Session-Persistierung (Beide Plattformen)

**Als** Nutzer
**moechte ich** dass die App sich merkt dass ich eingeloggt bin,
**damit** ich die App auch offline oeffnen und sofort nutzen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E02-S01/S02 (AuthManager)
**Parallelisierbar mit**: P1-E11-S02 (Schnelle Netzwerkerkennung)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `OfflineSession`-Modell erstellt mit folgenden Feldern:
  - `userId: String` (aus MSAL Account, OID-Claim)
  - `displayName: String` (aus Name-Claim)
  - `email: String` (aus Email-Claim)
  - `lastSuccessfulAuth: Date` (Zeitpunkt des letzten erfolgreichen Token-Erhalts)
  - `loginProvider: LoginProvider` (Apple/Google/Email)
- [ ] Sichere Speicherung:
  - iOS: Keychain via `SecItemAdd/SecItemCopyMatching` (Service: `com.fakturus.track.offline-session`)
  - Android: EncryptedSharedPreferences (MasterKey, AES256-GCM)
- [ ] Session wird automatisch geschrieben/aktualisiert bei:
  - Erfolgreicher interaktiver Anmeldung
  - Erfolgreichem Silent Token Refresh
- [ ] Session wird geloescht bei:
  - Explizitem Logout durch den Nutzer
  - Manueller Session-Invalidierung (z.B. bei Sicherheitsvorfall)
- [ ] Session ist NICHT gueltig wenn:
  - `lastSuccessfulAuth` aelter als 14 Tage
  - Keine Session vorhanden (erster App-Start)
- [ ] Given Nutzer loggt sich erfolgreich ein
  When App wird geschlossen und ohne Netz neu geoeffnet
  Then ist eine gueltige OfflineSession vorhanden
- [ ] Given Nutzer loggt sich aus
  When App wird ohne Netz geoeffnet
  Then ist keine OfflineSession vorhanden, Login-Screen wird gezeigt

**Technische Hinweise**:
- iOS: Keychain ist sicherer als UserDefaults und ueberlebt App-Updates
- Android: EncryptedSharedPreferences nutzt AndroidX Security, benoetigt `implementation("androidx.security:security-crypto:1.1.0-alpha06")`
- NICHT in der SQLite-Datenbank speichern (zu leicht auslesbar)
- Session-Daten sind minimal gehalten -- nur was fuer die Offline-Anzeige noetig ist

---

### P1-E11-S02: Schnelle Netzwerkerkennung beim App-Start (Beide Plattformen)

**Als** Nutzer
**moechte ich** beim App-Start sofort wissen ob ich online oder offline bin,
**damit** ich nicht 30 Sekunden warten muss bis ein Login-Versuch fehlschlaegt.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E04-S03/S04 (NetworkMonitor)
**Parallelisierbar mit**: P1-E11-S01 (Session-Persistierung)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Netzwerkstatus ist innerhalb von max. 500ms nach App-Start verfuegbar
- [ ] iOS: `NWPathMonitor` liefert sofort den aktuellen Status beim Start
  - `pathUpdateHandler` wird synchron beim Start aufgerufen
  - Kein HTTP-Request noetig fuer die initiale Erkennung
- [ ] Android: `ConnectivityManager.getActiveNetwork()` liefert sofort den Status
  - Kein Timeout, kein HTTP-Request noetig
- [ ] `NetworkMonitor.isConnected` ist beim ersten UI-Frame korrekt gesetzt
- [ ] Given Geraet hat kein Netz
  When App startet
  Then weiss die App innerhalb <500ms dass kein Netz vorhanden ist
- [ ] Given Geraet hat WLAN ohne Internet (Captive Portal)
  When App startet
  Then wird der Status als "offline" behandelt (konservativer Ansatz)
  Note: NWPathMonitor.status == .satisfied bedeutet nur Link-Layer, kein Internet. Optional kann ein schneller Ping ergaenzt werden.

**Technische Hinweise**:
- Der bestehende `NetworkMonitor` (E04) wird erweitert, NICHT ersetzt
- iOS `NWPathMonitor` gibt den Status sofort beim ersten `pathUpdateHandler`-Callback
- Android `ConnectivityManager` hat sofort verfuegbare Methoden (`getActiveNetwork()`, `getNetworkCapabilities()`)
- Fuer die Captive-Portal-Erkennung (Nice-to-have): Ein schneller HEAD-Request auf `https://api.track.fakturus.com/health` mit 2s Timeout. Nur wenn isConnected == true, um falsches "online" zu vermeiden. NICHT blockierend fuer den App-Start.
- Die Hauptlogik basiert auf dem OS-Level Netzwerkstatus (sofort verfuegbar), der Health-Check ist optional und verbessert nur die Genauigkeit

---

### P1-E11-S03: App-Start-Entscheidungslogik (Beide Plattformen)

**Als** Nutzer
**moechte ich** beim App-Start automatisch in den richtigen Modus geleitet werden,
**damit** ich die App so schnell wie moeglich nutzen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E11-S01 (Session), P1-E11-S02 (Netzwerkerkennung), P1-E02-S01/S02 (AuthManager)
**Parallelisierbar mit**: P1-E11-S04 (Login-Screen Erweiterung)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `AppStartCoordinator` (oder Integration in bestehenden AuthManager) implementiert folgende Entscheidungsmatrix:

| Netzwerk | MSAL-Token gueltig | Offline-Session gueltig | Ergebnis |
|----------|-------------------|------------------------|----------|
| Online   | Ja (Cache-Hit)    | --                     | Direkt rein, Token nutzen |
| Online   | Nein              | --                     | Silent Refresh versuchen, bei Erfolg rein, bei Fehler Interactive Login |
| Offline  | Ja (Cache-Hit)    | --                     | Direkt rein, Token aus Cache nutzen (fuer lokale Ops) |
| Offline  | Nein              | Ja (<14 Tage)          | Offline-Modus, App nutzbar, Sync wenn wieder online |
| Offline  | Nein              | Nein (>14 Tage)        | Login-Screen mit Hinweis: "Internetverbindung erforderlich" |
| Offline  | Nein              | Nicht vorhanden        | Login-Screen mit Hinweis: "Internetverbindung erforderlich" |

- [ ] Die Entscheidung dauert maximal 1 Sekunde (kein Netzwerk-Timeout abwarten)
- [ ] Given Nutzer war vor 3 Tagen zuletzt online
  When App offline geoeffnet wird
  Then kommt der Nutzer direkt in die App (Offline-Modus)
  And OfflineBanner wird angezeigt
- [ ] Given Nutzer war vor 20 Tagen zuletzt online
  When App offline geoeffnet wird
  Then sieht der Nutzer den Login-Screen mit Hinweis
- [ ] Given Nutzer oeffnet App zum allerersten Mal (kein Account, keine Session)
  When kein Netz vorhanden
  Then sieht der Nutzer den Login-Screen mit Hinweis "Fuer die erste Anmeldung wird eine Internetverbindung benoetigt"
- [ ] Given Nutzer ist im Offline-Modus
  When Netzwerk wird verfuegbar
  Then versucht die App im Hintergrund einen Silent Token Refresh
  And bei Erfolg wechselt der Status zu "Online" und Sync startet
  And bei Fehler (Refresh Token abgelaufen) wird der Nutzer informiert, kann aber weiterarbeiten
- [ ] Kein Datenverlust: Auch wenn der Nutzer nach Offline-Arbeit einen Re-Login braucht, bleiben alle lokalen Daten erhalten und werden nach Login synchronisiert

**Technische Hinweise**:
- Die Reihenfolge ist entscheidend: Erst Netzwerkstatus pruefen, dann Token-Cache, dann lokale Session
- MSAL cached Tokens intern -- `acquireTokenSilently()` kann auch offline einen gueltigen Cache-Hit liefern wenn der Access Token noch nicht abgelaufen ist
- Der Offline-Modus setzt `AuthManager.isAuthenticated = true` basierend auf der lokalen Session
- Der `userId` aus der lokalen Session wird fuer lokale Datenbankoperationen verwendet
- Wenn online und Silent Refresh fehlschlaegt: NICHT sofort zum Login-Screen, sondern pruefen ob Offline-Session als Fallback nutzbar ist

---

### P1-E11-S04: Login-Screen Erweiterung fuer Offline-Szenarien (Beide Plattformen)

**Als** Nutzer
**moechte ich** auf dem Login-Screen verstehen warum ich mich anmelden muss und was ich tun kann,
**damit** ich nicht verwirrt bin wenn die App eine Anmeldung verlangt.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E02-S03/S04 (Login-Screen), P1-E11-S02 (Netzwerkerkennung)
**Parallelisierbar mit**: P1-E11-S01 (Session), P1-E11-S03 (Entscheidungslogik)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Login-Screen zeigt kontextabhaengige Meldungen:
  - **Erster Start, kein Netz**: 
    - Info-Box (gelb/orange): "Fuer die erste Anmeldung wird eine Internetverbindung benoetigt."
    - Login-Buttons sind deaktiviert (ausgegraut)
  - **Session abgelaufen, kein Netz**: 
    - Info-Box: "Ihre Sitzung ist abgelaufen. Bitte stellen Sie eine Internetverbindung her, um sich erneut anzumelden."
    - Login-Buttons sind deaktiviert
  - **Netz vorhanden, normaler Login**: 
    - Kein zusaetzlicher Hinweis (Standard-Verhalten wie bisher)
  - **Netz wird wiederhergestellt waehrend Login-Screen offen**: 
    - Info-Box verschwindet mit Animation
    - Login-Buttons werden aktiviert
- [ ] Given Nutzer ist auf Login-Screen ohne Netz
  When WiFi wird eingeschaltet
  Then werden die Login-Buttons innerhalb von 2 Sekunden aktiviert
  And die Offline-Meldung verschwindet
- [ ] Keine technischen Fehlermeldungen (kein "MSAL Error", kein "Token expired")
- [ ] Texte sind verstaendlich fuer nicht-technische Nutzer

**Technische Hinweise**:
- Bestehende `LoginView.swift` / `LoginScreen.kt` erweitern, NICHT neu erstellen
- `@Environment(NetworkMonitor.self)` / `networkMonitor.isConnected.collectAsState()` einbinden
- Neuer Parameter oder State: `loginContext: LoginContext` (.firstLogin, .sessionExpired, .normal)
- Buttons deaktivieren: `.disabled(!networkMonitor.isConnected)` / `enabled = isConnected`

---

### P1-E11-S05: Offline-Session-Ablauf-Warnung (Beide Plattformen)

**Als** Nutzer
**moechte ich** gewarnt werden bevor meine Offline-Sitzung ablaeuft,
**damit** ich rechtzeitig eine Internetverbindung herstellen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E11-S01 (Session), P1-E11-S03 (Entscheidungslogik)
**Parallelisierbar mit**: P1-E11-S04 (Login-Screen Erweiterung)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Warnung wird angezeigt wenn:
  - Nutzer ist im Offline-Modus UND
  - `lastSuccessfulAuth` ist aelter als 12 Tage (2 Tage vor Ablauf)
- [ ] Warnung als nicht-blockierender Banner/Toast:
  - Text: "Ihre Offline-Sitzung laeuft in X Tagen ab. Bitte stellen Sie eine Internetverbindung her, um sich erneut anzumelden."
  - X = Differenz zwischen 14 Tagen und aktuellem Alter der Session
- [ ] Warnung erscheint maximal einmal pro App-Start (nicht wiederholt nerven)
- [ ] Given Nutzer ist seit 13 Tagen offline
  When App geoeffnet wird
  Then Warnung: "Ihre Offline-Sitzung laeuft in 1 Tag ab..."
- [ ] Given Nutzer ist seit 10 Tagen offline
  When App geoeffnet wird
  Then keine Warnung (noch mehr als 2 Tage)
- [ ] Given Nutzer stellt Internetverbindung her und Token-Refresh gelingt
  When Warnung wurde angezeigt
  Then verschwindet die Warnung und Session-Timer wird zurueckgesetzt

**Technische Hinweise**:
- Berechnung: `14 - daysSince(lastSuccessfulAuth)`
- Warnung ab Tag 12 (also wenn <= 2 Tage uebrig)
- iOS: `.alert()` oder InfoBanner oberhalb des Contents
- Android: `Snackbar` mit `SnackbarDuration.Long` oder InfoBanner
- Flag `hasShownExpiryWarning` im ViewModel (nicht persistiert, pro App-Start)

---

### P1-E11-S06: Hintergrund-Token-Refresh bei Netzwerkwechsel (Beide Plattformen)

**Als** Nutzer im Offline-Modus
**moechte ich** dass die App automatisch versucht meine Sitzung zu verlaengern wenn wieder Netz da ist,
**damit** ich mich nicht manuell neu anmelden muss.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E11-S03 (Entscheidungslogik), P1-E04-S03/S04 (NetworkMonitor), P1-E02-S01/S02 (AuthManager)
**Parallelisierbar mit**: P1-E11-S05 (Ablauf-Warnung)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Wenn `NetworkMonitor` einen Wechsel von offline -> online erkennt:
  - Automatischer `acquireTokenSilently()` Versuch
  - Bei Erfolg: `lastSuccessfulAuth` in OfflineSession aktualisieren, Sync starten
  - Bei Fehler: Kein sichtbarer Fehler (Nutzer kann weiterarbeiten), naechster Versuch beim naechsten Netzwerkwechsel oder App-Start
- [ ] Kein Retry-Loop: Maximal 1 Versuch pro Netzwerkwechsel-Event
- [ ] Given Nutzer war 5 Tage offline
  When WiFi wird eingeschaltet
  Then versucht die App im Hintergrund einen Silent Token Refresh
  And bei Erfolg wird die Offline-Session erneuert (lastSuccessfulAuth = jetzt)
  And Sync-Engine wird getriggert
- [ ] Given Refresh Token ist abgelaufen (>14 Tage offline)
  When Netz wird verfuegbar
  Then schlaegt Silent Refresh fehl
  And Nutzer kann weiterarbeiten
  And beim naechsten App-Start wird Interactive Login verlangt

**Technische Hinweise**:
- Bestehenden NetworkMonitor-Listener erweitern
- iOS: `networkMonitor.pathUpdateHandler` -> wenn `path.status == .satisfied` UND vorher `.unsatisfied`
- Android: `ConnectivityManager.NetworkCallback.onAvailable()` -> Token Refresh triggern
- Debounce: Netzwerkwechsel koennen schnell hintereinander kommen (z.B. Flugmodus an/aus), daher 2 Sekunden Debounce
- Der Token Refresh laeuft auf einem Background-Thread, blockiert nicht die UI
