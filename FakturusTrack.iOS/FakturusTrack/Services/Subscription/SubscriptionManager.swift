import Foundation
import Observation

@Observable @MainActor
final class SubscriptionManager {
    private(set) var currentTier: Tier = .free

    private let tierCacheKey = "cached_subscription_tier"

    init() {
        // Gecachten Tier laden (Offline-Faehigkeit)
        let cached = UserDefaults.standard.integer(forKey: tierCacheKey)
        currentTier = Tier(rawValue: cached) ?? .free
    }

    func isAvailable(_ feature: FeatureGate) -> Bool {
        feature.requiredTier <= currentTier
    }

    /// Wird von StoreKitManager aufgerufen bei Kauf/Verlaengerung/Kuendigung
    func updateTier(_ newTier: Tier) {
        currentTier = newTier
        UserDefaults.standard.set(newTier.rawValue, forKey: tierCacheKey)
    }

    /// History-Filter: nur 30 Tage im FREE-Tier
    var historyDateLimit: Date? {
        guard currentTier < .starter else { return nil }
        return Calendar.current.date(byAdding: .day, value: -30, to: Date())
    }
}
