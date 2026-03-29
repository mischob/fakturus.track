package com.fakturus.track

object Configuration {
    val apiBaseUrl: String
        get() = if (BuildConfig.DEBUG) "https://10.0.2.2:7001"
                else "https://api.track.fakturus.com"

    const val B2C_TENANT = "fakturus"
    const val B2C_CLIENT_ID = "3fb35bc6-8825-495e-b0a2-18e00352f968"
    const val B2C_POLICY = "B2C_1_BetaSignInOnly"
    val B2C_SCOPES = listOf(
        "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
    )
    val B2C_AUTHORITY_URL: String
        get() = "https://$B2C_TENANT.b2clogin.com/$B2C_TENANT.onmicrosoft.com/$B2C_POLICY"
}
