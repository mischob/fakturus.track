package com.fakturus.track.features.timetracking

import android.view.HapticFeedbackConstants
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Cloud
import androidx.compose.material.icons.filled.CloudUpload
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.SwipeToDismissBox
import androidx.compose.material3.SwipeToDismissBoxValue
import androidx.compose.material3.Text
import androidx.compose.material3.rememberSwipeToDismissBoxState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.fakturus.track.R
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.ui.theme.PauseColor
import com.fakturus.track.ui.theme.SyncDone
import com.fakturus.track.ui.theme.SyncPending
import com.fakturus.track.util.DateFormatting
import java.time.Instant
import java.time.LocalDate

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SessionRow(
    session: WorkSessionEntity,
    onTap: () -> Unit,
    onDelete: () -> Unit
) {
    val view = LocalView.current
    val dismissState = rememberSwipeToDismissBoxState(
        confirmValueChange = { value ->
            if (value == SwipeToDismissBoxValue.EndToStart) {
                view.performHapticFeedback(HapticFeedbackConstants.CONTEXT_CLICK)
                onDelete()
                true
            } else {
                false
            }
        }
    )

    // Build accessibility description
    val date = LocalDate.parse(session.date)
    val weekday = DateFormatting.formatWeekdayShort(date)
    val dateStr = DateFormatting.formatDate(date)
    val startStr = DateFormatting.formatTime(Instant.parse(session.startTime))
    val stopStr = session.stopTime?.let { DateFormatting.formatTime(Instant.parse(it)) } ?: "--:--"
    val netDuration = DateFormatting.formatDurationHHMM(session.netDurationMinutes)
    val sessionDesc = stringResource(R.string.a11y_session_summary, weekday, dateStr, startStr, stopStr, netDuration)
    val syncDesc = if (session.isSynced) stringResource(R.string.a11y_sync_done) else stringResource(R.string.a11y_sync_pending)

    SwipeToDismissBox(
        state = dismissState,
        enableDismissFromStartToEnd = false,
        backgroundContent = {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(
                        color = MaterialTheme.colorScheme.error,
                        shape = RoundedCornerShape(8.dp)
                    )
                    .padding(horizontal = 20.dp),
                contentAlignment = Alignment.CenterEnd
            ) {
                Icon(
                    Icons.Default.Delete,
                    contentDescription = stringResource(R.string.times_session_delete),
                    tint = MaterialTheme.colorScheme.onError
                )
            }
        }
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(MaterialTheme.colorScheme.surface)
                .clickable { onTap() }
                .padding(vertical = 10.dp, horizontal = 4.dp)
                .semantics(mergeDescendants = true) {
                    contentDescription = sessionDesc
                },
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            // Sync icon
            Icon(
                imageVector = if (session.isSynced) Icons.Default.Cloud else Icons.Default.CloudUpload,
                contentDescription = syncDesc,
                tint = if (session.isSynced) SyncDone else SyncPending,
                modifier = Modifier.width(20.dp)
            )

            // Date + time range
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "$weekday $dateStr",
                    style = MaterialTheme.typography.bodyMedium
                )
                Text(
                    text = "$startStr - $stopStr",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            // Pause badge
            if (session.pauseMinutes > 0) {
                Text(
                    text = "P${session.pauseMinutes}",
                    style = MaterialTheme.typography.labelSmall,
                    color = PauseColor,
                    modifier = Modifier
                        .background(
                            color = PauseColor.copy(alpha = 0.1f),
                            shape = RoundedCornerShape(4.dp)
                        )
                        .padding(horizontal = 4.dp, vertical = 2.dp)
                )
            }

            // Net duration
            Text(
                text = netDuration,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Bold,
                fontFamily = FontFamily.Monospace
            )
        }
    }
}
