import SwiftUI
import AuthenticationServices

struct LoginView: View {
    @Environment(AuthManager.self) private var authManager
    @State private var isLoading = false
    @State private var errorMessage: String?

    var body: some View {
        VStack(spacing: 32) {
            Spacer()

            // Logo
            Image(systemName: "clock.badge.checkmark")
                .font(.system(size: 72))
                .foregroundStyle(Theme.primary)

            VStack(spacing: 8) {
                Text("Fakturus Track")
                    .font(.largeTitle.bold())
                Text("Arbeitszeit erfassen. Einfach. Überall.")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            // Login Buttons
            VStack(spacing: 12) {
                SignInWithAppleButton(.signIn) { _ in
                    // ASAuthorizationAppleIDButton styling
                } onCompletion: { _ in }
                .frame(height: 50)
                .overlay {
                    Button { login(provider: .apple) } label: { Color.clear }
                }

                loginButton("Mit Google anmelden", icon: "g.circle.fill", provider: .google)
                loginButton("Mit E-Mail anmelden", icon: "envelope.fill", provider: .email)
            }
            .disabled(isLoading)

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
                errorMessage = "Anmeldung fehlgeschlagen. Bitte versuchen Sie es erneut."
                #endif
            }
            isLoading = false
        }
    }
}
