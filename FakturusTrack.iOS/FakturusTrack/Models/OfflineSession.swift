import Foundation

struct OfflineSession: Codable {
    let userId: String
    let lastSuccessfulAuth: Date

    var isValid: Bool {
        let maxOfflineDays = 14.0
        let daysSinceAuth = Date().timeIntervalSince(lastSuccessfulAuth) / 86400
        return daysSinceAuth <= maxOfflineDays
    }

    var daysUntilExpiry: Int {
        let daysSinceAuth = Date().timeIntervalSince(lastSuccessfulAuth) / 86400
        return max(0, Int(14.0 - daysSinceAuth))
    }
}
