package com.fakturus.track.features.timetracking

import androidx.compose.animation.AnimatedVisibility
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
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
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

    Column(modifier = Modifier.fillMaxWidth()) {
        // Header
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .clickable { isExpanded = !isExpanded }
                .padding(vertical = 8.dp),
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
                text = DateFormatting.formatDurationHHMM(totalNetMinutes),
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Bold,
                fontFamily = FontFamily.Monospace
            )
            Icon(
                Icons.Default.ChevronRight,
                contentDescription = if (isExpanded) "Einklappen" else "Ausklappen",
                tint = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.rotate(if (isExpanded) 90f else 0f)
            )
        }

        // Content - regular Column, NOT LazyColumn
        AnimatedVisibility(
            visible = isExpanded,
            enter = expandVertically(),
            exit = shrinkVertically()
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
