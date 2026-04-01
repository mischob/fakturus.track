# Tech-Spec: EPIC 04 -- Paywall-UI & Upgrade-Flow

## Uebersicht

Die Paywall ist der zentrale Conversion-Screen. Sie wird als Sheet (iOS) bzw. BottomSheet (Android) dargestellt und zeigt einen Feature-Vergleich mit dynamischen Preisen. Das kontextsensitive Highlighting zeigt dem Nutzer direkt, welches Feature den Paywall-Flow ausgeloest hat.

---

## PaywallView.swift (iOS)

```swift
import SwiftUI
import StoreKit

struct PaywallView: View {
    let highlightedFeature: FeatureGate?
    @Environment(SubscriptionManager.self) private var subscriptionManager
    @Environment(StoreKitManager.self) private var storeKitManager
    @Environment(\.dismiss) private var dismiss
    @State private var isPurchasing = false
    @State private var purchaseError: String?
    @State private var showSuccess = false

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 24) {
                    // Header
                    headerSection

                    // Feature-Vergleichstabelle
                    featureComparisonTable

                    // Kauf-Buttons
                    purchaseButtons

                    // Restore + Legal
                    footerSection
                }
                .padding()
            }
            .navigationTitle(String(localized: "paywall_title"))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button(String(localized: "paywall_close")) { dismiss() }
                }
            }
        }
        .overlay {
            if showSuccess {
                successOverlay
            }
        }
    }

    // MARK: - Header

    @ViewBuilder
    private var headerSection: some View {
        VStack(spacing: 8) {
            Image(systemName: "star.circle.fill")
                .font(.system(size: 56))
                .foregroundStyle(.yellow)

            Text(String(localized: "paywall_headline"))
                .font(.title2.bold())
                .multilineTextAlignment(.center)

            Text(String(localized: "paywall_subheadline"))
                .font(.body)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
        }
        .padding(.top)
    }

    // MARK: - Feature-Vergleichstabelle

    @ViewBuilder
    private var featureComparisonTable: some View {
        VStack(spacing: 0) {
            // Header-Zeile
            HStack {
                Text(String(localized: "paywall_feature"))
                    .font(.caption.bold())
                    .frame(maxWidth: .infinity, alignment: .leading)
                Text("Free")
                    .font(.caption.bold())
                    .frame(width: 50)
                Text("Starter")
                    .font(.caption.bold())
                    .frame(width: 60)
                Text("Pro")
                    .font(.caption.bold())
                    .frame(width: 50)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(Color(.systemGray6))

            Divider()

            // Feature-Zeilen
            ForEach(paywallFeatures, id: \.name) { feature in
                featureRow(feature)
                Divider()
            }
        }
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(Color(.systemGray4), lineWidth: 1)
        )
    }

    @ViewBuilder
    private func featureRow(_ feature: PaywallFeature) -> some View {
        let isHighlighted = feature.gate == highlightedFeature
        HStack {
            Text(feature.name)
                .font(.subheadline)
                .fontWeight(isHighlighted ? .bold : .regular)
                .frame(maxWidth: .infinity, alignment: .leading)
            tierCheck(feature.free).frame(width: 50)
            tierCheck(feature.starter).frame(width: 60)
            tierCheck(feature.pro).frame(width: 50)
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 8)
        .background(isHighlighted ? Color.accentColor.opacity(0.08) : Color.clear)
    }

    @ViewBuilder
    private func tierCheck(_ available: Bool) -> some View {
        if available {
            Image(systemName: "checkmark.circle.fill")
                .foregroundStyle(.green)
                .font(.subheadline)
        } else {
            Image(systemName: "minus")
                .foregroundStyle(.quaternary)
                .font(.subheadline)
        }
    }

    // MARK: - Kauf-Buttons

    @ViewBuilder
    private var purchaseButtons: some View {
        VStack(spacing: 12) {
            if let starterProduct = storeKitManager.products.first(where: { $0.id == "starter_monthly" }) {
                Button {
                    Task { await purchase(starterProduct) }
                } label: {
                    HStack {
                        Text(String(localized: "paywall_subscribe_starter"))
                        Spacer()
                        Text("\(starterProduct.displayPrice)/\(String(localized: "paywall_month"))")
                            .fontWeight(.bold)
                    }
                    .font(.headline)
                    .padding(.vertical, 14)
                    .padding(.horizontal, 20)
                }
                .buttonStyle(.bordered)
                .disabled(isPurchasing)
            }

            if let proProduct = storeKitManager.products.first(where: { $0.id == "pro_monthly" }) {
                Button {
                    Task { await purchase(proProduct) }
                } label: {
                    HStack {
                        Text(String(localized: "paywall_subscribe_pro"))
                        Spacer()
                        Text("\(proProduct.displayPrice)/\(String(localized: "paywall_month"))")
                            .fontWeight(.bold)
                    }
                    .font(.headline)
                    .padding(.vertical, 14)
                    .padding(.horizontal, 20)
                }
                .buttonStyle(.borderedProminent)
                .disabled(isPurchasing)
            }

            if isPurchasing {
                ProgressView()
            }

            if let error = purchaseError {
                Text(error)
                    .font(.caption)
                    .foregroundStyle(.red)
            }
        }
    }

    // MARK: - Footer (Restore + Legal)

    @ViewBuilder
    private var footerSection: some View {
        VStack(spacing: 8) {
            Button(String(localized: "paywall_restore")) {
                Task {
                    try? await storeKitManager.restorePurchases()
                }
            }
            .font(.subheadline)

            Text(String(localized: "paywall_auto_renew_notice"))
                .font(.caption2)
                .foregroundStyle(.tertiary)
                .multilineTextAlignment(.center)

            HStack(spacing: 16) {
                Link(String(localized: "paywall_terms"),
                     destination: URL(string: "https://track.fakturus.com/terms")!)
                Link(String(localized: "paywall_privacy"),
                     destination: URL(string: "https://track.fakturus.com/privacy")!)
            }
            .font(.caption2)
        }
        .padding(.top)
    }

    // MARK: - Success Overlay

    @ViewBuilder
    private var successOverlay: some View {
        VStack(spacing: 16) {
            Image(systemName: "checkmark.circle.fill")
                .font(.system(size: 72))
                .foregroundStyle(.green)
            Text(String(localized: "paywall_success \(subscriptionManager.currentTier.displayName)"))
                .font(.title3.bold())
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .background(.ultraThinMaterial)
        .onAppear {
            HapticManager.shared.notification(.success)
            DispatchQueue.main.asyncAfter(deadline: .now() + 1.5) {
                dismiss()
            }
        }
    }

    // MARK: - Purchase

    private func purchase(_ product: Product) async {
        isPurchasing = true
        purchaseError = nil
        do {
            let transaction = try await storeKitManager.purchase(product)
            if transaction != nil {
                showSuccess = true
            }
        } catch {
            purchaseError = String(localized: "paywall_purchase_error")
        }
        isPurchasing = false
    }
}

// MARK: - Feature-Tabelle Daten

struct PaywallFeature {
    let name: String
    let gate: FeatureGate?
    let free: Bool
    let starter: Bool
    let pro: Bool
}

private let paywallFeatures: [PaywallFeature] = [
    PaywallFeature(name: "Timer & Pausen", gate: nil, free: true, starter: true, pro: true),
    PaywallFeature(name: "History (365 Tage)", gate: nil, free: true, starter: true, pro: true),
    PaywallFeature(name: "Feiertage", gate: nil, free: true, starter: true, pro: true),
    PaywallFeature(name: "History (unbegrenzt)", gate: .csvExport, free: false, starter: true, pro: true),
    PaywallFeature(name: "Widgets", gate: nil, free: true, starter: true, pro: true),
    PaywallFeature(name: "Ueberstunden-Dashboard", gate: nil, free: true, starter: true, pro: true),
    PaywallFeature(name: "PDF-Export", gate: .pdfExport, free: false, starter: true, pro: true),
    PaywallFeature(name: "CSV-Export", gate: .csvExport, free: false, starter: true, pro: true),
    PaywallFeature(name: "Urlaub & Krankheit", gate: .vacation, free: false, starter: true, pro: true),
    PaywallFeature(name: "DATEV-Export", gate: .datevExport, free: false, starter: false, pro: true),
    PaywallFeature(name: "Schulferien", gate: .schoolHolidays, free: false, starter: false, pro: true),
    PaywallFeature(name: "Kalender-Integration", gate: .calendarIntegration, free: false, starter: false, pro: true),
]
```

