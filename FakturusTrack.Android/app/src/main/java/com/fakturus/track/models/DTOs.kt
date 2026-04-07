package com.fakturus.track.models

import kotlinx.serialization.Serializable
import java.time.Instant

// Backend liefert camelCase JSON - Property-Namen matchen direkt, keine @SerialName nötig

// Request Types

@Serializable
data class SyncWorkSessionsRequest(
    val workSessions: List<WorkSessionSyncItem>
)

@Serializable
data class WorkSessionSyncItem(
    val id: String,
    val date: String,
    val startTime: String,
    val stopTime: String? = null,
    val pauseMinutes: Int = 0
)

@Serializable
data class SyncVacationDaysRequest(
    val vacationDays: List<VacationDaySyncItem>
)

@Serializable
data class VacationDaySyncItem(
    val id: String,
    val date: String,
    val createdAt: String? = null,
    val updatedAt: String? = null,
    val syncedAt: String? = null
)

// Response Types

@Serializable
data class WorkSessionDTO(
    val id: String,
    val userId: String? = null,
    val date: String,
    val startTime: String,
    val stopTime: String? = null,
    val pauseMinutes: Int = 0,
    val createdAt: String? = null,
    val updatedAt: String? = null,
    val syncedAt: String? = null
) {
    fun toEntity() = WorkSessionEntity(
        id = id, userId = userId ?: "", date = date,
        startTime = startTime, stopTime = stopTime,
        pauseMinutes = pauseMinutes,
        createdAt = createdAt ?: Instant.now().toString(),
        updatedAt = updatedAt ?: Instant.now().toString(),
        syncedAt = Instant.now().toString(),
        isPendingSync = false, isSynced = true, isFinished = stopTime != null
    )
}

@Serializable
data class VacationDayDTO(
    val id: String,
    val userId: String? = null,
    val date: String,
    val createdAt: String? = null,
    val updatedAt: String? = null,
    val syncedAt: String? = null
) {
    fun toEntity() = VacationDayEntity(
        id = id, userId = userId ?: "", date = date,
        createdAt = createdAt ?: Instant.now().toString(),
        updatedAt = updatedAt ?: Instant.now().toString(),
        syncedAt = Instant.now().toString(),
        isPendingSync = false, isSynced = true
    )
}

@Serializable
data class SyncVacationDaysResponse(
    val serverVacationDays: List<VacationDayDTO>,
    val deletedIds: List<String>
)

// SickDay DTOs

@Serializable
data class SyncSickDaysRequest(
    val sickDays: List<SickDaySyncItem>
)

@Serializable
data class SickDaySyncItem(
    val id: String,
    val date: String,
    val createdAt: String? = null,
    val updatedAt: String? = null,
    val syncedAt: String? = null
)

@Serializable
data class SickDayDTO(
    val id: String,
    val userId: String? = null,
    val date: String,
    val createdAt: String? = null,
    val updatedAt: String? = null,
    val syncedAt: String? = null
) {
    fun toEntity() = SickDayEntity(
        id = id, userId = userId ?: "", date = date,
        createdAt = createdAt ?: Instant.now().toString(),
        updatedAt = updatedAt ?: Instant.now().toString(),
        syncedAt = Instant.now().toString(),
        isPendingSync = false, isSynced = true
    )
}

@Serializable
data class SyncSickDaysResponse(
    val serverSickDays: List<SickDayDTO>,
    val deletedIds: List<String>
)

@Serializable
data class UserSettingsDTO(
    val calendarUrl: String? = null,
    val vacationDaysPerYear: Int = 30,
    val workHoursPerWeek: Double = 40.0,
    val workDays: Int = 31,
    val bundesland: String = "NW",
    val updatedAt: String? = null
)

@Serializable
data class OvertimeSummaryDTO(
    val totalOvertimeHours: Double,
    val monthlyOvertime: List<MonthlyOvertimeDTO>,
    val vacationDaysTaken: Int,
    val vacationDaysRemaining: Int,
    val vacationDaysPerYear: Int,
    val holidaysTaken: Int,
    val schoolHolidayHoursNotWorked: Double,
    val sickDaysTaken: Int = 0
)

@Serializable
data class MonthlyOvertimeDTO(
    val year: Int,
    val month: Int,
    val monthName: String,
    val overtimeHours: Double,
    val workedHours: Double,
    val expectedHours: Double,
    val sickDays: Int = 0
)
