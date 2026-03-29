# EPIC 04: API-Client & Netzwerk

## Ziel

Funktionsfaehige HTTP-Clients auf beiden Plattformen, die mit dem bestehenden Backend kommunizieren. Automatische Token-Injection, PascalCase-Handling, Fehlerbehandlung und Netzwerk-Monitoring.

## Abhaengigkeiten

- **E01**: Projekt-Setup (Dependencies)
- **E02**: Auth (Token fuer Bearer-Header)

---

## Stories

### P1-E04-S01: iOS APIClient

**Als** Entwickler
**moechte ich** einen konfigurierten HTTP-Client,
**damit** die App mit dem Backend kommunizieren kann.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E02-S01 (AuthManager fuer Token)
**Parallelisierbar mit**: P1-E04-S02 (Android APIClient), P1-E03-S01 (DB), P1-E05-* (UI mit Mocks)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `APIClient.swift` implementiert (adaptiert von fakturus.poi):
  - `baseURL`: aus Configuration
  - `authManager`: Referenz fuer Token-Beschaffung
  - Generische Request-Methoden: `get<T>()`, `post<T>()`, `put<T>()`, `delete()`
- [ ] PascalCase-Konvertierung:
  - JSONDecoder mit custom `keyDecodingStrategy` (PascalCase -> camelCase)
  - JSONEncoder mit custom `keyEncodingStrategy` (camelCase -> PascalCase)
- [ ] ISO8601 Date-Handling:
  - Custom DateDecodingStrategy mit Fallback (mit/ohne Millisekunden)
- [ ] Automatische Token-Injection:
  - Jeder Request ruft `authManager.acquireTokenSilently()` auf
  - Setzt `Authorization: Bearer {token}` Header
- [ ] User-Agent Header: `FakturusTrack-iOS/{version}`
- [ ] 401-Retry:
  - Bei 401: Token refreshen via `acquireTokenSilently(forceRefresh: true)`
  - Request wiederholen (maximal 1x)
  - Bei erneutem 401: `AuthError.sessionExpired` werfen
- [ ] `APIError` Enum:
  - `.network(Error)`, `.unauthorized`, `.forbidden`, `.notFound`, `.serverError(Int)`, `.decodingError(Error)`
- [ ] Timeout: 30s Request, 60s Resource
- [ ] WorkSession-Endpunkte:
  - `getWorkSessions() -> [WorkSessionDTO]` (GET /v1/work-sessions)
  - `syncWorkSessions(SyncWorkSessionsRequest) -> [WorkSessionDTO]` (POST /v1/work-sessions/sync)
  - `deleteWorkSession(id: String)` (DELETE /v1/work-sessions/{id})
- [ ] VacationDay-Endpunkte:
  - `syncVacationDays(SyncVacationDaysRequest) -> SyncVacationDaysResponse` (POST /v1/vacation-days/sync)
- [ ] Settings-Endpunkte:
  - `getSettings() -> UserSettingsDTO` (GET /v1/settings)
  - `putSettings(UserSettingsDTO)` (PUT /v1/settings)
- [ ] Logging: Debug = verbose (URL, Status, Body), Release = nur Fehler

**Technische Hinweise**:
- Adaptieren von fakturus.poi `APIClient.swift`
- URLSession mit async/await (kein Alamofire noetig)
- `AnyCodingKey` Helper fuer custom Key-Strategien (siehe shared-concepts.md)

---

### P1-E04-S02: Android APIClient (Ktor)

**Als** Entwickler
**moechte ich** einen konfigurierten HTTP-Client,
**damit** die App mit dem Backend kommunizieren kann.

**Plattform**: Android
**Abhaengigkeiten**: P1-E02-S02 (AuthManager fuer Token)
**Parallelisierbar mit**: P1-E04-S01 (iOS APIClient), P1-E03-S02 (DB), P1-E05-* (UI mit Mocks)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `APIClient.kt` implementiert mit Ktor HttpClient:
  - `baseUrl`: aus Configuration
  - `authManager`: Referenz fuer Token-Beschaffung
  - Generische Request-Methoden: `get<T>()`, `post<T>()`, `put<T>()`, `delete()`
