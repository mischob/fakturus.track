# Plattformuebergreifende Konzepte

## 1. API-Client Architektur

Beide Plattformen implementieren einen identischen API-Client, adaptiert vom bewaehrten fakturus.poi `APIClient.swift`.

### Gemeinsame Anforderungen

| Anforderung | Loesung |
|-------------|---------|
| PascalCase JSON (Backend) | Custom Key-Decoding: PascalCase -> camelCase |
| ISO 8601 Dates (mit/ohne Millisekunden) | Custom Date-Decoding mit Fallback |
| Bearer Token Injection | Automatisch via AuthManager.acquireTokenSilently() |
| User-Agent Header | `FakturusTrack-iOS/1.0.0` bzw. `FakturusTrack-Android/1.0.0` |
| 401 Handling | Token Refresh, bei Scheitern: Logout |
| Timeout | 30s Request, 60s Resource |
| Logging | Debug: verbose, Release: nur Fehler |

### User-Agent Header

**Pflicht ab Woche 1** -- wird fuer die MAUI-Migration-Analyse benoetigt (Backend kann zwischen MAUI- und nativen Clients unterscheiden).

**Swift:**
```swift
request.setValue("FakturusTrack-iOS/\(appVersion)", forHTTPHeaderField: "User-Agent")
```

**Kotlin:**
```kotlin
header("User-Agent", "FakturusTrack-Android/${BuildConfig.VERSION_NAME}")
```

### PascalCase-Konvertierung

Das Backend liefert PascalCase (`StartTime`, `WorkSessions`). Beide Plattformen konvertieren automatisch:

**Swift:**
```swift
decoder.keyDecodingStrategy = .custom { keys in
    let str = keys.last!.stringValue
    return AnyCodingKey(stringValue: str.prefix(1).lowercased() + str.dropFirst())!
}
encoder.keyEncodingStrategy = .custom { keys in
    let str = keys.last!.stringValue
    return AnyCodingKey(stringValue: str.prefix(1).uppercased() + str.dropFirst())!
}
```

**Kotlin (kotlinx.serialization):**
```kotlin
// Nutze @SerialName fuer explizites Mapping -- am klarsten fuer AI
@Serializable
data class WorkSessionDTO(
    @SerialName("Id") val id: String,
    @SerialName("Date") val date: String,
    @SerialName("StartTime") val startTime: String,
    @SerialName("StopTime") val stopTime: String? = null,
    @SerialName("PauseMinutes") val pauseMinutes: Int = 0,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
)
```

### 401-Retry Strategie

```
API Request
    |
    v
Response?
    |
    +-- 200-299: Erfolg
    |
    +-- 401: Token abgelaufen
    |     |
    |     v
    |   acquireTokenSilently()
    |     |
    |     +-- Erfolg: Request wiederholen (1x)
    |     |
    |     +-- Fehler: User zum Login schicken
    |
    +-- 403: Fehlende Berechtigung
    +-- 404: Nicht gefunden
    +-- 5xx: Server-Fehler (Retry mit Backoff)
```

---

## 2. Offline-First Strategie & Sync-Engine

### Grundprinzip

Die Sync-Engine folgt dem bewaehrten Muster der bestehenden MAUI-App (`SyncService.cs`), vereinfacht und ohne die MAUI-spezifischen Patterns (Timer, Events).

```
Lokale DB (Source of Truth fuer UI)
    |
    +-- Schreibe lokal -> markiere als isPendingSync=true
    |
    +-- SyncEngine prüft periodisch:
    |     1. Pending Changes hochladen (via /sync Endpoint)
    |     2. Backend-Response = komplette Server-Liste
    |     3. Merge: Server-wins bei Konflikten
    |     4. Markiere als isSynced=true
    |
    +-- UI zeigt immer lokale Daten (via @Query / Flow)
```

### SyncEngine (beide Plattformen identisch)

```
SyncEngine
    |
    +-- syncAll()           -- Alle Bereiche synchronisieren
    |     +-- syncWorkSessions()
    |     +-- syncVacationDays()
    |     +-- syncSickDays()       // Phase 2
    |     +-- syncUserSettings()
    |
    +-- Sync-Trigger:
          +-- App-Start (nach Login)
          +-- Netzwerk-Wiederherstellung
          +-- Session beendet (Finish)
          +-- Pull-to-Refresh (manuell)
          +-- Background: iOS BGAppRefreshTask / Android WorkManager
          +-- In-App Timer: 30s wenn App aktiv
```

### Sync-Algorithmus WorkSessions

