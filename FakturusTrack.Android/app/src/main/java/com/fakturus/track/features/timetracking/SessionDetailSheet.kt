package com.fakturus.track.features.timetracking

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TimePicker
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.material3.rememberTimePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.util.DateFormatting
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.LocalTime
import java.time.ZoneId
import java.time.ZonedDateTime

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SessionDetailSheet(
    session: WorkSessionEntity,
    onDismiss: () -> Unit,
    onSave: (date: String, startTime: String, stopTime: String?, pauseMinutes: Int) -> Unit,
    onDelete: () -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)
    val zone = ZoneId.systemDefault()

    val sessionDate = remember(session.id) { LocalDate.parse(session.date) }
    val sessionStart = remember(session.id) { Instant.parse(session.startTime) }
    val hasStopTime = remember(session.id) { session.stopTime != null }
    val sessionStop = remember(session.id) {
        session.stopTime?.let { Instant.parse(it) }
            ?: sessionStart.plusSeconds(3600)
    }

    var editDate by remember(session.id) { mutableStateOf(sessionDate) }
    var editStartTime by remember(session.id) {
        mutableStateOf(sessionStart.atZone(zone).toLocalTime())
    }
    var editStopTime by remember(session.id) {
        mutableStateOf(sessionStop.atZone(zone).toLocalTime())
    }
    var editPauseMinutes by remember(session.id) { mutableIntStateOf(session.pauseMinutes) }

    var showDatePicker by remember { mutableStateOf(false) }
    var showStartTimePicker by remember { mutableStateOf(false) }
    var showStopTimePicker by remember { mutableStateOf(false) }
    var showDeleteConfirmation by remember { mutableStateOf(false) }

    // Compute brutto/netto
    val startZoned = ZonedDateTime.of(editDate, editStartTime, zone)
    val stopZoned = ZonedDateTime.of(editDate, editStopTime, zone)
    val bruttoMinutes = maxOf(0, Duration.between(startZoned, stopZoned).toMinutes())
    val nettoMinutes = maxOf(0, bruttoMinutes - editPauseMinutes)
    val isValid = if (hasStopTime) {
        editStopTime.isAfter(editStartTime) && bruttoMinutes <= 1440
    } else true

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Text(
                text = "Session bearbeiten",
                style = MaterialTheme.typography.titleLarge
            )

            HorizontalDivider()

            // Date
            OutlinedButton(
                onClick = { showDatePicker = true },
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Datum: ${DateFormatting.formatDate(editDate)}")
            }

            // Start time
            OutlinedButton(
                onClick = { showStartTimePicker = true },
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Start: %02d:%02d".format(editStartTime.hour, editStartTime.minute))
            }

            // Stop time
            if (hasStopTime) {
                OutlinedButton(
                    onClick = { showStopTimePicker = true },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Ende: %02d:%02d".format(editStopTime.hour, editStopTime.minute))
                }
            } else {
                OutlinedButton(
                    onClick = { },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = false
                ) {
                    Text("Ende: Laeuft noch")
                }
            }

            // Pause
            OutlinedTextField(
                value = if (editPauseMinutes > 0) editPauseMinutes.toString() else "",
                onValueChange = { value ->
                    editPauseMinutes = value.filter { it.isDigit() }.toIntOrNull() ?: 0
                },
                label = { Text("Pause (Minuten)") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            HorizontalDivider()

            // Brutto / Netto
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column {
                    Text("Brutto", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Text(
                        text = DateFormatting.formatDurationHHMM(bruttoMinutes),
                        style = MaterialTheme.typography.bodyLarge,
                        fontFamily = FontFamily.Monospace
                    )
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text("Netto", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Text(
                        text = DateFormatting.formatDurationHHMM(nettoMinutes),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        fontFamily = FontFamily.Monospace
                    )
                }
            }

            if (hasStopTime && !isValid && editStopTime <= editStartTime) {
                Text(
                    text = "Endzeit muss nach Startzeit liegen",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.error
                )
            }

            Spacer(Modifier.height(8.dp))

            // Action buttons
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Delete
                OutlinedButton(
                    onClick = { showDeleteConfirmation = true },
                    colors = ButtonDefaults.outlinedButtonColors(
                        contentColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Icon(Icons.Default.Delete, contentDescription = null)
                    Spacer(Modifier.width(4.dp))
                    Text("Loeschen")
                }

                Spacer(Modifier.weight(1f))

                // Cancel
                TextButton(onClick = onDismiss) {
                    Text("Abbrechen")
                }

                // Save
                Button(
                    onClick = {
                        val startInstant = ZonedDateTime.of(editDate, editStartTime, zone).toInstant()
                        val stopInstant = if (hasStopTime) {
                            ZonedDateTime.of(editDate, editStopTime, zone).toInstant().toString()
                        } else null
                        onSave(
                            editDate.toString(),
                            startInstant.toString(),
                            stopInstant,
                            editPauseMinutes
                        )
                    },
                    enabled = isValid
                ) {
                    Text("Speichern")
                }
            }
        }
    }

    // Date Picker Dialog
    if (showDatePicker) {
        val datePickerState = rememberDatePickerState(
            initialSelectedDateMillis = editDate.atStartOfDay(zone).toInstant().toEpochMilli()
        )
        DatePickerDialog(
            onDismissRequest = { showDatePicker = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        editDate = Instant.ofEpochMilli(millis).atZone(zone).toLocalDate()
                    }
                    showDatePicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showDatePicker = false }) { Text("Abbrechen") }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }

    // Start Time Picker Dialog
    if (showStartTimePicker) {
        val timePickerState = rememberTimePickerState(
            initialHour = editStartTime.hour,
            initialMinute = editStartTime.minute,
            is24Hour = true
        )
        DetailTimePickerDialog(
            onDismiss = { showStartTimePicker = false },
            onConfirm = {
                editStartTime = LocalTime.of(timePickerState.hour, timePickerState.minute)
                showStartTimePicker = false
            }
        ) {
            TimePicker(state = timePickerState)
        }
    }

    // Stop Time Picker Dialog
    if (showStopTimePicker) {
        val timePickerState = rememberTimePickerState(
            initialHour = editStopTime.hour,
            initialMinute = editStopTime.minute,
            is24Hour = true
        )
        DetailTimePickerDialog(
            onDismiss = { showStopTimePicker = false },
            onConfirm = {
                editStopTime = LocalTime.of(timePickerState.hour, timePickerState.minute)
                showStopTimePicker = false
            }
        ) {
            TimePicker(state = timePickerState)
        }
    }

    // Delete confirmation
    if (showDeleteConfirmation) {
        AlertDialog(
            onDismissRequest = { showDeleteConfirmation = false },
            title = { Text("Session loeschen?") },
            text = { Text("Diese Session wird unwiderruflich geloescht.") },
            confirmButton = {
                TextButton(
                    onClick = {
                        showDeleteConfirmation = false
                        onDelete()
                    },
                    colors = ButtonDefaults.textButtonColors(
                        contentColor = MaterialTheme.colorScheme.error
                    )
                ) { Text("Loeschen") }
            },
            dismissButton = {
                TextButton(onClick = { showDeleteConfirmation = false }) { Text("Abbrechen") }
            }
        )
    }
}

@Composable
private fun DetailTimePickerDialog(
    onDismiss: () -> Unit,
    onConfirm: () -> Unit,
    content: @Composable () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        confirmButton = {
            TextButton(onClick = onConfirm) { Text("OK") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Abbrechen") }
        },
        text = { content() }
    )
}
