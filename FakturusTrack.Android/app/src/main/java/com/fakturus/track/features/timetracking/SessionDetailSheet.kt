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
import java.time.ZoneId
import java.time.ZonedDateTime

/**
 * Edit/create sheet for a work session.
 *
 * Each timestamp (start, stop) is represented as a full [ZonedDateTime] so that
 * multi-day sessions (e.g. forgot to stop the timer overnight) survive editing
 * without their date components being collapsed onto a single shared "edit
 * date" — which was the previous bug.
 */
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

    val sessionStart = remember(session.id) { Instant.parse(session.startTime).atZone(zone) }
    val sessionStop = remember(session.id) {
        session.stopTime?.let { Instant.parse(it).atZone(zone) }
            ?: sessionStart.plusHours(1)
    }

    var editStart by remember(session.id) { mutableStateOf(sessionStart) }
    var editStop by remember(session.id) { mutableStateOf(sessionStop) }
    var editPauseMinutes by remember(session.id) { mutableIntStateOf(session.pauseMinutes) }

    var showStartDatePicker by remember { mutableStateOf(false) }
    var showStartTimePicker by remember { mutableStateOf(false) }
    var showStopDatePicker by remember { mutableStateOf(false) }
    var showStopTimePicker by remember { mutableStateOf(false) }
    var showDeleteConfirmation by remember { mutableStateOf(false) }

    val bruttoMinutes = maxOf(0, Duration.between(editStart, editStop).toMinutes())
    val nettoMinutes = maxOf(0, bruttoMinutes - editPauseMinutes)

    // 72h ceiling covers "forgot to stop" recovery while still rejecting
    // accidental garbage input.
    val maxDurationMinutes = 72L * 60L
    val isValid = editStop.isAfter(editStart) && bruttoMinutes <= maxDurationMinutes

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

            // --- Start ----------------------------------------------------
            Text(
                text = "Start",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                OutlinedButton(
                    onClick = { showStartDatePicker = true },
                    modifier = Modifier.weight(1f)
                ) {
                    Text(DateFormatting.formatDate(editStart.toLocalDate()))
                }
                OutlinedButton(
                    onClick = { showStartTimePicker = true },
                    modifier = Modifier.weight(1f)
                ) {
                    Text("%02d:%02d".format(editStart.hour, editStart.minute))
                }
            }

            // --- Ende -----------------------------------------------------
            Text(
                text = "Ende",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                OutlinedButton(
                    onClick = { showStopDatePicker = true },
                    modifier = Modifier.weight(1f)
                ) {
                    Text(DateFormatting.formatDate(editStop.toLocalDate()))
                }
                OutlinedButton(
                    onClick = { showStopTimePicker = true },
                    modifier = Modifier.weight(1f)
                ) {
                    Text("%02d:%02d".format(editStop.hour, editStop.minute))
                }
            }

            // --- Pause ----------------------------------------------------
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

            // --- Brutto / Netto -------------------------------------------
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

            if (!isValid) {
                val msg = when {
                    !editStop.isAfter(editStart) -> "Endzeit muss nach Startzeit liegen"
                    bruttoMinutes > maxDurationMinutes -> "Dauer ueber 72 Stunden — bitte pruefen"
                    else -> ""
                }
                if (msg.isNotEmpty()) {
                    Text(
                        text = msg,
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.error
                    )
                }
            }

            Spacer(Modifier.height(8.dp))

            // --- Action buttons -------------------------------------------
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
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

                TextButton(onClick = onDismiss) {
                    Text("Abbrechen")
                }

                Button(
                    onClick = {
                        // Date is derived from the start timestamp so the
                        // grouping/sort key always matches reality.
                        val derivedDate = editStart.toLocalDate().toString()
                        val startInstant = editStart.toInstant().toString()
                        val stopInstant = editStop.toInstant().toString()
                        onSave(derivedDate, startInstant, stopInstant, editPauseMinutes)
                    },
                    enabled = isValid
                ) {
                    Text("Speichern")
                }
            }
        }
    }

    // --- Pickers ------------------------------------------------------------

    if (showStartDatePicker) {
        val datePickerState = rememberDatePickerState(
            initialSelectedDateMillis = editStart.toLocalDate().atStartOfDay(zone).toInstant().toEpochMilli()
        )
        DatePickerDialog(
            onDismissRequest = { showStartDatePicker = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        val newDate = Instant.ofEpochMilli(millis).atZone(zone).toLocalDate()
                        editStart = editStart
                            .withYear(newDate.year)
                            .withMonth(newDate.monthValue)
                            .withDayOfMonth(newDate.dayOfMonth)
                    }
                    showStartDatePicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showStartDatePicker = false }) { Text("Abbrechen") }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }

    if (showStartTimePicker) {
        val timePickerState = rememberTimePickerState(
            initialHour = editStart.hour,
            initialMinute = editStart.minute,
            is24Hour = true
        )
        DetailTimePickerDialog(
            onDismiss = { showStartTimePicker = false },
            onConfirm = {
                editStart = editStart
                    .withHour(timePickerState.hour)
                    .withMinute(timePickerState.minute)
                    .withSecond(0)
                    .withNano(0)
                showStartTimePicker = false
            }
        ) {
            TimePicker(state = timePickerState)
        }
    }

    if (showStopDatePicker) {
        val datePickerState = rememberDatePickerState(
            initialSelectedDateMillis = editStop.toLocalDate().atStartOfDay(zone).toInstant().toEpochMilli()
        )
        DatePickerDialog(
            onDismissRequest = { showStopDatePicker = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        val newDate = Instant.ofEpochMilli(millis).atZone(zone).toLocalDate()
                        editStop = editStop
                            .withYear(newDate.year)
                            .withMonth(newDate.monthValue)
                            .withDayOfMonth(newDate.dayOfMonth)
                    }
                    showStopDatePicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showStopDatePicker = false }) { Text("Abbrechen") }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }

    if (showStopTimePicker) {
        val timePickerState = rememberTimePickerState(
            initialHour = editStop.hour,
            initialMinute = editStop.minute,
            is24Hour = true
        )
        DetailTimePickerDialog(
            onDismiss = { showStopTimePicker = false },
            onConfirm = {
                editStop = editStop
                    .withHour(timePickerState.hour)
                    .withMinute(timePickerState.minute)
                    .withSecond(0)
                    .withNano(0)
                showStopTimePicker = false
            }
        ) {
            TimePicker(state = timePickerState)
        }
    }

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

