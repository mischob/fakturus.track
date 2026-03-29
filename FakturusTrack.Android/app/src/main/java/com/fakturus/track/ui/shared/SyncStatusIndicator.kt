package com.fakturus.track.ui.shared

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CloudDone
import androidx.compose.material.icons.filled.CloudUpload
import androidx.compose.material.icons.filled.Sync
import androidx.compose.material.icons.filled.SyncProblem
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import com.fakturus.track.ui.theme.SyncDone
import com.fakturus.track.ui.theme.SyncPending

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SyncStatusIndicator(
    isSyncing: Boolean,
    pendingCount: Int,
    lastError: String?,
    onSync: () -> Unit
) {
    val infiniteTransition = rememberInfiniteTransition(label = "sync")
    val rotationAngle by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(
            animation = tween(1000, easing = LinearEasing)
        ),
        label = "rotation"
    )

    IconButton(onClick = onSync) {
        when {
            isSyncing -> Icon(
                Icons.Default.Sync,
                contentDescription = "Synchronisiere...",
                modifier = Modifier.rotate(rotationAngle)
            )
            lastError != null -> Icon(
                Icons.Default.SyncProblem,
                contentDescription = "Sync fehlgeschlagen",
                tint = MaterialTheme.colorScheme.error
            )
            pendingCount > 0 -> BadgedBox(
                badge = {
                    Badge { Text("$pendingCount") }
                }
            ) {
                Icon(
                    Icons.Default.CloudUpload,
                    contentDescription = "Ausstehende Aenderungen",
                    tint = SyncPending
                )
            }
            else -> Icon(
                Icons.Default.CloudDone,
                contentDescription = "Synchronisiert",
                tint = SyncDone
            )
        }
    }
}
