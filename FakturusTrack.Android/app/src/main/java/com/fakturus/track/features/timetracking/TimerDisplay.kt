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
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.fakturus.track.ui.theme.TimerRunning
import com.fakturus.track.util.DateFormatting
import kotlinx.coroutines.delay
import java.time.Duration
import java.time.Instant

enum class TimerSize(val fontSize: TextUnit) {
    LARGE(48.sp),
    MEDIUM(28.sp),
    SMALL(16.sp)
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

    Row(verticalAlignment = Alignment.CenterVertically) {
        if (isRunning) {
            PulsingDot(color = TimerRunning)
            Spacer(Modifier.width(8.dp))
        }
        Text(
            text = DateFormatting.formatDurationHHMMSS(maxOf(0, elapsedMillis)),
            fontFamily = FontFamily.Monospace,
            fontSize = size.fontSize
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
