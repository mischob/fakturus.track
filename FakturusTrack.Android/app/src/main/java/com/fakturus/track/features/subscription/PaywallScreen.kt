package com.fakturus.track.features.subscription

import android.app.Activity
import android.os.VibrationEffect
import android.os.Vibrator
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Remove
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fakturus.track.R
import com.fakturus.track.services.subscription.BillingManager
import com.fakturus.track.services.subscription.FeatureGate
import com.fakturus.track.services.subscription.SubscriptionManager
import com.fakturus.track.services.subscription.Tier
import kotlinx.coroutines.delay

data class PaywallFeature(
    val nameRes: Int,
    val gate: FeatureGate?,
    val free: Boolean,
    val starter: Boolean,
    val pro: Boolean
)

private val paywallFeatures = listOf(
    PaywallFeature(R.string.paywall_feature_timer, null, free = true, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_history_30, null, free = true, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_holidays, null, free = true, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_overtime, null, free = true, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_widgets, null, free = true, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_history_unlimited, FeatureGate.CSV_EXPORT, free = false, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_pdf_export, FeatureGate.PDF_EXPORT, free = false, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_csv_export, FeatureGate.CSV_EXPORT, free = false, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_vacation_sick, FeatureGate.VACATION, free = false, starter = true, pro = true),
    PaywallFeature(R.string.paywall_feature_datev_export, FeatureGate.DATEV_EXPORT, free = false, starter = false, pro = true),
    PaywallFeature(R.string.paywall_feature_school_holidays, FeatureGate.SCHOOL_HOLIDAYS, free = false, starter = false, pro = true),
    PaywallFeature(R.string.paywall_feature_calendar, FeatureGate.CALENDAR_INTEGRATION, free = false, starter = false, pro = true),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PaywallBottomSheet(
    highlightedFeature: FeatureGate? = null,
    billingManager: BillingManager,
    subscriptionManager: SubscriptionManager,
    activity: Activity,
    onDismiss: () -> Unit
) {
    val viewModel: PaywallViewModel = viewModel(
        factory = PaywallViewModelFactory(billingManager, subscriptionManager)
    )
    val products by viewModel.products.collectAsState()
    val tier by viewModel.tier.collectAsState()
    val isPurchasing by viewModel.isPurchasing.collectAsState()
    val showSuccess by viewModel.showSuccess.collectAsState()
    val context = LocalContext.current
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    // Success detection via tier change
    LaunchedEffect(tier) {
        viewModel.onTierChanged(tier)
        if (showSuccess) {
            val vibrator = context.getSystemService(Vibrator::class.java)
            vibrator?.vibrate(VibrationEffect.createOneShot(100, VibrationEffect.DEFAULT_AMPLITUDE))
            delay(1500)
            onDismiss()
        }
    }

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState
    ) {
        if (showSuccess) {
            // Success overlay
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(48.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Icon(
                    imageVector = Icons.Default.Check,
                    contentDescription = null,
                    modifier = Modifier.size(72.dp),
                    tint = MaterialTheme.colorScheme.primary
                )
                Spacer(Modifier.height(16.dp))
                Text(
                    text = stringResource(R.string.paywall_success, tier.displayName),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center
                )
            }
        } else {
            LazyColumn(
                modifier = Modifier.padding(horizontal = 16.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // Header
                item {
                    Column(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Icon(
                            imageVector = Icons.Default.Star,
                            contentDescription = null,
                            modifier = Modifier.size(56.dp),
                            tint = MaterialTheme.colorScheme.primary
                        )
                        Spacer(Modifier.height(8.dp))
                        Text(
                            text = stringResource(R.string.paywall_headline),
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold,
                            textAlign = TextAlign.Center
                        )
                        Spacer(Modifier.height(4.dp))
                        Text(
                            text = stringResource(R.string.paywall_subheadline),
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            textAlign = TextAlign.Center
                        )
                    }
                }

                // Feature comparison table
                item {
                    FeatureComparisonTable(highlightedFeature = highlightedFeature)
                }

                // Purchase buttons with dynamic prices
                item {
                    Column(
                        modifier = Modifier.fillMaxWidth(),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        if (isPurchasing) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.Center
                            ) {
                                CircularProgressIndicator(modifier = Modifier.size(24.dp))
                            }
                        }

                        products.forEach { productDetails ->
                            val price = viewModel.getFormattedPrice(productDetails)
                            val isStarter = productDetails.productId == "starter_monthly"

                            if (isStarter) {
                                OutlinedButton(
                                    onClick = {
                                        viewModel.purchase(activity, productDetails)
                                    },
                                    modifier = Modifier.fillMaxWidth(),
                                    enabled = !isPurchasing
                                ) {
                                    Row(
                                        modifier = Modifier.fillMaxWidth(),
                                        horizontalArrangement = Arrangement.SpaceBetween
                                    ) {
                                        Text(stringResource(R.string.paywall_subscribe_starter))
                                        Text(
                                            "$price / ${stringResource(R.string.paywall_month)}",
                                            fontWeight = FontWeight.Bold
                                        )
                                    }
                                }
                            } else {
                                Button(
                                    onClick = {
                                        viewModel.purchase(activity, productDetails)
                                    },
                                    modifier = Modifier.fillMaxWidth(),
                                    enabled = !isPurchasing
                                ) {
                                    Row(
                                        modifier = Modifier.fillMaxWidth(),
                                        horizontalArrangement = Arrangement.SpaceBetween
                                    ) {
                                        Text(stringResource(R.string.paywall_subscribe_pro))
                                        Text(
                                            "$price / ${stringResource(R.string.paywall_month)}",
                                            fontWeight = FontWeight.Bold
                                        )
                                    }
                                }
                            }
                        }
                    }
                }

                // Footer: Restore + Legal
                item {
                    Column(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        TextButton(onClick = { viewModel.restorePurchases() }) {
                            Text(stringResource(R.string.paywall_restore))
                        }
                        Text(
                            text = stringResource(R.string.paywall_auto_renew_notice),
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            textAlign = TextAlign.Center
                        )
                        Spacer(Modifier.height(4.dp))
                        Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                            TextButton(onClick = {
                                // Open terms URL
                            }) {
                                Text(
                                    stringResource(R.string.paywall_terms),
                                    style = MaterialTheme.typography.labelSmall
                                )
                            }
                            TextButton(onClick = {
                                // Open privacy URL
                            }) {
                                Text(
                                    stringResource(R.string.paywall_privacy),
                                    style = MaterialTheme.typography.labelSmall
                                )
                            }
                        }
                    }
                }

                // Bottom spacing
                item { Spacer(Modifier.height(32.dp)) }
            }
        }
    }
}

