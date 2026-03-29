package com.fakturus.track.features.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fakturus.track.ServiceContainer
import com.fakturus.track.ui.shared.BundeslandPicker
import com.fakturus.track.ui.shared.WorkdaySelector

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    services: ServiceContainer,
    onLogout: () -> Unit,
    onNavigateToSchoolHolidays: () -> Unit = {}
) {
    val viewModel: SettingsViewModel = viewModel(
        factory = SettingsViewModelFactory(services)
    )
    val settings by viewModel.settings.collectAsState()
    val isSaving by viewModel.isSaving.collectAsState()

    LaunchedEffect(Unit) {
        viewModel.initializeSettingsIfNeeded()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Einstellungen") }
            )
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Section: Arbeitszeit
            item {
                Spacer(Modifier.height(8.dp))
                Text(
                    text = "Arbeitszeit",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                var hoursText by remember(settings?.workHoursPerWeek) {
                    mutableStateOf(settings?.workHoursPerWeek?.toString() ?: "40.0")
                }
                OutlinedTextField(
                    value = hoursText,
                    onValueChange = { newValue ->
                        hoursText = newValue
                        newValue.toDoubleOrNull()?.let { viewModel.updateWorkHoursPerWeek(it) }
                    },
                    label = { Text("Wochenstunden") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            item {
                Text(
                    text = "Arbeitstage",
                    style = MaterialTheme.typography.bodyMedium
                )
                Spacer(Modifier.height(4.dp))
                WorkdaySelector(
                    workDays = settings?.workDays ?: 31,
                    onWorkDaysChange = { viewModel.updateWorkDays(it) }
                )
            }

            item {
                HorizontalDivider()
            }

            // Section: Urlaub
            item {
                Text(
                    text = "Urlaub",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                var daysText by remember(settings?.vacationDaysPerYear) {
                    mutableStateOf(settings?.vacationDaysPerYear?.toString() ?: "30")
                }
                OutlinedTextField(
                    value = daysText,
                    onValueChange = { newValue ->
                        daysText = newValue
                        newValue.toIntOrNull()?.let { viewModel.updateVacationDaysPerYear(it) }
                    },
                    label = { Text("Urlaubstage pro Jahr") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            item {
                HorizontalDivider()
            }

            // Section: Region
            item {
                Text(
                    text = "Region",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                BundeslandPicker(
                    selectedBundesland = settings?.bundesland ?: "NW",
                    onBundeslandChange = { viewModel.updateBundesland(it) }
                )
            }

            item {
                HorizontalDivider()
            }

            // Section: Schulferien
            item {
                Text(
                    text = "Schulferien",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                Button(
                    onClick = onNavigateToSchoolHolidays,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Schulferien verwalten")
                }
            }

            item {
                HorizontalDivider()
            }

            // Section: Kalender
            item {
                Text(
                    text = "Kalender",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                var urlText by remember(settings?.calendarUrl) {
                    mutableStateOf(settings?.calendarUrl ?: "")
                }
                OutlinedTextField(
                    value = urlText,
                    onValueChange = { newValue ->
                        urlText = newValue
                        viewModel.updateCalendarUrl(newValue.ifBlank { null })
                    },
                    label = { Text("Kalender-URL (ICS)") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            item {
                HorizontalDivider()
            }

            // Logout
            item {
                Button(
                    onClick = onLogout,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    ),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = null)
                        Text("Abmelden")
                    }
                }
                Spacer(Modifier.height(32.dp))
            }
        }
    }
}
