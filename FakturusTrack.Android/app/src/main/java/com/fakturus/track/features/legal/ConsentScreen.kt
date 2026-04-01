package com.fakturus.track.features.legal

import android.content.Intent
import android.net.Uri
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.OpenInNew
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.fakturus.track.R

@Composable
fun ConsentScreen(
    onConsented: () -> Unit,
    onDeclined: () -> Unit
) {
    val context = LocalContext.current
    var agreedToTerms by remember { mutableStateOf(false) }
    var openedPrivacy by remember { mutableStateOf(false) }
    var showDeclineDialog by remember { mutableStateOf(false) }

    // Block back button
    BackHandler { showDeclineDialog = true }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Spacer(Modifier.height(48.dp))

        // Header
        Icon(
            Icons.Default.Description,
            contentDescription = null,
            modifier = Modifier.size(56.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(Modifier.height(16.dp))
        Text(
            text = stringResource(R.string.consent_title),
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            textAlign = TextAlign.Center
        )
        Spacer(Modifier.height(8.dp))
        Text(
            text = stringResource(R.string.consent_subtitle),
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center
        )

        Spacer(Modifier.height(32.dp))

        // Privacy Policy — Kenntnisnahme
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Checkbox(checked = openedPrivacy, onCheckedChange = null, enabled = false)
                    Column(modifier = Modifier.padding(start = 8.dp)) {
                        Text(
                            stringResource(R.string.consent_privacy_label),
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            stringResource(R.string.consent_privacy_hint),
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                Spacer(Modifier.height(8.dp))
                TextButton(
                    onClick = {
                        openedPrivacy = true
                        context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse("https://track.fakturus.com/privacy")))
                    },
                    modifier = Modifier.padding(start = 40.dp)
                ) {
                    Icon(Icons.Default.OpenInNew, contentDescription = null, modifier = Modifier.size(16.dp))
                    Text(stringResource(R.string.consent_read_privacy), modifier = Modifier.padding(start = 4.dp))
                }
            }
        }

        Spacer(Modifier.height(16.dp))

        // Terms — Active Checkbox (BGB §305)
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier.clickable { agreedToTerms = !agreedToTerms }
                ) {
                    Checkbox(checked = agreedToTerms, onCheckedChange = { agreedToTerms = it })
                    Column(modifier = Modifier.padding(start = 8.dp)) {
                        Text(
                            stringResource(R.string.consent_terms_label),
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            stringResource(R.string.consent_terms_hint),
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                Spacer(Modifier.height(8.dp))
                TextButton(
                    onClick = {
                        context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse("https://track.fakturus.com/terms")))
                    },
                    modifier = Modifier.padding(start = 40.dp)
                ) {
                    Icon(Icons.Default.OpenInNew, contentDescription = null, modifier = Modifier.size(16.dp))
                    Text(stringResource(R.string.consent_read_terms), modifier = Modifier.padding(start = 4.dp))
                }
            }
        }

        Spacer(Modifier.height(32.dp))

        // Proceed
        Button(
            onClick = onConsented,
            enabled = agreedToTerms && openedPrivacy,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(stringResource(R.string.consent_agree_button), modifier = Modifier.padding(vertical = 8.dp))
        }

        Spacer(Modifier.height(8.dp))

        // Decline
        TextButton(onClick = { showDeclineDialog = true }) {
            Text(
                stringResource(R.string.consent_decline_button),
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }

    // Decline dialog
    if (showDeclineDialog) {
        AlertDialog(
            onDismissRequest = { showDeclineDialog = false },
            title = { Text(stringResource(R.string.consent_decline_title)) },
            text = { Text(stringResource(R.string.consent_decline_message)) },
            confirmButton = {
                TextButton(onClick = {
                    showDeclineDialog = false
                    onDeclined()
                }) { Text(stringResource(R.string.consent_decline_logout)) }
            },
            dismissButton = {
                TextButton(onClick = { showDeclineDialog = false }) {
                    Text(stringResource(R.string.consent_decline_back))
                }
            }
        )
    }
}
