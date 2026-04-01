package com.fakturus.track.features.legal

import android.content.Context
import android.util.Log
import com.fakturus.track.services.api.APIClient
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import org.json.JSONArray
import org.json.JSONObject

class ConsentManager(private val context: Context) {
    private val prefs = context.getSharedPreferences("consent", Context.MODE_PRIVATE)

    private val _hasRequiredConsents = MutableStateFlow(false)
    val hasRequiredConsents: StateFlow<Boolean> = _hasRequiredConsents.asStateFlow()

    private var apiClient: APIClient? = null

    fun configure(apiClient: APIClient?) {
        this.apiClient = apiClient
    }

    fun checkConsent() {
        val termsVersion = prefs.getInt("terms_version", 0)
        val privacyAcknowledged = prefs.getBoolean("privacy_acknowledged", false)
        _hasRequiredConsents.value = termsVersion > 0 && privacyAcknowledged
    }

    fun recordConsent(termsVersion: Int) {
        prefs.edit()
            .putInt("terms_version", termsVersion)
            .putBoolean("terms_synced", false)
            .putBoolean("privacy_acknowledged", true)
            .apply()
        _hasRequiredConsents.value = true
    }

    suspend fun syncPendingConsent() {
        val synced = prefs.getBoolean("terms_synced", false)
        if (synced) return
        val version = prefs.getInt("terms_version", 0)
        if (version > 0) {
            syncConsentToBackend(version)
        }
    }

    suspend fun checkForVersionUpdates() {
        val client = apiClient ?: return
        try {
            val data = client.fetchRaw("/api/legal/versions")
            val json = JSONObject(data)
            val documents = json.getJSONArray("documents")
            val currentTermsVersion = prefs.getInt("terms_version", 0)

            for (i in 0 until documents.length()) {
                val doc = documents.getJSONObject(i)
                if (doc.getString("type") == "terms_of_service" &&
                    doc.getBoolean("requiresReConsent") &&
                    doc.getInt("version") > currentTermsVersion
                ) {
                    _hasRequiredConsents.value = false
                    return
                }
            }
        } catch (e: Exception) {
            Log.w("ConsentManager", "Version check failed", e)
        }
    }

    fun clearConsent() {
        prefs.edit().clear().apply()
        _hasRequiredConsents.value = false
    }

    private suspend fun syncConsentToBackend(termsVersion: Int) {
        val client = apiClient ?: return
        try {
            val body = JSONObject().apply {
                put("consents", JSONArray().apply {
                    put(JSONObject().apply {
                        put("documentType", "terms_of_service")
                        put("documentVersion", termsVersion)
                        put("consentGiven", true)
                    })
                })
            }
            client.postRaw("/api/legal/consent", body.toString())
            prefs.edit().putBoolean("terms_synced", true).apply()
            Log.i("ConsentManager", "Consent synced to backend")
        } catch (e: Exception) {
            Log.w("ConsentManager", "Backend sync failed, will retry", e)
        }
    }
}
