# EPIC 07: Sync-Engine

## Ziel

Zuverlaessige bidirektionale Synchronisation zwischen lokaler Datenbank und Backend-API. Offline-Aenderungen werden bei Netzwerk-Verfuegbarkeit automatisch hochgeladen. Server ist Source of Truth bei Konflikten (Server-wins).

## Abhaengigkeiten

- **E03**: Lokale Datenschicht (Models/Entities mit Sync-Flags)
- **E04**: API-Client (HTTP-Kommunikation mit Backend)

---

## Stories

### P1-E07-S01: iOS SyncEngine (Kern-Orchestrierung)

**Als** Nutzer
**moechte ich** dass meine Daten automatisch synchronisiert werden,
**damit** sie auf allen Geraeten aktuell sind und nicht verloren gehen.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E04-S01 (APIClient), P1-E03-S01 (SwiftData), P1-E04-S03 (NetworkMonitor)
**Parallelisierbar mit**: P1-E07-S02 (Android SyncEngine), P1-E06-* (History UI)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `SyncEngine.swift` als Swift `actor`:
  - `isSyncing: Bool` Property (verhindert parallele Syncs)
  - `lastSyncDate: Date?` Property
  - Abhaengigkeiten: APIClient, NetworkMonitor, ModelContext
- [ ] `syncAll()` async Methode:
  - Guard: Nicht syncing UND Netzwerk verfuegbar
  - Ruft nacheinander auf: `syncWorkSessions()`, `syncVacationDays()`, `syncUserSettings()`
  - Fehler werden geloggt, nicht geworfen (kein Crash bei Sync-Fehler)
  - `isSyncing` wird korrekt auf true/false gesetzt (auch bei Fehler via defer)

- [ ] `syncWorkSessions()`:
  - Schritt 1: Pending Sessions sammeln (`isPendingSync == true && isFinished == true`)
  - Schritt 2a: Falls pending vorhanden -> POST `/v1/work-sessions/sync`
  - Schritt 2b: Falls keine pending -> GET `/v1/work-sessions`
  - Schritt 3: Synced Sessions lokal laden (`isSynced == true`)
  - Schritt 4: Server-ID-Set erstellen
  - Schritt 5: Set-Differenz -- lokale synced Sessions loeschen die nicht mehr im Backend sind
  - Schritt 6: Server-Sessions upserten (existiert lokal? -> update, neu? -> insert)
  - Schritt 7: Pending als synced markieren
  - Given 3 lokale pending Sessions und 5 Server-Sessions
  - When Sync ausgefuehrt wird
  - Then werden die 3 pending hochgeladen, alle 5 Server-Sessions lokal gemergt
  - And lokale Sessions die nicht im Server-Set sind werden geloescht

- [ ] `syncVacationDays()` (ACHTUNG: anders als WorkSessions!):
  - ALLE lokalen VacationDays senden (nicht nur pending)
  - POST `/v1/vacation-days/sync`
  - `DeletedIds` aus Response verarbeiten: lokale Eintraege gezielt loeschen
  - Server-VacationDays upserten
  - Given 10 lokale Urlaubstage (8 synced, 2 pending)
  - When Sync ausgefuehrt wird
  - Then werden ALLE 10 Tage an das Backend gesendet
  - And DeletedIds werden lokal entfernt

- [ ] `syncUserSettings()`:
  - GET `/v1/settings`
  - Last-Write-Wins basierend auf UpdatedAt
  - Lokale Settings neuer? -> PUT zum Server
  - Server neuer? -> Lokale ueberschreiben

**Technische Hinweise**:
- Siehe `.project/architecture/shared-concepts.md` fuer vollstaendigen Algorithmus
- `actor` Isolation verhindert Data Races bei parallelen Sync-Aufrufen
- ModelContext muss auf dem gleichen Thread/Actor genutzt werden (SwiftData Requirement)

---

### P1-E07-S02: Android SyncEngine (Kern-Orchestrierung)

