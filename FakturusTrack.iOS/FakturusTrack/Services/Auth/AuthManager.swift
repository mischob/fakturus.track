import MSAL
import Observation
import UIKit

enum LoginProvider {
    case apple, google, email

    var domainHint: String? {
        switch self {
        case .apple: return "apple.com"
        case .google: return "google.com"
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

@Observable @MainActor
final class AuthManager {
    var isAuthenticated = false
    var currentAccount: MSALAccount?
    private(set) var accessToken: String?

    private var msalApp: MSALPublicClientApplication?

    init() {
        configureMSAL()
        Task { await checkExistingSession() }
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

            msalApp = try MSALPublicClientApplication(configuration: config)
            print("[AuthManager] MSAL configured successfully")
        } catch {
            print("[AuthManager] ERROR configuring MSAL: \(error)")
        }
    }

    private func checkExistingSession() async {
        guard let msalApp else {
            print("[AuthManager] Skipping session check - msalApp is nil")
            return
        }
        do {
            let accounts = try msalApp.allAccounts()
            guard let account = accounts.first else { return }
            let token = try await acquireTokenSilently()
            accessToken = token
            currentAccount = account
            isAuthenticated = true
            print("[AuthManager] Restored existing session")
        } catch {
            print("[AuthManager] No existing session: \(error)")
        }
    }

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
            currentAccount = result.account
            isAuthenticated = true
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
            return result.accessToken
        } catch {
            throw AuthError.tokenExpired
        }
    }

    func logout() {
        guard let msalApp, let account = currentAccount else { return }
        try? msalApp.remove(account)
        accessToken = nil
        currentAccount = nil
        isAuthenticated = false
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
