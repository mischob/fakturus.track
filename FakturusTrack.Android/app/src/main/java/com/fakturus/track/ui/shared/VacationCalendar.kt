package com.fakturus.track.ui.shared

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.LocalHospital
import androidx.compose.material.icons.filled.SwapHoriz
import androidx.compose.material.icons.filled.WbSunny
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.fakturus.track.ui.theme.Vacation
import com.fakturus.track.util.Holiday
import java.time.DayOfWeek
import java.time.LocalDate
import java.time.Month
import java.time.YearMonth
import java.time.format.TextStyle
import java.util.Locale

enum class DayType {
    EMPTY, WORKDAY, WEEKEND, HOLIDAY, VACATION, SICK_DAY, TODAY
}

data class DayCell(
    val date: LocalDate? = null,
    val dayNumber: Int? = null,
    val type: DayType = DayType.EMPTY,
    val isToday: Boolean = false,
    val holidayName: String? = null
) {
    val isTappable: Boolean
        get() = type == DayType.WORKDAY || type == DayType.VACATION || type == DayType.SICK_DAY
}

fun isWorkday(dayOfWeek: DayOfWeek, workDays: Int): Boolean {
    val index = dayOfWeek.value - 1
    return (workDays and (1 shl index)) != 0
}

fun buildCells(
    year: Int,
    month: Int,
    vacationDates: Set<LocalDate>,
    sickDayDates: Set<LocalDate>,
    holidays: List<Holiday>,
    workDays: Int
): List<DayCell> {
    val yearMonth = YearMonth.of(year, month)
    val firstDay = yearMonth.atDay(1)
    val offset = firstDay.dayOfWeek.value - 1 // 0=Mo, 6=So
    val daysInMonth = yearMonth.lengthOfMonth()
    val today = LocalDate.now()
    val holidayDates = holidays.associateBy { it.date }

    val cells = mutableListOf<DayCell>()

    // Empty cells before first day
    repeat(offset) {
        cells.add(DayCell())
    }

    // Day cells
    for (day in 1..daysInMonth) {
        val date = LocalDate.of(year, month, day)
        val isToday = date == today
        val dow = date.dayOfWeek

        val type = when {
            date in vacationDates -> DayType.VACATION
            date in sickDayDates -> DayType.SICK_DAY
            date in holidayDates -> DayType.HOLIDAY
            !isWorkday(dow, workDays) -> DayType.WEEKEND
            else -> DayType.WORKDAY
        }

        cells.add(
            DayCell(
                date = date,
                dayNumber = day,
                type = type,
                isToday = isToday,
                holidayName = holidayDates[date]?.name
            )
        )
    }

    // Pad to 42 cells (6 rows x 7)
    while (cells.size < 42) {
        cells.add(DayCell())
    }

    return cells
}

@Composable
fun VacationCalendar(
    year: Int,
    month: Int,
    vacationDates: Set<LocalDate>,
    sickDayDates: Set<LocalDate> = emptySet(),
    holidays: List<Holiday>,
    workDays: Int,
    onDayTap: (LocalDate) -> Unit,
    onDayLongPress: (LocalDate) -> Unit = {},
    onSickDayTap: (LocalDate) -> Unit = {},
    onSwitchAbsenceType: (LocalDate) -> Unit = {},
    onRemoveAbsence: (LocalDate) -> Unit = {},
    onMonthChange: (Int, Int) -> Unit
) {
    val cells = remember(year, month, vacationDates, sickDayDates, holidays, workDays) {
        buildCells(year, month, vacationDates, sickDayDates, holidays, workDays)
    }

    Column {
        // Month header with navigation
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = {
                if (month == 1) onMonthChange(year - 1, 12)
                else onMonthChange(year, month - 1)
            }) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, "Vorheriger Monat")
            }
            Text(
                text = "${Month.of(month).getDisplayName(TextStyle.FULL, Locale.GERMAN)} $year",
                style = MaterialTheme.typography.titleMedium
            )
            IconButton(onClick = {
                if (month == 12) onMonthChange(year + 1, 1)
                else onMonthChange(year, month + 1)
            }) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, "Naechster Monat")
            }
        }

        // Weekday header
        Row(modifier = Modifier.fillMaxWidth()) {
            listOf("Mo", "Di", "Mi", "Do", "Fr", "Sa", "So").forEach { day ->
                Text(
                    text = day,
                    modifier = Modifier.weight(1f),
                    textAlign = TextAlign.Center,
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        // Grid (6 x 7)
        for (row in cells.chunked(7)) {
            Row(modifier = Modifier.fillMaxWidth()) {
                row.forEach { cell ->
                    DayCellComposable(
                        cell = cell,
                        modifier = Modifier
                            .weight(1f)
                            .height(40.dp),
                        onVacationTap = { cell.date?.let(onDayTap) },
                        onSickDayTap = { cell.date?.let(onSickDayTap) },
                        onSwitchType = { cell.date?.let(onSwitchAbsenceType) },
                        onRemove = { cell.date?.let(onRemoveAbsence) }
                    )
                }
            }
        }

        // Legend
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 8.dp),
            horizontalArrangement = Arrangement.spacedBy(16.dp, Alignment.CenterHorizontally)
        ) {
            LegendItem(color = Vacation.copy(alpha = 0.3f), label = "Urlaub")
            LegendItem(color = Color.Red.copy(alpha = 0.3f), label = "Krank")
            LegendItem(color = Color(0xFF8B5CF6).copy(alpha = 0.3f), label = "Feiertag")
        }
    }
}

