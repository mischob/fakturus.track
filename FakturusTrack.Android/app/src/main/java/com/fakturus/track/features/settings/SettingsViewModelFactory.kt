package com.fakturus.track.features.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.fakturus.track.ServiceContainer

class SettingsViewModelFactory(
    private val services: ServiceContainer
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(SettingsViewModel::class.java)) {
            return SettingsViewModel(
                database = services.database,
                syncEngine = services.syncEngine
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
