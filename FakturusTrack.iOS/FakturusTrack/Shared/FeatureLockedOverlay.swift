import SwiftUI

struct FeatureLockedOverlay: ViewModifier {
    let feature: FeatureGate
    @Environment(SubscriptionManager.self) private var subscriptionManager
    @State private var showPaywall = false

    func body(content: Content) -> some View {
        if subscriptionManager.isAvailable(feature) {
            content
        } else {
            content
                .disabled(true)
                .overlay(alignment: .topTrailing) {
                    Image(systemName: "lock.fill")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(6)
                        .background(.ultraThinMaterial, in: Circle())
                        .padding(4)
                }
                .opacity(0.6)
                .onTapGesture {
                    showPaywall = true
                }
                .sheet(isPresented: $showPaywall) {
                    PaywallView(highlightedFeature: feature)
                }
        }
    }
}

extension View {
    func featureLocked(_ feature: FeatureGate) -> some View {
        modifier(FeatureLockedOverlay(feature: feature))
    }
}
