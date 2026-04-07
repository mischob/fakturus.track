package com.fakturus.track.widget

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.longPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import androidx.glance.appwidget.updateAll
import kotlinx.coroutines.flow.first

val Context.widgetDataStore: DataStore<Preferences> by preferencesDataStore(name = "timer_widget")

object WidgetKeys {
    val IS_TIMER_RUNNING = booleanPreferencesKey("isTimerRunning")
    val TIMER_START_MILLIS = longPreferencesKey("timerStartMillis")
    val IS_PAUSED = booleanPreferencesKey("isPaused")
    val PAUSE_MINUTES = intPreferencesKey("pauseMinutes")
    val TODAY_TOTAL_SECONDS = longPreferencesKey("todayTotalSeconds")
}

object WidgetStateHelper {
    suspend fun writeTimerState(
        context: Context,
        isRunning: Boolean,
        startTimeMillis: Long?,
        isPaused: Boolean,
        pauseMinutes: Int,
        todayTotalSeconds: Long
    ) {
        context.widgetDataStore.edit { prefs ->
            prefs[WidgetKeys.IS_TIMER_RUNNING] = isRunning
            startTimeMillis?.let { prefs[WidgetKeys.TIMER_START_MILLIS] = it }
                ?: prefs.remove(WidgetKeys.TIMER_START_MILLIS)
            prefs[WidgetKeys.IS_PAUSED] = isPaused
            prefs[WidgetKeys.PAUSE_MINUTES] = pauseMinutes
            prefs[WidgetKeys.TODAY_TOTAL_SECONDS] = todayTotalSeconds
        }

        // Trigger widget update
        TimerWidget().updateAll(context)
    }

    suspend fun readTimerState(context: Context): TimerWidgetState {
        val prefs = context.widgetDataStore.data.first()
        return TimerWidgetState(
            isRunning = prefs[WidgetKeys.IS_TIMER_RUNNING] ?: false,
            startTimeMillis = prefs[WidgetKeys.TIMER_START_MILLIS],
            isPaused = prefs[WidgetKeys.IS_PAUSED] ?: false,
            pauseMinutes = prefs[WidgetKeys.PAUSE_MINUTES] ?: 0,
            todayTotalSeconds = prefs[WidgetKeys.TODAY_TOTAL_SECONDS] ?: 0
        )
    }
}

data class TimerWidgetState(
    val isRunning: Boolean = false,
    val startTimeMillis: Long? = null,
    val isPaused: Boolean = false,
    val pauseMinutes: Int = 0,
    val todayTotalSeconds: Long = 0
)