Direkt uebernommen von `SyncService.cs`, Schritte 1-7:

```
1. Lokale pending Sessions sammeln (isPendingSync=true, isFinished=true)
2. Falls pending vorhanden:
     -> POST /v1/work-sessions/sync mit pending Sessions
     -> Response = alle Server-Sessions
   Falls keine pending:
     -> GET /v1/work-sessions
     -> Response = alle Server-Sessions
3. Lokale synced Sessions laden (isSynced=true)
4. Backend-ID-Set erstellen
5. Lokal-nur Sessions loeschen (existieren nicht mehr im Backend) -- Set-Differenz
6. Backend-Sessions in lokale DB mergen:
     -> Existiert lokal? -> Update (Server-wins)
     -> Neu? -> Insert
7. Pending Sessions als synced markieren (isPendingSync=false, isSynced=true)
```

### Sync-Algorithmus VacationDays (ACHTUNG: anders als WorkSessions!)

Der VacationDay-Sync sendet ALLE lokalen Tage, nicht nur pending. Grund: Das Backend vergleicht die gesendete Liste mit seiner eigenen, um Loeschungen zu erkennen. Wenn nur pending gesendet wuerden, wuerden geloeschte Tage nie auf dem Server entfernt.

```
1. ALLE lokalen VacationDays sammeln (synced + pending, nicht nur pending!)
2. POST /v1/vacation-days/sync mit ALLEN lokalen Tagen
3. Response enthaelt:
     -> ServerVacationDays: Aktuelle Server-Liste
     -> DeletedIds: IDs der auf dem Server geloeschten Tage
4. DeletedIds verarbeiten: Lokale Eintraege mit diesen IDs loeschen
5. Server-VacationDays in lokale DB mergen:
     -> Existiert lokal? -> Update (Server-wins)
     -> Neu? -> Insert
6. Alle lokalen Tage als synced markieren (isPendingSync=false, isSynced=true)
```

> **Warum der Unterschied?** WorkSessions sind individuelle CRUD-Eintraege. VacationDays sind eine "Menge von markierten Tagen" -- der Nutzer toggled Tage an/aus. Das Backend braucht die komplette Liste um Demarkierungen (Loeschungen) zu erkennen.

### Sync-Algorithmus SickDays (identisch zu VacationDays) -- Phase 2

> **Phase 2**: SickDays werden erst in Phase 2 implementiert. Der Algorithmus ist hier der Vollstaendigkeit halber dokumentiert.

SickDays nutzen exakt den gleichen Sync-Algorithmus wie VacationDays: ALLE lokalen Krankheitstage senden, nicht nur pending. Das Backend liefert `ServerSickDays` + `DeletedIds` zurueck.

```
1. ALLE lokalen SickDays sammeln (synced + pending, nicht nur pending!)
2. POST /v1/sick-days/sync mit ALLEN lokalen Tagen
3. Response enthaelt:
     -> ServerSickDays: Aktuelle Server-Liste
     -> DeletedIds: IDs der auf dem Server geloeschten Tage
4. DeletedIds verarbeiten: Lokale Eintraege mit diesen IDs loeschen
5. Server-SickDays in lokale DB mergen:
     -> Existiert lokal? -> Update (Server-wins)
     -> Neu? -> Insert
6. Alle lokalen Tage als synced markieren (isPendingSync=false, isSynced=true)
```

### Swift Implementation (SyncEngine)

