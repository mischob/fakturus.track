# Tech-Spec: EPIC 03 -- In-App Purchase Android (Google Play Billing)

## Uebersicht

Eine einzige Datei `BillingManager.kt` kapselt die Google Play Billing Library v7. Sie verbindet sich mit BillingClient, laedt Produkte, verarbeitet Kaeufe und fuettert den SubscriptionManager.

---

## BillingManager.kt

```kotlin
import android.app.Activity
import android.content.Context
import com.android.billingclient.api.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class BillingManager(
    private val context: Context,
    private val subscriptionManager: SubscriptionManager
) : PurchasesUpdatedListener {

    private var billingClient: BillingClient? = null
    private val scope = CoroutineScope(Dispatchers.Main + SupervisorJob())

    // Geladene Produkte
    private val _products = MutableStateFlow<List<ProductDetails>>(emptyList())
    val products: StateFlow<List<ProductDetails>> = _products.asStateFlow()

    // Lade-Status
    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    companion object {
        val PRODUCT_IDS = listOf("starter_monthly", "pro_monthly")
    }

    // MARK: - Connection

    fun startConnection() {
        billingClient = BillingClient.newBuilder(context)
            .setListener(this)
            .enablePendingPurchases(
                PendingPurchasesParams.newBuilder()
                    .enableOneTimeProducts()
                    .build()
            )
            .build()

        billingClient?.startConnection(object : BillingClientStateListener {
            override fun onBillingSetupFinished(result: BillingResult) {
                if (result.responseCode == BillingClient.BillingResponseCode.OK) {
                    scope.launch {
                        queryProducts()
                        queryExistingPurchases()
                    }
                }
            }

            override fun onBillingServiceDisconnected() {
                // Retry nach kurzer Pause
                scope.launch {
                    delay(3000)
                    startConnection()
                }
            }
        })
    }

    fun endConnection() {
        billingClient?.endConnection()
        billingClient = null
        scope.cancel()
    }

    // MARK: - Produkte laden

    private suspend fun queryProducts() {
        val client = billingClient ?: return

        val params = QueryProductDetailsParams.newBuilder()
            .setProductList(
                PRODUCT_IDS.map { productId ->
                    QueryProductDetailsParams.Product.newBuilder()
                        .setProductId(productId)
                        .setProductType(BillingClient.ProductType.SUBS)
                        .build()
                }
            )
            .build()

        val result = client.queryProductDetails(params)
        if (result.billingResult.responseCode == BillingClient.BillingResponseCode.OK) {
            _products.value = result.productDetailsList
                ?.sortedBy { it.subscriptionOfferDetails?.firstOrNull()?.pricingPhases
                    ?.pricingPhaseList?.firstOrNull()?.priceAmountMicros ?: 0 }
                ?: emptyList()
        }
    }

    // MARK: - Kauf starten

    fun launchPurchaseFlow(activity: Activity, productDetails: ProductDetails) {
        val client = billingClient ?: return

        val offerToken = productDetails.subscriptionOfferDetails
            ?.firstOrNull()?.offerToken ?: return

        val params = BillingFlowParams.newBuilder()
            .setProductDetailsParamsList(
                listOf(
                    BillingFlowParams.ProductDetailsParams.newBuilder()
                        .setProductDetails(productDetails)
                        .setOfferToken(offerToken)
                        .build()
                )
            )
            .build()

        client.launchBillingFlow(activity, params)
    }

    // MARK: - PurchasesUpdatedListener

    override fun onPurchasesUpdated(
        billingResult: BillingResult,
        purchases: MutableList<Purchase>?
    ) {
        when (billingResult.responseCode) {
            BillingClient.BillingResponseCode.OK -> {
                purchases?.forEach { purchase ->
                    scope.launch { handlePurchase(purchase) }
                }
            }
            BillingClient.BillingResponseCode.USER_CANCELED -> {
                // Kein Fehler anzeigen
            }
            BillingClient.BillingResponseCode.ITEM_ALREADY_OWNED -> {
                // Tier wiederherstellen
                scope.launch { queryExistingPurchases() }
            }
            else -> {
                // Fehler loggen
            }
        }
    }

    // MARK: - Kauf verarbeiten

    private suspend fun handlePurchase(purchase: Purchase) {
        if (purchase.purchaseState != Purchase.PurchaseState.PURCHASED) return

        // Tier aktualisieren
        val productId = purchase.products.firstOrNull() ?: return
        val tier = Tier.fromProductId(productId) ?: return
        subscriptionManager.updateTier(tier)

        // PFLICHT: Kauf bestaetigen (sonst Erstattung nach 3 Tagen!)
        if (!purchase.isAcknowledged) {
            acknowledgePurchase(purchase)
        }
    }

    private suspend fun acknowledgePurchase(purchase: Purchase) {
        val client = billingClient ?: return

        val params = AcknowledgePurchaseParams.newBuilder()
            .setPurchaseToken(purchase.purchaseToken)
            .build()

        val result = client.acknowledgePurchase(params)
        if (result.responseCode != BillingClient.BillingResponseCode.OK) {
            // PFLICHT-Retry: Kauf darf NICHT unbestaetigt bleiben!
            scheduleAcknowledgeRetry(purchase.purchaseToken)
        }
    }

    /**
     * Plant einen WorkManager-Retry fuer fehlgeschlagene Acknowledge-Aufrufe.
     * Exponential Backoff: 30s -> 60s -> 120s (max 3 Versuche).
     * Ohne Acknowledge erstattet Google den Kauf nach 3 Tagen automatisch.
     */
    private fun scheduleAcknowledgeRetry(purchaseToken: String) {
        val data = workDataOf("purchaseToken" to purchaseToken)

        val request = OneTimeWorkRequestBuilder<AcknowledgeRetryWorker>()
            .setInputData(data)
            .setBackoffCriteria(
                BackoffPolicy.EXPONENTIAL,
                30, TimeUnit.SECONDS
            )
            .setConstraints(
                Constraints.Builder()
                    .setRequiredNetworkType(NetworkType.CONNECTED)
                    .build()
            )
            .build()

        WorkManager.getInstance(context)
            .enqueueUniqueWork(
                "acknowledge_$purchaseToken",
                ExistingWorkPolicy.KEEP,
                request
            )
    }

    // MARK: - Bestehende Abos pruefen (App-Start + Restore)

    suspend fun queryExistingPurchases() {
        val client = billingClient ?: return
        _isLoading.value = true

        val params = QueryPurchasesParams.newBuilder()
            .setProductType(BillingClient.ProductType.SUBS)
            .build()

        val result = client.queryPurchasesAsync(params)

        var highestTier = Tier.FREE
        if (result.billingResult.responseCode == BillingClient.BillingResponseCode.OK) {
            for (purchase in result.purchasesList) {
                if (purchase.purchaseState == Purchase.PurchaseState.PURCHASED) {
                    val productId = purchase.products.firstOrNull() ?: continue
                    val tier = Tier.fromProductId(productId) ?: continue
                    if (tier > highestTier) highestTier = tier

                    // Sicherheitshalber acknowledge pruefen
                    if (!purchase.isAcknowledged) {
                        acknowledgePurchase(purchase)
                    }
                }
            }
        }

        subscriptionManager.updateTier(highestTier)
        _isLoading.value = false
    }

    // MARK: - Preis-Formatierung

    fun getFormattedPrice(productDetails: ProductDetails): String? {
        return productDetails.subscriptionOfferDetails
            ?.firstOrNull()
            ?.pricingPhases
            ?.pricingPhaseList
            ?.firstOrNull()
            ?.formattedPrice
    }
}
```

