package com.fakturus.track.features.timetracking

import android.content.SharedPreferences
import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.models.PendingDeleteEntity
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.services.api.APIClient
import com.fakturus.track.services.sync.SyncEngine
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import kotlin.math.ceil

class TimeTrackingViewModel(
    private val database: AppDatabase,
    private val syncEngine: SyncEngine? = null,
    private val apiClient: APIClient? = null,
    private val prefs: SharedPreferences? = null
) : ViewModel() {

    private val dao = database.workSessionDao()

    private val _activeSession = MutableStateFlow<WorkSessionEntity?>(null)
    val activeSession: StateFlow<WorkSessionEntity?> = _activeSession.asStateFlow()

    val sessions: Flow<List<WorkSessionEntity>> = dao.getAllOrderedByDate()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private val _error = MutableStateFlow<String?>(null)
    val error: StateFlow<String?> = _error.asStateFlow()

    // Pause state (E08)
    private val _isPaused = MutableStateFlow(false)
    val isPaused: StateFlow<Boolean> = _isPaused.asStateFlow()

    private val _currentPauseStart = MutableStateFlow<Instant?>(null)
    val currentPauseStart: StateFlow<Instant?> = _currentPauseStart.asStateFlow()

    private val _accumulatedPauseMinutes = MutableStateFlow(0)
    val accumulatedPauseMinutes: StateFlow<Int> = _accumulatedPauseMinutes.asStateFlow()

    // ArbZG banner dismiss tracking
    private val _hasShown6h = MutableStateFlow(false)
    val hasShown6h: StateFlow<Boolean> = _hasShown6h.asStateFlow()
    private val _hasShown9h = MutableStateFlow(false)
    val hasShown9h: StateFlow<Boolean> = _hasShown9h.asStateFlow()
    private val _hasShown10h = MutableStateFlow(false)
    val hasShown10h: StateFlow<Boolean> = _hasShown10h.asStateFlow()

    // Expose sync state
    val isSyncing: StateFlow<Boolean> = syncEngine?.isSyncing ?: MutableStateFlow(false)
    val syncLastError: StateFlow<String?> = syncEngine?.lastError ?: MutableStateFlow(null)

    init {
        viewModelScope.launch {
            try {
                val session = dao.getActiveSession()
                _activeSession.value = session

                // Crash recovery: check if a pause was running
                if (session != null) {
                    _accumulatedPauseMinutes.value = session.pauseMinutes
                    val savedPauseStart = prefs?.getString("currentPauseStart", null)
                    if (savedPauseStart != null) {
                        try {
                            _currentPauseStart.value = Instant.parse(savedPauseStart)
                            _isPaused.value = true
                        } catch (_: Exception) {
                            prefs.edit().remove("currentPauseStart").apply()
                        }
                    }
                }
            } catch (e: Exception) {
                _error.value = "Fehler beim Laden der aktiven Sitzung"
            }
        }
    }

    fun startSession() {
        viewModelScope.launch {
            try {
                val now = Instant.now()
                val session = WorkSessionEntity(
                    date = LocalDate.now().toString(),
                    startTime = now.toString(),
                    isPendingSync = true,
                    isSynced = false,
                    isFinished = false
                )
                dao.insert(session)
                _activeSession.value = session
            } catch (e: Exception) {
                _error.value = "Fehler beim Starten der Sitzung"
            }
        }
    }

    fun stopSession() {
        viewModelScope.launch {
            try {
                val session = _activeSession.value ?: return@launch
                if (!session.isRunning) return@launch
                val updated = session.copy(
                    stopTime = Instant.now().toString(),
                    updatedAt = Instant.now().toString()
                )
                dao.update(updated)
                _activeSession.value = updated
            } catch (e: Exception) {
                _error.value = "Fehler beim Stoppen der Sitzung"
            }
        }
    }

    fun finishSession() {
        viewModelScope.launch {
            try {
                // If still paused, resume first to accumulate pause minutes
                if (_isPaused.value) {
                    resumeSessionInternal()
                }

                val session = _activeSession.value ?: return@launch
                val updated = session.copy(
                    stopTime = session.stopTime ?: Instant.now().toString(),
                    pauseMinutes = _accumulatedPauseMinutes.value,
                    isFinished = true,
                    isPendingSync = true,
                    updatedAt = Instant.now().toString()
                )
                dao.update(updated)
                _activeSession.value = null
                _isPaused.value = false
                _currentPauseStart.value = null
                _accumulatedPauseMinutes.value = 0
                _hasShown6h.value = false
                _hasShown9h.value = false
                _hasShown10h.value = false
                prefs?.edit()?.remove("currentPauseStart")?.apply()

                // Trigger sync after finishing session
                syncEngine?.let { engine ->
                    viewModelScope.launch {
                        try {
                            engine.syncAll()
                        } catch (e: Exception) {
                            Log.w("TimeTrackingVM", "Sync after finish failed", e)
                        }
                    }
                }
            } catch (e: Exception) {
                _error.value = "Fehler beim Abschliessen der Sitzung"
            }
        }
    }

    fun updateSession(date: String, startTime: String, stopTime: String?, pauseMinutes: Int) {
        viewModelScope.launch {
            try {
                val session = _activeSession.value ?: return@launch
                // Validation: stopTime must be after startTime
                if (stopTime != null) {
                    val start = Instant.parse(startTime)
                    val stop = Instant.parse(stopTime)
                    if (!stop.isAfter(start)) return@launch
                }
                val updated = session.copy(
                    date = date,
                    startTime = startTime,
                    stopTime = stopTime,
                    pauseMinutes = pauseMinutes,
                    isPendingSync = true,
                    updatedAt = Instant.now().toString()
                )
                dao.update(updated)
                _activeSession.value = updated
            } catch (e: Exception) {
                _error.value = "Fehler beim Aktualisieren der Sitzung"
            }
        }
    }

    fun updateFinishedSession(session: WorkSessionEntity, date: String, startTime: String, stopTime: String?, pauseMinutes: Int) {
        viewModelScope.launch {
            try {
                if (stopTime != null) {
                    val start = Instant.parse(startTime)
                    val stop = Instant.parse(stopTime)
                    if (!stop.isAfter(start)) return@launch
                }
                val updated = session.copy(
                    date = date,
                    startTime = startTime,
                    stopTime = stopTime,
                    pauseMinutes = pauseMinutes,
                    isPendingSync = true,
                    updatedAt = Instant.now().toString()
                )
                dao.update(updated)
            } catch (e: Exception) {
                _error.value = "Fehler beim Aktualisieren der Sitzung"
            }
        }
    }

    fun deleteSession(session: WorkSessionEntity) {
        viewModelScope.launch {
            try {
                dao.delete(session)
                if (session.id == _activeSession.value?.id) {
                    _activeSession.value = null
                }

                // If synced, try API DELETE immediately or queue as pending delete
                if (session.isSynced) {
                    val client = apiClient
                    if (client != null) {
                        try {
                            client.deleteWorkSession(session.id)
                        } catch (e: Exception) {
                            // API delete failed, queue as pending delete for next sync
                            database.pendingDeleteDao().insert(
                                PendingDeleteEntity(
                                    entityId = session.id,
                                    entityType = "WorkSession"
                                )
                            )
                            Log.w("TimeTrackingVM", "API delete failed, queued pending delete", e)
                        }
                    } else {
                        // No API client, queue as pending delete
                        database.pendingDeleteDao().insert(
                            PendingDeleteEntity(
                                entityId = session.id,
                                entityType = "WorkSession"
                            )
                        )
                    }
                }
            } catch (e: Exception) {
                _error.value = "Fehler beim Loeschen der Sitzung"
            }
        }
    }

    fun refresh() {
        viewModelScope.launch {
            try {
                syncEngine?.syncAll()
            } catch (e: Exception) {
                Log.w("TimeTrackingVM", "Pull-to-refresh sync failed", e)
            }
        }
    }

    // Pause methods (E08)
    fun pauseSession() {
        val session = _activeSession.value ?: return
        if (!session.isRunning || _isPaused.value) return
        _isPaused.value = true
        val now = Instant.now()
        _currentPauseStart.value = now
        prefs?.edit()?.putString("currentPauseStart", now.toString())?.apply()
    }

    fun resumeSession() {
        viewModelScope.launch {
            resumeSessionInternal()
        }
    }

    private suspend fun resumeSessionInternal() {
        if (!_isPaused.value) return
        val pauseStart = _currentPauseStart.value ?: return

        val pauseDuration = Duration.between(pauseStart, Instant.now())
        val pauseMinutes = ceil(pauseDuration.seconds / 60.0).toInt().coerceAtLeast(0)
        _accumulatedPauseMinutes.value += pauseMinutes

        val session = _activeSession.value
        if (session != null) {
            val updated = session.copy(
                pauseMinutes = _accumulatedPauseMinutes.value,
                updatedAt = Instant.now().toString()
            )
            dao.update(updated)
            _activeSession.value = updated
        }

        _isPaused.value = false
        _currentPauseStart.value = null
        prefs?.edit()?.remove("currentPauseStart")?.apply()
    }

    fun dismissArbZGBanner() {
        // The banner checks thresholds; dismissing just acknowledges the current one
    }

    fun markShown6h() { _hasShown6h.value = true }
    fun markShown9h() { _hasShown9h.value = true }
    fun markShown10h() { _hasShown10h.value = true }

    /// Create a manual session for retroactive entry (E06-S09)
    fun createManualSession(onCreated: (WorkSessionEntity) -> Unit) {
        viewModelScope.launch {
            try {
                val now = Instant.now()
                val oneHourAgo = now.minus(Duration.ofHours(1))
                val session = WorkSessionEntity(
                    date = LocalDate.now().toString(),
                    startTime = oneHourAgo.toString(),
                    stopTime = now.toString(),
                    pauseMinutes = 0,
                    isPendingSync = true,
                    isSynced = false,
                    isFinished = false
                )
                dao.insert(session)
                onCreated(session)
            } catch (e: Exception) {
                _error.value = "Fehler beim Erstellen der manuellen Sitzung"
            }
        }
    }

    fun triggerSync() {
        viewModelScope.launch {
            try {
                syncEngine?.syncAll()
            } catch (e: Exception) {
                Log.w("TimeTrackingVM", "Manual sync failed", e)
            }
        }
    }
}
