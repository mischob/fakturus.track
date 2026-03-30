package com.fakturus.track.features.subscription

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.fakturus.track.services.subscription.BillingManager
import com.fakturus.track.services.subscription.SubscriptionManager

class PaywallViewModelFactory(
    private val billingManager: BillingManager,
    private val subscriptionManager: SubscriptionManager
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(PaywallViewModel::class.java)) {
            return PaywallViewModel(
                billingManager = billingManager,
                subscriptionManager = subscriptionManager
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
