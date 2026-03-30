package com.fakturus.track.ui.shared

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.unit.dp
import com.fakturus.track.services.subscription.FeatureGate
import com.fakturus.track.services.subscription.SubscriptionManager

@Composable
fun FeatureLockedCard(
    feature: FeatureGate,
    subscriptionManager: SubscriptionManager,
    onShowPaywall: (FeatureGate) -> Unit,
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit
) {
    val tier by subscriptionManager.tier.collectAsState()

    if (feature.requiredTier <= tier) {
        content()
    } else {
        Box(
            modifier = modifier
                .alpha(0.6f)
                .clickable { onShowPaywall(feature) }
        ) {
            content()
            Icon(
                imageVector = Icons.Default.Lock,
                contentDescription = null,
                modifier = Modifier
                    .align(Alignment.TopEnd)
                    .padding(4.dp)
                    .size(16.dp),
                tint = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}