---

## PaywallScreen.kt (Android)

Die Android-Variante folgt demselben Aufbau, nutzt aber Material 3 Composables:

```kotlin
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PaywallBottomSheet(
    highlightedFeature: FeatureGate? = null,
    billingManager: BillingManager,
    subscriptionManager: SubscriptionManager,
    activity: Activity,
    onDismiss: () -> Unit
) {
    val products by billingManager.products.collectAsState()
    val tier by subscriptionManager.tier.collectAsState()
    var isPurchasing by remember { mutableStateOf(false) }
    var showSuccess by remember { mutableStateOf(false) }

    ModalBottomSheet(onDismissRequest = onDismiss) {
        LazyColumn(
            modifier = Modifier.padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Header
            item { PaywallHeader() }

            // Feature-Vergleichstabelle
            item {
                FeatureComparisonTable(highlightedFeature = highlightedFeature)
            }

            // Kauf-Buttons mit dynamischen Preisen
            item {
                products.forEach { productDetails ->
                    val price = billingManager.getFormattedPrice(productDetails)
                    val isStarter = productDetails.productId == "starter_monthly"

                    Button(
                        onClick = {
                            isPurchasing = true
                            billingManager.launchPurchaseFlow(activity, productDetails)
                        },
                        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
                        colors = if (isStarter) ButtonDefaults.outlinedButtonColors()
                                 else ButtonDefaults.buttonColors(),
                        enabled = !isPurchasing
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                if (isStarter) stringResource(R.string.paywall_subscribe_starter)
                                else stringResource(R.string.paywall_subscribe_pro)
                            )
                            Text("$price / ${stringResource(R.string.paywall_month)}")
                        }
                    }
                }
            }

            // Restore + Legal Footer
            item { PaywallFooter(billingManager = billingManager) }

            // Spacing am Ende
            item { Spacer(Modifier.height(32.dp)) }
        }
    }
}
```

