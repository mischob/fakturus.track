package com.fakturus.track.services.subscription

import android.app.Activity
import android.content.Context
import androidx.work.BackoffPolicy
import androidx.work.Constraints
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.workDataOf
import com.android.billingclient.api.AcknowledgePurchaseParams
import com.android.billingclient.api.BillingClient
import com.android.billingclient.api.BillingClientStateListener
import com.android.billingclient.api.BillingFlowParams
import com.android.billingclient.api.BillingResult
import com.android.billingclient.api.PendingPurchasesParams
import com.android.billingclient.api.ProductDetails
import com.android.billingclient.api.ProductDetailsResponseListener
import com.android.billingclient.api.Purchase
import com.android.billingclient.api.PurchasesResponseListener
import com.android.billingclient.api.PurchasesUpdatedListener
import com.android.billingclient.api.QueryProductDetailsParams
import com.android.billingclient.api.QueryPurchasesParams
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import java.util.concurrent.TimeUnit
import kotlin.coroutines.resume

class BillingManager(
    private val context: Context,
    private val subscriptionManager: SubscriptionManager
) : PurchasesUpdatedListener {

    private var billingClient: BillingClient? = null
    private val scope = CoroutineScope(Dispatchers.Main + SupervisorJob())

    private val _products = MutableStateFlow<List<ProductDetails>>(emptyList())
    val products: StateFlow<List<ProductDetails>> = _products.asStateFlow()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    companion object {
        val PRODUCT_IDS = listOf("starter_monthly", "pro_monthly")
    }

    // -- Connection --

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

    // -- Query products --

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

        val (billingResult, productDetailsList) = suspendCancellableCoroutine { cont ->
            client.queryProductDetailsAsync(params) { result, details ->
                cont.resume(Pair(result, details))
            }
        }

        if (billingResult.responseCode == BillingClient.BillingResponseCode.OK) {
            _products.value = productDetailsList
                ?.sortedBy {
                    it.subscriptionOfferDetails?.firstOrNull()?.pricingPhases
                        ?.pricingPhaseList?.firstOrNull()?.priceAmountMicros ?: 0
                }
                ?: emptyList()
        }
    }

    // -- Launch purchase --

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

    // -- PurchasesUpdatedListener --

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
                // No error to show
            }
            BillingClient.BillingResponseCode.ITEM_ALREADY_OWNED -> {
                scope.launch { queryExistingPurchases() }
            }
            else -> {
                // Log error
            }
        }
    }

    // -- Handle purchase --

    private suspend fun handlePurchase(purchase: Purchase) {
        if (purchase.purchaseState != Purchase.PurchaseState.PURCHASED) return

        val productId = purchase.products.firstOrNull() ?: return
        val tier = Tier.fromProductId(productId) ?: return
        subscriptionManager.updateTier(tier)

        // REQUIRED: Acknowledge purchase (auto-refund after 3 days if not acknowledged!)
        if (!purchase.isAcknowledged) {
            acknowledgePurchase(purchase)
        }
    }

    private suspend fun acknowledgePurchase(purchase: Purchase) {
        val client = billingClient ?: return

        val params = AcknowledgePurchaseParams.newBuilder()
            .setPurchaseToken(purchase.purchaseToken)
            .build()

        val result = suspendCancellableCoroutine { cont ->
            client.acknowledgePurchase(params) { billingResult ->
                cont.resume(billingResult)
            }
        }

        if (result.responseCode != BillingClient.BillingResponseCode.OK) {
            // REQUIRED retry: Purchase must not remain unacknowledged!
            scheduleAcknowledgeRetry(purchase.purchaseToken)
        }
    }

    /**
     * Schedules a WorkManager retry for failed acknowledge calls.
     * Exponential backoff: 30s -> 60s -> 120s (max 3 attempts).
     * Without acknowledge, Google auto-refunds the purchase after 3 days.
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

    // -- Query existing purchases (app start + restore) --

    suspend fun queryExistingPurchases() {
        val client = billingClient ?: return
        _isLoading.value = true

        val params = QueryPurchasesParams.newBuilder()
            .setProductType(BillingClient.ProductType.SUBS)
            .build()

        val (billingResult, purchasesList) = suspendCancellableCoroutine { cont ->
            client.queryPurchasesAsync(params) { result, purchases ->
                cont.resume(Pair(result, purchases))
            }
        }

        var highestTier = Tier.FREE
        if (billingResult.responseCode == BillingClient.BillingResponseCode.OK) {
            for (purchase in purchasesList) {
                if (purchase.purchaseState == Purchase.PurchaseState.PURCHASED) {
                    val productId = purchase.products.firstOrNull() ?: continue
                    val tier = Tier.fromProductId(productId) ?: continue
                    if (tier > highestTier) highestTier = tier

                    if (!purchase.isAcknowledged) {
                        acknowledgePurchase(purchase)
                    }
                }
            }
        }

        subscriptionManager.updateTier(highestTier)
        _isLoading.value = false
    }

    // -- Price formatting --

    fun getFormattedPrice(productDetails: ProductDetails): String? {
        return productDetails.subscriptionOfferDetails
            ?.firstOrNull()
            ?.pricingPhases
            ?.pricingPhaseList
            ?.firstOrNull()
            ?.formattedPrice
    }
}
