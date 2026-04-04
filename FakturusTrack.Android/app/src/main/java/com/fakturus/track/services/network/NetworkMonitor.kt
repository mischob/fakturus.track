package com.fakturus.track.services.network

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.net.NetworkRequest
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.concurrent.atomic.AtomicReference

class NetworkMonitor(context: Context) {
    private val _isConnected = MutableStateFlow(true)
    val isConnected: StateFlow<Boolean> = _isConnected.asStateFlow()

    private var onBecameOnline: (() -> Unit)? = null
    private val debounceJob = AtomicReference<Job?>(null)

    private val connectivityManager =
        context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager

    init {
        // Check current state
        val activeNetwork = connectivityManager.activeNetwork
        val capabilities = activeNetwork?.let { connectivityManager.getNetworkCapabilities(it) }
        _isConnected.value = capabilities?.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET) == true

        // Register for updates
        val request = NetworkRequest.Builder()
            .addCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
            .build()

        connectivityManager.registerNetworkCallback(request, object : ConnectivityManager.NetworkCallback() {
            override fun onAvailable(network: Network) {
                val wasOffline = !_isConnected.value
                _isConnected.value = true

                if (wasOffline) {
                    // Debounce: 2s warten bevor Callback
                    debounceJob.getAndSet(
                        CoroutineScope(Dispatchers.IO).launch {
                            delay(2000)
                            if (_isConnected.value) {
                                onBecameOnline?.invoke()
                            }
                        }
                    )?.cancel()
                }
            }

            override fun onLost(network: Network) {
                _isConnected.value = false
            }

            override fun onCapabilitiesChanged(network: Network, capabilities: NetworkCapabilities) {
                _isConnected.value = capabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
            }
        })
    }

    fun setOnBecameOnline(handler: () -> Unit) {
        onBecameOnline = handler
    }
}
