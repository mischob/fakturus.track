import MSAL
import Observation
import UIKit

enum LoginProvider {
    case apple, google, microsoft, amazon, email

    var domainHint: String? {
        switch self {
        case .apple: return "apple.com"
        case .google: return "google.com"
        case .microsoft: return "live.com"
        case .amazon: return "amazon.com"
        case .email: return nil
        }
    }
}

enum AuthError: Error {
    case notAuthenticated
    case tokenExpired
    case cancelled
    case failed(String)
}

enum AppStartResult {
    case authenticated
    case offlineWithSession
    case loginRequired
    case loginRequiredNoNetwork
}

enum LoginContext {
    case normal
    case sessionExpired
    case firstLogin
}

@Observable @MainActor
final class AuthManager {
    var isAuthenticated = false
    var isOfflineMode = false
    var currentAccount: MSALAccount?
    private(set) var accessToken: String?
    private var tokenExpiry: Date?

    private var msalApp: MSALPublicClientApplication?
    private let offlineSessionManager = OfflineSessionManager()

    init() {
        configureMSAL()
    }

    private func configureMSAL() {
        do {
            guard let authorityURL = URL(string: Configuration.b2cAuthorityUrl) else {
                print("[AuthManager] ERROR: Invalid authority URL: \(Configuration.b2cAuthorityUrl)")
                return
            }

            let authority = try MSALB2CAuthority(url: authorityURL)

            let config = MSALPublicClientApplicationConfig(
                clientId: Configuration.b2cClientId,
                redirectUri: Configuration.b2cRedirectUri,
                authority: authority
            )
            config.knownAuthorities = [authority]
            config.cacheConfig.keychainSharingGroup = "com.fakturus.track"

            msalApp = try MSALPublicClientApplication(configuration: config)
            print("[AuthManager] MSAL configured successfully")
        } catch {
            print("[AuthManager] ERROR configuring MSAL: \(error)")
        }
    }

    // MARK: - App Start Decision Logic

    func resolveStartState(networkMonitor: NetworkMonitor) async -> (result: AppStartResult, context: LoginContext) {
        let isOnline = networkMonitor.isConnected

        if isOnline {
            // Online: MSAL darf Netzwerk nutzen
            guard let msalApp else {
                return (.loginRequired, .normal)
            }
            do {
                let accounts = try msalApp.allAccounts()
                guard let account = accounts.first else {
                    return (.loginRequired, .normal)
                }
                currentAccount = account
                let token = try await acquireTokenSilently()
                accessToken = token
                updateOfflineSession()
                isAuthenticated = true
                print("[AuthManager] Online: restored session via silent refresh")
                return (.authenticated, .normal)
            } catch {
                return (.loginRequired, .normal)
            }
        }

        // Offline: KEIN MSAL-Netzwerkaufruf!
        // Nur lokalen Cache pruefen (Access Token noch gueltig?)
        if let token = accessToken, let expiry = tokenExpiry, expiry > Date() {
            isAuthenticated = true
            isOfflineMode = true
            print("[AuthManager] Offline: cached access token still valid")
            return (.offlineWithSession, .normal)
        }

        // Access Token abgelaufen/nicht vorhanden -> Offline-Session pruefen
        if let session = offlineSessionManager.load() {
            if session.isValid {
                isAuthenticated = true
                isOfflineMode = true
                print("[AuthManager] Offline: valid offline session (userId: \(session.userId), \(session.daysUntilExpiry) days left)")
                return (.offlineWithSession, .normal)
            } else {
                print("[AuthManager] Offline: session expired (\(session.daysUntilExpiry) days until expiry)")
                return (.loginRequiredNoNetwork, .sessionExpired)
            }
        }

        // Noch nie eingeloggt
        print("[AuthManager] Offline: no session found (first login)")
        return (.loginRequiredNoNetwork, .firstLogin)
    }

    // MARK: - Background Token Refresh (offline -> online)

