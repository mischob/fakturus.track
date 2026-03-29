package com.fakturus.track.features.settings

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.models.UserSettingsDTO
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.services.sync.SyncEngine
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import java.time.Instant

class SettingsViewModel(
    private val database: AppDatabase,
    private val syncEngine: SyncEngine? = null
) : ViewModel() {

    private val settingsDao = database.userSettingsDao()

    val settings: StateFlow<UserSettingsEntity?> = settingsDao.getSettings()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), null)

    private val _isSaving = MutableStateFlow(false)
    val isSaving: StateFlow<Boolean> = _isSaving.asStateFlow()

    private var debounceJob: Job? = null

    fun updateWorkHoursPerWeek(hours: Double) {
        updateSettings { it.copy(workHoursPerWeek = hours) }
    }

    fun updateVacationDaysPerYear(days: Int) {
        updateSettings { it.copy(vacationDaysPerYear = days) }
    }

    fun updateWorkDays(workDays: Int) {
        updateSettings { it.copy(workDays = workDays) }
    }

    fun updateBundesland(bundesland: String) {
        updateSettings { it.copy(bundesland = bundesland) }
    }

    fun updateCalendarUrl(url: String?) {
        updateSettings { it.copy(calendarUrl = url) }
    }

    private fun updateSettings(transform: (UserSettingsEntity) -> UserSettingsEntity) {
        val current = settings.value ?: UserSettingsEntity(userId = "")
        val updated = transform(current).copy(
            updatedAt = Instant.now().toString(),
            isPendingSync = true,
            isSynced = false
        )

        debounceJob?.cancel()
        debounceJob = viewModelScope.launch {
            _isSaving.value = true
            delay(500) // Debounce 500ms
            try {
                settingsDao.upsert(updated)
                syncEngine?.syncAll()
            } catch (e: Exception) {
                Log.e("SettingsViewModel", "Failed to save settings", e)
            } finally {
                _isSaving.value = false
            }
        }
    }

    fun initializeSettingsIfNeeded() {
        viewModelScope.launch {
            if (settings.value == null) {
                settingsDao.upsert(UserSettingsEntity(userId = ""))
            }
        }
    }
}
