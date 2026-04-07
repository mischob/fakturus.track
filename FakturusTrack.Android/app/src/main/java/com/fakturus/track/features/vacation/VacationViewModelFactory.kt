package com.fakturus.track.features.vacation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.fakturus.track.ServiceContainer

class VacationViewModelFactory(
    private val services: ServiceContainer
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(VacationViewModel::class.java)) {
            return VacationViewModel(
                database = services.database,
                syncEngine = services.syncEngine
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
