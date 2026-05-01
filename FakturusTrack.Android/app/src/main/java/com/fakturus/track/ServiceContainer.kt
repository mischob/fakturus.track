package com.fakturus.track

import android.content.Context
import androidx.room.Room
import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.services.api.APIClient
import com.fakturus.track.services.auth.AuthManager
import com.fakturus.track.services.auth.OfflineSessionManager
import com.fakturus.track.services.network.NetworkMonitor
import com.fakturus.track.services.subscription.BillingManager
import com.fakturus.track.services.subscription.SubscriptionManager
import com.fakturus.track.services.sync.SyncEngine
import com.fakturus.track.services.sync.SyncWorker
import com.fakturus.track.features.legal.ConsentManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class ServiceContainer(private val context: Context) {
    val authManager: AuthManager by lazy { AuthManager(context) }

    // Phase 4: Subscription & Billing
    val subscriptionManager = SubscriptionManager(context)
    val billingManager = BillingManager(context, subscriptionManager)

    init {
        // Play Billing bei App-Start initialisieren (NICHT erst bei Login!)
        // Abo-Status und Produkte muessen sofort verfuegbar sein.
        billingManager.startConnection()
    }

    val networkMonitor: NetworkMonitor by lazy { NetworkMonitor(context) }
    val consentManager = ConsentManager(context)

    val database: AppDatabase by lazy {
        Room.databaseBuilder(
            context,
            AppDatabase::class.java,
            "fakturus_track.db"
        )
            .addMigrations(MIGRATION_1_2, MIGRATION_2_3, MIGRATION_3_4, MIGRATION_4_5, MIGRATION_5_6)
            .build()
    }

    var apiClient: APIClient? = null
        private set

    var syncEngine: SyncEngine? = null
        private set

    // Hintergrund-Token-Refresh bei Netzwerkwechsel verdrahten
    fun setupBackgroundRefresh() {
        networkMonitor.setOnBecameOnline {
            CoroutineScope(Dispatchers.IO).launch {
                authManager.attemptBackgroundTokenRefresh()
                // Wenn Refresh erfolgreich und Sync-Engine vorhanden, synchronisieren
                if (!authManager.isOfflineMode.value) {
                    syncEngine?.syncAll()
                }
            }
        }
    }

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

        consentManager.configure(apiClient = client)
        consentManager.checkConsent()
    }

    fun onLogout() {
        SyncWorker.cancel(context)
        apiClient?.close()
        apiClient = null
        syncEngine = null
        consentManager.configure(apiClient = null)
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

        val MIGRATION_2_3 = object : Migration(2, 3) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    "ALTER TABLE `user_settings` ADD COLUMN `updatedAt` TEXT NOT NULL DEFAULT ''"
                )
            }
        }

        val MIGRATION_3_4 = object : Migration(3, 4) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    """CREATE TABLE IF NOT EXISTS `sick_days` (
                        `id` TEXT NOT NULL,
                        `userId` TEXT NOT NULL,
                        `date` TEXT NOT NULL,
                        `createdAt` TEXT NOT NULL,
                        `updatedAt` TEXT NOT NULL,
                        `syncedAt` TEXT,
                        `isPendingSync` INTEGER NOT NULL,
                        `isSynced` INTEGER NOT NULL,
                        PRIMARY KEY(`id`)
                    )""".trimIndent()
                )
            }
        }

        val MIGRATION_4_5 = object : Migration(4, 5) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    "ALTER TABLE `user_settings` ADD COLUMN `personalNumber` TEXT DEFAULT NULL"
                )
            }
        }

        val MIGRATION_5_6 = object : Migration(5, 6) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    "ALTER TABLE `user_settings` ADD COLUMN `pendingEffectiveDate` TEXT DEFAULT NULL"
                )
            }
        }
    }
}
