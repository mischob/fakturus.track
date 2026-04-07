# Tech-Spec: EPIC 02 -- In-App Purchase iOS (StoreKit 2)

## Uebersicht

Eine einzige Datei `StoreKitManager.swift` kapselt die gesamte StoreKit 2 Integration. Sie laedt Produkte, verarbeitet Kaeufe und haelt den SubscriptionManager aktuell.

---

## StoreKitManager.swift

```swift
import StoreKit
import Foundation

@MainActor
final class StoreKitManager {
    private var subscriptionManager: SubscriptionManager?
    private var transactionListener: Task<Void, Never>?

    // Geladene Produkte
    private(set) var products: [Product] = []

    // Product IDs
    static let productIDs: Set<String> = ["starter_monthly", "pro_monthly"]

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
        do {
            products = try await Product.products(for: Self.productIDs)
                .sorted { $0.price < $1.price } // Starter zuerst
        } catch {
            print("StoreKit: Failed to fetch products: \(error)")
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
```

---

## Integration in FakturusTrackApp.swift

```swift
@main
struct FakturusTrackApp: App {
    @State private var services = ServiceContainer()

    var body: some Scene {
        WindowGroup {
            // ... bestehender Auth-Check + ContentView ...
        }
        .environment(services.subscriptionManager)
        .environment(services.storeKitManager) // Fuer PaywallView Zugriff auf Produkte+Preise
        .task {
            // Transaction Listener laeuft die gesamte App-Lebenszeit
            await services.storeKitManager.listenForTransactions()
        }
    }
}
```

---

## Integration in ServiceContainer.swift

```swift
// NEU in ServiceContainer:
let subscriptionManager = SubscriptionManager()
let storeKitManager = StoreKitManager()

init() {
    // ... bestehender Code ...

    // StoreKit bei App-Start initialisieren (NICHT erst bei Login!)
    // Grund: Abo-Status und Produkte muessen sofort verfuegbar sein,
    // auch bevor der User eingeloggt ist (z.B. Paywall in Onboarding).
    Task {
        await storeKitManager.configure(subscriptionManager: subscriptionManager)
    }
}
```

**Hinweis**: StoreKit 2 funktioniert ohne Login (Apple-ID reicht). Die Initialisierung erfolgt in `init()` statt `onLogin()`, damit der Abo-Status und die Produktpreise sofort bei App-Start verfuegbar sind. SubscriptionManager und StoreKitManager sind Login-unabhaengig.

---

## StoreKit Configuration File (Xcode Testing)

Datei: `FakturusTrack/Resources/StoreKit/FakturusTrack.storekit`

Erstellt via Xcode: File -> New -> File -> StoreKit Configuration File.

Konfiguration:
- Subscription Group: `fakturus_track_premium`
- Product `starter_monthly`: Auto-Renewable, EUR 2.99
- Product `pro_monthly`: Auto-Renewable, EUR 4.99
- Upgrade: starter -> pro (immediate)
- Downgrade: pro -> starter (deferred)

**Vorteil**: Lokales Testing ohne App Store Connect. Sandbox-Abos laufen alle 5 Minuten ab.

---

## Restore Purchases in SettingsView

```swift
// SettingsViewModel.swift
@Observable
final class SettingsViewModel {
    // ... bestehende Properties ...

    var isRestoringPurchases = false
    var restoreResult: String?

    func restorePurchases() async {
        isRestoringPurchases = true
        defer { isRestoringPurchases = false }

        do {
            try await storeKitManager.restorePurchases()
            let tier = subscriptionManager.currentTier
            if tier > .free {
                restoreResult = String(localized: "restore_success \(tier.displayName)")
            } else {
                restoreResult = String(localized: "restore_no_subscription")
            }
        } catch {
            restoreResult = String(localized: "restore_error")
        }
    }
}
```

---

## Error-Handling

StoreKit 2 Fehler die abgefangen werden muessen:

| Fehler | Behandlung |
|--------|-----------|
| `StoreKitError.userCancelled` | Kein Fehler anzeigen, Paywall bleibt offen |
| `StoreKitError.networkError` | "Netzwerkfehler. Bitte pruefen Sie Ihre Verbindung." |
| `StoreKitError.systemError` | "Kauf konnte nicht abgeschlossen werden." |
| `StoreKitError.notAvailableInStorefront` | Sollte nicht auftreten (DACH-Region) |
| `VerificationResult.unverified` | Transaction ignorieren, Log schreiben |

---

## App Store Connect Produkt-Konfiguration

Muss manuell in App Store Connect konfiguriert werden (nicht Code):

1. **Subscription Group**: `fakturus_track_premium`
2. **starter_monthly**:
   - Type: Auto-Renewable
   - Preis: Tier 3 (EUR 2.99)
   - Anzeigename DE/EN: "Fakturus Track Starter"
   - Beschreibung: Feature-Liste
3. **pro_monthly**:
   - Type: Auto-Renewable
   - Preis: Tier 5 (EUR 4.99)
   - Anzeigename DE/EN: "Fakturus Track Pro"
   - Beschreibung: Feature-Liste
4. **Upgrade/Downgrade**: Innerhalb der Subscription Group automatisch
5. **Sandbox Tester**: 3 Accounts in App Store Connect anlegen
6. **Review Information**: Testaccount + Anleitung hinterlegen
