package com.fakturus.track.features.vacation

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.models.SickDayEntity
import com.fakturus.track.models.VacationDayEntity
import com.fakturus.track.services.sync.SyncEngine
import com.fakturus.track.util.HolidayCalculator
import com.fakturus.track.util.Holiday
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.YearMonth

data class VacationUiState(
    val displayedYear: Int = LocalDate.now().year,
    val displayedMonth: Int = LocalDate.now().monthValue,
    val vacationDates: Set<LocalDate> = emptySet(),
    val sickDayDates: Set<LocalDate> = emptySet(),
    val holidays: List<Holiday> = emptyList(),
    val workDays: Int = 31,
    val vacationDaysPerYear: Int = 30,
    val bundesland: String = "NW",
    val vacationDaysTaken: Int = 0,
    val vacationDaysRemaining: Int = 30
)

class VacationViewModel(
    private val database: AppDatabase,
    private val syncEngine: SyncEngine? = null
) : ViewModel() {

    private val _displayedYear = MutableStateFlow(LocalDate.now().year)
    private val _displayedMonth = MutableStateFlow(LocalDate.now().monthValue)

    val uiState: StateFlow<VacationUiState> = combine(
        _displayedYear,
        _displayedMonth,
        database.vacationDayDao().getAllOrderedByDate(),
        database.sickDayDao().getAllOrderedByDate(),
        database.userSettingsDao().getSettings()
    ) { values ->
        val year = values[0] as Int
        val month = values[1] as Int
        @Suppress("UNCHECKED_CAST")
        val vacationDays = values[2] as List<VacationDayEntity>
        @Suppress("UNCHECKED_CAST")
        val sickDays = values[3] as List<SickDayEntity>
        val settings = values[4] as? com.fakturus.track.models.UserSettingsEntity

        val bundesland = settings?.bundesland ?: "NW"
        val workDays = settings?.workDays ?: 31
        val vacationDaysPerYear = settings?.vacationDaysPerYear ?: 30

        val vacationDates = vacationDays.map { LocalDate.parse(it.date) }.toSet()
        val sickDayDates = sickDays.map { LocalDate.parse(it.date) }.toSet()
        val currentYearVacationCount = vacationDates.count { it.year == year }

        val holidays = HolidayCalculator.holidays(bundesland, year)

        VacationUiState(
            displayedYear = year,
            displayedMonth = month,
            vacationDates = vacationDates,
            sickDayDates = sickDayDates,
            holidays = holidays,
            workDays = workDays,
            vacationDaysPerYear = vacationDaysPerYear,
            bundesland = bundesland,
            vacationDaysTaken = currentYearVacationCount,
            vacationDaysRemaining = vacationDaysPerYear - currentYearVacationCount
        )
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), VacationUiState())

    fun changeMonth(year: Int, month: Int) {
        _displayedYear.value = year
        _displayedMonth.value = month
    }

    fun toggleVacationDay(date: LocalDate) {
        viewModelScope.launch {
            try {
                val allDays = database.vacationDayDao().getAll()
                val existing = allDays.firstOrNull { it.date == date.toString() }

                if (existing != null) {
                    database.vacationDayDao().delete(existing)
                } else {
                    database.vacationDayDao().insert(
                        VacationDayEntity(date = date.toString())
                    )
                }

                syncEngine?.syncAll()
            } catch (e: Exception) {
                Log.e("VacationViewModel", "Failed to toggle vacation day", e)
            }
        }
    }

    fun toggleSickDay(date: LocalDate) {
        viewModelScope.launch {
            try {
                val dateStr = date.toString()
                val allDays = database.sickDayDao().getAll()
                val existing = allDays.firstOrNull { it.date == dateStr }

                if (existing != null) {
                    database.sickDayDao().delete(existing)
                } else {
                    // Remove vacation day for this date if exists (same as iOS)
                    val vacDays = database.vacationDayDao().getAll()
                    vacDays.firstOrNull { it.date == dateStr }?.let {
                        database.vacationDayDao().delete(it)
                    }
                    database.sickDayDao().insert(
                        SickDayEntity(date = dateStr)
                    )
                }

                syncEngine?.syncAll()
            } catch (e: Exception) {
                Log.e("VacationViewModel", "Failed to toggle sick day", e)
            }
        }
    }

    fun switchAbsenceType(date: LocalDate) {
        viewModelScope.launch {
            try {
                val dateStr = date.toString()

                // Check if vacation
                val vacDays = database.vacationDayDao().getAll()
                val existingVac = vacDays.firstOrNull { it.date == dateStr }
                if (existingVac != null) {
                    database.vacationDayDao().delete(existingVac)
                    database.sickDayDao().insert(SickDayEntity(date = dateStr))
                    syncEngine?.syncAll()
                    return@launch
                }

                // Check if sick day
                val sickDays = database.sickDayDao().getAll()
                val existingSick = sickDays.firstOrNull { it.date == dateStr }
                if (existingSick != null) {
                    database.sickDayDao().delete(existingSick)
                    database.vacationDayDao().insert(VacationDayEntity(date = dateStr))
                    syncEngine?.syncAll()
                    return@launch
                }
            } catch (e: Exception) {
                Log.e("VacationViewModel", "Failed to switch absence type", e)
            }
        }
    }

    fun removeAbsence(date: LocalDate) {
        viewModelScope.launch {
            try {
                val dateStr = date.toString()

                val vacDays = database.vacationDayDao().getAll()
                vacDays.firstOrNull { it.date == dateStr }?.let {
                    database.vacationDayDao().delete(it)
                }

                val sickDays = database.sickDayDao().getAll()
                sickDays.firstOrNull { it.date == dateStr }?.let {
                    database.sickDayDao().delete(it)
                }

                syncEngine?.syncAll()
            } catch (e: Exception) {
                Log.e("VacationViewModel", "Failed to remove absence", e)
            }
        }
    }
}
