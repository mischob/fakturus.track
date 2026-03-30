import Foundation

enum Tier: Int, Comparable, Codable, Sendable {
    case free = 0
    case starter = 1
    case pro = 2

    static func < (lhs: Tier, rhs: Tier) -> Bool {
        lhs.rawValue < rhs.rawValue
    }

    /// Product-ID Mapping (StoreKit 2)
    init?(productID: String) {
        switch productID {
        case "starter_monthly": self = .starter
        case "pro_monthly": self = .pro
        default: return nil
        }
    }

    var displayName: String {
        switch self {
        case .free: "Free"
        case .starter: "Starter"
        case .pro: "Pro"
        }
    }
}
