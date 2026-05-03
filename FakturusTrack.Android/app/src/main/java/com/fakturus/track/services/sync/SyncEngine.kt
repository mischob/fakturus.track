package com.fakturus.track.services.sync

import android.util.Log
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.models.SyncSickDaysRequest
import com.fakturus.track.models.SyncVacationDaysRequest
import com.fakturus.track.models.SyncWorkSessionsRequest
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.services.api.APIClient
import com.fakturus.track.services.network.NetworkMonitor
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.sync.Mutex
import java.time.Instant

class SyncEngine(
    private val apiClient: APIClient,
    private val database: AppDatabase,
    private val networkMonitor: NetworkMonitor
) {
    private val _isSyncing = MutableStateFlow(false)
    val isSyncing: StateFlow<Boolean> = _isSyncing.asStateFlow()

    private val _lastSyncDate = MutableStateFlow<Instant?>(null)
    val lastSyncDate: StateFlow<Instant?> = _lastSyncDate.asStateFlow()

    private val _lastError = MutableStateFlow<String?>(null)
    val lastError: StateFlow<String?> = _lastError.asStateFlow()

    private val mutex = Mutex()

    suspend fun syncAll() {
        if (!mutex.tryLock()) return
        if (!networkMonitor.isConnected.value) {
            mutex.unlock()
            return
        }

        _isSyncing.value = true
        _lastError.value = null
        try {
            syncPendingDeletes()
            Log.d("SyncEngine", "Syncing work sessions...")
            syncWorkSessions()
            Log.d("SyncEngine", "Syncing vacation days...")
            syncVacationDays()
            Log.d("SyncEngine", "Syncing sick days...")
            try {
                syncSickDays()
            } catch (e: com.fakturus.track.services.api.APIError.NotFound) {
                Log.d("SyncEngine", "Sick days endpoint not available (404), skipping")
            }
            Log.d("SyncEngine", "Syncing user settings...")
            syncUserSettings()
            _lastSyncDate.value = Instant.now()
            Log.d("SyncEngine", "Sync completed successfully")
        } catch (e: Exception) {
            _lastError.value = e.message ?: "Sync failed"
            Log.e("SyncEngine", "Sync failed", e)
        } finally {
            _isSyncing.value = false
            mutex.unlock()
        }
    }

    private suspend fun syncPendingDeletes() {
        val pendingDeleteDao = database.pendingDeleteDao()

        // WorkSession deletes
        val workSessionDeletes = pendingDeleteDao.getByType("WorkSession")
        for (delete in workSessionDeletes) {
            try {
                apiClient.deleteWorkSession(delete.entityId)
                pendingDeleteDao.deleteByEntityId(delete.entityId)
            } catch (e: Exception) {
                Log.w("SyncEngine", "Failed to delete work session ${delete.entityId}", e)
            }
        }
    }

    private suspend fun syncWorkSessions() {
        val dao = database.workSessionDao()
        val pendingDeleteDao = database.pendingDeleteDao()
        val pending = dao.getPendingSessions()

        // Pending delete IDs to skip during upsert
        val pendingDeleteIds = pendingDeleteDao.getByType("WorkSession")
            .map { it.entityId }.toSet()

        val serverSessions = if (pending.isNotEmpty()) {
            apiClient.syncWorkSessions(
                SyncWorkSessionsRequest(workSessions = pending.map { it.toDTO() })
            )
        } else {
            apiClient.getWorkSessions()
        }

        // Set-Differenz: lokale synced loeschen die nicht mehr auf Server
        val serverIds = serverSessions.map { it.id }.toSet()
        val synced = dao.getSyncedSessions()
        synced.filter { it.id !in serverIds }.forEach { dao.delete(it) }

        // Upsert server sessions (skip pending deletes)
        serverSessions
            .filter { it.id !in pendingDeleteIds }
            .forEach { dto ->
                dao.insert(dto.toEntity())
            }
    }

    private suspend fun syncVacationDays() {
        val dao = database.vacationDayDao()
        val allLocal = dao.getAll()
        val pending = allLocal.filter { it.isPendingSync }

        if (pending.isNotEmpty()) {
            // Pending vorhanden -> ALLE lokalen Tage senden
            val request = SyncVacationDaysRequest(
                vacationDays = allLocal.map { it.toDTO() }
            )
            val response = apiClient.syncVacationDays(request)

            // DeletedIds verarbeiten
            response.deletedIds.forEach { dao.deleteById(it) }

            // Server-Tage upserten
            response.serverVacationDays.forEach { dto ->
                dao.insert(dto.toEntity())
            }
        } else {
            // Keine Pending -> einfacher GET genuegt
            val serverDays = apiClient.getVacationDays()
            serverDays.forEach { dto ->
                dao.insert(dto.toEntity())
            }
        }
    }

    private suspend fun syncSickDays() {
        val dao = database.sickDayDao()
        val allLocal = dao.getAll()
        val pending = allLocal.filter { it.isPendingSync }

        if (pending.isNotEmpty()) {
            val request = SyncSickDaysRequest(
                sickDays = allLocal.map { it.toDTO() }
            )
            val response = apiClient.syncSickDays(request)

            response.deletedIds.forEach { dao.deleteById(it) }
            response.serverSickDays.forEach { dto -> dao.insert(dto.toEntity()) }
        } else {
            val serverDays = apiClient.getSickDays("2000-01-01", "2099-12-31")
            val localSynced = dao.getAll().filter { it.isSynced }

            val serverIds = serverDays.map { it.id }.toSet()
            localSynced.filter { it.id !in serverIds }.forEach { dao.deleteById(it.id) }

            serverDays.forEach { dto -> dao.insert(dto.toEntity()) }
        }
    }

    private suspend fun syncUserSettings() {
        val settingsDao = database.userSettingsDao()
        val local = settingsDao.getSettingsOnce() ?: return

        val serverSettings = apiClient.getUserSettings()

        val localUpdatedAt = try { Instant.parse(local.updatedAt) } catch (_: Exception) { Instant.EPOCH }
        val serverUpdatedAt = serverSettings.updatedAt?.let {
            try { Instant.parse(it) } catch (_: Exception) { null }
        }

        // Upload nur, wenn lokal wirklich eine ausstehende Aenderung vorliegt.
        // Fehlt sonst `serverUpdatedAt`, wuerde der Vergleich lokal immer als
        // neuer einstufen und bei jedem Sync ein PUT feuern.
        if (local.isPendingSync && (serverUpdatedAt == null || localUpdatedAt.isAfter(serverUpdatedAt))) {
            apiClient.updateUserSettings(local.toDTO())
            settingsDao.upsert(local.copy(
                isSynced = true,
                isPendingSync = false,
                pendingEffectiveDate = null
            ))
        } else if (serverUpdatedAt != null && serverUpdatedAt.isAfter(localUpdatedAt)) {
            // Server ist neuer -> lokal ueberschreiben
            settingsDao.upsert(local.copy(
                calendarUrl = serverSettings.calendarUrl,
                vacationDaysPerYear = serverSettings.vacationDaysPerYear,
                workHoursPerWeek = serverSettings.workHoursPerWeek,
                workDays = serverSettings.workDays,
                bundesland = serverSettings.bundesland,
                updatedAt = serverSettings.updatedAt ?: local.updatedAt,
                isSynced = true,
                isPendingSync = false
            ))
        }
        // Gleich -> nichts tun
    }
}
