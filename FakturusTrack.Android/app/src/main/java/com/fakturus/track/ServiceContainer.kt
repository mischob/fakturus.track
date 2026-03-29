package com.fakturus.track

import android.content.Context
import androidx.room.Room
import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.services.api.APIClient
import com.fakturus.track.services.auth.AuthManager
import com.fakturus.track.services.network.NetworkMonitor
import com.fakturus.track.services.sync.SyncEngine
import com.fakturus.track.services.sync.SyncWorker

class ServiceContainer(private val context: Context) {
    val authManager: AuthManager by lazy { AuthManager(context) }

    val networkMonitor: NetworkMonitor by lazy { NetworkMonitor(context) }

    val database: AppDatabase by lazy {
        Room.databaseBuilder(
            context,
            AppDatabase::class.java,
            "fakturus_track.db"
        )
            .addMigrations(MIGRATION_1_2)
            .build()
    }

    var apiClient: APIClient? = null
        private set

    var syncEngine: SyncEngine? = null
        private set

    fun onLogin() {
        val client = APIClient(
            baseUrl = Configuration.apiBaseUrl,
            authManager = authManager
        )
        apiClient = client

        syncEngine = SyncEngine(
            apiClient = client,
            database = database,
            networkMonitor = networkMonitor
        )

        SyncWorker.schedule(context)
    }

    fun onLogout() {
        SyncWorker.cancel(context)
        apiClient?.close()
        apiClient = null
        syncEngine = null
    }

    companion object {
        val MIGRATION_1_2 = object : Migration(1, 2) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    """CREATE TABLE IF NOT EXISTS `pending_deletes` (
                        `id` TEXT NOT NULL,
                        `entityId` TEXT NOT NULL,
                        `entityType` TEXT NOT NULL,
                        `deletedAt` TEXT NOT NULL,
                        PRIMARY KEY(`id`)
                    )""".trimIndent()
                )
            }
        }
    }
}
