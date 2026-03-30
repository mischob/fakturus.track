package com.fakturus.track.features.timetracking

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.ExperimentalAnimationApi
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInVertically
import androidx.compose.animation.slideOutVertically
import androidx.compose.animation.togetherWith
import androidx.compose.animation.core.tween
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Pause
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TimePicker
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.material3.rememberTimePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.fakturus.track.R
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.ui.theme.PauseColor
import com.fakturus.track.ui.theme.TimerRunning
import com.fakturus.track.util.DateFormatting
import com.fakturus.track.util.HapticManager
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.LocalTime
import java.time.ZoneId
import java.time.ZonedDateTime

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ActiveSessionCard(
    session: WorkSessionEntity?,
    isPaused: Boolean,
    currentPauseStart: Instant? = null,
    accumulatedPauseMinutes: Int = 0,
    onStart: () -> Unit,
    onStop: () -> Unit,
    onFinish: () -> Unit,
    onPause: () -> Unit,
    onResume: () -> Unit,
    onSave: (date: String, startTime: String, stopTime: String?, pauseMinutes: Int) -> Unit,
    onDelete: () -> Unit
) {
    // Derive a state key for AnimatedContent transitions
    val cardState = when {
        session == null -> "idle"
        isPaused && session.isRunning -> "paused"
        session.isRunning -> "running"
        !session.isFinished -> "stopped"
        else -> "idle"
    }

    ElevatedCard(
        modifier = Modifier.fillMaxWidth()
    ) {
        AnimatedContent(
            targetState = cardState,
            transitionSpec = {
                (fadeIn(tween(300)) + slideInVertically { -it / 4 }) togetherWith
                    (fadeOut(tween(200)) + slideOutVertically { it / 4 })
            },
            label = "activeSessionTransition"
        ) { state ->
            when (state) {
                "idle" -> IdleContent(onStart = onStart)
                "paused" -> if (session != null) PausedContent(
                    session = session,
                    currentPauseStart = currentPauseStart,
                    accumulatedPauseMinutes = accumulatedPauseMinutes,
                    onResume = onResume,
                    onFinish = onFinish
                )
                "running" -> if (session != null) RunningContent(
                    session = session,
                    onStop = onStop,
                    onFinish = onFinish,
                    onPause = onPause
                )
                "stopped" -> if (session != null) StoppedContent(
                    session = session,
                    onFinish = onFinish,
                    onSave = onSave,
                    onDelete = onDelete
                )
            }
        }
    }
}

@Composable
private fun IdleContent(onStart: () -> Unit) {
    val view = LocalView.current
    val startDesc = stringResource(R.string.a11y_start_timer)
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(
            text = stringResource(R.string.times_session_idle),
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Button(
            onClick = {
                HapticManager.toggle(view)
                onStart()
            },
            modifier = Modifier
                .fillMaxWidth()
                .semantics { contentDescription = startDesc }
        ) {
            Icon(Icons.Default.PlayArrow, contentDescription = null, modifier = Modifier.size(20.dp))
            Spacer(Modifier.width(8.dp))
            Text(stringResource(R.string.times_timer_start), style = MaterialTheme.typography.titleMedium)
        }
    }
}

