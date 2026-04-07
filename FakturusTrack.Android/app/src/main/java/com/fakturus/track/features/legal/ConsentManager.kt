package com.fakturus.track.features.legal

import android.content.Context
import android.util.Log
import com.fakturus.track.services.api.APIClient
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.Serializable

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
            val response: LegalVersionsResponse = client.get("/api/legal/versions")
            val currentTermsVersion = prefs.getInt("terms_version", 0)

            for (doc in response.documents) {
                if (doc.type == "terms_of_service" &&
                    doc.requiresReConsent &&
                    doc.version > currentTermsVersion
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
            val request = ConsentSubmitRequest(
                consents = listOf(
                    ConsentSubmitItem(
                        documentType = "terms_of_service",
                        documentVersion = termsVersion,
                        consentGiven = true
                    )
                )
            )
            client.post<ConsentStatusResponse, ConsentSubmitRequest>("/api/legal/consent", request)
            prefs.edit().putBoolean("terms_synced", true).apply()
            Log.i("ConsentManager", "Consent synced to backend")
        } catch (e: Exception) {
            Log.w("ConsentManager", "Backend sync failed, will retry", e)
        }
    }
}

// API Models
@Serializable
data class LegalVersionsResponse(val documents: List<LegalDocumentInfo>)

@Serializable
data class LegalDocumentInfo(
    val type: String,
    val version: Int,
    val effectiveDate: String,
    val url: String,
    val requiresConsent: Boolean,
    val requiresReConsent: Boolean
)

@Serializable
data class ConsentSubmitRequest(val consents: List<ConsentSubmitItem>)

@Serializable
data class ConsentSubmitItem(
    val documentType: String,
    val documentVersion: Int,
    val consentGiven: Boolean
)

@Serializable
data class ConsentStatusResponse(
    val consents: List<ConsentRecord>,
    val allRequiredConsentsGiven: Boolean,
    val pendingConsents: List<String>
)

@Serializable
data class ConsentRecord(
    val documentType: String,
    val documentVersion: Int,
    val consentGiven: Boolean,
    val consentTimestamp: String
)
