package com.fakturus.track.features.subscription

import android.app.Activity
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.android.billingclient.api.ProductDetails
import com.fakturus.track.services.subscription.BillingManager
import com.fakturus.track.services.subscription.SubscriptionManager
import com.fakturus.track.services.subscription.Tier
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class PaywallViewModel(
    private val billingManager: BillingManager,
    private val subscriptionManager: SubscriptionManager
) : ViewModel() {

    val products: StateFlow<List<ProductDetails>> = billingManager.products
    val tier: StateFlow<Tier> = subscriptionManager.tier
    val isLoading: StateFlow<Boolean> = billingManager.isLoading

    private val _isPurchasing = MutableStateFlow(false)
    val isPurchasing: StateFlow<Boolean> = _isPurchasing.asStateFlow()

    private val _showSuccess = MutableStateFlow(false)
    val showSuccess: StateFlow<Boolean> = _showSuccess.asStateFlow()

    fun purchase(activity: Activity, productDetails: ProductDetails) {
        _isPurchasing.value = true
        billingManager.launchPurchaseFlow(activity, productDetails)
    }

    fun onTierChanged(newTier: Tier) {
        if (newTier > Tier.FREE && _isPurchasing.value) {
            _isPurchasing.value = false
            _showSuccess.value = true
        }
    }

    fun resetPurchasing() {
        _isPurchasing.value = false
    }

    fun restorePurchases() {
        viewModelScope.launch {
            billingManager.queryExistingPurchases()
        }
    }

    fun getFormattedPrice(productDetails: ProductDetails): String? {
        return billingManager.getFormattedPrice(productDetails)
    }
}