    func attemptBackgroundTokenRefresh() async {
        guard isOfflineMode else { return }

        do {
            let token = try await acquireTokenSilently(forceRefresh: true)
            accessToken = token
            isOfflineMode = false
            updateOfflineSession()
            print("[AuthManager] Background refresh successful, exiting offline mode")
        } catch {
            print("[AuthManager] Background refresh failed: \(error.localizedDescription)")
        }
    }

    // MARK: - Interactive Login

    func acquireTokenInteractively(provider: LoginProvider) async throws {
        guard let msalApp else {
            throw AuthError.failed("MSAL nicht initialisiert. Bitte App neu starten.")
        }

        guard let viewController = Self.topViewController() else {
            throw AuthError.failed("Kein ViewController verfuegbar fuer Login-Anzeige.")
        }

        print("[AuthManager] Starting interactive login with provider: \(provider), VC: \(type(of: viewController))")

        let webviewParams = MSALWebviewParameters(authPresentationViewController: viewController)
        let params = MSALInteractiveTokenParameters(
            scopes: Configuration.b2cScopes,
            webviewParameters: webviewParams
        )

        if let hint = provider.domainHint {
            params.extraQueryParameters = ["domain_hint": hint]
        }

        do {
            let result = try await msalApp.acquireToken(with: params)
            accessToken = result.accessToken
            tokenExpiry = result.expiresOn
            currentAccount = result.account
            isAuthenticated = true
            isOfflineMode = false
            updateOfflineSession()
            print("[AuthManager] Login successful")
        } catch let error as NSError {
            print("[AuthManager] Login error: \(error.domain) code=\(error.code) \(error.localizedDescription)")
            if error.domain == MSALErrorDomain,
               error.code == MSALError.userCanceled.rawValue {
                throw AuthError.cancelled
            }
            throw AuthError.failed(error.localizedDescription)
        }
    }

    // MARK: - Silent Token Refresh

    func acquireTokenSilently(forceRefresh: Bool = false) async throws -> String {
        guard let msalApp else { throw AuthError.notAuthenticated }

        let account: MSALAccount
        if let current = currentAccount {
            account = current
        } else {
            let accounts = try msalApp.allAccounts()
            guard let first = accounts.first else { throw AuthError.notAuthenticated }
            account = first
        }

        let params = MSALSilentTokenParameters(scopes: Configuration.b2cScopes, account: account)
        params.forceRefresh = forceRefresh

        do {
            let result = try await msalApp.acquireTokenSilent(with: params)
            accessToken = result.accessToken
            tokenExpiry = result.expiresOn
            updateOfflineSession()
            return result.accessToken
        } catch {
            throw AuthError.tokenExpired
        }
    }

    // MARK: - Logout

    func logout() {
        guard let msalApp, let account = currentAccount else { return }
        try? msalApp.remove(account)
        accessToken = nil
        tokenExpiry = nil
        currentAccount = nil
        isAuthenticated = false
        isOfflineMode = false
        offlineSessionManager.delete()
        print("[AuthManager] Logged out, offline session deleted")
    }

    // MARK: - Offline Session Helpers

    private func updateOfflineSession() {
        guard let account = currentAccount else { return }
        let session = OfflineSession(
            userId: account.identifier ?? "",
            lastSuccessfulAuth: Date()
        )
        try? offlineSessionManager.save(session)
    }

    // MARK: - Top ViewController Helper

    private static func topViewController() -> UIViewController? {
        guard let scene = UIApplication.shared.connectedScenes
            .compactMap({ $0 as? UIWindowScene })
            .first(where: { $0.activationState == .foregroundActive }),
              let rootVC = scene.windows.first(where: { $0.isKeyWindow })?.rootViewController
        else {
            print("[AuthManager] WARNING: No key window or root VC found")
            return nil
        }

        var topVC = rootVC
        while let presented = topVC.presentedViewController {
            topVC = presented
        }
        return topVC
    }
}