```swift
actor SyncEngine {
    private let apiClient: APIClient
    private let modelContext: ModelContext
    private let networkMonitor: NetworkMonitor

    private(set) var isSyncing = false

    init(apiClient: APIClient, networkMonitor: NetworkMonitor) {
        self.apiClient = apiClient
        self.networkMonitor = networkMonitor
        // modelContext wird spaeter per Dependency gesetzt
    }

    func syncAll() async {
        guard !isSyncing else { return }
        guard networkMonitor.isConnected else { return }

        isSyncing = true
        defer { isSyncing = false }

        do {
            try await syncWorkSessions()
            try await syncVacationDays()
            // try await syncSickDays()  // Phase 2
            try await syncUserSettings()
        } catch {
            // Log error, don't crash
        }
    }

    private func syncWorkSessions() async throws {
        // Step 1: Get pending
        let pending = try modelContext.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate {
                $0.isPendingSync && $0.isFinished
            })
        )

        // Step 2: Upload or fetch
        let serverSessions: [WorkSessionDTO]
        if !pending.isEmpty {
            let request = SyncWorkSessionsRequest(
                workSessions: pending.map { $0.toDTO() }
            )
            serverSessions = try await apiClient.syncWorkSessions(request)
        } else {
            serverSessions = try await apiClient.getWorkSessions()
        }

        // Step 3-7: Merge (server-wins)
        let serverIds = Set(serverSessions.map(\.id))

        // Delete local-only synced sessions
        let synced = try modelContext.fetch(
            FetchDescriptor<WorkSession>(predicate: #Predicate { $0.isSynced })
        )
        for local in synced where !serverIds.contains(local.id.uuidString) {
            modelContext.delete(local)
        }

        // Upsert server sessions
        for dto in serverSessions {
            if let existing = try modelContext.fetch(
                FetchDescriptor<WorkSession>(predicate: #Predicate { $0.id == dto.id })
            ).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(WorkSession(from: dto))
            }
        }

        try modelContext.save()
    }

    private func syncVacationDays() async throws {
        // ACHTUNG: Anders als WorkSessions -- ALLE lokalen Tage senden, nicht nur pending!
        let allLocal = try modelContext.fetch(FetchDescriptor<VacationDay>())

        let request = SyncVacationDaysRequest(
            vacationDays: allLocal.map { $0.toDTO() }
        )
        let response = try await apiClient.syncVacationDays(request)

        // DeletedIds verarbeiten: Lokale Eintraege gezielt loeschen
        for deletedId in response.deletedIds {
            if let toDelete = try modelContext.fetch(
                FetchDescriptor<VacationDay>(predicate: #Predicate { $0.id == deletedId })
            ).first {
                modelContext.delete(toDelete)
            }
        }

        // Upsert server vacation days
        for dto in response.serverVacationDays {
            if let existing = try modelContext.fetch(
                FetchDescriptor<VacationDay>(predicate: #Predicate { $0.id == dto.id })
            ).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(VacationDay(from: dto))
            }
        }

        try modelContext.save()
    }

    // Phase 2: SickDay-Sync
    private func syncSickDays() async throws {
        // Analog zu VacationDays: ALLE lokalen Krankheitstage senden, nicht nur pending!
        let allLocal = try modelContext.fetch(FetchDescriptor<SickDay>())

        let request = SyncSickDaysRequest(
            sickDays: allLocal.map { $0.toDTO() }
        )
        let response = try await apiClient.syncSickDays(request)

        // DeletedIds verarbeiten: Lokale Eintraege gezielt loeschen
        for deletedId in response.deletedIds {
            if let toDelete = try modelContext.fetch(
                FetchDescriptor<SickDay>(predicate: #Predicate { $0.id == deletedId })
            ).first {
                modelContext.delete(toDelete)
            }
        }

        // Upsert server sick days
        for dto in response.serverSickDays {
            if let existing = try modelContext.fetch(
                FetchDescriptor<SickDay>(predicate: #Predicate { $0.id == dto.id })
            ).first {
                existing.update(from: dto)
            } else {
                modelContext.insert(SickDay(from: dto))
            }
        }

        try modelContext.save()
    }
}
```

### Kotlin Implementation (SyncEngine)

