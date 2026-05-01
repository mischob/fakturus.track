package com.fakturus.track.features.settings

import android.content.Context
import android.util.Log
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.fakturus.track.BuildConfig
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.R
import com.fakturus.track.services.subscription.BillingManager
import com.fakturus.track.services.subscription.SubscriptionManager
import com.fakturus.track.services.subscription.Tier
import com.fakturus.track.services.api.APIClient
import com.fakturus.track.services.sync.SyncEngine
import com.fakturus.track.settingsDataStore
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.LocalDate

class SettingsViewModel(
    private val database: AppDatabase,
    private val syncEngine: SyncEngine? = null,
    private val apiClient: APIClient? = null,
    private val context: Context? = null,
    private val billingManager: BillingManager? = null,
    private val subscriptionManager: SubscriptionManager? = null
) : ViewModel() {

    private val settingsDao = database.userSettingsDao()

    val settings: StateFlow<UserSettingsEntity?> = settingsDao.getSettings()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), null)

    private val _isSaving = MutableStateFlow(false)
    val isSaving: StateFlow<Boolean> = _isSaving.asStateFlow()

    // Appearance from DataStore
    val appearance: StateFlow<String> = context?.let { ctx ->
        ctx.settingsDataStore.data
            .map { prefs -> prefs[stringPreferencesKey("appearance")] ?: "system" }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), "system")
    } ?: MutableStateFlow("system").asStateFlow()

    // Notifications from DataStore
    val notificationsEnabled: StateFlow<Boolean> = context?.let { ctx ->
        ctx.settingsDataStore.data
            .map { prefs -> prefs[booleanPreferencesKey("notifications")] ?: true }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), true)
    } ?: MutableStateFlow(true).asStateFlow()

    val appVersion: String
        get() = "${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})"

    // Restore purchases
    private val _isRestoringPurchases = MutableStateFlow(false)
    val isRestoringPurchases: StateFlow<Boolean> = _isRestoringPurchases.asStateFlow()

    private val _restoreResult = MutableStateFlow<String?>(null)
    val restoreResult: StateFlow<String?> = _restoreResult.asStateFlow()

    fun restorePurchases() {
        viewModelScope.launch {
            _isRestoringPurchases.value = true
            try {
                billingManager?.queryExistingPurchases()
                val tier = subscriptionManager?.tier?.value ?: Tier.FREE
                _restoreResult.value = if (tier > Tier.FREE) {
                    context?.getString(R.string.restore_success, tier.name)
                } else {
                    context?.getString(R.string.restore_no_subscription)
                }
            } catch (e: Exception) {
                _restoreResult.value = context?.getString(R.string.restore_error)
            } finally {
                _isRestoringPurchases.value = false
            }
        }
    }

    private var debounceJob: Job? = null

    /**
     * Effective date for pending changes to historized fields (workDays /
     * workHoursPerWeek). Default = today. The settings UI exposes a date
     * picker so the user can backdate corrections.
     */
    private val _effectiveDate = MutableStateFlow(LocalDate.now())
    val effectiveDate: StateFlow<LocalDate> = _effectiveDate.asStateFlow()

    fun updateEffectiveDate(date: LocalDate) {
        _effectiveDate.value = date
    }

    fun updateWorkHoursPerWeek(hours: Double) {
        updateSettings(historizedFieldChanged = true) { it.copy(workHoursPerWeek = hours) }
    }

    fun updateVacationDaysPerYear(days: Int) {
        updateSettings { it.copy(vacationDaysPerYear = days) }
    }

    fun updateWorkDays(workDays: Int) {
        updateSettings(historizedFieldChanged = true) { it.copy(workDays = workDays) }
    }

    fun updateBundesland(bundesland: String) {
        updateSettings { it.copy(bundesland = bundesland) }
    }

    fun updateCalendarUrl(url: String?) {
        updateSettings { it.copy(calendarUrl = url) }
    }

    fun updatePersonalNumber(number: String) {
        updateSettings { it.copy(personalNumber = number.ifBlank { null }) }
    }

    fun setAppearance(value: String) {
        val ctx = context ?: return
        viewModelScope.launch {
            ctx.settingsDataStore.edit { prefs ->
                prefs[stringPreferencesKey("appearance")] = value
            }
        }
    }

    fun setNotifications(enabled: Boolean) {
        val ctx = context ?: return
        viewModelScope.launch {
            ctx.settingsDataStore.edit { prefs ->
                prefs[booleanPreferencesKey("notifications")] = enabled
            }
        }
    }

    private fun updateSettings(
        historizedFieldChanged: Boolean = false,
        transform: (UserSettingsEntity) -> UserSettingsEntity
    ) {
        val current = settings.value ?: UserSettingsEntity(userId = "")
        val pendingEffectiveDate = if (historizedFieldChanged) {
            _effectiveDate.value.toString()
        } else {
            current.pendingEffectiveDate
        }
        val updated = transform(current).copy(
            updatedAt = Instant.now().toString(),
            isPendingSync = true,
            isSynced = false,
            pendingEffectiveDate = pendingEffectiveDate
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

    fun deleteAccount(onSuccess: () -> Unit) {
        viewModelScope.launch {
            try {
                apiClient?.delete("/api/account")
                onSuccess()
            } catch (e: Exception) {
                Log.e("SettingsVM", "Account deletion failed", e)
            }
        }
    }
}
