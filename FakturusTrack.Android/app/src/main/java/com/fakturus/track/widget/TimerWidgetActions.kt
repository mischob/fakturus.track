package com.fakturus.track.widget

import android.content.Context
import androidx.glance.GlanceId
import androidx.glance.action.ActionParameters
import androidx.glance.appwidget.action.ActionCallback

class StartTimerAction : ActionCallback {
    override suspend fun onAction(
        context: Context,
        glanceId: GlanceId,
        parameters: ActionParameters
    ) {
        // Update widget state immediately to show running
        WidgetStateHelper.writeTimerState(
            context = context,
            isRunning = true,
            startTimeMillis = System.currentTimeMillis(),
            isPaused = false,
            pauseMinutes = 0,
            todayTotalSeconds = 0
        )
    }
}

class StopTimerAction : ActionCallback {
    override suspend fun onAction(
        context: Context,
        glanceId: GlanceId,
        parameters: ActionParameters
    ) {
        WidgetStateHelper.writeTimerState(
            context = context,
            isRunning = false,
            startTimeMillis = null,
            isPaused = false,
            pauseMinutes = 0,
            todayTotalSeconds = 0
        )
    }
}

class PauseResumeAction : ActionCallback {
    override suspend fun onAction(
        context: Context,
        glanceId: GlanceId,
        parameters: ActionParameters
    ) {
        val state = WidgetStateHelper.readTimerState(context)

        WidgetStateHelper.writeTimerState(
            context = context,
            isRunning = state.isRunning,
            startTimeMillis = state.startTimeMillis,
            isPaused = !state.isPaused,
            pauseMinutes = state.pauseMinutes,
            todayTotalSeconds = state.todayTotalSeconds
        )
    }
}
