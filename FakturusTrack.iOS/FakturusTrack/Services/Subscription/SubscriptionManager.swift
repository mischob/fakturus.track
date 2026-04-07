import Foundation
import Observation

@Observable @MainActor
final class SubscriptionManager {
    private(set) var currentTier: Tier = .free

    private let tierCacheKey = "cached_subscription_tier"

    /// TestFlight: Alle Features frei. Debug + App Store Release: normales Abo-System.
    private static var isTestFlight: Bool {
        #if DEBUG
        return false
        #else
        return Bundle.main.appStoreReceiptURL?.lastPathComponent == "sandboxReceipt"
        #endif
    }

    init() {
        if Self.isTestFlight {
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
        // TestFlight: immer Pro, StoreKit-Updates ignorieren
        guard !Self.isTestFlight else { return }
        currentTier = newTier
        UserDefaults.standard.set(newTier.rawValue, forKey: tierCacheKey)
    }

    /// History-Filter: nur 365 Tage im FREE-Tier
    var historyDateLimit: Date? {
        guard currentTier < .starter else { return nil }
        return Calendar.current.date(byAdding: .day, value: -365, to: Date())
    }
}