---

## Upgrade-Erfolgs-Flow (E04-S03)

### iOS

```swift
// In PaywallView: showSuccess Overlay (siehe oben)
// + HapticManager.shared.notification(.success)
// + Automatisches Dismiss nach 1.5 Sekunden
```

### Android

```kotlin
// In PaywallScreen:
// LaunchedEffect auf subscriptionManager.tier
LaunchedEffect(tier) {
    if (tier > Tier.FREE && isPurchasing) {
        isPurchasing = false
        showSuccess = true
        // Haptic Feedback
        val vibrator = context.getSystemService(Vibrator::class.java)
        vibrator?.vibrate(VibrationEffect.createOneShot(100, VibrationEffect.DEFAULT_AMPLITUDE))
        delay(1500)
        onDismiss()
    }
}
```

### Wichtig: Sofortige UI-Aktualisierung

Nach erfolgreichem Kauf aktualisiert `SubscriptionManager.currentTier` (iOS) bzw. `SubscriptionManager.tier` (Android). Da alle Views diesen State beobachten, verschwinden Lock-Icons und Paywall-Teasers automatisch durch SwiftUI-Rerendering / Compose-Recomposition.

Kein manuelles "Alle Views aktualisieren" noetig -- das ist der Vorteil des reaktiven State-Managements.