---

## AcknowledgeRetryWorker.kt

```kotlin
import android.content.Context
import androidx.work.*
import com.android.billingclient.api.*
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume

/**
 * WorkManager-Worker fuer fehlgeschlagene Acknowledge-Aufrufe.
 * Max 3 Versuche mit exponential Backoff (30s -> 60s -> 120s).
 * KRITISCH: Ohne Acknowledge erstattet Google den Kauf nach 3 Tagen.
 */
class AcknowledgeRetryWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        val purchaseToken = inputData.getString("purchaseToken")
            ?: return Result.failure()

        // Max 3 Versuche
        if (runAttemptCount >= 3) {
            // Logging/Sentry: Acknowledge endgueltig fehlgeschlagen
            return Result.failure()
        }

        val client = BillingClient.newBuilder(applicationContext)
            .setListener { _, _ -> }
            .enablePendingPurchases(
                PendingPurchasesParams.newBuilder()
                    .enableOneTimeProducts()
                    .build()
            )
            .build()

        return try {
            val connected = suspendCancellableCoroutine { cont ->
                client.startConnection(object : BillingClientStateListener {
                    override fun onBillingSetupFinished(result: BillingResult) {
                        cont.resume(result.responseCode == BillingClient.BillingResponseCode.OK)
                    }
                    override fun onBillingServiceDisconnected() {
                        if (cont.isActive) cont.resume(false)
                    }
                })
            }

            if (!connected) return Result.retry()

            val params = AcknowledgePurchaseParams.newBuilder()
                .setPurchaseToken(purchaseToken)
                .build()

            val result = client.acknowledgePurchase(params)
            if (result.responseCode == BillingClient.BillingResponseCode.OK) {
                Result.success()
            } else {
                Result.retry()
            }
        } finally {
            client.endConnection()
        }
    }
}
```

