package com.fakturus.track.features.timetracking

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.fakturus.track.ServiceContainer

class TimeTrackingViewModelFactory(
    private val services: ServiceContainer,
    private val context: Context
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(TimeTrackingViewModel::class.java)) {
            return TimeTrackingViewModel(
                database = services.database,
                syncEngine = services.syncEngine,
                apiClient = services.apiClient,
                prefs = context.getSharedPreferences("fakturus_track_prefs", Context.MODE_PRIVATE),
                appContext = context.applicationContext
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
