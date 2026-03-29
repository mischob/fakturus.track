package com.fakturus.track.models

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import java.time.Instant

// Request Types

@Serializable
data class SyncWorkSessionsRequest(
    @SerialName("WorkSessions") val workSessions: List<WorkSessionSyncItem>
)

@Serializable
data class WorkSessionSyncItem(
    @SerialName("Id") val id: String,
    @SerialName("Date") val date: String,
    @SerialName("StartTime") val startTime: String,
    @SerialName("StopTime") val stopTime: String? = null,
    @SerialName("PauseMinutes") val pauseMinutes: Int = 0
)

@Serializable
data class SyncVacationDaysRequest(
    @SerialName("VacationDays") val vacationDays: List<VacationDaySyncItem>
)

@Serializable
data class VacationDaySyncItem(
    @SerialName("Id") val id: String,
    @SerialName("Date") val date: String,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
)

// Response Types

@Serializable
data class WorkSessionDTO(
    @SerialName("Id") val id: String,
    @SerialName("UserId") val userId: String? = null,
    @SerialName("Date") val date: String,
    @SerialName("StartTime") val startTime: String,
    @SerialName("StopTime") val stopTime: String? = null,
    @SerialName("PauseMinutes") val pauseMinutes: Int = 0,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
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
    @SerialName("Id") val id: String,
    @SerialName("UserId") val userId: String? = null,
    @SerialName("Date") val date: String,
    @SerialName("CreatedAt") val createdAt: String? = null,
    @SerialName("UpdatedAt") val updatedAt: String? = null,
    @SerialName("SyncedAt") val syncedAt: String? = null
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
    @SerialName("ServerVacationDays") val serverVacationDays: List<VacationDayDTO>,
    @SerialName("DeletedIds") val deletedIds: List<String>
)

@Serializable
data class UserSettingsDTO(
    @SerialName("CalendarUrl") val calendarUrl: String? = null,
    @SerialName("VacationDaysPerYear") val vacationDaysPerYear: Int = 30,
    @SerialName("WorkHoursPerWeek") val workHoursPerWeek: Double = 40.0,
    @SerialName("WorkDays") val workDays: Int = 31,
    @SerialName("Bundesland") val bundesland: String = "NW"
)

@Serializable
data class OvertimeSummaryDTO(
    @SerialName("TotalOvertimeHours") val totalOvertimeHours: Double,
    @SerialName("MonthlyOvertime") val monthlyOvertime: List<MonthlyOvertimeDTO>,
    @SerialName("VacationDaysTaken") val vacationDaysTaken: Int,
    @SerialName("VacationDaysRemaining") val vacationDaysRemaining: Int,
    @SerialName("VacationDaysPerYear") val vacationDaysPerYear: Int,
    @SerialName("HolidaysTaken") val holidaysTaken: Int,
    @SerialName("SchoolHolidayHoursNotWorked") val schoolHolidayHoursNotWorked: Double
)

@Serializable
data class MonthlyOvertimeDTO(
    @SerialName("Year") val year: Int,
    @SerialName("Month") val month: Int,
    @SerialName("MonthName") val monthName: String,
    @SerialName("OvertimeHours") val overtimeHours: Double,
    @SerialName("WorkedHours") val workedHours: Double,
    @SerialName("ExpectedHours") val expectedHours: Double
)
