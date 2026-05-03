package com.fakturus.track.features.settings

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.automirrored.filled.OpenInNew
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fakturus.track.R
import com.fakturus.track.ServiceContainer
import com.fakturus.track.features.subscription.PaywallBottomSheet
import com.fakturus.track.services.subscription.FeatureGate
import com.fakturus.track.services.subscription.Tier
import com.fakturus.track.models.UserSettingsHistoryEntryDTO
import com.fakturus.track.ui.shared.BundeslandPicker
import com.fakturus.track.ui.shared.FeatureLockedCard
import com.fakturus.track.ui.shared.WorkdaySelector
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    services: ServiceContainer,
    onLogout: () -> Unit,
    onNavigateToSchoolHolidays: () -> Unit = {}
) {
    val context = LocalContext.current
    val viewModel: SettingsViewModel = viewModel(
        factory = SettingsViewModelFactory(services, context)
    )
    val settings by viewModel.settings.collectAsState()
    val isSaving by viewModel.isSaving.collectAsState()
    val appearance by viewModel.appearance.collectAsState()
    val notificationsEnabled by viewModel.notificationsEnabled.collectAsState()
    val effectiveDate by viewModel.effectiveDate.collectAsState()
    val hasUnsyncedHistorizedChange by viewModel.hasUnsyncedHistorizedChange.collectAsState()
    val settingsHistory by viewModel.settingsHistory.collectAsState()
    val isLoadingHistory by viewModel.isLoadingHistory.collectAsState()
    val historyError by viewModel.historyError.collectAsState()
    val uriHandler = LocalUriHandler.current
    val tier by services.subscriptionManager.tier.collectAsState()
    var paywallFeature by remember { mutableStateOf<FeatureGate?>(null) }
    var showHistorySheet by remember { mutableStateOf(false) }
    val activity = context as? android.app.Activity
    val focusManager = LocalFocusManager.current
    val keyboardController = LocalSoftwareKeyboardController.current

    fun dismissKeyboard() {
        focusManager.clearFocus()
        keyboardController?.hide()
    }

    LaunchedEffect(Unit) {
        viewModel.initializeSettingsIfNeeded()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.settings_tab_title)) }
            )
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Section: Arbeitszeit
            item {
                Spacer(Modifier.height(8.dp))
                Text(
                    text = stringResource(R.string.settings_section_worktime),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                var hoursText by remember(settings?.workHoursPerWeek) {
                    mutableStateOf(settings?.workHoursPerWeek?.toString() ?: "40.0")
                }
                OutlinedTextField(
                    value = hoursText,
                    onValueChange = { newValue ->
                        hoursText = newValue
                        newValue.toDoubleOrNull()?.let { viewModel.updateWorkHoursPerWeek(it) }
                    },
                    label = { Text(stringResource(R.string.settings_work_hours)) },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Decimal,
                        imeAction = ImeAction.Done
                    ),
                    keyboardActions = KeyboardActions(onDone = { dismissKeyboard() }),
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            item {
                Text(
                    text = stringResource(R.string.settings_work_days),
                    style = MaterialTheme.typography.bodyMedium
                )
                Spacer(Modifier.height(4.dp))
                WorkdaySelector(
                    workDays = settings?.workDays ?: 31,
                    onWorkDaysChange = { viewModel.updateWorkDays(it) }
                )
            }

            // Stage 2: "Gültig ab" picker, latched in the VM so it doesn't
            // disappear during the debounced save / sync round-trip.
            val hasUnsyncedHistorized = hasUnsyncedHistorizedChange
            if (hasUnsyncedHistorized) {
                item {
                    EffectiveDateRow(
                        currentValue = effectiveDate,
                        onValueChange = { viewModel.updateEffectiveDate(it) }
                    )
                }
            }

            // History entry point
            item {
                Spacer(Modifier.height(4.dp))
                OutlinedButton(
                    onClick = {
                        showHistorySheet = true
                        viewModel.loadSettingsHistory()
                    },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Verlauf der Arbeitstage anzeigen")
                }
            }

            item {
                HorizontalDivider()
            }

            // Section: Urlaub
            item {
                Text(
                    text = stringResource(R.string.settings_section_vacation),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                var daysText by remember(settings?.vacationDaysPerYear) {
                    mutableStateOf(settings?.vacationDaysPerYear?.toString() ?: "30")
                }
                OutlinedTextField(
                    value = daysText,
                    onValueChange = { newValue ->
                        daysText = newValue
                        newValue.toIntOrNull()?.let { viewModel.updateVacationDaysPerYear(it) }
                    },
                    label = { Text(stringResource(R.string.settings_vacation_days)) },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Number,
                        imeAction = ImeAction.Done
                    ),
                    keyboardActions = KeyboardActions(onDone = { dismissKeyboard() }),
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
            }

            item {
                HorizontalDivider()
            }

            // Section: Region
            item {
                Text(
                    text = stringResource(R.string.settings_section_region),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                BundeslandPicker(
                    selectedBundesland = settings?.bundesland ?: "NW",
                    onBundeslandChange = { viewModel.updateBundesland(it) }
                )
            }

            item {
                HorizontalDivider()
            }

            // Section: Schulferien
            item {
                Text(
                    text = stringResource(R.string.settings_section_school_holidays),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                FeatureLockedCard(
                    feature = FeatureGate.SCHOOL_HOLIDAYS,
                    subscriptionManager = services.subscriptionManager,
                    onShowPaywall = { paywallFeature = it }
                ) {
                    Button(
                        onClick = onNavigateToSchoolHolidays,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text(stringResource(R.string.settings_school_holidays_manage))
                    }
                }
            }

            item {
                HorizontalDivider()
            }

            // Section: Kalender
            item {
                Text(
                    text = stringResource(R.string.settings_section_calendar),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                FeatureLockedCard(
                    feature = FeatureGate.CALENDAR_INTEGRATION,
                    subscriptionManager = services.subscriptionManager,
                    onShowPaywall = { paywallFeature = it }
                ) {
                    var urlText by remember(settings?.calendarUrl) {
                        mutableStateOf(settings?.calendarUrl ?: "")
                    }
                    OutlinedTextField(
                        value = urlText,
                        onValueChange = { newValue ->
                            urlText = newValue
                            viewModel.updateCalendarUrl(newValue.ifBlank { null })
                        },
                        label = { Text(stringResource(R.string.settings_calendar_url)) },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                }
            }

            item {
                HorizontalDivider()
            }

            // Section: APP
            item {
                Text(
                    text = stringResource(R.string.settings_section_app),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            // Erscheinungsbild
            item {
                var expanded by remember { mutableStateOf(false) }
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_appearance)) },
                    trailingContent = {
                        TextButton(onClick = { expanded = true }) {
                            Text(
                                when (appearance) {
                                    "light" -> stringResource(R.string.settings_appearance_light)
                                    "dark" -> stringResource(R.string.settings_appearance_dark)
                                    else -> stringResource(R.string.settings_appearance_system)
                                }
                            )
                        }
                        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.settings_appearance_system)) },
                                onClick = { viewModel.setAppearance("system"); expanded = false }
                            )
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.settings_appearance_light)) },
                                onClick = { viewModel.setAppearance("light"); expanded = false }
                            )
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.settings_appearance_dark)) },
                                onClick = { viewModel.setAppearance("dark"); expanded = false }
                            )
                        }
                    }
                )
            }

            // Benachrichtigungen
            item {
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_notifications)) },
                    trailingContent = {
                        Switch(
                            checked = notificationsEnabled,
                            onCheckedChange = { viewModel.setNotifications(it) }
                        )
                    }
                )
            }

            // Personalnummer
            item {
                var personalNumberText by remember(settings?.personalNumber) {
                    mutableStateOf(settings?.personalNumber ?: "")
                }
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_personal_number)) },
                    trailingContent = {
                        OutlinedTextField(
                            value = personalNumberText,
                            onValueChange = { newValue ->
                                personalNumberText = newValue
                                viewModel.updatePersonalNumber(newValue)
                            },
                            placeholder = { Text("12345") },
                            singleLine = true,
                            keyboardOptions = KeyboardOptions(
                                keyboardType = KeyboardType.Number,
                                imeAction = ImeAction.Done
                            ),
                            keyboardActions = KeyboardActions(onDone = { dismissKeyboard() }),
                            modifier = Modifier.width(120.dp)
                        )
                    }
                )
            }

            item {
                HorizontalDivider()
            }

            // Section: Abo
            item {
                Text(
                    text = stringResource(R.string.settings_section_subscription),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            item {
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_current_tier)) },
                    trailingContent = {
                        Text(
                            text = tier.displayName,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                )
            }

            item {
                TextButton(
                    onClick = { paywallFeature = FeatureGate.PDF_EXPORT },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(stringResource(R.string.settings_manage_subscription))
                }
            }

            item {
                val isRestoring by viewModel.isRestoringPurchases.collectAsState()
                val restoreResult by viewModel.restoreResult.collectAsState()
                Column {
                    TextButton(
                        onClick = { viewModel.restorePurchases() },
                        enabled = !isRestoring,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text(stringResource(R.string.settings_restore_purchases))
                    }
                    if (restoreResult != null) {
                        Text(
                            text = restoreResult ?: "",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.padding(horizontal = 16.dp)
                        )
                    }
                }
            }

            item {
                HorizontalDivider()
            }

            // Section: INFO
            item {
                Text(
                    text = stringResource(R.string.settings_section_info),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            // Version
            item {
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_version)) },
                    trailingContent = {
                        Text(
                            text = viewModel.appVersion,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                )
            }

            // Datenschutz
            item {
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_privacy)) },
                    trailingContent = { Icon(Icons.AutoMirrored.Default.OpenInNew, contentDescription = null) },
                    modifier = Modifier.clickable {
                        uriHandler.openUri("https://track.fakturus.com/privacy")
                    }
                )
            }

            // Impressum
            item {
                ListItem(
                    headlineContent = { Text(stringResource(R.string.settings_imprint)) },
                    trailingContent = { Icon(Icons.AutoMirrored.Default.OpenInNew, contentDescription = null) },
                    modifier = Modifier.clickable {
                        uriHandler.openUri("https://track.fakturus.com/imprint")
                    }
                )
            }

            item {
                HorizontalDivider()
            }

            // Logout
            item {
                Button(
                    onClick = onLogout,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    ),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = null)
                        Text(stringResource(R.string.settings_logout))
                    }
                }
                Spacer(Modifier.height(8.dp))
            }

            // Delete Account
            item {
                var showDeleteDialog by remember { mutableStateOf(false) }

                OutlinedButton(
                    onClick = { showDeleteDialog = true },
                    colors = ButtonDefaults.outlinedButtonColors(
                        contentColor = MaterialTheme.colorScheme.error
                    ),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Icon(Icons.Default.Delete, contentDescription = null)
                        Text(stringResource(R.string.settings_delete_account))
                    }
                }

                if (showDeleteDialog) {
                    AlertDialog(
                        onDismissRequest = { showDeleteDialog = false },
                        title = { Text(stringResource(R.string.delete_account_title)) },
                        text = { Text(stringResource(R.string.delete_account_message)) },
                        confirmButton = {
                            TextButton(
                                onClick = {
                                    showDeleteDialog = false
                                    // Account deletion handled via coroutine in ViewModel
                                    viewModel.deleteAccount(
                                        onSuccess = {
                                            services.consentManager.clearConsent()
                                            onLogout()
                                        }
                                    )
                                },
                                colors = ButtonDefaults.textButtonColors(
                                    contentColor = MaterialTheme.colorScheme.error
                                )
                            ) { Text(stringResource(R.string.delete_account_confirm)) }
                        },
                        dismissButton = {
                            TextButton(onClick = { showDeleteDialog = false }) {
                                Text(stringResource(R.string.delete_account_cancel))
                            }
                        }
                    )
                }

                Spacer(Modifier.height(32.dp))
            }
        }
    }

    // Paywall Bottom Sheet
    if (paywallFeature != null && activity != null) {
        PaywallBottomSheet(
            highlightedFeature = paywallFeature,
            billingManager = services.billingManager,
            subscriptionManager = services.subscriptionManager,
            activity = activity,
            onDismiss = { paywallFeature = null }
        )
    }

    if (showHistorySheet) {
        WorkSettingsHistorySheet(
            entries = settingsHistory,
            isLoading = isLoadingHistory,
            error = historyError,
            onRetry = { viewModel.loadSettingsHistory() },
            onDismiss = { showHistorySheet = false }
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun WorkSettingsHistorySheet(
    entries: List<UserSettingsHistoryEntryDTO>,
    isLoading: Boolean,
    error: String?,
    onRetry: () -> Unit,
    onDismiss: () -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)
    val dateFormatter = remember {
        DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM).withLocale(Locale.GERMANY)
    }
    val dayLabels = listOf("Mo", "Di", "Mi", "Do", "Fr", "Sa", "So")

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = "Verlauf der Arbeitstage",
                style = MaterialTheme.typography.titleLarge
            )
            Text(
                text = "Wochentage und Wochenstunden, die für die Soll-Berechnung galten. Neueste Änderung zuerst.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            HorizontalDivider()

            when {
                isLoading -> {
                    Text("Lädt …", style = MaterialTheme.typography.bodyMedium)
                }
                error != null -> {
                    Text(error, color = MaterialTheme.colorScheme.error)
                    OutlinedButton(onClick = onRetry) { Text("Erneut versuchen") }
                }
                entries.isEmpty() -> {
                    Text("Noch keine Änderungen erfasst.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
                else -> {
                    entries.forEach { entry ->
                        val from = runCatching { LocalDate.parse(entry.validFrom).format(dateFormatter) }.getOrDefault(entry.validFrom)
                        val to = entry.validTo?.let {
                            runCatching { LocalDate.parse(it).format(dateFormatter) }.getOrDefault(it)
                        } ?: "aktuell"
                        val days = (0 until 7)
                            .filter { (entry.workDays shr it) and 1 == 1 }
                            .map { dayLabels[it] }
                            .joinToString(", ")
                            .ifEmpty { "—" }

                        Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                            Text(
                                "$from – $to",
                                style = MaterialTheme.typography.titleSmall
                            )
                            Text(
                                "Tage: $days",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                "${"%.1f".format(entry.workHoursPerWeek)} h / Woche",
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
                        HorizontalDivider()
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun EffectiveDateRow(
    currentValue: LocalDate,
    onValueChange: (LocalDate) -> Unit
) {
    var showDialog by remember { mutableStateOf(false) }
    val formatter = remember {
        DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM).withLocale(Locale.GERMANY)
    }

    Column {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .clickable { showDialog = true }
                .padding(vertical = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = "Gültig ab",
                style = MaterialTheme.typography.bodyMedium
            )
            Text(
                text = currentValue.format(formatter),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.primary
            )
        }
        Text(
            text = "Standard: heute. Bei Korrekturen vergangener Wochen kann ein früheres Datum gewählt werden — Soll-Stunden werden ab dann mit den neuen Werten berechnet.",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }

    if (showDialog) {
        val initialMillis = currentValue.atStartOfDay(ZoneId.systemDefault())
            .toInstant().toEpochMilli()
        val datePickerState = rememberDatePickerState(
            initialSelectedDateMillis = initialMillis
        )
        DatePickerDialog(
            onDismissRequest = { showDialog = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        val date = Instant.ofEpochMilli(millis)
                            .atZone(ZoneId.systemDefault())
                            .toLocalDate()
                        onValueChange(date)
                    }
                    showDialog = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showDialog = false }) { Text("Abbrechen") }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }
}
