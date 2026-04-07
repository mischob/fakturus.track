package com.fakturus.track.models

import kotlinx.serialization.Serializable

@Serializable
data class OfflineSession(
    val userId: String,
    val lastSuccessfulAuthEpochMillis: Long
) {
    val isValid: Boolean
        get() {
            val maxOfflineDays = 14
            val daysSinceAuth = (System.currentTimeMillis() - lastSuccessfulAuthEpochMillis) / 86_400_000
            return daysSinceAuth <= maxOfflineDays
        }

    val daysUntilExpiry: Int
        get() {
            val daysSinceAuth = (System.currentTimeMillis() - lastSuccessfulAuthEpochMillis) / 86_400_000
            return maxOf(0, (14 - daysSinceAuth).toInt())
        }
}
