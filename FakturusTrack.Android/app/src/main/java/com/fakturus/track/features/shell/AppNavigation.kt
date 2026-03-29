package com.fakturus.track.features.shell

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.BarChart
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.WbSunny
import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import com.fakturus.track.ServiceContainer
import com.fakturus.track.features.timetracking.TimeTrackingScreen
import com.fakturus.track.services.auth.AuthManager

@Composable
fun AppNavigation(
    navController: NavHostController,
    authManager: AuthManager,
    services: ServiceContainer
) {
    NavHost(navController = navController, startDestination = "zeiten") {
        composable("zeiten") {
            TimeTrackingScreen(services = services)
        }
        composable("urlaub") {
            PlaceholderScreen(
                title = "Urlaub",
                icon = Icons.Default.WbSunny
            )
        }
        composable("gesamt") {
            PlaceholderScreen(
                title = "Gesamt",
                icon = Icons.Default.BarChart
            )
        }
        composable("einstellungen") {
            PlaceholderScreen(
                title = "Einstellungen",
                icon = Icons.Default.Settings,
                showLogout = true,
                onLogout = { authManager.logout() }
            )
        }
    }
}
