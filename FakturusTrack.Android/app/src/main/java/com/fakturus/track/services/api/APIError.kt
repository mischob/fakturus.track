package com.fakturus.track.services.api

sealed class APIError : Exception() {
    data class Network(override val cause: Throwable) : APIError()
    data object Unauthorized : APIError()
    data object Forbidden : APIError()
    data object NotFound : APIError()
    data class ServerError(val code: Int) : APIError()
    data class DecodingError(override val cause: Throwable) : APIError()
    data class Unknown(val code: Int) : APIError()
}
