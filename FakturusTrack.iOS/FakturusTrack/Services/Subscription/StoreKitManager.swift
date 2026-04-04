import StoreKit
import Foundation

@Observable @MainActor
final class StoreKitManager {
    private var subscriptionManager: SubscriptionManager?
    private var transactionListener: Task<Void, Never>?

    // Geladene Produkte
    private(set) var products: [Product] = []

    // Product IDs
    static let productIDs: Set<String> = [
        "com.fakturus.track.starter.monthly",
        "com.fakturus.track.starter.yearly",
        "com.fakturus.track.pro.monthly",
        "com.fakturus.track.pro.yearly",
    ]

    // MARK: - Setup

    func configure(subscriptionManager: SubscriptionManager) async {
        self.subscriptionManager = subscriptionManager

        // Produkte laden
        await fetchProducts()

        // Aktuellen Abo-Status pruefen
        await checkCurrentEntitlements()
    }

    // MARK: - Produkte laden

    func fetchProducts() async {
        print("[StoreKit] Fetching products: \(Self.productIDs)")
        do {
            products = try await Product.products(for: Self.productIDs)
                .sorted { $0.price < $1.price }
            print("[StoreKit] Loaded \(products.count) products: \(products.map { "\($0.id) (\($0.displayPrice))" })")
        } catch {
            print("[StoreKit] Failed to fetch products: \(error)")
        }
    }

    // MARK: - Kauf

    func purchase(_ product: Product) async throws -> Transaction? {
        let result = try await product.purchase()

        switch result {
        case .success(let verification):
            let transaction = try checkVerified(verification)
            await updateTierFromTransaction(transaction)
            await transaction.finish()
            return transaction

        case .userCancelled:
            return nil

        case .pending:
            // z.B. Ask-to-Buy bei Kindern
            return nil

        @unknown default:
            return nil
        }
    }

    // MARK: - Entitlements pruefen (App-Start + Restore)

    func checkCurrentEntitlements() async {
        var highestTier: Tier = .free

        for await result in Transaction.currentEntitlements {
            if let transaction = try? checkVerified(result) {
                if let tier = Tier(productID: transaction.productID),
                   tier > highestTier {
                    highestTier = tier
                }
            }
        }

        subscriptionManager?.updateTier(highestTier)
    }

    // MARK: - Restore Purchases

    func restorePurchases() async throws {
        try await AppStore.sync()
        await checkCurrentEntitlements()
    }

    // MARK: - Transaction Listener (laeuft dauerhaft)

    func listenForTransactions() async {
        for await result in Transaction.updates {
            guard let transaction = try? checkVerified(result) else { continue }

            await updateTierFromTransaction(transaction)
            await transaction.finish()
        }
    }

    // MARK: - Private Helpers

    private func checkVerified<T>(_ result: VerificationResult<T>) throws -> T {
        switch result {
        case .unverified(_, let error):
            throw error
        case .verified(let value):
            return value
        }
    }

    private func updateTierFromTransaction(_ transaction: Transaction) async {
        // Revoked = Tier runtersetzen
        if transaction.revocationDate != nil {
            await checkCurrentEntitlements() // Neu berechnen
            return
        }

        // Neuer Kauf oder Verlaengerung
        if let tier = Tier(productID: transaction.productID) {
            subscriptionManager?.updateTier(tier)
        }
    }
}
