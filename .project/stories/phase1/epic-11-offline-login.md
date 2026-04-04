# EPIC 11: Offline-Login & Session-Persistierung

## Ziel

Nutzer koennen die App auch ohne Netzwerkverbindung oeffnen und sofort nutzen, sofern sie sich zuvor mindestens einmal erfolgreich eingeloggt haben. Die App erkennt den Netzwerkstatus sofort (keine Wartezeit) und entscheidet automatisch, ob ein Offline-Zugang moeglich ist oder ein Online-Login erforderlich ist.

## Abhaengigkeiten

- **E02**: Authentifizierung (AuthManager, MSAL Integration)
- **E04**: API-Client & Netzwerk (NetworkMonitor)
- **E10**: Offline-UX (OfflineBanner, Fehlerbehandlung)

## Konzept-Zusammenfassung

**Kernidee**: Lokale Session-Persistierung mit zeitlich begrenztem Offline-Zugang.

Die App speichert nach jedem erfolgreichen Login/Token-Refresh eine minimale lokale Session (User-ID, letzter erfolgreicher Auth-Zeitpunkt). Beim App-Start wird sofort der Netzwerkstatus geprueft. Ist kein Netz vorhanden und existiert eine gueltige lokale Session (nicht aelter als 14 Tage), wird der Nutzer direkt in die App gelassen. Alle Daten werden lokal gespeichert und beim naechsten Online-Zugang synchronisiert.

**Sicherheitsmodell**: Der Offline-Zugang ist auf 14 Tage begrenzt (analog zur Refresh-Token-Lebensdauer der B2C-Policy). Danach muss sich der Nutzer online anmelden. Die lokale Session wird bei Logout explizit geloescht. Die Session enthaelt absichtlich keine PII (kein Name, keine E-Mail) -- nur eine User-ID und einen Zeitstempel.

**Architektur**: Die Entscheidungslogik wird als `resolveStartState()` Methode direkt im AuthManager implementiert (kein separater Coordinator -- konsistent mit ADR-002 und ADR-006).

---

## Stories

### P1-E11-S01: Lokale Session-Persistierung (Beide Plattformen)

**Als** Nutzer
**moechte ich** dass die App sich merkt dass ich eingeloggt bin,
**damit** ich die App auch offline oeffnen und sofort nutzen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E02-S01/S02 (AuthManager)
**Parallelisierbar mit**: P1-E11-S03 (Login-Screen Erweiterung)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `OfflineSession`-Modell erstellt mit folgenden Feldern:
  - `userId: String` (aus MSAL Account, OID-Claim)
  - `lastSuccessfulAuth: Date` (Zeitpunkt des letzten erfolgreichen Token-Erhalts)
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
- Android: EncryptedSharedPreferences nutzt AndroidX Security (`implementation("androidx.security:security-crypto:1.1.0-alpha06")`) -- keine stable Version verfuegbar, aber de-facto Branchenstandard
- NICHT in der SQLite-Datenbank speichern (zu leicht auslesbar)
- Session enthaelt absichtlich keine PII (kein displayName, keine email) -- diese Daten koennen nach Offline-Start aus der lokalen SQLite-DB geladen werden
- 14-Tage-Limit ist analog zur B2C Refresh-Token-Lebensdauer. Eine Aenderung ergibt nur Sinn wenn die B2C-Policy geaendert wird.

---

### P1-E11-S02: App-Start-Entscheidungslogik (Beide Plattformen)

**Als** Nutzer
**moechte ich** beim App-Start automatisch in den richtigen Modus geleitet werden,
**damit** ich die App so schnell wie moeglich nutzen kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E11-S01 (Session), P1-E04-S03/S04 (NetworkMonitor), P1-E02-S01/S02 (AuthManager)
**Parallelisierbar mit**: P1-E11-S03 (Login-Screen Erweiterung)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `AuthManager.resolveStartState()` implementiert folgende Entscheidungsmatrix:

