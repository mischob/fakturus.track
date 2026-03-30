package com.fakturus.track.util

import java.time.Instant
import java.time.LocalDate
import java.time.YearMonth
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle
import java.util.Locale

object DateFormatting {
    // UI-facing formatting uses system locale
    fun formatDate(date: LocalDate): String =
        date.format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM)
            .withLocale(Locale.getDefault()))

    fun formatTime(instant: Instant): String =
        instant.atZone(ZoneId.systemDefault())
            .toLocalTime()
            .format(DateTimeFormatter.ofLocalizedTime(FormatStyle.SHORT)
                .withLocale(Locale.getDefault()))

    fun formatMonthYear(date: LocalDate): String =
        date.format(DateTimeFormatter.ofPattern("MMMM yyyy", Locale.getDefault()))

    fun formatWeekdayShort(date: LocalDate): String =
        date.format(DateTimeFormatter.ofPattern("EE", Locale.getDefault()))

    fun localizedMonthYear(yearMonth: YearMonth): String =
        yearMonth.format(DateTimeFormatter.ofPattern("MMMM yyyy", Locale.getDefault()))

    fun formatDurationHHMMSS(durationMillis: Long): String {
        val total = durationMillis / 1000
        val h = total / 3600
        val m = (total % 3600) / 60
        val s = total % 60
        return "%02d:%02d:%02d".format(h, m, s)
    }

    fun formatDurationHHMM(durationMinutes: Long): String {
        val h = durationMinutes / 60
        val m = durationMinutes % 60
        return if (m > 0) "$h:%02dh".format(m) else "${h}h"
    }
}
