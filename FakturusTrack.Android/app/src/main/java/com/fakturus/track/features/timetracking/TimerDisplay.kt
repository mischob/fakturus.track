package com.fakturus.track.features.timetracking

import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.fakturus.track.R
import com.fakturus.track.ui.theme.TimerRunning
import com.fakturus.track.util.DateFormatting
import kotlinx.coroutines.delay
import java.time.Duration
import java.time.Instant

enum class TimerSize {
    LARGE,
    MEDIUM,
    SMALL
}

@Composable
fun TimerDisplay(
    startTime: Instant,
    pauseOffsetMillis: Long = 0,
    isRunning: Boolean = true,
    size: TimerSize = TimerSize.LARGE
) {
    var elapsedMillis by remember { mutableLongStateOf(0L) }

    LaunchedEffect(isRunning, startTime, pauseOffsetMillis) {
        if (isRunning) {
            while (true) {
                elapsedMillis = Duration.between(startTime, Instant.now()).toMillis() - pauseOffsetMillis
                delay(1000)
            }
        } else {
            elapsedMillis = Duration.between(startTime, Instant.now()).toMillis() - pauseOffsetMillis
        }
    }

    val totalSeconds = maxOf(0, elapsedMillis / 1000)
    val hours = (totalSeconds / 3600).toInt()
    val minutes = ((totalSeconds % 3600) / 60).toInt()

    val accessibilityText = if (hours > 0) {
        stringResource(R.string.a11y_timer_hours_minutes, hours, minutes)
    } else {
        stringResource(R.string.a11y_timer_minutes, minutes)
    }

    val textStyle = when (size) {
        TimerSize.LARGE -> MaterialTheme.typography.displayMedium
        TimerSize.MEDIUM -> MaterialTheme.typography.headlineMedium
        TimerSize.SMALL -> MaterialTheme.typography.bodyLarge
    }

    Row(verticalAlignment = Alignment.CenterVertically) {
        if (isRunning) {
            PulsingDot(color = TimerRunning)
            Spacer(Modifier.width(8.dp))
        }
        Text(
            text = DateFormatting.formatDurationHHMMSS(maxOf(0, elapsedMillis)),
            fontFamily = FontFamily.Monospace,
            style = textStyle,
            modifier = Modifier.semantics {
                contentDescription = accessibilityText
                liveRegion = LiveRegionMode.Polite
            }
        )
    }
}

@Composable
private fun PulsingDot(color: Color) {
    val infiniteTransition = rememberInfiniteTransition(label = "pulse")
    val alpha by infiniteTransition.animateFloat(
        initialValue = 1f,
        targetValue = 0.3f,
        animationSpec = infiniteRepeatable(
            animation = tween(1000),
            repeatMode = RepeatMode.Reverse
        ),
        label = "pulseAlpha"
    )
    Box(
        modifier = Modifier
            .size(10.dp)
            .clip(CircleShape)
            .background(color.copy(alpha = alpha))
    )
}