| Netzwerk | MSAL-Token gueltig | Offline-Session gueltig | Ergebnis |
|----------|-------------------|------------------------|----------|
| Online   | Ja (Cache-Hit)    | --                     | Direkt rein, Token nutzen |
| Online   | Nein              | --                     | Silent Refresh versuchen, bei Erfolg rein, bei Fehler Interactive Login |
| Offline  | Ja (Cache-Hit)    | --                     | Direkt rein, Token aus Cache nutzen (fuer lokale Ops) |
| Offline  | Nein              | Ja (<14 Tage)          | Offline-Modus, App nutzbar, Sync wenn wieder online |
| Offline  | Nein              | Nein (>14 Tage)        | Login-Screen mit Hinweis: "Internetverbindung erforderlich" |
| Offline  | Nein              | Nicht vorhanden        | Login-Screen mit Hinweis: "Internetverbindung erforderlich" |

- [ ] Die Entscheidung dauert maximal 1 Sekunde (kein Netzwerk-Timeout abwarten)
- [ ] **WICHTIG**: Im Offline-Pfad darf KEIN MSAL-Netzwerkaufruf stattfinden:
  - Nur lokaler MSAL-Cache-Check (iOS: `forceRefresh = false`)
  - Wenn Cache leer oder expired: Direkt zur Offline-Session springen, MSAL NICHT weiter befragen
  - Begruendung: MSAL-interne Netzwerk-Timeouts koennen 30+ Sekunden dauern
- [ ] Netzwerkstatus ist innerhalb von max. 500ms nach App-Start verfuegbar:
  - iOS: `NWPathMonitor` liefert sofort den aktuellen Status
  - Android: `ConnectivityManager.getActiveNetwork()` liefert sofort den Status
  - Kein HTTP-Request noetig fuer die initiale Erkennung
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
  And bei Fehler (Refresh Token abgelaufen) kann der Nutzer weiterarbeiten
- [ ] Kein Datenverlust: Auch wenn der Nutzer nach Offline-Arbeit einen Re-Login braucht, bleiben alle lokalen Daten erhalten und werden nach Login synchronisiert
- [ ] Optional (Nice-to-have): Captive-Portal-Erkennung via schnellem HEAD-Request auf Health-Endpoint mit 2s Timeout. Nur wenn `isConnected == true`, um falsches "online" zu vermeiden. NICHT blockierend fuer den App-Start.

**Technische Hinweise**:
- Die Reihenfolge ist entscheidend: Erst Netzwerkstatus pruefen, dann Token-Cache (nur lokal!), dann lokale Session
- MSAL cached Tokens intern -- im Offline-Fall nur den lokalen Cache pruefen, KEINEN Refresh-Versuch starten
- Der Offline-Modus setzt `AuthManager.isAuthenticated = true` und `AuthManager.isOfflineMode = true`
- Der `userId` aus der lokalen Session wird fuer lokale Datenbankoperationen verwendet
- Wenn online und Silent Refresh fehlschlaegt: NICHT sofort zum Login-Screen, sondern pruefen ob Offline-Session als Fallback nutzbar ist
- Die Logik wird als `resolveStartState()` direkt auf dem AuthManager implementiert (KEIN separater Coordinator -- ADR-002, ADR-006)

---

### P1-E11-S03: Login-Screen Erweiterung fuer Offline-Szenarien (Beide Plattformen)

**Als** Nutzer
**moechte ich** auf dem Login-Screen verstehen warum ich mich anmelden muss und was ich tun kann,
**damit** ich nicht verwirrt bin wenn die App eine Anmeldung verlangt.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E02-S03/S04 (Login-Screen), P1-E04-S03/S04 (NetworkMonitor)
**Parallelisierbar mit**: P1-E11-S01 (Session)
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

### P1-E11-S04: Hintergrund-Token-Refresh bei Netzwerkwechsel (Beide Plattformen)

**Als** Nutzer im Offline-Modus
**moechte ich** dass die App automatisch versucht meine Sitzung zu verlaengern wenn wieder Netz da ist,
**damit** ich mich nicht manuell neu anmelden muss.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E11-S02 (Entscheidungslogik), P1-E04-S03/S04 (NetworkMonitor), P1-E02-S01/S02 (AuthManager)
**Parallelisierbar mit**: P1-E11-S03 (Login-Screen Erweiterung)
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
