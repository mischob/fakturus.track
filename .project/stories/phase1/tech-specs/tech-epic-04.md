# Tech-Spec: EPIC 04 -- API-Client & Netzwerk

## Dateien die erstellt werden

| Datei | Plattform | Story | Zweck |
|-------|-----------|-------|-------|
| `Services/API/APIClient.swift` | iOS | E04-S01 | URLSession-basiert, PascalCase, Token-Injection |
| `Services/API/APIClient+Endpoints.swift` | iOS | E04-S01 | Convenience-Methoden fuer alle Endpoints |
| `Services/Network/NetworkMonitor.swift` | iOS | E04-S03 | NWPathMonitor Wrapper |
| `services/api/APIClient.kt` | Android | E04-S02 | Ktor Client, Endpoint-Methoden direkt enthalten |
| `services/api/APIError.kt` | Android | E04-S02 | Sealed class |
| `services/network/NetworkMonitor.kt` | Android | E04-S04 | ConnectivityManager Wrapper |

---

## API-Contracts

Basis-URL: `https://api.track.fakturus.com` (Release) / `https://localhost:7001` (Debug)

Alle Requests benoetigen: `Authorization: Bearer {accessToken}`

### Endpoints die in Phase 1 genutzt werden

| Methode | Pfad | Request Body | Response |
|---------|------|-------------|----------|
| GET | `/v1/work-sessions` | -- | `[WorkSessionDTO]` |
| POST | `/v1/work-sessions/sync` | `SyncWorkSessionsRequest` | `[WorkSessionDTO]` |
| DELETE | `/v1/work-sessions/{id}` | -- | 204 No Content |
| POST | `/v1/vacation-days/sync` | `SyncVacationDaysRequest` | `SyncVacationDaysResponse` |
| GET | `/v1/settings` | -- | `UserSettingsDTO` |
| PUT | `/v1/settings` | `UserSettingsDTO` | 204 No Content |

### JSON-Konventionen

- Backend liefert **PascalCase** Feldnamen (`StartTime`, `PauseMinutes`)
- iOS konvertiert automatisch via custom `keyDecodingStrategy`
- Android nutzt explizite `@SerialName` Annotationen (kein automatisches Mapping)
- Dates sind ISO 8601 Strings, teilweise mit Millisekunden (`.sss`), teilweise ohne

---

## Code-Skizzen

### iOS: APIClient.swift

```swift
import Foundation

/// Wiederverwendbarer CodingKey fuer PascalCase-Konvertierung
struct AnyCodingKey: CodingKey {
    var stringValue: String
    var intValue: Int?

    init?(stringValue: String) { self.stringValue = stringValue }
    init?(intValue: Int) { self.intValue = intValue; self.stringValue = String(intValue) }
}

enum APIError: Error, LocalizedError {
    case network(Error)
    case unauthorized
    case forbidden
    case notFound
    case serverError(Int)
    case decodingError(Error)
    case unknown(Int)

    var errorDescription: String? {
        switch self {
        case .network: return "Netzwerkfehler"
        case .unauthorized: return "Nicht autorisiert"
        case .forbidden: return "Zugriff verweigert"
        case .notFound: return "Nicht gefunden"
        case .serverError(let code): return "Serverfehler (\(code))"
        case .decodingError: return "Daten konnten nicht gelesen werden"
        case .unknown(let code): return "Unbekannter Fehler (\(code))"
        }
    }
}

final class APIClient {
    private let baseURL: String
    private let authManager: AuthManager
    private let session: URLSession
    private let decoder: JSONDecoder
    private let encoder: JSONEncoder

    init(authManager: AuthManager, baseURL: String = Configuration.apiBaseUrl) {
        self.authManager = authManager
        self.baseURL = baseURL

        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 30
        config.timeoutIntervalForResource = 60
        self.session = URLSession(configuration: config)

        // PascalCase -> camelCase Decoder
        self.decoder = JSONDecoder()
        decoder.keyDecodingStrategy = .custom { keys in
            let key = keys.last!.stringValue
            let camel = key.prefix(1).lowercased() + key.dropFirst()
            return AnyCodingKey(stringValue: camel)!
        }

        // camelCase -> PascalCase Encoder
        self.encoder = JSONEncoder()
        encoder.keyEncodingStrategy = .custom { keys in
            let key = keys.last!.stringValue
            let pascal = key.prefix(1).uppercased() + key.dropFirst()
            return AnyCodingKey(stringValue: pascal)!
        }
    }

    // MARK: - Generic Methods

    func get<T: Decodable>(
        _ path: String,
        queryItems: [URLQueryItem] = []
    ) async throws -> T {
        let request = try await buildRequest(path: path, method: "GET", queryItems: queryItems)
        return try await execute(request)
    }

    func post<T: Decodable, B: Encodable>(
        _ path: String,
        body: B
    ) async throws -> T {
        var request = try await buildRequest(path: path, method: "POST")
        request.httpBody = try encoder.encode(body)
        return try await execute(request)
    }

    func put<B: Encodable>(_ path: String, body: B) async throws {
        var request = try await buildRequest(path: path, method: "PUT")
        request.httpBody = try encoder.encode(body)
        let (_, response) = try await session.data(for: request)
        try validateResponse(response)
    }

    func delete(_ path: String) async throws {
        let request = try await buildRequest(path: path, method: "DELETE")
        let (_, response) = try await session.data(for: request)
        try validateResponse(response)
    }

    // MARK: - Internal

    private func buildRequest(
        path: String,
        method: String,
        queryItems: [URLQueryItem] = []
    ) async throws -> URLRequest {
        var components = URLComponents(string: "\(baseURL)\(path)")!
        if !queryItems.isEmpty { components.queryItems = queryItems }

        var request = URLRequest(url: components.url!)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let appVersion = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0"
        request.setValue("FakturusTrack-iOS/\(appVersion)", forHTTPHeaderField: "User-Agent")

        // Token Injection
        let token = try await authManager.acquireTokenSilently()
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        return request
    }

    private func execute<T: Decodable>(_ request: URLRequest) async throws -> T {
        let (data, response) = try await executeWithRetry(request)
        try validateResponse(response)
        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            throw APIError.decodingError(error)
        }
    }

    private func executeWithRetry(_ request: URLRequest) async throws -> (Data, URLResponse) {
        do {
            let (data, response) = try await session.data(for: request)
            let httpResponse = response as! HTTPURLResponse
            if httpResponse.statusCode == 401 {
                // 1x Retry mit erzwungenem Token-Refresh
                var retryRequest = request
                let newToken = try await authManager.acquireTokenSilently(forceRefresh: true)
                retryRequest.setValue("Bearer \(newToken)", forHTTPHeaderField: "Authorization")
                return try await session.data(for: retryRequest)
            }
            return (data, response)
        } catch let error as APIError {
            throw error
        } catch {
            throw APIError.network(error)
        }
    }

    private func validateResponse(_ response: URLResponse) throws {
        let code = (response as! HTTPURLResponse).statusCode
        switch code {
        case 200...299: return
        case 401: throw APIError.unauthorized
        case 403: throw APIError.forbidden
        case 404: throw APIError.notFound
        case 500...599: throw APIError.serverError(code)
        default: throw APIError.unknown(code)
        }
    }
}
```

