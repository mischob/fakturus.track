import SwiftUI

struct OfflineBanner: View {
    @Environment(NetworkMonitor.self) private var networkMonitor

    var body: some View {
        if !networkMonitor.isConnected {
            HStack(spacing: 8) {
                Image(systemName: "wifi.slash")
                    .font(.caption)
                Text("Offline -- Aenderungen werden lokal gespeichert")
                    .font(.caption)
                Spacer()
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
            .background(Theme.offlineBanner.opacity(0.2))
            .foregroundStyle(Theme.offlineBanner)
            .transition(.move(edge: .top).combined(with: .opacity))
        }
    }
}

// MARK: - Preview

#Preview("Offline") {
    let monitor = NetworkMonitor()
    OfflineBanner()
        .environment(monitor)
}
