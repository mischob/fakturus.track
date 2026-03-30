import SwiftUI

struct PaywallTeaserView: View {
    let feature: FeatureGate
    let title: String
    let description: String
    @State private var showPaywall = false

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "lock.shield")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)

            Text(title)
                .font(.title2.bold())

            Text(description)
                .font(.body)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal, 32)

            Button {
                showPaywall = true
            } label: {
                Text(String(localized: "paywall_upgrade_button"))
                    .font(.headline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .padding(.horizontal, 48)

            Text("\(String(localized: "paywall_required_tier")): \(feature.requiredTier.displayName)")
                .font(.caption)
                .foregroundStyle(.tertiary)

            Spacer()
        }
        .sheet(isPresented: $showPaywall) {
            PaywallView(highlightedFeature: feature)
        }
    }
}