### iOS: APIClient+Endpoints.swift

```swift
extension APIClient {
    // Work Sessions
    func getWorkSessions() async throws -> [WorkSessionDTO] {
        try await get("/v1/work-sessions")
    }

    func syncWorkSessions(_ request: SyncWorkSessionsRequest) async throws -> [WorkSessionDTO] {
        try await post("/v1/work-sessions/sync", body: request)
    }

    func deleteWorkSession(id: String) async throws {
        try await delete("/v1/work-sessions/\(id)")
    }

    // Vacation Days
    func syncVacationDays(_ request: SyncVacationDaysRequest) async throws -> SyncVacationDaysResponse {
        try await post("/v1/vacation-days/sync", body: request)
    }

    // Settings
    func getUserSettings() async throws -> UserSettingsDTO {
        try await get("/v1/settings")
    }

    func updateUserSettings(_ settings: UserSettingsDTO) async throws {
        try await put("/v1/settings", body: settings)
    }
}
```

### iOS: NetworkMonitor.swift

```swift
import Network

@Observable
final class NetworkMonitor {
    var isConnected = true

    private let monitor = NWPathMonitor()
    private let queue = DispatchQueue(label: "NetworkMonitor")

    init() {
        monitor.pathUpdateHandler = { [weak self] path in
            Task { @MainActor in
                self?.isConnected = path.status == .satisfied
            }
        }
        monitor.start(queue: queue)
    }

    deinit {
        monitor.cancel()
    }
}
```

### Android: APIClient.kt

