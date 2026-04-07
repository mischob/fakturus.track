package com.fakturus.track.features.overview

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.fakturus.track.ServiceContainer

class OverviewViewModelFactory(
    private val services: ServiceContainer,
    private val context: Context
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(OverviewViewModel::class.java)) {
            return OverviewViewModel(
                apiClient = services.apiClient,
                cache = OvertimeCache(context),
                database = services.database,
                context = context
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
