package com.fakturus.track.services.subscription

enum class Tier(val level: Int) : Comparable<Tier> {
    FREE(0),
    STARTER(1),
    PRO(2);

    val displayName: String
        get() = when (this) {
            FREE -> "Free"
            STARTER -> "Starter"
            PRO -> "Pro"
        }

    companion object {
        fun fromProductId(productId: String): Tier? = when (productId) {
            "starter_monthly" -> STARTER
            "pro_monthly" -> PRO
            else -> null
        }
    }
}
