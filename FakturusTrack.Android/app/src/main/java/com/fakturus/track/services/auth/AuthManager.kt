package com.fakturus.track.services.auth

import android.app.Activity
import android.content.Context
import android.util.Log
import com.fakturus.track.Configuration
import com.fakturus.track.R
import com.fakturus.track.models.OfflineSession
import com.fakturus.track.services.network.NetworkMonitor
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

enum class AppStartResult {
    AUTHENTICATED,
    OFFLINE_WITH_SESSION,
    LOGIN_REQUIRED,
    LOGIN_REQUIRED_NO_NETWORK
}

enum class LoginContext {
    NORMAL, SESSION_EXPIRED, FIRST_LOGIN
}

class AuthManager(private val context: Context) {
    private val _isAuthenticated = MutableStateFlow(false)
    val isAuthenticated: StateFlow<Boolean> = _isAuthenticated.asStateFlow()

    private val _isOfflineMode = MutableStateFlow(false)
    val isOfflineMode: StateFlow<Boolean> = _isOfflineMode.asStateFlow()

    private var msalApp: ISingleAccountPublicClientApplication? = null
    private var currentAccount: IAccount? = null

    val offlineSessionManager = OfflineSessionManager(context)

    init {
        PublicClientApplication.createSingleAccountPublicClientApplication(
            context,
            R.raw.auth_config,
            object : IPublicClientApplication.ISingleAccountApplicationCreatedListener {
                override fun onCreated(app: ISingleAccountPublicClientApplication) {
                    msalApp = app
                    // Don't auto-check session here anymore - resolveStartState handles it
                    Log.d("AuthManager", "MSAL initialized successfully")
                }

                override fun onError(exception: MsalException) {
                    Log.e("AuthManager", "MSAL init failed", exception)
                }
            }
        )
    }

    // MARK: - App Start Decision Logic

    suspend fun resolveStartState(networkMonitor: NetworkMonitor): Pair<AppStartResult, LoginContext> {
        val isOnline = networkMonitor.isConnected.value

        if (isOnline) {
            // Online: MSAL darf Netzwerk nutzen
            val app = msalApp
            if (app == null) {
                Log.w("AuthManager", "MSAL not initialized yet, requiring login")
                return AppStartResult.LOGIN_REQUIRED to LoginContext.NORMAL
            }

            return try {
                val account = app.currentAccount?.currentAccount
                if (account != null) {
                    currentAccount = account
                    acquireTokenSilently()
                    updateOfflineSession()
                    _isAuthenticated.value = true
                    Log.d("AuthManager", "Online: restored session via silent refresh")
                    AppStartResult.AUTHENTICATED to LoginContext.NORMAL
                } else {
                    AppStartResult.LOGIN_REQUIRED to LoginContext.NORMAL
                }
            } catch (e: Exception) {
                Log.d("AuthManager", "Online: silent refresh failed, requiring login: ${e.message}")
                AppStartResult.LOGIN_REQUIRED to LoginContext.NORMAL
            }
        }

        // Offline: KEIN MSAL-Netzwerkaufruf!
        // Offline-Session pruefen
        val session = offlineSessionManager.load()
        if (session != null) {
            return if (session.isValid) {
                _isAuthenticated.value = true
                _isOfflineMode.value = true
                Log.d("AuthManager", "Offline: valid session (userId: ${session.userId}, ${session.daysUntilExpiry} days left)")
                AppStartResult.OFFLINE_WITH_SESSION to LoginContext.NORMAL
            } else {
                Log.d("AuthManager", "Offline: session expired")
                AppStartResult.LOGIN_REQUIRED_NO_NETWORK to LoginContext.SESSION_EXPIRED
            }
        }

        // Noch nie eingeloggt
        Log.d("AuthManager", "Offline: no session found (first login)")
        return AppStartResult.LOGIN_REQUIRED_NO_NETWORK to LoginContext.FIRST_LOGIN
    }

    // MARK: - Background Token Refresh (offline -> online)

    suspend fun attemptBackgroundTokenRefresh() {
        if (!_isOfflineMode.value) return

        try {
            acquireTokenSilently()
            _isOfflineMode.value = false
            updateOfflineSession()
            Log.d("AuthManager", "Background refresh successful, exiting offline mode")
        } catch (e: Exception) {
            Log.d("AuthManager", "Background refresh failed: ${e.message}")
        }
    }

    // MARK: - Interactive Login

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
                    _isOfflineMode.value = false
                    updateOfflineSession()
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

    // MARK: - Silent Token Refresh

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
            updateOfflineSession()
            result.accessToken
        } catch (e: Exception) {
            throw AuthException.TokenExpired
        }
    }

    // MARK: - Logout

    fun logout() {
        msalApp?.signOut(object : ISingleAccountPublicClientApplication.SignOutCallback {
            override fun onSignOut() {
                _isAuthenticated.value = false
                _isOfflineMode.value = false
            }

            override fun onError(exception: MsalException) {
                _isAuthenticated.value = false
                _isOfflineMode.value = false
            }
        })
        currentAccount = null
        offlineSessionManager.delete()
        Log.d("AuthManager", "Logged out, offline session deleted")
    }

    // MARK: - Offline Session Helpers

    private fun updateOfflineSession() {
        val account = currentAccount ?: return
        val session = OfflineSession(
            userId = account.id ?: "",
            lastSuccessfulAuthEpochMillis = System.currentTimeMillis()
        )
        offlineSessionManager.save(session)
    }
}
