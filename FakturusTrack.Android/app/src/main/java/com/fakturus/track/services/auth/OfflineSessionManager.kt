package com.fakturus.track.services.auth

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import com.fakturus.track.models.OfflineSession
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class OfflineSessionManager(context: Context) {
    private val prefs: SharedPreferences

    init {
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()

        prefs = EncryptedSharedPreferences.create(
            context,
            "offline_session_prefs",
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
    }

    fun save(session: OfflineSession) {
        val json = Json.encodeToString(session)
        prefs.edit().putString(KEY_SESSION, json).apply()
        Log.d("OfflineSessionManager", "Session saved for user ${session.userId}")
    }

    fun load(): OfflineSession? {
        val json = prefs.getString(KEY_SESSION, null) ?: return null
        return try {
            Json.decodeFromString<OfflineSession>(json)
        } catch (e: Exception) {
            Log.w("OfflineSessionManager", "Failed to decode session", e)
            null
        }
    }

    fun delete() {
        prefs.edit().remove(KEY_SESSION).apply()
        Log.d("OfflineSessionManager", "Session deleted")
    }

    companion object {
        private const val KEY_SESSION = "current_session"
    }
}
