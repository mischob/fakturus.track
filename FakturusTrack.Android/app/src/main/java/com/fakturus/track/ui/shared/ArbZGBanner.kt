package com.fakturus.track.ui.shared

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInVertically
import androidx.compose.animation.slideOutVertically
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

@Composable
fun ArbZGBanner(
    netWorkMinutes: Long,
    pauseMinutes: Int,
    hasShown6h: Boolean,
    hasShown9h: Boolean,
    hasShown10h: Boolean,
    onShown6h: () -> Unit = {},
    onShown9h: () -> Unit = {},
    onShown10h: () -> Unit = {},
) {
    val netHours = netWorkMinutes / 60.0
    var currentMessage by remember { mutableStateOf<String?>(null) }
    var currentDismiss by remember { mutableStateOf<(() -> Unit)?>(null) }

    LaunchedEffect(netWorkMinutes, pauseMinutes, hasShown6h, hasShown9h, hasShown10h) {
        when {
            netHours >= 10 && !hasShown10h -> {
                currentMessage = "Hinweis: Sie arbeiten seit 10 Stunden. Die gesetzliche Hoechstarbeitszeit betraegt 10 Stunden."
                currentDismiss = { onShown10h() }
            }
            netHours >= 9 && pauseMinutes < 45 && !hasShown9h -> {
                currentMessage = "Erinnerung: Nach 9 Stunden Arbeit betraegt die Mindestpause 45 Minuten."
                currentDismiss = { onShown9h() }
            }
            netHours >= 6 && pauseMinutes < 30 && !hasShown6h -> {
                currentMessage = "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
                currentDismiss = { onShown6h() }
            }
        }
    }

    AnimatedVisibility(
        visible = currentMessage != null,
        enter = slideInVertically() + fadeIn(),
        exit = slideOutVertically() + fadeOut()
    ) {
        currentMessage?.let { message ->
            Card(
                colors = CardDefaults.cardColors(
                    containerColor = Color(0xFFFFA000).copy(alpha = 0.15f)
                ),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(bottom = 8.dp)
            ) {
                Row(
                    modifier = Modifier.padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Warning,
                        contentDescription = null,
                        tint = Color(0xFFFFA000)
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(
                        message,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.weight(1f)
                    )
                    TextButton(onClick = {
                        currentDismiss?.invoke()
                        currentMessage = null
                    }) {
                        Text("OK")
                    }
                }
            }
        }
    }
}
