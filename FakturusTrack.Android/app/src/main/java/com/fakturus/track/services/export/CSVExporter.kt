package com.fakturus.track.services.export

import com.fakturus.track.models.SickDayEntity
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.models.VacationDayEntity
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.ui.shared.isWorkday
import com.fakturus.track.util.Holiday
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

object CSVExporter {

    fun generateCSV(
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        holidays: List<Holiday>,
        settings: UserSettingsEntity,
        from: LocalDate,
        to: LocalDate
    ): String {
        val sb = StringBuilder()
        sb.append("\uFEFF") // UTF-8 BOM
        sb.appendLine("Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ")

        val formatter = DateTimeFormatter.ofPattern("dd.MM.yyyy")
        val weekdayFormatter = DateTimeFormatter.ofPattern("EEEE", Locale.GERMAN)

        var current = from
        while (!current.isAfter(to)) {
            val dateStr = current.format(formatter)
            val weekday = current.format(weekdayFormatter)
            val dateIso = current.toString()

            val isVacation = vacationDays.any { it.date == dateIso }
            val isSick = sickDays.any { it.date == dateIso }
            val holiday = holidays.firstOrNull { it.date == current }
            val isWeekend = !isWorkday(current.dayOfWeek, settings.workDays)
            val session = sessions.firstOrNull { it.date == dateIso }

            val typ = when {
                isSick -> "Krank"
                isVacation -> "Urlaub"
                holiday != null -> "Feiertag"
                isWeekend -> ""
                session != null -> "Arbeit"
                else -> ""
            }

            if (session != null) {
                val netHours = session.netDurationMinutes / 60.0
                val netStr = String.format(Locale.GERMAN, "%.2f", netHours)
                val startStr = formatTime(session.startTime)
                val endStr = session.stopTime?.let { formatTime(it) } ?: ""
                sb.appendLine("$dateStr;$weekday;$startStr;$endStr;${session.pauseMinutes};$netStr;$typ")
            } else {
                sb.appendLine("$dateStr;$weekday;;;;0;0,00;$typ")
            }

            current = current.plusDays(1)
        }

        return sb.toString()
    }

    private fun formatTime(isoInstant: String): String {
        return try {
            val instant = Instant.parse(isoInstant)
            val localTime = instant.atZone(ZoneId.systemDefault()).toLocalTime()
            localTime.format(DateTimeFormatter.ofPattern("HH:mm"))
        } catch (e: Exception) {
            ""
        }
    }
}
