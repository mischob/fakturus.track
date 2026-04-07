# EPIC 10: Offline-UX & Polish

## Ziel

Die Offline-First-Erfahrung abrunden: Offline-Banner, Sync-Status-Indikator, erste Synchronisation nach Login und uebergreifende Fehlerbehandlung. Die App fuehlt sich auch ohne Netzwerk vollwertig an.

## Abhaengigkeiten

- **E07**: Sync-Engine (Sync-Status fuer Anzeige)
- **E02**: Auth (fuer erste Synchronisation nach Login)
- **E04**: NetworkMonitor (fuer Online/Offline-Erkennung)

---

## Stories

### P1-E10-S01: iOS OfflineBanner

**Als** Nutzer
**moechte ich** sofort sehen wenn ich offline bin,
**damit** ich weiss dass meine Daten lokal gespeichert und spaeter synchronisiert werden.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E04-S03 (NetworkMonitor)
**Parallelisierbar mit**: P1-E10-S02 (Android Banner), P1-E10-S03/S04 (Sync-Status)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `OfflineBanner.swift`:
  - Gelber/oranger Hintergrund (`offline-banner` Farbe)
  - Warning-Icon + Text: "Offline -- Aenderungen werden lokal gespeichert und spaeter synchronisiert"
  - Positioniert am oberen Bildschirmrand, unter der Navigation Bar
- [ ] Animation:
  - Slide-In von oben wenn offline
  - Slide-Out nach oben wenn wieder online
  - `.transition(.move(edge: .top).combined(with: .opacity))`
- [ ] Reaktivitaet:
  - Given Nutzer ist online
  - When Netzwerk faellt aus
  - Then Banner erscheint mit Animation
  - When Netzwerk wird wiederhergestellt
  - Then Banner verschwindet mit Animation
- [ ] In ContentView integriert (ueber allen Tabs sichtbar)

**Technische Hinweise**:
- `@Environment(NetworkMonitor.self)` fuer `isConnected` State
- `.safeAreaInset(edge: .top)` oder Overlay fuer Positionierung
- `withAnimation(.spring()) { }` fuer Transitions

---

### P1-E10-S02: Android OfflineBanner

**Als** Nutzer
**moechte ich** sofort sehen wenn ich offline bin,
**damit** ich weiss dass meine Daten lokal gespeichert werden.

**Plattform**: Android
**Abhaengigkeiten**: P1-E04-S04 (NetworkMonitor)
**Parallelisierbar mit**: P1-E10-S01 (iOS Banner), P1-E10-S03/S04 (Sync-Status)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `OfflineBanner.kt` als Composable:
  - Material 3 Banner oder custom Surface mit `OfflineBanner`-Farbe
  - Warning-Icon + Text (analog iOS)
- [ ] Animation:
  - `AnimatedVisibility(visible = !isConnected, enter = slideInVertically(), exit = slideOutVertically())`
- [ ] Reaktivitaet analog iOS
- [ ] In MainScreen Scaffold integriert (ueber allen Tabs)

**Technische Hinweise**:
- `val isConnected by networkMonitor.isConnected.collectAsState()`
- Positionierung: `Column { OfflineBanner(); NavHost() }` oder `Scaffold { topBar = { OfflineBanner() } }`

---

### P1-E10-S03: iOS SyncStatusIndicator

**Als** Nutzer
**moechte ich** den Sync-Status meiner Daten sehen koennen,
**damit** ich weiss ob meine Aenderungen im Backend angekommen sind.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E07-S01 (SyncEngine mit isSyncing State)
**Parallelisierbar mit**: P1-E10-S04 (Android Sync-Status), P1-E10-S01 (Banner)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `SyncStatusView.swift`:
  - **Synced**: Gruener Haken + "Synchronisiert" (verschwindet nach 3 Sekunden)
  - **Syncing**: Rotierende Pfeile (SF Symbol `arrow.triangle.2.circlepath`) + "Synchronisiere..."
  - **Pending**: Gelber Pfeil + "Ausstehend" (X Aenderungen)
  - **Error**: Rotes Kreuz + "Sync fehlgeschlagen"
- [ ] In der Navigation Bar des Zeiten-Tabs (rechts, als Toolbar-Item)
- [ ] Sync-Button in Toolbar: Tap -> manueller Sync
  - Waehrend Sync: Button zeigt rotierendes Icon
  - Nach Sync: Kurz gruener Haken, dann normales Icon

**Technische Hinweise**:
- `.rotationEffect(Angle.degrees(rotation))` mit Animation fuer rotierende Pfeile
- Timer fuer "Synchronisiert"-Anzeige: 3 Sekunden, dann zurueck zu Normal
- `.toolbar { ToolbarItem(placement: .topBarTrailing) { SyncButton() } }`

