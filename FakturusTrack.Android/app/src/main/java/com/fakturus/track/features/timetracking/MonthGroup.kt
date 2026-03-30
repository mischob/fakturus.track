package com.fakturus.track.features.timetracking

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.spring
import androidx.compose.animation.expandVertically
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.fakturus.track.R
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.util.DateFormatting

@Composable
fun MonthGroup(
    monthName: String,
    sessions: List<WorkSessionEntity>,
    isCurrentMonth: Boolean = false,
    onDeleteSession: (WorkSessionEntity) -> Unit,
    onSelectSession: (WorkSessionEntity) -> Unit
) {
    var isExpanded by remember(monthName) { mutableStateOf(isCurrentMonth) }

    val totalNetMinutes = sessions.sumOf { it.netDurationMinutes }
    val totalFormatted = DateFormatting.formatDurationHHMM(totalNetMinutes)
    val headerDesc = stringResource(R.string.a11y_month_group, monthName, sessions.size, totalFormatted)
    val expandHint = if (isExpanded) stringResource(R.string.a11y_month_collapse) else stringResource(R.string.a11y_month_expand)

    Column(modifier = Modifier.fillMaxWidth()) {
        // Header
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .clickable { isExpanded = !isExpanded }
                .padding(vertical = 8.dp)
                .semantics(mergeDescendants = true) {
                    contentDescription = "$headerDesc. $expandHint"
                    heading()
                },
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Text(
                text = monthName,
                style = MaterialTheme.typography.titleSmall,
                modifier = Modifier.weight(1f)
            )
            Text(
                text = "${sessions.size} Eintr.",
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = totalFormatted,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Bold,
                fontFamily = FontFamily.Monospace
            )
            Icon(
                Icons.Default.ChevronRight,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.rotate(if (isExpanded) 90f else 0f)
            )
        }

        // Content - regular Column, NOT LazyColumn
        AnimatedVisibility(
            visible = isExpanded,
            enter = expandVertically(animationSpec = spring(dampingRatio = 0.7f)),
            exit = shrinkVertically(animationSpec = spring(dampingRatio = 0.7f))
        ) {
            Column {
                HorizontalDivider()
                sessions.forEachIndexed { index, session ->
                    SessionRow(
                        session = session,
                        onTap = { onSelectSession(session) },
                        onDelete = { onDeleteSession(session) }
                    )
                    if (index < sessions.lastIndex) {
                        HorizontalDivider(
                            modifier = Modifier.padding(start = 32.dp)
                        )
                    }
                }
            }
        }
    }
}