```kotlin
class SyncEngine(
    private val apiClient: APIClient,
    private val database: AppDatabase,
    private val networkMonitor: NetworkMonitor
) {
    private val _isSyncing = MutableStateFlow(false)
    val isSyncing: StateFlow<Boolean> = _isSyncing.asStateFlow()

    suspend fun syncAll() {
        if (_isSyncing.value) return
        if (!networkMonitor.isConnected.value) return

        _isSyncing.value = true
        try {
            syncWorkSessions()
            syncVacationDays()
            // syncSickDays()  // Phase 2
            syncUserSettings()
        } catch (e: Exception) {
            Log.e("SyncEngine", "Sync failed", e)
        } finally {
            _isSyncing.value = false
        }
    }

    private suspend fun syncWorkSessions() {
        val dao = database.workSessionDao()
        val pending = dao.getPendingSessions()

        val serverSessions = if (pending.isNotEmpty()) {
            val request = SyncWorkSessionsRequest(
                workSessions = pending.map { it.toDTO() }
            )
            apiClient.syncWorkSessions(request)
        } else {
            apiClient.getWorkSessions()
        }

        val serverIds = serverSessions.map { it.id }.toSet()

        // Delete local-only synced sessions
        val synced = dao.getSyncedSessions()
        synced.filter { it.id !in serverIds }.forEach { dao.delete(it) }

        // Upsert server sessions
        serverSessions.forEach { dto ->
            dao.insert(dto.toEntity()) // REPLACE strategy handles update
        }
    }

    private suspend fun syncVacationDays() {
        val dao = database.vacationDayDao()

        // ACHTUNG: Anders als WorkSessions -- ALLE lokalen Tage senden, nicht nur pending!
        val allLocal = dao.getAll()

        val request = SyncVacationDaysRequest(
            vacationDays = allLocal.map { it.toDTO() }
        )
        val response = apiClient.syncVacationDays(request)

        // DeletedIds verarbeiten: Lokale Eintraege gezielt loeschen
        response.deletedIds.forEach { deletedId ->
            dao.deleteById(deletedId)
        }

        // Upsert server vacation days
        response.serverVacationDays.forEach { dto ->
            dao.insert(dto.toEntity()) // REPLACE strategy handles update
        }
    }

    // Phase 2: SickDay-Sync
    private suspend fun syncSickDays() {
        val dao = database.sickDayDao()

        // Analog zu VacationDays: ALLE lokalen Krankheitstage senden, nicht nur pending!
        val allLocal = dao.getAll()

        val request = SyncSickDaysRequest(
            sickDays = allLocal.map { it.toDTO() }
        )
        val response = apiClient.syncSickDays(request)

        // DeletedIds verarbeiten: Lokale Eintraege gezielt loeschen
        response.deletedIds.forEach { deletedId ->
            dao.deleteById(deletedId)
        }

        // Upsert server sick days
        response.serverSickDays.forEach { dto ->
            dao.insert(dto.toEntity()) // REPLACE strategy handles update
        }
    }
}
```

### Sync-Trigger Timing

| Trigger | iOS | Android | Intervall |
|---------|-----|---------|-----------|
| App-Start | `onAppear` | `onCreate` | Sofort |
| Netzwerk-Wiederherstellung | NWPathMonitor Callback | ConnectivityManager | Sofort |
| Session Finish | Nach `finishSession()` | Nach `finishSession()` | Sofort |
| Pull-to-Refresh | `.refreshable` | PullToRefresh | Manuell |
| Background | BGAppRefreshTask | WorkManager | 15-30min |
| In-App Polling | Timer.publish | coroutine delay | 30s (App aktiv) |

### Settings-Sync-Strategie: Last-Write-Wins

Settings (Wochenstunden, Arbeitstage, Bundesland etc.) nutzen eine **andere Sync-Strategie** als WorkSessions und VacationDays:

**Strategie: Last-Write-Wins basierend auf `UpdatedAt`**

```
1. Lokale Settings laden
2. Server-Settings via GET /v1/settings laden
3. Vergleiche UpdatedAt Timestamps:
     -> Lokal neuer? -> PUT /v1/settings mit lokalen Werten
     -> Server neuer? -> Lokale Settings mit Server-Werten ueberschreiben
     -> Gleich? -> Nichts tun
4. Lokale Settings als synced markieren
```

**Begruendung:** Bei reinem Server-wins wuerden lokale Settings-Aenderungen (z.B. Nutzer aendert offline seine Wochenstunden) verworfen. Das waere kontraintuitiv -- der Nutzer hat aktiv etwas geaendert. Last-Write-Wins basierend auf dem Timestamp ist fair und vorhersehbar.

**Wichtig:** Bei Settings-Aenderung sofort `UpdatedAt = now()` setzen (lokal). So gewinnt die neueste Aenderung, egal ob lokal oder remote.

---

## 3. Authentication Flow (Azure B2C)

### Gemeinsamer Flow (beide Plattformen)

```
App Start
    |
    +-- MSAL: Account im Cache?
          |
          +-- Ja: acquireTokenSilently()
          |     |
          |     +-- Erfolg -> Authenticated (direkt zur App)
          |     +-- Fehler -> Login Screen anzeigen
          |
          +-- Nein: Login Screen anzeigen
                |
                +-- User tippt "Mit Apple anmelden"
                |     -> domain_hint: "apple.com"
                |
                +-- User tippt "Mit Google anmelden"
                |     -> domain_hint: "google.com"
                |
                +-- User tippt "Mit E-Mail anmelden"
                      -> domain_hint: null (B2C Standard-Flow)
                |
                v
            MSAL acquireToken (interaktiv)
                |
                +-- Erfolg -> Token speichern, Services initialisieren
                +-- Abbruch -> Zurueck zum Login Screen
                +-- Fehler -> Fehlermeldung anzeigen
```

### B2C Konfiguration (Phase 1)

