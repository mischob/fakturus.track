package com.fakturus.track.models

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update
import androidx.room.Database
import androidx.room.RoomDatabase
import kotlinx.coroutines.flow.Flow

@Dao
interface WorkSessionDao {
    @Query("SELECT * FROM work_sessions ORDER BY date DESC, startTime DESC")
    fun getAllOrderedByDate(): Flow<List<WorkSessionEntity>>

    @Query("SELECT * FROM work_sessions WHERE isPendingSync = 1 AND isFinished = 1")
    suspend fun getPendingSessions(): List<WorkSessionEntity>

    @Query("SELECT * FROM work_sessions WHERE isSynced = 1")
    suspend fun getSyncedSessions(): List<WorkSessionEntity>

    @Query("SELECT * FROM work_sessions WHERE isFinished = 0 LIMIT 1")
    suspend fun getActiveSession(): WorkSessionEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(session: WorkSessionEntity)

    @Update
    suspend fun update(session: WorkSessionEntity)

    @Delete
    suspend fun delete(session: WorkSessionEntity)

    @Query("DELETE FROM work_sessions WHERE id = :id")
    suspend fun deleteById(id: String)
}

@Dao
interface VacationDayDao {
    @Query("SELECT * FROM vacation_days ORDER BY date")
    fun getAllOrderedByDate(): Flow<List<VacationDayEntity>>

    @Query("SELECT * FROM vacation_days")
    suspend fun getAll(): List<VacationDayEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(day: VacationDayEntity)

    @Delete
    suspend fun delete(day: VacationDayEntity)

    @Query("DELETE FROM vacation_days WHERE id = :id")
    suspend fun deleteById(id: String)
}

@Dao
interface SickDayDao {
    @Query("SELECT * FROM sick_days ORDER BY date")
    fun getAllOrderedByDate(): Flow<List<SickDayEntity>>

    @Query("SELECT * FROM sick_days")
    suspend fun getAll(): List<SickDayEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(day: SickDayEntity)

    @Delete
    suspend fun delete(day: SickDayEntity)

    @Query("DELETE FROM sick_days WHERE id = :id")
    suspend fun deleteById(id: String)
}

@Dao
interface PendingDeleteDao {
    @Query("SELECT * FROM pending_deletes WHERE entityType = :type")
    suspend fun getByType(type: String): List<PendingDeleteEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(entry: PendingDeleteEntity)

    @Query("DELETE FROM pending_deletes WHERE entityId = :entityId")
    suspend fun deleteByEntityId(entityId: String)
}

@Dao
interface UserSettingsDao {
    @Query("SELECT * FROM user_settings LIMIT 1")
    fun getSettings(): Flow<UserSettingsEntity?>

    @Query("SELECT * FROM user_settings LIMIT 1")
    suspend fun getSettingsOnce(): UserSettingsEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(settings: UserSettingsEntity)
}

@Database(
    entities = [WorkSessionEntity::class, VacationDayEntity::class, SickDayEntity::class, UserSettingsEntity::class, PendingDeleteEntity::class],
    version = 4,
    exportSchema = true
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun workSessionDao(): WorkSessionDao
    abstract fun vacationDayDao(): VacationDayDao
    abstract fun userSettingsDao(): UserSettingsDao
    abstract fun sickDayDao(): SickDayDao
    abstract fun pendingDeleteDao(): PendingDeleteDao
}