---

## Integration in ServiceContainer.kt

```kotlin
class ServiceContainer(private val context: Context) {
    // ... bestehende Properties ...

    // NEU Phase 4:
    val subscriptionManager = SubscriptionManager(context)
    val billingManager = BillingManager(context, subscriptionManager)

    init {
        // Play Billing bei App-Start initialisieren (NICHT erst bei Login!)
        // Grund: Abo-Status und Produkte muessen sofort verfuegbar sein,
        // auch bevor der User eingeloggt ist (z.B. Paywall in Onboarding).
        billingManager.startConnection()
    }

    fun onLogout() {
        // ... bestehender Code ...
        // Hinweis: BillingManager wird NICHT bei Logout getrennt,
        // da Abo-Status Login-unabhaengig ist.
    }
}
```

---

## Integration in MainActivity.kt

```kotlin
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val services = (application as FakturusTrackApp).serviceContainer

        // BillingClient muss Activity-Referenz fuer Purchase-Flow haben
        // wird ueber launchPurchaseFlow(activity, productDetails) uebergeben
    }

    override fun onDestroy() {
        super.onDestroy()
        // BillingClient wird ueber ServiceContainer.onLogout() getrennt
    }
}
```

---

## Restore Purchases in SettingsViewModel.kt

```kotlin
class SettingsViewModel(...) : ViewModel() {
    // ... bestehende Properties ...

    private val _isRestoringPurchases = MutableStateFlow(false)
    val isRestoringPurchases: StateFlow<Boolean> = _isRestoringPurchases.asStateFlow()

    private val _restoreResult = MutableStateFlow<String?>(null)
    val restoreResult: StateFlow<String?> = _restoreResult.asStateFlow()

    fun restorePurchases() {
        viewModelScope.launch {
            _isRestoringPurchases.value = true
            try {
                billingManager.queryExistingPurchases()
                val tier = subscriptionManager.tier.value
                _restoreResult.value = if (tier > Tier.FREE) {
                    context.getString(R.string.restore_success, tier.name)
                } else {
                    context.getString(R.string.restore_no_subscription)
                }
            } catch (e: Exception) {
                _restoreResult.value = context.getString(R.string.restore_error)
            } finally {
                _isRestoringPurchases.value = false
            }
        }
    }
}
```

---

## Gradle Dependency

```toml
# gradle/libs.versions.toml
[versions]
billing = "7.0.0"

[libraries]
billing-ktx = { group = "com.android.billingclient", name = "billing-ktx", version.ref = "billing" }
```

```kotlin
// app/build.gradle.kts
dependencies {
    implementation(libs.billing.ktx)
}
```

---

## ProGuard/R8 Rules

```proguard
# Google Play Billing
-keep class com.android.vending.billing.** { *; }
-keep class com.android.billingclient.** { *; }
```

---

## Google Play Console Produkt-Konfiguration

Manuell in Play Console:

1. **Monetization -> Subscriptions**
2. **starter_monthly**:
   - Base Plan: Monatlich, EUR 2.99
   - Titel: "Fakturus Track Starter"
3. **pro_monthly**:
   - Base Plan: Monatlich, EUR 4.99
   - Titel: "Fakturus Track Pro"
4. **Upgrade/Downgrade**:
   - Starter -> Pro: `CHARGE_PRORATED_PRICE`
   - Pro -> Starter: `DEFERRED`
5. **License Testing**: Entwickler-Google-Accounts als Tester

---

## Kritische Punkte

1. **`purchase.acknowledge()` ist PFLICHT** -- innerhalb von 3 Tagen, sonst automatische Erstattung durch Google
2. **BillingClient Reconnection** -- bei `SERVICE_DISCONNECTED` muss erneut verbunden werden
3. **`enablePendingPurchases()`** -- Pflicht seit Billing Library v5, sonst Exception
4. **Activity-Referenz** -- `launchBillingFlow` braucht die aktuelle Activity (nicht Application Context)
5. **Kein Caching der ProductDetails** -- immer frisch laden, Preise koennen sich aendern
