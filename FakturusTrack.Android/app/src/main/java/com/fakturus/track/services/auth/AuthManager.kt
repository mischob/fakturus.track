package com.fakturus.track.services.auth

import android.app.Activity
import android.content.Context
import android.util.Log
import com.fakturus.track.Configuration
import com.fakturus.track.R
import com.microsoft.identity.client.AcquireTokenParameters
import com.microsoft.identity.client.AcquireTokenSilentParameters
import com.microsoft.identity.client.IAccount
import com.microsoft.identity.client.IAuthenticationResult
import com.microsoft.identity.client.IPublicClientApplication
import com.microsoft.identity.client.ISingleAccountPublicClientApplication
import com.microsoft.identity.client.PublicClientApplication
import com.microsoft.identity.client.exception.MsalException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

enum class LoginProvider { APPLE, GOOGLE, MICROSOFT, AMAZON, EMAIL }

sealed class AuthException : Exception() {
    object NotAuthenticated : AuthException()
    object TokenExpired : AuthException()
    object Cancelled : AuthException()
    data class Failed(override val message: String) : AuthException()
}

class AuthManager(private val context: Context) {
    private val _isAuthenticated = MutableStateFlow(false)
    val isAuthenticated: StateFlow<Boolean> = _isAuthenticated.asStateFlow()

    private var msalApp: ISingleAccountPublicClientApplication? = null
    private var currentAccount: IAccount? = null

    init {
        PublicClientApplication.createSingleAccountPublicClientApplication(
            context,
            R.raw.auth_config,
            object : IPublicClientApplication.ISingleAccountApplicationCreatedListener {
                override fun onCreated(app: ISingleAccountPublicClientApplication) {
                    msalApp = app
                    checkExistingSession()
                }

                override fun onError(exception: MsalException) {
                    Log.e("AuthManager", "MSAL init failed", exception)
                }
            }
        )
    }

    private fun checkExistingSession() {
        msalApp?.getCurrentAccountAsync(object :
            ISingleAccountPublicClientApplication.CurrentAccountCallback {
            override fun onAccountLoaded(account: IAccount?) {
                if (account != null) {
                    currentAccount = account
                    CoroutineScope(Dispatchers.IO).launch {
                        try {
                            acquireTokenSilently()
                            _isAuthenticated.value = true
                        } catch (_: Exception) {
                            // No valid token, show login screen
                        }
                    }
                }
            }

            override fun onAccountChanged(prev: IAccount?, current: IAccount?) {}
            override fun onError(exception: MsalException) {}
        })
    }

    suspend fun acquireTokenInteractively(
        activity: Activity,
        provider: LoginProvider
    ) = suspendCancellableCoroutine { continuation ->
        val app = msalApp
        if (app == null) {
            continuation.resumeWithException(AuthException.Failed("MSAL not initialized"))
            return@suspendCancellableCoroutine
        }

        val params = AcquireTokenParameters.Builder()
            .startAuthorizationFromActivity(activity)
            .withScopes(Configuration.B2C_SCOPES)
            .apply {
                val hint = when (provider) {
                    LoginProvider.APPLE -> "apple.com"
                    LoginProvider.GOOGLE -> "google.com"
                    LoginProvider.MICROSOFT -> "live.com"
                    LoginProvider.AMAZON -> "amazon.com"
                    LoginProvider.EMAIL -> null
                }
                if (hint != null) {
                    withAuthorizationQueryStringParameters(
                        listOf(java.util.AbstractMap.SimpleEntry("domain_hint", hint))
                    )
                }
            }
            .withCallback(object : com.microsoft.identity.client.AuthenticationCallback {
                override fun onSuccess(result: IAuthenticationResult) {
                    currentAccount = result.account
                    _isAuthenticated.value = true
                    continuation.resume(Unit)
                }

                override fun onError(exception: MsalException) {
                    continuation.resumeWithException(
                        AuthException.Failed(exception.message ?: "Login failed")
                    )
                }

                override fun onCancel() {
                    continuation.resumeWithException(AuthException.Cancelled)
                }
            })
            .build()

        app.acquireToken(params)
    }

    suspend fun acquireTokenSilently(): String {
        val app = msalApp ?: throw AuthException.NotAuthenticated
        val account = currentAccount ?: throw AuthException.NotAuthenticated
        val params = AcquireTokenSilentParameters.Builder()
            .forAccount(account)
            .fromAuthority(Configuration.B2C_AUTHORITY_URL)
            .withScopes(Configuration.B2C_SCOPES)
            .build()
        return try {
            val result = app.acquireTokenSilent(params)
            result.accessToken
        } catch (e: Exception) {
            throw AuthException.TokenExpired
        }
    }

    fun logout() {
        msalApp?.signOut(object : ISingleAccountPublicClientApplication.SignOutCallback {
            override fun onSignOut() {
                _isAuthenticated.value = false
            }

            override fun onError(exception: MsalException) {
                _isAuthenticated.value = false
            }
        })
        currentAccount = null
    }
}
