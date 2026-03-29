package com.fakturus.track.features.overview

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.fakturus.track.models.MonthlyOvertimeDTO
import kotlin.math.abs

fun formatHours(totalHours: Double): String {
    val isNegative = totalHours < 0
    val absHours = abs(totalHours)
    val hours = absHours.toInt()
    val minutes = ((absHours - hours) * 60).toInt()
    val sign = if (isNegative) "-" else if (totalHours > 0) "+" else ""
    return "$sign$hours:${"%02d".format(minutes)}h"
}

@Composable
fun MonthlyOvertimeTable(
    months: List<MonthlyOvertimeDTO>,
    modifier: Modifier = Modifier
) {
    val totalWorked = months.sumOf { it.workedHours }
    val totalExpected = months.sumOf { it.expectedHours }
    val totalOvertime = months.sumOf { it.overtimeHours }

    // Header
    Row(
        modifier = modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = "Monat",
            style = MaterialTheme.typography.labelMedium,
            modifier = Modifier.weight(1.2f),
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = "Gearbeitet",
            style = MaterialTheme.typography.labelMedium,
            modifier = Modifier.weight(1f),
            textAlign = TextAlign.End,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = "Erwartet",
            style = MaterialTheme.typography.labelMedium,
            modifier = Modifier.weight(1f),
            textAlign = TextAlign.End,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = "+/-",
            style = MaterialTheme.typography.labelMedium,
            modifier = Modifier.weight(0.8f),
            textAlign = TextAlign.End,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }

    HorizontalDivider()

    // Month rows
    months.forEach { month ->
        val overtimeColor = if (month.overtimeHours >= 0)
            MaterialTheme.colorScheme.primary
        else
            MaterialTheme.colorScheme.error

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 6.dp),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = month.monthName,
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.weight(1.2f)
            )
            Text(
                text = formatHours(month.workedHours),
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.weight(1f),
                textAlign = TextAlign.End
            )
            Text(
                text = formatHours(month.expectedHours),
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.weight(1f),
                textAlign = TextAlign.End
            )
            Text(
                text = formatHours(month.overtimeHours),
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.weight(0.8f),
                textAlign = TextAlign.End,
                color = overtimeColor
            )
        }
    }

    // Footer / Total
    if (months.isNotEmpty()) {
        HorizontalDivider()
        val totalColor = if (totalOvertime >= 0)
            MaterialTheme.colorScheme.primary
        else
            MaterialTheme.colorScheme.error

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = "Gesamt",
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.weight(1.2f)
            )
            Text(
                text = formatHours(totalWorked),
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.weight(1f),
                textAlign = TextAlign.End
            )
            Text(
                text = formatHours(totalExpected),
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.weight(1f),
                textAlign = TextAlign.End
            )
            Text(
                text = formatHours(totalOvertime),
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.weight(0.8f),
                textAlign = TextAlign.End,
                color = totalColor
            )
        }
    }
}
