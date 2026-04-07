package com.fakturus.track.ui.shared

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WorkdaySelector(
    workDays: Int,
    onWorkDaysChange: (Int) -> Unit,
    modifier: Modifier = Modifier
) {
    val dayLabels = listOf("Mo", "Di", "Mi", "Do", "Fr", "Sa", "So")

    Row(
        modifier = modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        dayLabels.forEachIndexed { index, label ->
            val bitValue = 1 shl index
            val isSelected = (workDays and bitValue) != 0

            FilterChip(
                selected = isSelected,
                onClick = {
                    val newValue = if (isSelected) {
                        workDays and bitValue.inv()
                    } else {
                        workDays or bitValue
                    }
                    onWorkDaysChange(newValue)
                },
                label = {
                    Text(
                        text = label,
                        style = MaterialTheme.typography.labelSmall
                    )
                },
                modifier = Modifier.weight(1f)
            )
        }
    }
}
