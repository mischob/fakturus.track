import SwiftUI

struct ErrorBanner: View {
    let message: String
    let action: (() -> Void)?
    let actionLabel: String?

    init(_ message: String, action: (() -> Void)? = nil, actionLabel: String? = nil) {
        self.message = message
        self.action = action
        self.actionLabel = actionLabel
    }

    var body: some View {
        HStack {
            Image(systemName: "exclamationmark.triangle.fill")
                .foregroundStyle(Theme.danger)
            Text(message)
                .font(.subheadline)
            Spacer()
            if let action, let label = actionLabel {
                Button(label, action: action)
                    .font(.subheadline.bold())
            }
        }
        .padding(12)
        .background(Theme.danger.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}

// MARK: - Preview

#Preview("Error with action") {
    ErrorBanner(
        "Synchronisation fehlgeschlagen",
        action: {},
        actionLabel: String(localized: "error_retry")
    )
    .padding()
}

#Preview("Error without action") {
    ErrorBanner("Verbindung zum Server verloren")
        .padding()
}