@Composable
private fun RunningContent(
    session: WorkSessionEntity,
    onStop: () -> Unit,
    onFinish: () -> Unit,
    onPause: () -> Unit
) {
    val view = LocalView.current
    val startInstant = remember(session.startTime) { Instant.parse(session.startTime) }
    val pauseDesc = stringResource(R.string.a11y_pause_timer)
    val stopDesc = stringResource(R.string.a11y_stop_timer)
    val finishDesc = stringResource(R.string.a11y_finish_session)

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Status header
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                text = stringResource(R.string.times_session_running),
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        // Timer
        TimerDisplay(
            startTime = startInstant,
            pauseOffsetMillis = session.pauseMinutes.toLong() * 60_000,
            isRunning = true,
            size = TimerSize.LARGE
        )

        // Info row
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text(
                    text = stringResource(R.string.times_label_start),
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = DateFormatting.formatTime(startInstant),
                    style = MaterialTheme.typography.bodyMedium
                )
            }
            if (session.pauseMinutes > 0) {
                Text(
                    text = stringResource(R.string.times_label_pause_format, session.pauseMinutes),
                    style = MaterialTheme.typography.labelSmall,
                    color = PauseColor
                )
            }
        }

        // Buttons
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            // Pause button
            OutlinedButton(
                onClick = {
                    HapticManager.toggle(view)
                    onPause()
                },
                modifier = Modifier
                    .weight(1f)
                    .semantics { contentDescription = pauseDesc },
                colors = ButtonDefaults.outlinedButtonColors(contentColor = PauseColor)
            ) {
                Icon(Icons.Default.Pause, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(4.dp))
                Text(stringResource(R.string.times_timer_pause))
            }

            // Stop button
            OutlinedButton(
                onClick = {
                    HapticManager.toggle(view)
                    onStop()
                },
                modifier = Modifier
                    .weight(1f)
                    .semantics { contentDescription = stopDesc }
            ) {
                Icon(Icons.Default.Stop, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(4.dp))
                Text(stringResource(R.string.times_timer_stop))
            }

            // Finish button
            Button(
                onClick = {
                    HapticManager.toggle(view)
                    onFinish()
                },
                modifier = Modifier
                    .weight(1f)
                    .semantics { contentDescription = finishDesc }
            ) {
                Icon(Icons.Default.CheckCircle, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(4.dp))
                Text(stringResource(R.string.times_timer_finish))
            }
        }
    }
}

