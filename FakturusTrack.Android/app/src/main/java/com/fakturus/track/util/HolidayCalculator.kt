package com.fakturus.track.util

import java.time.DayOfWeek
import java.time.LocalDate
import java.time.temporal.TemporalAdjusters

data class Holiday(val date: LocalDate, val name: String)

object HolidayCalculator {

    private val cache = mutableMapOf<String, List<Holiday>>()

    fun holidays(bundesland: String, year: Int): List<Holiday> {
        val key = "$bundesland-$year"
        return cache.getOrPut(key) {
            (nationalHolidays(year) + stateHolidays(bundesland, year))
                .sortedBy { it.date }
        }
    }

    fun holidayCount(bundesland: String, year: Int): Int =
        holidays(bundesland, year).size

    fun isHoliday(date: LocalDate, bundesland: String): Boolean =
        holidays(bundesland, date.year).any { it.date == date }

    fun holidayName(date: LocalDate, bundesland: String): String? =
        holidays(bundesland, date.year).firstOrNull { it.date == date }?.name

    // Gauss-Osterformel
    fun easterSunday(year: Int): LocalDate {
        val a = year % 19
        val b = year / 100
        val c = year % 100
        val d = b / 4
        val e = b % 4
        val f = (b + 8) / 25
        val g = (b - f + 1) / 3
        val h = (19 * a + b - d - g + 15) % 30
        val i = c / 4
        val k = c % 4
        val l = (32 + 2 * e + 2 * i - h - k) % 7
        val m = (a + 11 * h + 22 * l) / 451
        val month = (h + l - 7 * m + 114) / 31
        val day = ((h + l - 7 * m + 114) % 31) + 1
        return LocalDate.of(year, month, day)
    }

    private fun nationalHolidays(year: Int): List<Holiday> {
        val easter = easterSunday(year)
        return listOf(
            Holiday(LocalDate.of(year, 1, 1), "Neujahr"),
            Holiday(easter.minusDays(2), "Karfreitag"),
            Holiday(easter.plusDays(1), "Ostermontag"),
            Holiday(LocalDate.of(year, 5, 1), "Tag der Arbeit"),
            Holiday(easter.plusDays(39), "Christi Himmelfahrt"),
            Holiday(easter.plusDays(50), "Pfingstmontag"),
            Holiday(LocalDate.of(year, 10, 3), "Tag der Deutschen Einheit"),
            Holiday(LocalDate.of(year, 12, 25), "1. Weihnachtstag"),
            Holiday(LocalDate.of(year, 12, 26), "2. Weihnachtstag"),
        )
    }

    private fun stateHolidays(bundesland: String, year: Int): List<Holiday> {
        val easter = easterSunday(year)
        val result = mutableListOf<Holiday>()

        if (bundesland in listOf("BW", "BY", "ST"))
            result += Holiday(LocalDate.of(year, 1, 6), "Heilige Drei Koenige")

        if (bundesland == "BE")
            result += Holiday(LocalDate.of(year, 3, 8), "Internationaler Frauentag")

        if (bundesland in listOf("BW", "BY", "HE", "NW", "RP", "SL"))
            result += Holiday(easter.plusDays(60), "Fronleichnam")

        if (bundesland in listOf("BY", "SL"))
            result += Holiday(LocalDate.of(year, 8, 15), "Mariae Himmelfahrt")

        if (bundesland == "TH")
            result += Holiday(LocalDate.of(year, 9, 20), "Weltkindertag")

        if (bundesland in listOf("BB", "HB", "HH", "MV", "NI", "SH", "SN", "ST", "TH"))
            result += Holiday(LocalDate.of(year, 10, 31), "Reformationstag")

        if (bundesland in listOf("BW", "BY", "NW", "RP", "SL"))
            result += Holiday(LocalDate.of(year, 11, 1), "Allerheiligen")

        if (bundesland == "SN")
            result += Holiday(bussUndBettag(year), "Buss- und Bettag")

        return result
    }

    private fun bussUndBettag(year: Int): LocalDate {
        val nov23 = LocalDate.of(year, 11, 23)
        return nov23.with(TemporalAdjusters.previous(DayOfWeek.WEDNESDAY))
    }
}
