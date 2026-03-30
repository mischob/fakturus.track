package com.fakturus.track.services.subscription

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import com.android.billingclient.api.AcknowledgePurchaseParams
import com.android.billingclient.api.BillingClient
import com.android.billingclient.api.BillingClientStateListener
import com.android.billingclient.api.BillingResult
import com.android.billingclient.api.PendingPurchasesParams
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume

/**
 * WorkManager worker for failed acknowledge calls.
 * Max 3 attempts with exponential backoff (30s -> 60s -> 120s).
 * CRITICAL: Without acknowledge, Google auto-refunds the purchase after 3 days.
 */
class AcknowledgeRetryWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        val purchaseToken = inputData.getString("purchaseToken")
            ?: return Result.failure()

        // Max 3 attempts
        if (runAttemptCount >= 3) {
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

            val acknowledgeResult = suspendCancellableCoroutine { cont ->
                client.acknowledgePurchase(params) { billingResult ->
                    cont.resume(billingResult)
                }
            }

            if (acknowledgeResult.responseCode == BillingClient.BillingResponseCode.OK) {
                Result.success()
            } else {
                Result.retry()
            }
        } finally {
            client.endConnection()
        }
    }
}