@Composable
private fun PausedContent(
    session: WorkSessionEntity,
    currentPauseStart: Instant?,
    accumulatedPauseMinutes: Int,
    onResume: () -> Unit,
    onFinish: () -> Unit
) {
    val view = LocalView.current
    val resumeDesc = stringResource(R.string.a11y_resume_timer)
    val finishDesc = stringResource(R.string.a11y_finish_session)

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Status header - paused
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                text = "\u25CF",
                color = PauseColor,
                style = MaterialTheme.typography.labelSmall
            )
            Spacer(Modifier.width(6.dp))
            Text(
                text = stringResource(R.string.times_session_paused),
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        // Pause timer (how long the current pause has been running)
        if (currentPauseStart != null) {
            TimerDisplay(
                startTime = currentPauseStart,
                isRunning = true,
                size = TimerSize.MEDIUM
            )
        }

        // Accumulated pause info
        if (accumulatedPauseMinutes > 0) {
            Text(
                text = stringResource(R.string.times_label_pause_format, accumulatedPauseMinutes),
                style = MaterialTheme.typography.labelSmall,
                color = PauseColor
            )
        }

        // Buttons
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            // Resume button
            Button(
                onClick = {
                    HapticManager.toggle(view)
                    onResume()
                },
                modifier = Modifier
                    .weight(1f)
                    .semantics { contentDescription = resumeDesc }
            ) {
                Icon(Icons.Default.PlayArrow, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(4.dp))
                Text(stringResource(R.string.times_timer_resume))
            }

            // Finish button
            FilledTonalButton(
                onClick = {
                    HapticManager.toggle(view)
                    onFinish()
                },
                modifier = Modifier
                    .weight(1f)
                    .semantics { contentDescription = finishDesc }
            ) {
                Icon(Icons.Default.CheckCircle, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(4.dp))
                Text(stringResource(R.string.times_timer_finish))
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StoppedContent(
    session: WorkSessionEntity,
    onFinish: () -> Unit,
    onSave: (date: String, startTime: String, stopTime: String?, pauseMinutes: Int) -> Unit,
    onDelete: () -> Unit
) {
    val view = LocalView.current
    val zone = ZoneId.systemDefault()

    val sessionDate = remember(session.date) { LocalDate.parse(session.date) }
    val sessionStart = remember(session.startTime) { Instant.parse(session.startTime) }
    val sessionStop = remember(session.stopTime) {
        session.stopTime?.let { Instant.parse(it) } ?: Instant.now()
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
    val isValid = editStopTime.isAfter(editStartTime) && bruttoMinutes <= 1440

    val finishDesc = stringResource(R.string.a11y_finish_session)

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(
            text = stringResource(R.string.times_session_edit),
            style = MaterialTheme.typography.titleMedium
        )

        // Date
        OutlinedButton(
            onClick = { showDatePicker = true },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("${stringResource(R.string.times_label_date)}: ${DateFormatting.formatDate(editDate)}")
        }

        // Start time
        OutlinedButton(
            onClick = { showStartTimePicker = true },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("${stringResource(R.string.times_label_start)}: %02d:%02d".format(editStartTime.hour, editStartTime.minute))
        }

        // Stop time
        OutlinedButton(
            onClick = { showStopTimePicker = true },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("${stringResource(R.string.times_label_end)}: %02d:%02d".format(editStopTime.hour, editStopTime.minute))
        }

        // Pause
        OutlinedTextField(
            value = if (editPauseMinutes > 0) editPauseMinutes.toString() else "",
            onValueChange = { value ->
                editPauseMinutes = value.filter { it.isDigit() }.toIntOrNull() ?: 0
            },
            label = { Text(stringResource(R.string.times_label_pause_minutes)) },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        // Brutto / Netto
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column {
                Text(stringResource(R.string.times_label_brutto), style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                Text(
                    text = DateFormatting.formatDurationHHMM(bruttoMinutes),
                    style = MaterialTheme.typography.bodyMedium,
                    fontFamily = FontFamily.Monospace
                )
            }
            Column(horizontalAlignment = Alignment.End) {
                Text(stringResource(R.string.times_label_netto), style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                Text(
                    text = DateFormatting.formatDurationHHMM(nettoMinutes),
                    style = MaterialTheme.typography.titleMedium,
                    fontFamily = FontFamily.Monospace
                )
            }
        }

        if (!isValid && editStopTime <= editStartTime) {
            Text(
                text = stringResource(R.string.times_validation_end_after_start),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.error
            )
        }

        // Action buttons
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            // Delete
            IconButton(onClick = { showDeleteConfirmation = true }) {
                Icon(
                    Icons.Default.Delete,
                    contentDescription = stringResource(R.string.times_session_delete),
                    tint = MaterialTheme.colorScheme.error
                )
            }

            Spacer(Modifier.weight(1f))

            // Save
            FilledTonalButton(
                onClick = {
                    HapticManager.toggle(view)
                    val startInstant = ZonedDateTime.of(editDate, editStartTime, zone).toInstant()
                    val stopInstant = ZonedDateTime.of(editDate, editStopTime, zone).toInstant()
                    onSave(
                        editDate.toString(),
                        startInstant.toString(),
                        stopInstant.toString(),
                        editPauseMinutes
                    )
                },
                enabled = isValid
            ) {
                Text(stringResource(R.string.times_session_save))
            }

            // Finish
            Button(
                onClick = {
                    HapticManager.toggle(view)
                    val startInstant = ZonedDateTime.of(editDate, editStartTime, zone).toInstant()
                    val stopInstant = ZonedDateTime.of(editDate, editStopTime, zone).toInstant()
                    onSave(
                        editDate.toString(),
                        startInstant.toString(),
                        stopInstant.toString(),
                        editPauseMinutes
                    )
                    onFinish()
                },
                enabled = isValid,
                modifier = Modifier.semantics { contentDescription = finishDesc }
            ) {
                Icon(Icons.Default.CheckCircle, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(4.dp))
                Text(stringResource(R.string.times_timer_finish))
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
                }) { Text(stringResource(R.string.dialog_ok)) }
            },
            dismissButton = {
                TextButton(onClick = { showDatePicker = false }) { Text(stringResource(R.string.dialog_cancel)) }
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
        TimePickerDialog(
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
        TimePickerDialog(
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
            title = { Text(stringResource(R.string.times_session_delete_title)) },
            text = { Text(stringResource(R.string.times_session_delete_message)) },
            confirmButton = {
                TextButton(
                    onClick = {
                        showDeleteConfirmation = false
                        onDelete()
                    },
                    colors = ButtonDefaults.textButtonColors(
                        contentColor = MaterialTheme.colorScheme.error
                    )
                ) { Text(stringResource(R.string.times_session_delete)) }
            },
            dismissButton = {
                TextButton(onClick = { showDeleteConfirmation = false }) { Text(stringResource(R.string.dialog_cancel)) }
            }
        )
    }
}

@Composable
private fun TimePickerDialog(
    onDismiss: () -> Unit,
    onConfirm: () -> Unit,
    content: @Composable () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        confirmButton = {
            TextButton(onClick = onConfirm) { Text(stringResource(R.string.dialog_ok)) }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(stringResource(R.string.dialog_cancel)) }
        },
        text = { content() }
    )
}
