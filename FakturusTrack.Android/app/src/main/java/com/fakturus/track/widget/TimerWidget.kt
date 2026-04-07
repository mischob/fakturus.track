package com.fakturus.track.widget

import android.content.Context
import androidx.compose.runtime.Composable
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.glance.GlanceId
import androidx.glance.GlanceModifier
import androidx.glance.GlanceTheme
import androidx.glance.LocalSize
import androidx.glance.action.actionStartActivity
import androidx.glance.action.clickable
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.SizeMode
import androidx.glance.appwidget.action.actionRunCallback
import androidx.glance.appwidget.provideContent
import androidx.compose.ui.unit.DpSize
import androidx.glance.background
import androidx.glance.layout.Alignment
import androidx.glance.layout.Column
import androidx.glance.layout.Row
import androidx.glance.layout.Spacer
import androidx.glance.layout.fillMaxSize
import androidx.glance.layout.fillMaxWidth
import androidx.glance.layout.height
import androidx.glance.layout.padding
import androidx.glance.layout.width
import androidx.glance.text.FontWeight
import androidx.glance.text.Text
import androidx.glance.text.TextStyle
import androidx.glance.unit.ColorProvider
import com.fakturus.track.MainActivity
import java.time.Duration
import java.time.Instant
import java.time.ZoneId

class TimerWidget : GlanceAppWidget() {
    override val sizeMode = SizeMode.Responsive(
        setOf(
            DpSize(110.dp, 110.dp),
            DpSize(250.dp, 110.dp)
        )
    )

    override suspend fun provideGlance(context: Context, id: GlanceId) {
        val state = WidgetStateHelper.readTimerState(context)

        provideContent {
            val size = LocalSize.current
            GlanceTheme {
                if (size.width >= 250.dp) {
                    MediumTimerWidget(state)
                } else {
                    SmallTimerWidget(state)
                }
            }
        }
    }
}

@Composable
private fun SmallTimerWidget(state: TimerWidgetState) {
    Column(
        modifier = GlanceModifier
            .fillMaxSize()
            .padding(12.dp)
            .background(GlanceTheme.colors.widgetBackground)
            .clickable(actionStartActivity<MainActivity>()),
        verticalAlignment = Alignment.CenterVertically,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        if (state.isRunning && !state.isPaused) {
            val elapsed = formatElapsed(state.startTimeMillis)
            Text(
                text = elapsed,
                style = TextStyle(
                    fontSize = 24.sp,
                    fontWeight = FontWeight.Bold,
                    color = GlanceTheme.colors.onSurface
                )
            )
            Spacer(modifier = GlanceModifier.height(4.dp))
            Text(
                text = "Laeuft",
                style = TextStyle(
                    fontSize = 12.sp,
                    color = GlanceTheme.colors.secondary
                )
            )
        } else if (state.isPaused) {
            Text(
                text = "Pausiert",
                style = TextStyle(
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold,
                    color = GlanceTheme.colors.onSurface
                )
            )
        } else {
            Text(
                text = "Bereit",
                style = TextStyle(
                    fontSize = 16.sp,
                    color = GlanceTheme.colors.secondary
                )
            )
        }
    }
}

@Composable
private fun MediumTimerWidget(state: TimerWidgetState) {
    Column(
        modifier = GlanceModifier
            .fillMaxSize()
            .padding(12.dp)
            .background(GlanceTheme.colors.widgetBackground)
    ) {
        Row(
            modifier = GlanceModifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Left: Timer status
            Column(modifier = GlanceModifier.defaultWeight()) {
                if (state.isRunning && !state.isPaused) {
                    Text(
                        text = formatElapsed(state.startTimeMillis),
                        style = TextStyle(
                            fontSize = 28.sp,
                            fontWeight = FontWeight.Bold,
                            color = GlanceTheme.colors.onSurface
                        )
                    )
                    Text(
                        text = "Seit ${formatStartTime(state.startTimeMillis)}",
                        style = TextStyle(
                            fontSize = 12.sp,
                            color = GlanceTheme.colors.secondary
                        )
                    )
                } else if (state.isPaused) {
                    Text(
                        text = "Pausiert",
                        style = TextStyle(
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold,
                            color = GlanceTheme.colors.onSurface
                        )
                    )
                    Text(
                        text = "${state.pauseMinutes} Min Pause",
                        style = TextStyle(
                            fontSize = 12.sp,
                            color = GlanceTheme.colors.secondary
                        )
                    )
                } else {
                    Text(
                        text = "Bereit",
                        style = TextStyle(
                            fontSize = 20.sp,
                            color = GlanceTheme.colors.secondary
                        )
                    )
                }
            }

            // Right: Today total
            Column(horizontalAlignment = Alignment.End) {
                val h = state.todayTotalSeconds / 3600
                val m = (state.todayTotalSeconds % 3600) / 60
                Text(
                    text = "${h}:${"%02d".format(m)}h",
                    style = TextStyle(
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Bold,
                        color = GlanceTheme.colors.onSurface
                    )
                )
                Text(
                    text = "Heute",
                    style = TextStyle(
                        fontSize = 10.sp,
                        color = GlanceTheme.colors.secondary
                    )
                )
            }
        }

        Spacer(modifier = GlanceModifier.height(8.dp))

        // Quick action buttons
        Row(
            modifier = GlanceModifier.fillMaxWidth(),
            horizontalAlignment = Alignment.End
        ) {
            if (state.isRunning) {
                androidx.glance.Button(
                    text = if (state.isPaused) "Weiter" else "Pause",
                    onClick = actionRunCallback<PauseResumeAction>()
                )
                Spacer(modifier = GlanceModifier.width(4.dp))
                androidx.glance.Button(
                    text = "Stop",
                    onClick = actionRunCallback<StopTimerAction>()
                )
            } else {
                androidx.glance.Button(
                    text = "Start",
                    onClick = actionRunCallback<StartTimerAction>()
                )
            }
        }
    }
}

private fun formatElapsed(startTimeMillis: Long?): String {
    if (startTimeMillis == null) return "--:--"
    val elapsed = Duration.between(Instant.ofEpochMilli(startTimeMillis), Instant.now())
    val h = elapsed.toHours()
    val m = elapsed.toMinutes() % 60
    return "${h}:${"%02d".format(m)}"
}

private fun formatStartTime(startTimeMillis: Long?): String {
    if (startTimeMillis == null) return "--:--"
    val instant = Instant.ofEpochMilli(startTimeMillis)
    val time = instant.atZone(ZoneId.systemDefault()).toLocalTime()
    return "${time.hour}:${"%02d".format(time.minute)}"
}