@Composable
private fun LegendItem(color: Color, label: String) {
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        Box(
            modifier = Modifier
                .size(12.dp)
                .clip(CircleShape)
                .background(color)
        )
        Text(
            text = label,
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun DayCellComposable(
    cell: DayCell,
    modifier: Modifier = Modifier,
    onVacationTap: () -> Unit,
    onSickDayTap: () -> Unit,
    onSwitchType: () -> Unit,
    onRemove: () -> Unit
) {
    val hapticFeedback = LocalHapticFeedback.current
    var showMenu by remember { mutableStateOf(false) }

    Box(
        modifier = modifier
            .then(
                if (cell.date != null && cell.type != DayType.EMPTY && cell.type != DayType.WEEKEND && cell.type != DayType.HOLIDAY) {
                    Modifier.combinedClickable(
                        onClick = {
                            when (cell.type) {
                                DayType.VACATION -> onVacationTap()
                                DayType.SICK_DAY -> onSickDayTap()
                                DayType.WORKDAY -> onVacationTap()
                                else -> {}
                            }
                        },
                        onLongClick = {
                            hapticFeedback.performHapticFeedback(HapticFeedbackType.LongPress)
                            showMenu = true
                        }
                    )
                } else Modifier
            ),
        contentAlignment = Alignment.Center
    ) {
        // Background
        when (cell.type) {
            DayType.VACATION -> {
                Box(
                    modifier = Modifier
                        .size(36.dp)
                        .clip(CircleShape)
                        .background(Vacation.copy(alpha = 0.3f))
                )
            }
            DayType.SICK_DAY -> {
                Box(
                    modifier = Modifier
                        .size(36.dp)
                        .clip(CircleShape)
                        .background(Color.Red.copy(alpha = 0.3f))
                )
            }
            else -> {}
        }

        // Today ring
        if (cell.isToday) {
            Box(
                modifier = Modifier
                    .size(36.dp)
                    .border(2.dp, Color.Red, CircleShape)
            )
        }

        // Day number
        cell.dayNumber?.let { dayNumber ->
            val textColor = when (cell.type) {
                DayType.WEEKEND -> MaterialTheme.colorScheme.onSurface.copy(alpha = 0.38f)
                DayType.HOLIDAY -> Color(0xFF8B5CF6) // Purple
                else -> MaterialTheme.colorScheme.onSurface
            }
            Text(
                text = dayNumber.toString(),
                style = MaterialTheme.typography.bodyMedium,
                color = textColor
            )
        }

        // Holiday indicator dot
        if (cell.type == DayType.HOLIDAY) {
            Box(
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset(y = 14.dp)
                    .size(4.dp)
                    .clip(CircleShape)
                    .background(Color(0xFF8B5CF6))
            )
        }

        // Context menu
        DropdownMenu(
            expanded = showMenu,
            onDismissRequest = { showMenu = false }
        ) {
            if (cell.type == DayType.WORKDAY) {
                DropdownMenuItem(
                    text = { Text("Urlaub") },
                    leadingIcon = { Icon(Icons.Default.WbSunny, null) },
                    onClick = { onVacationTap(); showMenu = false }
                )
                DropdownMenuItem(
                    text = { Text("Krank") },
                    leadingIcon = { Icon(Icons.Default.LocalHospital, null) },
                    onClick = { onSickDayTap(); showMenu = false }
                )
            }
            if (cell.type == DayType.VACATION || cell.type == DayType.SICK_DAY) {
                DropdownMenuItem(
                    text = { Text("Typ wechseln") },
                    leadingIcon = { Icon(Icons.Default.SwapHoriz, null) },
                    onClick = { onSwitchType(); showMenu = false }
                )
                DropdownMenuItem(
                    text = { Text("Entfernen") },
                    leadingIcon = {
                        Icon(
                            Icons.Default.Delete, null,
                            tint = MaterialTheme.colorScheme.error
                        )
                    },
                    onClick = { onRemove(); showMenu = false }
                )
            }
        }
    }
}
