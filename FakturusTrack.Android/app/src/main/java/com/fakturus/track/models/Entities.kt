package com.fakturus.track.models

import androidx.room.Entity
import androidx.room.PrimaryKey
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.util.Locale
import java.util.UUID

@Entity(tableName = "work_sessions")
data class WorkSessionEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,
    val startTime: String,
    val stopTime: String? = null,
    val pauseMinutes: Int = 0,
    val calendarEventId: String? = null,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false,
    val isFinished: Boolean = false
) {
    val isRunning: Boolean get() = !isFinished && stopTime == null

    val durationMinutes: Long
        get() {
            val start = Instant.parse(startTime)
            val end = stopTime?.let { Instant.parse(it) } ?: Instant.now()
            return Duration.between(start, end).toMinutes()
        }

    val netDurationMinutes: Long get() = maxOf(0, durationMinutes - pauseMinutes)

    val monthKey: String
        get() {
            val ld = LocalDate.parse(date)
            return ld.format(DateTimeFormatter.ofPattern("MMMM yyyy", Locale.GERMAN))
        }

    fun toDTO() = WorkSessionSyncItem(
        id = id, date = date, startTime = startTime,
        stopTime = stopTime, pauseMinutes = pauseMinutes
    )
}

@Entity(tableName = "vacation_days")
data class VacationDayEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false
) {
    fun toDTO() = VacationDaySyncItem(
        id = id, date = date,
        createdAt = createdAt, updatedAt = updatedAt, syncedAt = syncedAt
    )
}

@Entity(tableName = "sick_days")
data class SickDayEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val userId: String = "",
    val date: String,
    val createdAt: String = Instant.now().toString(),
    val updatedAt: String = Instant.now().toString(),
    val syncedAt: String? = null,
    val isPendingSync: Boolean = true,
    val isSynced: Boolean = false
) {
    fun toDTO() = SickDaySyncItem(
        id = id, date = date,
        createdAt = createdAt, updatedAt = updatedAt, syncedAt = syncedAt
    )
}

@Entity(tableName = "pending_deletes")
data class PendingDeleteEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val entityId: String,
    val entityType: String,
    val deletedAt: String = Instant.now().toString()
)

@Entity(tableName = "user_settings")
data class UserSettingsEntity(
    @PrimaryKey val userId: String,
    val calendarUrl: String? = null,
    val vacationDaysPerYear: Int = 30,
    val workHoursPerWeek: Double = 40.0,
    val workDays: Int = 31,
    val bundesland: String = "NW",
    val personalNumber: String? = null,
    val updatedAt: String = Instant.now().toString(),
    val isSynced: Boolean = false,
    val isPendingSync: Boolean = true
) {
    fun toDTO() = UserSettingsDTO(
        calendarUrl = calendarUrl,
        vacationDaysPerYear = vacationDaysPerYear,
        workHoursPerWeek = workHoursPerWeek,
        workDays = workDays,
        bundesland = bundesland,
        updatedAt = updatedAt
    )
}