- [ ] Ktor Plugins konfiguriert:
  - `ContentNegotiation` mit `kotlinx.serialization` (Json)
  - `Auth` oder manueller `defaultRequest`-Block fuer Bearer Token
  - `HttpTimeout`: request = 30_000, socket = 60_000
  - `Logging`: Debug = ALL, Release = NONE
- [ ] PascalCase-Handling: via `@SerialName` Annotationen in DTOs (explizit, nicht automatisch)
- [ ] Automatische Token-Injection:
  - In `defaultRequest` Block: `authManager.acquireTokenSilently()` aufrufen
  - `Authorization: Bearer {token}` Header setzen
- [ ] User-Agent Header: `FakturusTrack-Android/{version}`
- [ ] 401-Retry:
  - HttpResponseValidator: bei 401 Token refreshen und Request wiederholen (1x)
  - Bei erneutem 401: `AuthException.SessionExpired` werfen
- [ ] `APIError` sealed class:
  - `Network(cause)`, `Unauthorized`, `Forbidden`, `NotFound`, `ServerError(code)`, `DecodingError(cause)`
- [ ] Gleiche Endpunkte wie iOS (WorkSessions, VacationDays, Settings)
- [ ] APIClient-Initialisierung im ServiceContainer nach Login (`onLogin()`)

**Technische Hinweise**:
- Ktor Client Engine: CIO (oder OkHttp falls MSAL-Interceptor noetig)
- ADR-007: Ktor statt Retrofit (konsistent mit fakturus.poi Android)
- `Json { ignoreUnknownKeys = true; isLenient = true }` konfigurieren

---

### P1-E04-S03: iOS NetworkMonitor

**Als** Nutzer
**moechte ich** dass die App meinen Netzwerkstatus kennt,
**damit** sie automatisch zwischen Online- und Offline-Modus wechselt.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S01
**Parallelisierbar mit**: P1-E04-S04 (Android NetworkMonitor), alle anderen Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `NetworkMonitor.swift` als `@Observable` Klasse:
  - Nutzt `NWPathMonitor` aus Network Framework
  - `isConnected: Bool` Property (reactive)
  - Startet Monitoring automatisch bei Initialisierung
  - Laeuft auf eigenem DispatchQueue (nicht Main)
- [ ] Korrekte Erkennung:
  - Given Geraet hat WLAN-Verbindung
  - When NetworkMonitor gestartet
  - Then `isConnected == true`
  - Given WLAN wird deaktiviert
  - When Netzwerkstatus aendert sich
  - Then `isConnected == false`
- [ ] Wird als Environment-Objekt in App bereitgestellt

**Technische Hinweise**:
- `NWPathMonitor().start(queue:)` auf Background-Queue
- `@MainActor` fuer Property-Updates (UI-Thread)

---

### P1-E04-S04: Android NetworkMonitor

**Als** Nutzer
**moechte ich** dass die App meinen Netzwerkstatus kennt,
**damit** sie automatisch zwischen Online- und Offline-Modus wechselt.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S02
**Parallelisierbar mit**: P1-E04-S03 (iOS NetworkMonitor), alle anderen Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `NetworkMonitor.kt` Klasse:
  - Nutzt `ConnectivityManager` mit `NetworkCallback`
  - `isConnected: StateFlow<Boolean>` (reactive, Compose-kompatibel)
  - Startet Monitoring automatisch bei Erstellung
- [ ] Korrekte Erkennung:
  - Given Geraet hat Netzwerkverbindung
  - Then `isConnected.value == true`
  - Given Flugmodus aktiviert
  - Then `isConnected.value == false`
- [ ] Registrierung im ServiceContainer (fruehe Initialisierung, vor Login)
- [ ] `registerDefaultNetworkCallback` fuer globales Monitoring

**Technische Hinweise**:
- `ConnectivityManager.registerDefaultNetworkCallback()` (API 24+)
- `Dispatchers.Main` fuer StateFlow-Updates nicht noetig (StateFlow ist thread-safe)
- Permissions: `ACCESS_NETWORK_STATE` in AndroidManifest
