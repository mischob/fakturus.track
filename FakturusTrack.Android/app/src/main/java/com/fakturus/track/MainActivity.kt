package com.fakturus.track

import android.content.Context
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.fakturus.track.features.auth.LoginScreen
import com.fakturus.track.features.legal.ConsentScreen
import com.fakturus.track.features.shell.MainScreen
import com.fakturus.track.services.auth.AppStartResult
import com.fakturus.track.services.auth.LoginContext
import com.fakturus.track.ui.theme.FakturusTrackTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.withContext

val Context.settingsDataStore: DataStore<Preferences> by preferencesDataStore(name = "app_settings")

class MainActivity : ComponentActivity() {

    companion object {
        const val ACTION_START_TIMER = "com.fakturus.track.ACTION_START_TIMER"
        const val ACTION_VIEW_HISTORY = "com.fakturus.track.ACTION_VIEW_HISTORY"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)

        val app = application as FakturusTrackApp
        val authManager = app.serviceContainer.authManager
        val networkMonitor = app.serviceContainer.networkMonitor

        // Process shortcut intents
        val startTimerOnLaunch = intent?.action == ACTION_START_TIMER

        // Hintergrund-Refresh bei Netzwerkwechsel aktivieren
        app.serviceContainer.setupBackgroundRefresh()

        setContent {
            // Observe appearance setting from DataStore
            val appearance by settingsDataStore.data
                .map { prefs -> prefs[stringPreferencesKey("appearance")] ?: "system" }
                .collectAsState(initial = "system")

            FakturusTrackTheme(overrideAppearance = appearance) {
                val isAuthenticated by authManager.isAuthenticated.collectAsState()
                var isResolvingStartState by remember { mutableStateOf(true) }
                var loginContext by remember { mutableStateOf(LoginContext.NORMAL) }

                // App-Start Entscheidungslogik
                LaunchedEffect(Unit) {
                    val (result, context) = withContext(Dispatchers.IO) {
                        authManager.resolveStartState(networkMonitor)
                    }
                    loginContext = context

                    when (result) {
                        AppStartResult.AUTHENTICATED -> {
                            withContext(Dispatchers.IO) {
                                app.serviceContainer.onLogin()
                                app.serviceContainer.syncEngine?.syncAll()
                            }
                        }
                        AppStartResult.OFFLINE_WITH_SESSION -> {
                            withContext(Dispatchers.IO) {
                                app.serviceContainer.onLogin()
                            }
                        }
                        AppStartResult.LOGIN_REQUIRED,
                        AppStartResult.LOGIN_REQUIRED_NO_NETWORK -> {
                            // Zeige Login-Screen
                        }
                    }
                    isResolvingStartState = false
                }

                // Reagiere auf interaktiven Login (nach resolveStartState)
                LaunchedEffect(isAuthenticated) {
                    if (isAuthenticated && !isResolvingStartState) {
                        withContext(Dispatchers.IO) {
                            app.serviceContainer.onLogin()
                            app.serviceContainer.syncEngine?.syncAll()
                        }
                    } else if (!isAuthenticated && !isResolvingStartState) {
                        app.serviceContainer.onLogout()
                    }
                }

                if (isResolvingStartState) {
                    // Splash / Loading
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Icon(
                                imageVector = Icons.Default.Schedule,
                                contentDescription = null,
                                modifier = Modifier.size(72.dp),
                                tint = MaterialTheme.colorScheme.primary
                            )
                            Spacer(Modifier.height(16.dp))
                            CircularProgressIndicator()
                        }
                    }
                } else if (isAuthenticated) {
                    val hasConsent by app.serviceContainer.consentManager.hasRequiredConsents.collectAsState()

                    if (hasConsent) {
                        MainScreen(
                            authManager = authManager,
                            services = app.serviceContainer,
                            startTimerOnLaunch = startTimerOnLaunch
                        )
                    } else {
                        ConsentScreen(
                            onConsented = {
                                app.serviceContainer.consentManager.recordConsent(termsVersion = 1)
                            },
                            onDeclined = {
                                app.serviceContainer.authManager.logout()
                            }
                        )
                    }
                } else {
                    LoginScreen(
                        authManager = authManager,
                        networkMonitor = networkMonitor,
                        loginContext = loginContext
                    )
                }
            }
        }
    }
}
