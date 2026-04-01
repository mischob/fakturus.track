package com.fakturus.track

import android.content.Context
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.fakturus.track.features.auth.LoginScreen
import com.fakturus.track.features.legal.ConsentScreen
import com.fakturus.track.features.shell.MainScreen
import com.fakturus.track.ui.theme.FakturusTrackTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.first
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

        // Process shortcut intents
        val startTimerOnLaunch = intent?.action == ACTION_START_TIMER

        setContent {
            // Observe appearance setting from DataStore
            val appearance by settingsDataStore.data
                .map { prefs -> prefs[stringPreferencesKey("appearance")] ?: "system" }
                .collectAsState(initial = "system")

            FakturusTrackTheme(overrideAppearance = appearance) {
                val isAuthenticated by authManager.isAuthenticated.collectAsState()

                LaunchedEffect(isAuthenticated) {
                    if (isAuthenticated) {
                        withContext(Dispatchers.IO) {
                            app.serviceContainer.onLogin()
                        }
                    } else {
                        app.serviceContainer.onLogout()
                    }
                }

                if (isAuthenticated) {
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
                    LoginScreen(authManager = authManager)
                }
            }
        }
    }
}
