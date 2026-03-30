package com.fakturus.track.services.export

import com.fakturus.track.models.SickDayEntity
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.models.VacationDayEntity
import com.fakturus.track.models.WorkSessionEntity
import java.time.Instant
import java.time.LocalDate
import java.time.YearMonth
import java.time.ZoneId
import java.time.format.DateTimeFormatter

object DATEVExporter {

    fun generateExport(
        month: Int,
        year: Int,
        sessions: List<WorkSessionEntity>,
        vacationDays: List<VacationDayEntity>,
        sickDays: List<SickDayEntity>,
        settings: UserSettingsEntity,
        personalNumber: String
    ): String {
        val ym = YearMonth.of(year, month)
        val from = ym.atDay(1).toString()
        val to = ym.atEndOfMonth().toString()

        val dateFormatter = DateTimeFormatter.ofPattern("dd.MM.yyyy")
        val pn = personalNumber.ifEmpty { "00000" }

        // Soll-Stunden pro Tag
        val workDaysPerWeek = countWorkDays(settings.workDays).coerceAtLeast(1)
        val dailyHours = settings.workHoursPerWeek / workDaysPerWeek

        val sb = StringBuilder()
        sb.append("Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause\r\n")

        // Arbeitstage (Lohnart 200)
        val monthSessions = sessions
            .filter { it.date >= from && it.date <= to }
            .sortedBy { it.date }

        for (session in monthSessions) {
            val dateStr = LocalDate.parse(session.date).format(dateFormatter)
            val netHours = session.netDurationMinutes / 60.0
            val hoursStr = "%.2f".format(netHours)
            val startStr = formatTime(session.startTime)
            val endStr = session.stopTime?.let { formatTime(it) } ?: ""
            sb.append("$pn;$dateStr;200;$hoursStr;$startStr;$endStr;${session.pauseMinutes}\r\n")
        }

        // Urlaubstage (Lohnart 400)
        val monthVacation = vacationDays
            .filter { it.date >= from && it.date <= to }
            .sortedBy { it.date }

        for (day in monthVacation) {
            val dateStr = LocalDate.parse(day.date).format(dateFormatter)
            val hoursStr = "%.2f".format(dailyHours)
            sb.append("$pn;$dateStr;400;$hoursStr;;;\r\n")
        }

        // Krankheitstage (Lohnart 500)
        val monthSick = sickDays
            .filter { it.date >= from && it.date <= to }
            .sortedBy { it.date }

        for (day in monthSick) {
            val dateStr = LocalDate.parse(day.date).format(dateFormatter)
            val hoursStr = "%.2f".format(dailyHours)
            sb.append("$pn;$dateStr;500;$hoursStr;;;\r\n")
        }

        return sb.toString()
    }

    fun fileName(month: Int, year: Int): String =
        "DATEV_Lohn_${year}-${"%02d".format(month)}.csv"

    private fun formatTime(isoInstant: String): String {
        return try {
            val instant = Instant.parse(isoInstant)
            val time = instant.atZone(ZoneId.systemDefault()).toLocalTime()
            time.format(DateTimeFormatter.ofPattern("HH:mm"))
        } catch (_: Exception) { "" }
    }

    private fun countWorkDays(bitmask: Int): Int {
        var count = 0
        for (i in 0 until 7) {
            if (bitmask and (1 shl i) != 0) count++
        }
        return count
    }
}
