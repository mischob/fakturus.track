import Foundation
import Observation

@Observable @MainActor
final class SubscriptionManager {
    private(set) var currentTier: Tier = .free

    private let tierCacheKey = "cached_subscription_tier"

    /// TestFlight + Debug: Alle Features frei. App Store Release: normales Abo-System.
    private static var isTestFlightOrDebug: Bool {
        #if DEBUG
        return true
        #else
        return Bundle.main.appStoreReceiptURL?.lastPathComponent == "sandboxReceipt"
        #endif
    }

    init() {
        if Self.isTestFlightOrDebug {
            currentTier = .pro
        } else {
            let cached = UserDefaults.standard.integer(forKey: tierCacheKey)
            currentTier = Tier(rawValue: cached) ?? .free
        }
    }

    func isAvailable(_ feature: FeatureGate) -> Bool {
        feature.requiredTier <= currentTier
    }

    /// Wird von StoreKitManager aufgerufen bei Kauf/Verlaengerung/Kuendigung
    func updateTier(_ newTier: Tier) {
        // TestFlight/Debug: immer Pro, StoreKit-Updates ignorieren
        guard !Self.isTestFlightOrDebug else { return }
        currentTier = newTier
        UserDefaults.standard.set(newTier.rawValue, forKey: tierCacheKey)
    }

    /// History-Filter: nur 30 Tage im FREE-Tier
    var historyDateLimit: Date? {
        guard currentTier < .starter else { return nil }
        return Calendar.current.date(byAdding: .day, value: -30, to: Date())
    }
}
