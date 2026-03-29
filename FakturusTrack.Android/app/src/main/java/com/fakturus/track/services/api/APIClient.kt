package com.fakturus.track.services.api

import android.util.Log
import com.fakturus.track.BuildConfig
import com.fakturus.track.models.SyncVacationDaysRequest
import com.fakturus.track.models.SyncVacationDaysResponse
import com.fakturus.track.models.SyncWorkSessionsRequest
import com.fakturus.track.models.UserSettingsDTO
import com.fakturus.track.models.VacationDayDTO
import com.fakturus.track.models.WorkSessionDTO
import com.fakturus.track.services.auth.AuthManager
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.engine.cio.CIO
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.timeout
import io.ktor.client.request.header
import io.ktor.client.request.parameter
import io.ktor.client.request.request
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.http.ContentType
import io.ktor.http.HttpMethod
import io.ktor.http.contentType
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json

class APIClient(
    private val baseUrl: String,
    private val authManager: AuthManager
) {
    private val jsonConfig = Json {
        ignoreUnknownKeys = true
        isLenient = true
    }

    private val client = HttpClient(CIO) {
        install(ContentNegotiation) { json(jsonConfig) }
        engine {
            requestTimeout = 30_000
            if (BuildConfig.DEBUG) {
                https {
                    trustManager = object : javax.net.ssl.X509TrustManager {
                        override fun checkClientTrusted(chain: Array<java.security.cert.X509Certificate>?, authType: String?) {}
                        override fun checkServerTrusted(chain: Array<java.security.cert.X509Certificate>?, authType: String?) {}
                        override fun getAcceptedIssuers(): Array<java.security.cert.X509Certificate> = emptyArray()
                    }
                }
            }
        }
    }

    @PublishedApi
    internal suspend fun authenticatedRequest(
        method: HttpMethod,
        path: String,
        body: Any? = null,
        queryParams: Map<String, String> = emptyMap()
    ): HttpResponse {
        val token = try {
            authManager.acquireTokenSilently()
        } catch (e: Exception) {
            throw APIError.Unauthorized
        }

        val response = try {
            client.request("$baseUrl$path") {
                this.method = method
                contentType(ContentType.Application.Json)
                header("Authorization", "Bearer $token")
                header("User-Agent", "FakturusTrack-Android/${BuildConfig.VERSION_NAME}")
                queryParams.forEach { (k, v) -> parameter(k, v) }
                if (body != null) setBody(body)
            }
        } catch (e: Exception) {
            if (e is APIError) throw e
            throw APIError.Network(e)
        }

        // 401 Retry (1x) with forced new token
        if (response.status.value == 401) {
            val newToken = try {
                authManager.acquireTokenSilently()
            } catch (e: Exception) {
                throw APIError.Unauthorized
            }
            return try {
                client.request("$baseUrl$path") {
                    this.method = method
                    contentType(ContentType.Application.Json)
                    header("Authorization", "Bearer $newToken")
                    header("User-Agent", "FakturusTrack-Android/${BuildConfig.VERSION_NAME}")
                    queryParams.forEach { (k, v) -> parameter(k, v) }
                    if (body != null) setBody(body)
                }
            } catch (e: Exception) {
                if (e is APIError) throw e
                throw APIError.Network(e)
            }
        }

        return response
    }

    @PublishedApi
    internal fun validateResponse(response: HttpResponse) {
        when (response.status.value) {
            in 200..299 -> return
            401 -> throw APIError.Unauthorized
            403 -> throw APIError.Forbidden
            404 -> throw APIError.NotFound
            in 500..599 -> throw APIError.ServerError(response.status.value)
            else -> throw APIError.Unknown(response.status.value)
        }
    }

    suspend inline fun <reified T> get(
        path: String,
        queryParams: Map<String, String> = emptyMap()
    ): T {
        val response = authenticatedRequest(HttpMethod.Get, path, queryParams = queryParams)
        validateResponse(response)
        return try {
            response.body()
        } catch (e: Exception) {
            throw APIError.DecodingError(e)
        }
    }

    suspend inline fun <reified T, reified B> post(path: String, body: B): T {
        val response = authenticatedRequest(HttpMethod.Post, path, body = body)
        validateResponse(response)
        return try {
            response.body()
        } catch (e: Exception) {
            throw APIError.DecodingError(e)
        }
    }

    suspend fun delete(path: String) {
        val response = authenticatedRequest(HttpMethod.Delete, path)
        validateResponse(response)
    }

    suspend inline fun <reified B> put(path: String, body: B) {
        val response = authenticatedRequest(HttpMethod.Put, path, body = body)
        validateResponse(response)
    }

    // --- Convenience Endpoints ---

    suspend fun getWorkSessions(): List<WorkSessionDTO> = get("/v1/work-sessions")

    suspend fun syncWorkSessions(request: SyncWorkSessionsRequest): List<WorkSessionDTO> =
        post("/v1/work-sessions/sync", request)

    suspend fun deleteWorkSession(id: String) = delete("/v1/work-sessions/$id")

    suspend fun getVacationDays(): List<VacationDayDTO> = get("/v1/vacation-days")

    suspend fun syncVacationDays(request: SyncVacationDaysRequest): SyncVacationDaysResponse =
        post("/v1/vacation-days/sync", request)

    suspend fun getUserSettings(): UserSettingsDTO = get("/v1/settings")

    suspend fun updateUserSettings(settings: UserSettingsDTO) = put("/v1/settings", settings)

    fun close() {
        client.close()
    }
}
