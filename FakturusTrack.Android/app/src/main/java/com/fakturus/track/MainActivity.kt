package com.fakturus.track

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import com.fakturus.track.features.auth.LoginScreen
import com.fakturus.track.features.shell.MainScreen
import com.fakturus.track.ui.theme.FakturusTrackTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)

        val app = application as FakturusTrackApp
        val authManager = app.serviceContainer.authManager

        setContent {
            FakturusTrackTheme {
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
                    MainScreen(authManager = authManager, services = app.serviceContainer)
                } else {
                    LoginScreen(authManager = authManager)
                }
            }
        }
    }
}
