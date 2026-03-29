package com.fakturus.track

import android.app.Application

class FakturusTrackApp : Application() {
    lateinit var serviceContainer: ServiceContainer
        private set

    override fun onCreate() {
        super.onCreate()
        serviceContainer = ServiceContainer(this)
    }
}