Beide Plattformen nutzen die **gleiche App-Registration** wie die MAUI-App:

| Parameter | Wert |
|-----------|------|
| Client ID | `3fb35bc6-8825-495e-b0a2-18e00352f968` |
| Policy | `B2C_1_BetaSignInOnly` |
| API Scope | `https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access` |
| iOS Redirect | `msauth.com.fakturus.track://auth` |
| Android Redirect | `msauth://com.fakturus.track/{hash}` |

### Login-Pflicht (Kein anonymer Modus)

Die nativen Apps erfordern Login. Begruendung:
1. Sync funktioniert nur authentifiziert
2. Vereinfacht Architektur (kein Dual-Pfad)
3. BAG-Urteil erfordert personenbezogene Zeiterfassung
4. Offline funktioniert nach erstem Login (Token-Cache)

---

## 4. Error Handling Strategie

### Fehler-Kategorien

| Kategorie | Beispiel | User-Aktion |
|-----------|---------|-------------|
| **Netzwerk** | Kein Internet, Timeout | Offline-Banner, automatischer Retry |
| **Auth** | Token abgelaufen, 401 | Automatischer Refresh, ggf. Re-Login |
| **Validation** | Endzeit vor Startzeit | Inline-Fehlermeldung im Formular |
| **Server** | 500, Backend down | Toast/Snackbar, Daten lokal gespeichert |
| **Lokal** | DB-Fehler (selten) | Log, User sieht nichts |

### Darstellung

```
Netzwerk-Fehler:
    -> OfflineBanner (persistent, am oberen Rand)
    -> Verschwindet automatisch bei Wiederherstellung

Auth-Fehler:
    -> Alert/Dialog: "Sitzung abgelaufen. Bitte erneut anmelden."
    -> Redirect zu LoginScreen

Sync-Fehler:
    -> Toast/Snackbar: "Synchronisation fehlgeschlagen"
    -> Daten bleiben lokal gespeichert (kein Datenverlust)

Validation:
    -> Inline unter dem Eingabefeld: "Endzeit muss nach Startzeit liegen"
```

### Kein globaler Error-Handler

Fehler werden **dort behandelt wo sie auftreten** (im ViewModel). Kein zentraler Error-Bus, keine Error-Middleware. Begruendung: Direkter, einfacher nachzuvollziehen.

---

## 5. Logging

### Strategie

| Umgebung | Level | Ziel |
|----------|-------|------|
| Debug | Verbose (alle API-Calls, Sync-Schritte) | Console (Xcode/Logcat) |
| Release | Error + Warning | os.Logger (iOS) / Log (Android) |

### Keine Analytics-SDKs

Kein Firebase Analytics, kein Crashlytics, kein Sentry in V1. Begruendung:
- DSGVO-Konformitaet ohne Consent-Flow
- Weniger Dependencies
- Crash-Reports via TestFlight (iOS) und Play Console (Android) ausreichend
- Kann in Phase 3 optional hinzugefuegt werden

---

## 6. Push Notifications

### Phase 1: Keine Push Notifications

Push Notifications sind fuer Phase 1 **nicht vorgesehen**. Begruendung:
- Backend muesste Push-Service implementieren (Scope-Erweiterung)
- ArbZG-Hinweise (10h-Grenze) funktionieren als lokale Notifications
- Sync funktioniert ohne Push (In-App Timer + Background Fetch)

### Lokale Notifications (Phase 2)

| Trigger | Inhalt | Plattform |
|---------|--------|-----------|
| 10h Arbeitszeit | ArbZG-Hinweis | iOS + Android |
| 6h Arbeitszeit | Pausenerinnerung | iOS + Android |
| Sync-Fehler (laenger als 24h) | "Daten nicht synchronisiert" | iOS + Android |

**iOS:**
```swift
let content = UNMutableNotificationContent()
content.title = "Arbeitszeithinweis"
content.body = "Sie arbeiten seit 10 Stunden. Die gesetzliche Hoechstarbeitszeit betraegt 10 Stunden."
```

**Android:**
```kotlin
val notification = NotificationCompat.Builder(context, CHANNEL_ID)
    .setContentTitle("Arbeitszeithinweis")
    .setContentText("Sie arbeiten seit 10 Stunden.")
    .setSmallIcon(R.drawable.ic_timer)
    .build()
```

### Push Notifications (Phase 3+)

Erst wenn Backend einen Push-Service implementiert. Dann:
- APNs fuer iOS
- FCM fuer Android
- Device Token Registration Endpoint im Backend
