import SwiftUI
import AuthenticationServices

struct LoginView: View {
    @Environment(AuthManager.self) private var authManager
    @Environment(NetworkMonitor.self) private var networkMonitor
    @State private var isLoading = false
    @State private var errorMessage: String?

    var loginContext: LoginContext = .normal

    var body: some View {
        VStack(spacing: 32) {
            Spacer()

            // Logo
            Image(systemName: "clock.badge.checkmark")
                .font(.system(size: 72))
                .foregroundStyle(Theme.primary)
                .accessibilityHidden(true)

            VStack(spacing: 8) {
                Text(String(localized: "login_title"))
                    .font(.largeTitle.bold())
                Text(String(localized: "login_subtitle"))
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            // Offline-Hinweis
            if !networkMonitor.isConnected {
                offlineInfoBox
                    .transition(.opacity.combined(with: .move(edge: .top)))
            }

            // Login Buttons
            VStack(spacing: 12) {
                SignInWithAppleButton(.signIn) { _ in
                    // ASAuthorizationAppleIDButton styling
                } onCompletion: { _ in }
                .frame(height: 50)
                .overlay {
                    Button { login(provider: .apple) } label: { Color.clear }
                }

                loginButton(String(localized: "login_google"), icon: "g.circle.fill", provider: .google)
                loginButton(String(localized: "login_microsoft"), icon: "window.casement", provider: .microsoft)
                loginButton(String(localized: "login_amazon"), icon: "cart.fill", provider: .amazon)
                loginButton(String(localized: "login_email"), icon: "envelope.fill", provider: .email)
            }
            .disabled(isLoading || !networkMonitor.isConnected)

            if isLoading {
                ProgressView()
            }

            if let error = errorMessage {
                Text(error)
                    .font(.caption)
                    .foregroundStyle(.red)
                    .multilineTextAlignment(.center)
            }

            Spacer()
        }
        .padding(.horizontal, 32)
        .animation(.easeInOut(duration: 0.3), value: networkMonitor.isConnected)
    }

    @ViewBuilder
    private var offlineInfoBox: some View {
        HStack(spacing: 12) {
            Image(systemName: "wifi.slash")
                .foregroundStyle(.white)
            Text(offlineMessage)
                .font(.callout)
                .foregroundStyle(.white)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding()
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color.orange.opacity(0.9))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var offlineMessage: String {
        switch loginContext {
        case .firstLogin:
            return "Fuer die erste Anmeldung wird eine Internetverbindung benoetigt."
        case .sessionExpired:
            return "Ihre Sitzung ist abgelaufen. Bitte stellen Sie eine Internetverbindung her, um sich erneut anzumelden."
        case .normal:
            return "Keine Internetverbindung. Bitte stellen Sie eine Verbindung her, um sich anzumelden."
        }
    }

    private func loginButton(_ title: String, icon: String, provider: LoginProvider) -> some View {
        Button {
            login(provider: provider)
        } label: {
            Label(title, systemImage: icon)
                .frame(maxWidth: .infinity)
                .padding(.vertical, 12)
        }
        .buttonStyle(.bordered)
    }

    private func login(provider: LoginProvider) {
        isLoading = true
        errorMessage = nil
        Task {
            do {
                try await authManager.acquireTokenInteractively(provider: provider)
            } catch AuthError.cancelled {
                // User cancelled, not an error
            } catch {
                #if DEBUG
                errorMessage = "Anmeldung fehlgeschlagen: \(error)"
                #else
                errorMessage = String(localized: "login_failed")
                #endif
            }
            isLoading = false
        }
    }
}
