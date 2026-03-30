package com.fakturus.track.services.subscription

import android.content.Context
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.time.LocalDate

class SubscriptionManager(private val context: Context) {
    private val prefs = context.getSharedPreferences("subscription", Context.MODE_PRIVATE)
    private val _tier = MutableStateFlow(loadCachedTier())
    val tier: StateFlow<Tier> = _tier.asStateFlow()

    fun isAvailable(feature: FeatureGate): Boolean =
        feature.requiredTier <= _tier.value

    fun updateTier(newTier: Tier) {
        _tier.value = newTier
        prefs.edit().putInt("cached_tier", newTier.level).apply()
    }

    val historyDateLimit: LocalDate?
        get() = if (_tier.value < Tier.STARTER) {
            LocalDate.now().minusDays(30)
        } else null

    private fun loadCachedTier(): Tier {
        val level = prefs.getInt("cached_tier", 0)
        return Tier.entries.firstOrNull { it.level == level } ?: Tier.FREE
    }
}
