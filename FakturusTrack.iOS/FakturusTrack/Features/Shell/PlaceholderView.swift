import SwiftUI

struct PlaceholderView: View {
    @Environment(AuthManager.self) private var authManager

    let icon: String
    let title: String
    let message: String
    var showLogout: Bool = false

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Spacer()

                Image(systemName: icon)
                    .font(.system(size: 64))
                    .foregroundStyle(.secondary)

                Text(title)
                    .font(.title2.bold())

                Text(message)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)

                Spacer()

                if showLogout {
                    Button("Abmelden", role: .destructive) {
                        authManager.logout()
                    }
                    .padding(.bottom, 32)
                }
            }
            .frame(maxWidth: .infinity)
            .navigationTitle(title)
        }
    }
}
