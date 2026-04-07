import Foundation

// MARK: - AnyCodingKey

/// Reusable CodingKey for PascalCase conversion
struct AnyCodingKey: CodingKey {
    var stringValue: String
    var intValue: Int?

    init?(stringValue: String) { self.stringValue = stringValue }
    init?(intValue: Int) { self.intValue = intValue; self.stringValue = String(intValue) }
}

// MARK: - APIError

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

// MARK: - APIClient

final class APIClient: @unchecked Sendable {
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

        // Backend liefert camelCase JSON - kein custom Strategy nötig
        // DTOs nutzen explizite CodingKeys für das Mapping
        self.decoder = JSONDecoder()
        self.encoder = JSONEncoder()
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
        let (data, response) = try await session.data(for: request)
        try validateResponse(response, data: data)
    }

    func delete(_ path: String) async throws {
        let request = try await buildRequest(path: path, method: "DELETE")
        let (data, response) = try await session.data(for: request)
        try validateResponse(response, data: data)
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
        // Only set Content-Type for requests with body (POST, PUT, PATCH)
        if method != "GET" && method != "DELETE" {
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        }

        let appVersion = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0"
        request.setValue("FakturusTrack-iOS/\(appVersion)", forHTTPHeaderField: "User-Agent")

        // Token Injection: use cached token first, only refresh on 401
        let token: String
        if let cached = await MainActor.run(body: { authManager.accessToken }) {
            token = cached
        } else {
            print("[API] No cached token, acquiring silently...")
            token = try await authManager.acquireTokenSilently()
        }
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        print("[API] \(method) \(baseURL)\(path)")
        return request
    }

    private func execute<T: Decodable>(_ request: URLRequest) async throws -> T {
        let (data, response) = try await executeWithRetry(request)
        let httpResponse = response as! HTTPURLResponse
        print("[API] Response \(httpResponse.statusCode) (\(data.count) bytes)")
        try validateResponse(response, data: data)
        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            print("[API] Decode error: \(error)")
            if let body = String(data: data, encoding: .utf8) {
                print("[API] Response body: \(body.prefix(500))")
            }
            throw APIError.decodingError(error)
        }
    }

    private func executeWithRetry(_ request: URLRequest) async throws -> (Data, URLResponse) {
        do {
            let (data, response) = try await session.data(for: request)
            let httpResponse = response as! HTTPURLResponse
            if httpResponse.statusCode == 401 {
                print("[API] 401 - Retrying with forced token refresh")
                var retryRequest = request
                let newToken = try await authManager.acquireTokenSilently(forceRefresh: true)
                retryRequest.setValue("Bearer \(newToken)", forHTTPHeaderField: "Authorization")
                return try await session.data(for: retryRequest)
            }
            return (data, response)
        } catch let error as APIError {
            throw error
        } catch {
            print("[API] Network error: \(error)")
            throw APIError.network(error)
        }
    }

    private func validateResponse(_ response: URLResponse, data: Data? = nil) throws {
        let code = (response as! HTTPURLResponse).statusCode
        switch code {
        case 200...299: return
        case 401: throw APIError.unauthorized
        case 403: throw APIError.forbidden
        case 404: throw APIError.notFound
        case 500...599:
            if let data, let body = String(data: data, encoding: .utf8) {
                print("[API] Server error \(code): \(body.prefix(300))")
            }
            throw APIError.serverError(code)
        default:
            if let data, let body = String(data: data, encoding: .utf8) {
                print("[API] Error \(code): \(body.prefix(300))")
            }
            throw APIError.unknown(code)
        }
    }
}
