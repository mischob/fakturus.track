import SwiftUI

struct ArbZGBanner: View {
    let netWorkMinutes: Int
    let pauseMinutes: Int

    @State private var hasShown6h = false
    @State private var hasShown9h = false
    @State private var hasShown10h = false
    @State private var currentBanner: ArbZGHint?

    enum ArbZGHint: Identifiable {
        case sixHours, nineHours, tenHours

        var id: Self { self }

        var message: String {
            switch self {
            case .sixHours:
                return "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
            case .nineHours:
                return "Erinnerung: Nach 9 Stunden Arbeit betraegt die Mindestpause 45 Minuten."
            case .tenHours:
                return "Hinweis: Sie arbeiten seit 10 Stunden. Die gesetzliche Hoechstarbeitszeit betraegt 10 Stunden."
            }
        }
    }

    var body: some View {
        if let banner = currentBanner {
            HStack {
                Image(systemName: "exclamationmark.triangle.fill")
                    .foregroundStyle(.orange)
                Text(banner.message)
                    .font(.caption)
                Spacer()
                Button("OK") {
                    withAnimation { currentBanner = nil }
                }
                .font(.caption.bold())
            }
            .padding(12)
            .background(Color.orange.opacity(0.1))
            .clipShape(RoundedRectangle(cornerRadius: 8))
            .transition(.move(edge: .top).combined(with: .opacity))
        }
    }

    /// Call this on every timer update to check ArbZG thresholds
    func checkThresholds() -> ArbZGBanner {
        var copy = self
        let netHours = Double(netWorkMinutes) / 60.0

        if netHours >= 10 && !hasShown10h {
            copy.hasShown10h = true
            copy.currentBanner = .tenHours
        } else if netHours >= 9 && pauseMinutes < 45 && !hasShown9h {
            copy.hasShown9h = true
            copy.currentBanner = .nineHours
        } else if netHours >= 6 && pauseMinutes < 30 && !hasShown6h {
            copy.hasShown6h = true
            copy.currentBanner = .sixHours
        }

        return copy
    }
}

/// Wrapper that handles threshold checking with a timer
struct ArbZGBannerContainer: View {
    let netWorkMinutes: Int
    let pauseMinutes: Int
    let isActive: Bool

    @State private var hasShown6h = false
    @State private var hasShown9h = false
    @State private var hasShown10h = false
    @State private var currentBanner: ArbZGBanner.ArbZGHint?

    var body: some View {
        if let banner = currentBanner {
            HStack {
                Image(systemName: "exclamationmark.triangle.fill")
                    .foregroundStyle(.orange)
                Text(banner.message)
                    .font(.caption)
                Spacer()
                Button("OK") {
                    withAnimation { currentBanner = nil }
                }
                .font(.caption.bold())
            }
            .padding(12)
            .background(Color.orange.opacity(0.1))
            .clipShape(RoundedRectangle(cornerRadius: 8))
            .transition(.move(edge: .top).combined(with: .opacity))
        }

        Color.clear
            .frame(height: 0)
            .onChange(of: netWorkMinutes) { _, newValue in
                guard isActive else { return }
                checkThresholds(netMinutes: newValue)
            }
    }

    private func checkThresholds(netMinutes: Int) {
        let netHours = Double(netMinutes) / 60.0

        if netHours >= 10 && !hasShown10h {
            hasShown10h = true
            withAnimation { currentBanner = .tenHours }
        } else if netHours >= 9 && pauseMinutes < 45 && !hasShown9h {
            hasShown9h = true
            withAnimation { currentBanner = .nineHours }
        } else if netHours >= 6 && pauseMinutes < 30 && !hasShown6h {
            hasShown6h = true
            withAnimation { currentBanner = .sixHours }
        }
    }
}

// MARK: - Preview

#Preview("6h Warning") {
    ArbZGBannerContainer(
        netWorkMinutes: 365,
        pauseMinutes: 10,
        isActive: true
    )
    .padding()
}
