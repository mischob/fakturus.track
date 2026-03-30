package com.fakturus.track.features.vacation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fakturus.track.R
import com.fakturus.track.ServiceContainer
import com.fakturus.track.ui.shared.VacationCalendar

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun VacationScreen(services: ServiceContainer) {
    val viewModel: VacationViewModel = viewModel(
        factory = VacationViewModelFactory(services)
    )
    val state by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            TopAppBar(title = { Text(stringResource(R.string.vacation_tab_title)) })
        }
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 16.dp)
        ) {
            // Resturlaub Card
            ElevatedCard(
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "Resturlaub",
                            style = MaterialTheme.typography.titleSmall
                        )
                        Text(
                            text = "${state.vacationDaysRemaining} von ${state.vacationDaysPerYear} Tagen",
                            style = MaterialTheme.typography.bodyMedium,
                            color = if (state.vacationDaysRemaining <= 0)
                                MaterialTheme.colorScheme.error
                            else MaterialTheme.colorScheme.onSurface
                        )
                    }
                    Spacer(Modifier.height(8.dp))
                    val progress = if (state.vacationDaysPerYear > 0) {
                        state.vacationDaysTaken.toFloat() / state.vacationDaysPerYear.toFloat()
                    } else 0f
                    LinearProgressIndicator(
                        progress = { progress.coerceIn(0f, 1f) },
                        modifier = Modifier.fillMaxWidth(),
                        color = if (state.vacationDaysRemaining <= 0)
                            MaterialTheme.colorScheme.error
                        else MaterialTheme.colorScheme.primary
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = "${state.vacationDaysTaken} Tage genommen",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            Spacer(Modifier.height(16.dp))

            // Calendar
            VacationCalendar(
                year = state.displayedYear,
                month = state.displayedMonth,
                vacationDates = state.vacationDates,
                sickDayDates = state.sickDayDates,
                holidays = state.holidays,
                workDays = state.workDays,
                onDayTap = { date -> viewModel.toggleVacationDay(date) },
                onSickDayTap = { date -> viewModel.toggleSickDay(date) },
                onSwitchAbsenceType = { date -> viewModel.switchAbsenceType(date) },
                onRemoveAbsence = { date -> viewModel.removeAbsence(date) },
                onMonthChange = { year, month -> viewModel.changeMonth(year, month) }
            )

            // Warning when no vacation days remaining
            if (state.vacationDaysRemaining <= 0) {
                Spacer(Modifier.height(8.dp))
                Text(
                    text = "Alle Urlaubstage aufgebraucht",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.error
                )
            }
        }
    }
}