**Als** Nutzer
**moechte ich** dass meine Daten automatisch synchronisiert werden,
**damit** sie auf allen Geraeten aktuell sind und nicht verloren gehen.

**Plattform**: Android
**Abhaengigkeiten**: P1-E04-S02 (APIClient), P1-E03-S02 (Room), P1-E04-S04 (NetworkMonitor)
**Parallelisierbar mit**: P1-E07-S01 (iOS SyncEngine), P1-E06-* (History UI)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `SyncEngine.kt` Klasse:
  - `isSyncing: StateFlow<Boolean>`
  - `lastSyncDate: StateFlow<Instant?>`
  - Abhaengigkeiten: APIClient, AppDatabase, NetworkMonitor
- [ ] `syncAll()` suspend:
  - Guard: `_isSyncing.value == false` und `networkMonitor.isConnected.value == true`
  - Gleiche Schritte wie iOS (WorkSessions, VacationDays, UserSettings)
  - try/catch um alles, Fehler loggen, nicht werfen
  - `_isSyncing.value` korrekt in finally-Block zuruecksetzen
- [ ] `syncWorkSessions()`: Gleicher Algorithmus wie iOS
  - Pending sammeln, Upload oder Fetch, Set-Differenz, Upsert
  - Room DAO-Methoden nutzen
- [ ] `syncVacationDays()`: Gleicher Algorithmus (ALLE senden, DeletedIds)
- [ ] `syncUserSettings()`: Last-Write-Wins
- [ ] SyncEngine-Initialisierung im ServiceContainer nach Login (`onLogin()`)

**Technische Hinweise**:
- Kein `actor` in Kotlin -- stattdessen `Mutex` oder `_isSyncing` Check
- Room DAOs sind suspend functions (Coroutine-kompatibel)
- `withContext(Dispatchers.IO)` fuer DB-Operationen (Room macht das meist automatisch)

---

### P1-E07-S03: iOS Sync-Trigger (Automatisch & Manuell)

**Als** Nutzer
**moechte ich** dass Sync automatisch bei bestimmten Ereignissen startet,
**damit** ich mich nicht manuell darum kuemmern muss.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E07-S01 (SyncEngine)
**Parallelisierbar mit**: P1-E07-S04 (Android Sync-Trigger)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **App-Start Sync**: Nach erfolgreichem Login sofort `syncAll()` aufrufen
- [ ] **Netzwerk-Wiederherstellung**: NetworkMonitor Callback -> wenn `isConnected` von false auf true wechselt -> `syncAll()`
- [ ] **Session Finish**: Nach `finishSession()` im ViewModel -> `syncAll()` triggern
- [ ] **Pull-to-Refresh**: `.refreshable { await syncEngine.syncAll() }` im TimeTrackingView
- [ ] **In-App Timer**: Wenn App aktiv, alle 30 Sekunden Sync ausfuehren
  - Timer starten bei `scenePhase == .active`
  - Timer stoppen bei `scenePhase != .active`
  - Kein Sync wenn bereits syncing oder offline
- [ ] **Background Fetch**: BGAppRefreshTask registrieren
  - Sync im Background ausfuehren
  - completionHandler korrekt aufrufen
  - BGTaskScheduler.shared.register(...) in AppDelegate

**Technische Hinweise**:
- In-App Timer: `Timer.publish(every: 30, on: .main, in: .common)` oder Task mit sleep
- Background Fetch: `BGAppRefreshTaskRequest(identifier: "com.fakturus.track.sync")`
- Scene-Phase via `@Environment(\.scenePhase)`

---

### P1-E07-S04: Android Sync-Trigger (Automatisch & Manuell)

**Als** Nutzer
**moechte ich** dass Sync automatisch bei bestimmten Ereignissen startet,
**damit** ich mich nicht manuell darum kuemmern muss.

