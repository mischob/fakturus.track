package com.fakturus.track

import android.app.Application
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class FakturusTrackApp : Application() {
    lateinit var serviceContainer: ServiceContainer
        private set

    override fun onCreate() {
        super.onCreate()
        serviceContainer = ServiceContainer(this)

        // Pre-initialize Room database on background thread
        // so it's ready when ViewModels need it
        CoroutineScope(Dispatchers.IO).launch {
            serviceContainer.database.workSessionDao().getActiveSession()
        }
    }
}
