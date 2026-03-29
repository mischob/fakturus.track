package com.fakturus.track.ui.shared

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.fakturus.track.services.sync.SyncEngine
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withTimeout

@Composable
fun InitialSyncScreen(
    syncEngine: SyncEngine?,
    onComplete: () -> Unit,
    onSkip: () -> Unit
) {
    var isSyncing by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        // Wait briefly for ServiceContainer.onLogin() to finish on IO thread
        var engine = syncEngine
        var attempts = 0
        while (engine == null && attempts < 10) {
            delay(500)
            engine = syncEngine
            attempts++
        }

        if (engine == null) {
            // No sync engine available, proceed without sync
            onComplete()
            return@LaunchedEffect
        }

        try {
            withTimeout(10_000) {
                engine.syncAll()
            }
            onComplete()
        } catch (e: Exception) {
            // Sync failed (no backend, timeout, etc.) - proceed anyway
            // Data will sync later when backend is available
            onComplete()
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        if (isSyncing) {
            CircularProgressIndicator()
            Spacer(Modifier.height(16.dp))
            Text(
                "Daten werden geladen...",
                style = MaterialTheme.typography.headlineSmall
            )
        } else if (error != null) {
            Icon(
                Icons.Default.Warning,
                contentDescription = null,
                modifier = Modifier.size(48.dp),
                tint = Color(0xFFFFA000)
            )
            Spacer(Modifier.height(16.dp))
            Text(
                "Synchronisation fehlgeschlagen",
                style = MaterialTheme.typography.headlineSmall
            )
            Spacer(Modifier.height(8.dp))
            Text(
                error!!,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(Modifier.height(24.dp))
            Button(onClick = {
                isSyncing = true
                error = null
                scope.launch {
                    try {
                        withTimeout(10_000) { syncEngine?.syncAll() }
                        onComplete()
                    } catch (e: Exception) {
                        isSyncing = false
                        error = "Synchronisation fehlgeschlagen."
                    }
                }
            }) {
                Text("Erneut versuchen")
            }
            Spacer(Modifier.height(8.dp))
            TextButton(onClick = onSkip) {
                Text("Ohne Daten fortfahren")
            }
        }
    }
}