```kotlin
sealed class APIError : Exception() {
    data class Network(override val cause: Throwable) : APIError()
    object Unauthorized : APIError()
    object Forbidden : APIError()
    object NotFound : APIError()
    data class ServerError(val code: Int) : APIError()
    data class DecodingError(override val cause: Throwable) : APIError()
}

class APIClient(
    private val baseUrl: String,
    private val authManager: AuthManager
) {
    private val json = Json {
        ignoreUnknownKeys = true
        isLenient = true
    }

    private val client = HttpClient(CIO) {
        install(ContentNegotiation) { json(this@APIClient.json) }
        install(HttpTimeout) {
            requestTimeoutMillis = 30_000
            socketTimeoutMillis = 60_000
        }
        if (BuildConfig.DEBUG) {
            install(Logging) { level = LogLevel.BODY }
        }
        defaultRequest {
            contentType(ContentType.Application.Json)
        }
    }

    private suspend fun authenticatedRequest(
        method: HttpMethod,
        path: String,
        body: Any? = null,
        queryParams: Map<String, String> = emptyMap()
    ): HttpResponse {
        val token = authManager.acquireTokenSilently()
        val response = client.request("$baseUrl$path") {
            this.method = method
            header("Authorization", "Bearer $token")
            header("User-Agent", "FakturusTrack-Android/${BuildConfig.VERSION_NAME}")
            queryParams.forEach { (k, v) -> parameter(k, v) }
            if (body != null) setBody(body)
        }

        // 401 Retry (1x)
        if (response.status.value == 401) {
            val newToken = authManager.acquireTokenSilently()
            return client.request("$baseUrl$path") {
                this.method = method
                header("Authorization", "Bearer $newToken")
                header("User-Agent", "FakturusTrack-Android/${BuildConfig.VERSION_NAME}")
                queryParams.forEach { (k, v) -> parameter(k, v) }
                if (body != null) setBody(body)
            }
        }
        return response
    }

    private fun validateResponse(response: HttpResponse) {
        when (response.status.value) {
            in 200..299 -> return
            401 -> throw APIError.Unauthorized
            403 -> throw APIError.Forbidden
            404 -> throw APIError.NotFound
            in 500..599 -> throw APIError.ServerError(response.status.value)
        }
    }

    suspend inline fun <reified T> get(
        path: String, queryParams: Map<String, String> = emptyMap()
    ): T {
        val response = authenticatedRequest(HttpMethod.Get, path, queryParams = queryParams)
        validateResponse(response)
        return response.body()
    }

    suspend inline fun <reified T, reified B> post(path: String, body: B): T {
        val response = authenticatedRequest(HttpMethod.Post, path, body = body)
        validateResponse(response)
        return response.body()
    }

    suspend fun delete(path: String) {
        val response = authenticatedRequest(HttpMethod.Delete, path)
        validateResponse(response)
    }

    suspend inline fun <reified B> put(path: String, body: B) {
        val response = authenticatedRequest(HttpMethod.Put, path, body = body)
        validateResponse(response)
    }

    // Convenience Endpoints
    suspend fun getWorkSessions(): List<WorkSessionDTO> = get("/v1/work-sessions")
    suspend fun syncWorkSessions(req: SyncWorkSessionsRequest): List<WorkSessionDTO> = post("/v1/work-sessions/sync", req)
    suspend fun deleteWorkSession(id: String) = delete("/v1/work-sessions/$id")
    suspend fun syncVacationDays(req: SyncVacationDaysRequest): SyncVacationDaysResponse = post("/v1/vacation-days/sync", req)
    suspend fun getUserSettings(): UserSettingsDTO = get("/v1/settings")
    suspend fun updateUserSettings(settings: UserSettingsDTO) = put("/v1/settings", settings)
}
```

### Android: NetworkMonitor.kt

```kotlin
class NetworkMonitor(context: Context) {
    private val _isConnected = MutableStateFlow(true)
    val isConnected: StateFlow<Boolean> = _isConnected.asStateFlow()

    init {
        val connectivityManager = context.getSystemService(Context.CONNECTIVITY_SERVICE)
            as ConnectivityManager
        connectivityManager.registerDefaultNetworkCallback(object :
            ConnectivityManager.NetworkCallback() {
            override fun onAvailable(network: Network) { _isConnected.value = true }
            override fun onLost(network: Network) { _isConnected.value = false }
        })
    }
}
```

---

## Datenfluss

```
ViewModel / SyncEngine
    |
    | .getWorkSessions() / .syncWorkSessions(request)
    v
APIClient
    |
    | 1. acquireTokenSilently() -> AuthManager -> MSAL Cache/Refresh
    | 2. URLRequest / Ktor Request mit Bearer Token
    v
Backend API (https://api.track.fakturus.com)
    |
    | JSON Response (PascalCase)
    v
APIClient
    |
    | Decode: PascalCase -> camelCase (iOS custom decoder / Android @SerialName)
    v
DTOs (WorkSessionDTO, SyncVacationDaysResponse, ...)
    |
    v
ViewModel / SyncEngine verarbeitet DTOs weiter
```

---

## Testbare Kriterien

- [ ] iOS: APIClient dekodiert PascalCase JSON korrekt zu camelCase Properties
- [ ] iOS: APIClient enkodiert camelCase Properties zu PascalCase JSON
- [ ] iOS: 401 Response loest Token-Refresh + Retry aus
- [ ] iOS: NetworkMonitor.isConnected ist `false` wenn WLAN deaktiviert
- [ ] Android: APIClient dekodiert JSON korrekt via @SerialName
- [ ] Android: 401 wird mit Retry behandelt
- [ ] Android: NetworkMonitor.isConnected StateFlow aktualisiert bei Netzwerkwechsel
- [ ] Beide: User-Agent Header wird korrekt gesetzt
- [ ] Beide: Timeout nach 30s wird korrekt ausgeloest

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| PascalCase-Konvertierung bei verschachtelten Keys | Mittel | Testen mit echten Backend-Responses, ggf. eigene CodingKeys |
| ISO8601 Date mit Millisekunden (`.sss`) | Hoch | Multiple DateFormatter mit Fallback: mit ms -> ohne ms -> nur Datum |
| Ktor CIO Engine SSL-Probleme mit localhost | Mittel | OkHttp-Engine als Fallback, trust-all fuer Debug |
| MSAL Token-Refresh failt silent | Mittel | acquireTokenSilently mit forceRefresh, dann interaktiver Login |
| Backend aendert API Response Format | Niedrig | `ignoreUnknownKeys = true` schon konfiguriert |
