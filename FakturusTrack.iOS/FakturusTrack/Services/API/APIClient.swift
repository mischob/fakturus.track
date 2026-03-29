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
                // 1x Retry with forced token refresh
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