---

### P1-E10-S04: Android SyncStatusIndicator

**Als** Nutzer
**moechte ich** den Sync-Status meiner Daten sehen koennen,
**damit** ich weiss ob alles synchronisiert ist.

**Plattform**: Android
**Abhaengigkeiten**: P1-E07-S02 (SyncEngine)
**Parallelisierbar mit**: P1-E10-S03 (iOS Sync-Status), P1-E10-S02 (Banner)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `SyncStatusIndicator.kt`:
  - Gleiche 4 Zustaende wie iOS (Synced, Syncing, Pending, Error)
  - Material Icons: `CloudDone`, `Sync`, `CloudUpload`, `SyncProblem`
- [ ] In TopAppBar des TimeTrackingScreen als Action-Icon
- [ ] Sync-Button: IconButton, Tap -> manueller Sync
  - Waehrend Sync: `animateFloatAsState` Rotation auf Icon
  - Nach Sync: kurz CloudDone, dann Normal

**Technische Hinweise**:
- `val isSyncing by syncEngine.isSyncing.collectAsState()`
- Rotation: `Modifier.rotate(rotationAngle)` mit `InfiniteTransition`

---

### P1-E10-S05: Erste Synchronisation nach Login (Beide Plattformen)

**Als** neuer Nutzer
**moechte ich** nach dem ersten Login meine bestehenden Daten vom Server laden,
**damit** ich sofort meine vollstaendige Historie sehe.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E07-S01/S02 (SyncEngine), P1-E02-S01/S02 (Auth)
**Parallelisierbar mit**: P1-E10-S01/S02 (Banner)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Nach erfolgreichem Login:
  - Ladebildschirm: "Daten werden geladen..." mit Spinner
  - `syncEngine.syncAll()` wird aufgerufen
  - Nach Abschluss: Navigation zum Zeiten-Tab
- [ ] Given neuer Nutzer mit 50 Sessions im Backend
  When erster Login in der nativen App
  Then werden alle 50 Sessions in die lokale DB synchronisiert
  And der Nutzer sieht seine vollstaendige Historie
- [ ] Given Nutzer ohne Backend-Daten (neuer Account)
  When erster Login
  Then leerer Zeiten-Tab mit "Bereit fuer den ersten Eintrag"
- [ ] Fehlerfall:
  - Given Netzwerk bricht waehrend Erst-Sync ab
  - Then Fehlermeldung mit "Erneut versuchen"-Button
  - And Nutzer kann auch ohne Sync weitermachen (leere lokale DB)
- [ ] Ladeanzeige:
  - iOS: ProgressView mit Text
  - Android: CircularProgressIndicator mit Text

**Technische Hinweise**:
- Zwischenschirm nach Login, vor Tab-Navigation
- iOS: Eigene `InitialSyncView` als Zwischenzustand
- Android: Eigene `InitialSyncScreen` Composable
- Timeout: Nach 30 Sekunden abbrechen und trotzdem zur App navigieren

---

### P1-E10-S06: Error-Handling Polish (Beide Plattformen)

**Als** Nutzer
**moechte ich** bei Fehlern verstaendliche Meldungen sehen,
**damit** ich weiss was passiert ist und was ich tun kann.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E07-S05 (Sync-Integration), P1-E05-S05/S06 (ViewModels)
**Parallelisierbar mit**: P1-E10-S01-S04 (andere Polish-Stories)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Auth-Fehler** (Session abgelaufen):
  - Alert/Dialog: "Sitzung abgelaufen. Bitte melden Sie sich erneut an."
  - "Anmelden"-Button -> Login-Screen
  - Lokale Daten bleiben erhalten
- [ ] **Sync-Fehler** (Netzwerk/Server):
  - Toast/Snackbar: "Synchronisation fehlgeschlagen. Ihre Daten sind lokal gespeichert."
  - Kein Datenverlust
  - Automatischer Retry beim naechsten Trigger
- [ ] **Validierungsfehler** (Zeiterfassung):
  - Inline-Fehler unter dem Feld: "Endzeit muss nach Startzeit liegen"
  - Speichern-Button deaktiviert bei Fehler
- [ ] **Server-Fehler** (500):
  - Toast: "Serverfehler. Bitte versuchen Sie es spaeter erneut."
  - Daten bleiben lokal
- [ ] Alle Fehlermeldungen auf Deutsch
- [ ] Keine technischen Details in User-facing Fehlern (kein Stacktrace, kein HTTP-Code)

**Technische Hinweise**:
- iOS: `.alert()` fuer Auth, `.snackbar()` custom oder Toast-Library fuer Sync
- Android: `SnackbarHost` fuer Sync, `AlertDialog` fuer Auth
- Fehler-Texte als String-Konstanten (spaeter Lokalisierung)