@Composable
private fun FeatureComparisonTable(highlightedFeature: FeatureGate?) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(
                MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.3f),
                RoundedCornerShape(12.dp)
            )
    ) {
        // Header row
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(
                    MaterialTheme.colorScheme.surfaceVariant,
                    RoundedCornerShape(topStart = 12.dp, topEnd = 12.dp)
                )
                .padding(horizontal = 12.dp, vertical = 8.dp)
        ) {
            Text(
                text = stringResource(R.string.paywall_feature),
                style = MaterialTheme.typography.labelSmall,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.weight(1f)
            )
            Text(
                text = "Free",
                style = MaterialTheme.typography.labelSmall,
                fontWeight = FontWeight.Bold,
                textAlign = TextAlign.Center,
                modifier = Modifier.weight(0.4f)
            )
            Text(
                text = "Starter",
                style = MaterialTheme.typography.labelSmall,
                fontWeight = FontWeight.Bold,
                textAlign = TextAlign.Center,
                modifier = Modifier.weight(0.5f)
            )
            Text(
                text = "Pro",
                style = MaterialTheme.typography.labelSmall,
                fontWeight = FontWeight.Bold,
                textAlign = TextAlign.Center,
                modifier = Modifier.weight(0.4f)
            )
        }

        HorizontalDivider()

        // Feature rows
        paywallFeatures.forEach { feature ->
            val isHighlighted = feature.gate == highlightedFeature
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .then(
                        if (isHighlighted)
                            Modifier.background(MaterialTheme.colorScheme.primary.copy(alpha = 0.08f))
                        else Modifier
                    )
                    .padding(horizontal = 12.dp, vertical = 8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = stringResource(feature.nameRes),
                    style = MaterialTheme.typography.bodySmall,
                    fontWeight = if (isHighlighted) FontWeight.Bold else FontWeight.Normal,
                    modifier = Modifier.weight(1f)
                )
                TierCheck(available = feature.free, modifier = Modifier.weight(0.4f))
                TierCheck(available = feature.starter, modifier = Modifier.weight(0.5f))
                TierCheck(available = feature.pro, modifier = Modifier.weight(0.4f))
            }
            HorizontalDivider()
        }
    }
}

@Composable
private fun TierCheck(available: Boolean, modifier: Modifier = Modifier) {
    Row(
        modifier = modifier,
        horizontalArrangement = Arrangement.Center
    ) {
        if (available) {
            Icon(
                imageVector = Icons.Default.Check,
                contentDescription = null,
                modifier = Modifier.size(16.dp),
                tint = MaterialTheme.colorScheme.primary
            )
        } else {
            Icon(
                imageVector = Icons.Default.Remove,
                contentDescription = null,
                modifier = Modifier.size(16.dp),
                tint = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.3f)
            )
        }
    }
}
