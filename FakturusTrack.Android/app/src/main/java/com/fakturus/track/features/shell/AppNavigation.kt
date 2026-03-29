package com.fakturus.track.features.shell

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import com.fakturus.track.ServiceContainer
import com.fakturus.track.features.overview.OverviewScreen
import com.fakturus.track.features.settings.SchoolHolidaysScreen
import com.fakturus.track.features.settings.SettingsScreen
import com.fakturus.track.features.timetracking.TimeTrackingScreen
import com.fakturus.track.features.vacation.VacationScreen
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
            VacationScreen(services = services)
        }
        composable("gesamt") {
            OverviewScreen(services = services)
        }
        composable("einstellungen") {
            SettingsScreen(
                services = services,
                onLogout = { authManager.logout() },
                onNavigateToSchoolHolidays = {
                    navController.navigate("schulferien")
                }
            )
        }
        composable("schulferien") {
            SchoolHolidaysScreen(
                onNavigateBack = { navController.popBackStack() }
            )
        }
    }
}