**Plattform**: Android
**Abhaengigkeiten**: P1-E07-S02 (SyncEngine)
**Parallelisierbar mit**: P1-E07-S03 (iOS Sync-Trigger)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **App-Start Sync**: Nach Login in ServiceContainer.onLogin() -> syncAll()
- [ ] **Netzwerk-Wiederherstellung**: NetworkMonitor Flow collecten -> bei `true` -> syncAll()
- [ ] **Session Finish**: Nach finishSession() im ViewModel -> syncAll()
- [ ] **Pull-to-Refresh**: PullToRefresh im TimeTrackingScreen -> syncAll()
- [ ] **In-App Timer**: Coroutine-Loop mit 30s delay wenn App im Foreground
  - Lifecycle-aware (ProcessLifecycleOwner oder Activity-Lifecycle)
  - Stoppen bei Background
- [ ] **WorkManager**: Periodischer Sync im Background
  - `SyncWorker` als `CoroutineWorker`
  - `PeriodicWorkRequest` mit 15min Intervall (WorkManager Minimum)
  - Constraints: Network required
  - Enqueue in ServiceContainer.onLogin()
- [ ] WorkManager-Job abbrechen bei Logout (`WorkManager.cancelUniqueWork(...)`)

**Technische Hinweise**:
- WorkManager: `PeriodicWorkRequestBuilder<SyncWorker>(15, TimeUnit.MINUTES)`
- Constraints: `.setRequiredNetworkType(NetworkType.CONNECTED)`
- In-App Timer: `lifecycleScope.launch { while(isActive) { delay(30_000); syncEngine.syncAll() } }`
- SyncWorker benoetigt Zugriff auf ServiceContainer -- via `applicationContext` + cast

---

### P1-E07-S05: Sync-Integration in ViewModel (Beide Plattformen)

**Als** Nutzer
**moechte ich** dass meine Aenderungen sofort synchronisiert werden,
**damit** ich mich darauf verlassen kann dass nichts verloren geht.

**Plattform**: Beide
**Abhaengigkeiten**: P1-E07-S01/S02 (SyncEngine), P1-E05-S05/S06 (ViewModel)
**Parallelisierbar mit**: Nicht mit E05-S05/S06 (modifiziert diese)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `TimeTrackingViewModel` erhaelt SyncEngine als Dependency
- [ ] `finishSession()` triggert `syncEngine.syncAll()` am Ende
- [ ] `deleteSession()` nutzt Pending-Deletes-Liste fuer zuverlaessiges Offline-Delete:
  - Session lokal loeschen UND ID + Entity-Type in `PendingDelete`-Tabelle eintragen
  - Wenn online: sofortiger API DELETE Call, bei Erfolg PendingDelete-Eintrag entfernen
  - Wenn offline: PendingDelete-Eintrag bleibt bis naechster Sync
  - Beim Sync: DELETE API-Calls fuer alle PendingDelete-Eintraege ausfuehren, dann aus Tabelle entfernen
  - Beim Upsert aus Server-Response: IDs die in PendingDeletes sind werden ignoriert (nicht neu anlegen)
  - Given Nutzer loescht Session offline
  - When Netzwerk wiederhergestellt wird und Sync laeuft
  - Then wird DELETE /v1/work-sessions/{id} aufgerufen
  - And die Session wird nicht aus der Server-Response zurueck-eingefuegt
- [ ] Pull-to-Refresh im View ruft `syncEngine.syncAll()` auf
- [ ] Sync-Status (`isSyncing`) wird im ViewModel/View reflektiert:
  - Given Sync laeuft
  - When Nutzer den Zeiten-Tab sieht
  - Then wird ein Sync-Indikator angezeigt (z.B. rotierende Pfeile)

**Technische Hinweise**:
- iOS: SyncEngine als @Environment oder direkte Injection
- Android: SyncEngine aus ServiceContainer
- Delete: `apiClient.deleteWorkSession(id)` direkt aufrufen wenn online, lokal immer sofort loeschen
