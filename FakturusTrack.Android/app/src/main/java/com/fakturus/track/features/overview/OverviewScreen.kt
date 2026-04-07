package com.fakturus.track.features.overview

import android.content.Intent
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.MedicalServices
import androidx.compose.material.icons.filled.PictureAsPdf
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material.icons.filled.Share
import androidx.compose.material.icons.filled.WbSunny
import androidx.compose.material3.AssistChip
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fakturus.track.R
import com.fakturus.track.ServiceContainer
import com.fakturus.track.features.subscription.PaywallBottomSheet
import com.fakturus.track.services.subscription.FeatureGate
import com.fakturus.track.ui.shared.ErrorBanner
import com.fakturus.track.ui.shared.FeatureLockedCard
import com.fakturus.track.ui.shared.OvertimeCard
import com.fakturus.track.ui.shared.ShimmerCardPlaceholder
import com.fakturus.track.ui.shared.ShimmerOvertimeCards
import java.time.LocalDate
import java.time.Month
import java.time.format.TextStyle
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OverviewScreen(services: ServiceContainer) {
    val context = LocalContext.current
    val viewModel: OverviewViewModel = viewModel(
        factory = OverviewViewModelFactory(services, context)
    )
    val state by viewModel.uiState.collectAsState()
    var paywallFeature by remember { mutableStateOf<FeatureGate?>(null) }
    val activity = context as? android.app.Activity

    // Share exported file
    LaunchedEffect(state.exportedFile) {
        val file = state.exportedFile ?: return@LaunchedEffect
        val mimeType = state.exportMimeType ?: return@LaunchedEffect

        try {
            val uri = FileProvider.getUriForFile(
                context,
                "${context.packageName}.fileprovider",
                file
            )
            val intent = Intent(Intent.ACTION_SEND).apply {
                type = mimeType
                putExtra(Intent.EXTRA_STREAM, uri)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            context.startActivity(Intent.createChooser(intent, context.getString(R.string.overview_export_share)))
        } catch (e: Exception) {
            // FileProvider not configured or share failed
        }
        viewModel.clearExport()
    }

    Scaffold(
        topBar = {
            TopAppBar(title = { Text(stringResource(R.string.overview_tab_title)) })
        }
    ) { innerPadding ->
        PullToRefreshBox(
            isRefreshing = state.isLoading,
            onRefresh = { viewModel.refresh() },
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            if (state.isLoading && state.summary == null) {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(horizontal = 16.dp)
                ) {
                    Spacer(Modifier.height(8.dp))
                    ShimmerOvertimeCards()
                    Spacer(Modifier.height(16.dp))
                    ShimmerCardPlaceholder()
                    Spacer(Modifier.height(8.dp))
                    ShimmerCardPlaceholder()
                }
            } else if (state.error != null && state.summary == null) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(16.dp),
                    contentAlignment = Alignment.Center
                ) {
                    ErrorBanner(
                        message = state.error ?: "",
                        actionLabel = stringResource(R.string.error_retry),
                        onAction = { viewModel.refresh() }
                    )
                }
            } else {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .verticalScroll(rememberScrollState())
                        .padding(horizontal = 16.dp)
                ) {
                    Spacer(Modifier.height(8.dp))

                    // Summary Cards
                    val summary = state.summary
                    if (summary != null) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .horizontalScroll(rememberScrollState()),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            val overtimeColor = if (summary.totalOvertimeHours >= 0)
                                MaterialTheme.colorScheme.primary
                            else
                                MaterialTheme.colorScheme.error

                            OvertimeCard(
                                title = stringResource(R.string.overview_overtime),
                                value = formatHours(summary.totalOvertimeHours),
                                icon = Icons.Default.Schedule,
                                valueColor = overtimeColor
                            )
                            OvertimeCard(
                                title = stringResource(R.string.overview_vacation),
                                value = "${summary.vacationDaysTaken} / ${summary.vacationDaysPerYear}",
                                icon = Icons.Default.WbSunny,
                                valueColor = MaterialTheme.colorScheme.onSurface
                            )
                            OvertimeCard(
                                title = stringResource(R.string.overview_holidays),
                                value = "${summary.holidaysTaken}",
                                icon = Icons.Default.CalendarToday,
                                valueColor = MaterialTheme.colorScheme.onSurface
                            )
                            OvertimeCard(
                                title = stringResource(R.string.overview_sick_days),
                                value = "${summary.sickDaysTaken}",
                                icon = Icons.Default.MedicalServices,
                                valueColor = MaterialTheme.colorScheme.error
                            )
                        }
                    }

                    Spacer(Modifier.height(16.dp))

                    // Year navigation
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.Center,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        TextButton(onClick = { viewModel.selectYear(state.selectedYear - 1) }) {
                            Text("${state.selectedYear - 1}")
                        }
                        Text(
                            text = "${state.selectedYear}",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier.padding(horizontal = 16.dp)
                        )
                        TextButton(onClick = { viewModel.selectYear(state.selectedYear + 1) }) {
                            Text("${state.selectedYear + 1}")
                        }
                    }

                    Spacer(Modifier.height(8.dp))

                    // Monthly overtime table
                    if (summary != null) {
                        MonthlyOvertimeTable(months = summary.monthlyOvertime)
                    }

                    // Cache hint
                    if (state.isShowingCachedData && state.lastUpdated != null) {
                        Spacer(Modifier.height(8.dp))
                        Text(
                            text = stringResource(R.string.overview_last_updated, state.lastUpdated ?: ""),
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }

                    // Export Section
                    Spacer(Modifier.height(24.dp))
                    ExportSection(
                        selectedYear = state.selectedYear,
                        isExporting = state.isExporting,
                        subscriptionManager = services.subscriptionManager,
                        onShowPaywall = { feature -> paywallFeature = feature },
                        onGeneratePDF = { month -> viewModel.generatePDF(month, state.selectedYear) },
                        onGenerateCSV = { range -> viewModel.generateCSV(range) },
                        onGenerateDATEV = { month -> viewModel.generateDATEV(month) }
                    )

                    Spacer(Modifier.height(32.dp))
                }
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
}

@Composable
private fun ExportSection(
    selectedYear: Int,
    isExporting: Boolean,
    subscriptionManager: com.fakturus.track.services.subscription.SubscriptionManager,
    onShowPaywall: (FeatureGate) -> Unit,
    onGeneratePDF: (Int) -> Unit,
    onGenerateCSV: (CSVRange) -> Unit,
    onGenerateDATEV: (Int) -> Unit
) {
    ElevatedCard(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = stringResource(R.string.overview_export),
                style = MaterialTheme.typography.titleSmall
            )
            Spacer(Modifier.height(12.dp))

            if (isExporting) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    CircularProgressIndicator(modifier = Modifier.height(20.dp).width(20.dp))
                    Text(stringResource(R.string.overview_export_creating), style = MaterialTheme.typography.bodySmall)
                }
            } else {
                // PDF Export
                FeatureLockedCard(
                    feature = FeatureGate.PDF_EXPORT,
                    subscriptionManager = subscriptionManager,
                    onShowPaywall = onShowPaywall
                ) {
                    Column {
                        Text(
                            text = stringResource(R.string.overview_export_pdf_label),
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Spacer(Modifier.height(4.dp))

                        var showPdfMonthPicker by remember { mutableStateOf(false) }
                        Box {
                            OutlinedButton(onClick = { showPdfMonthPicker = true }) {
                                Icon(Icons.Default.PictureAsPdf, null, modifier = Modifier.padding(end = 4.dp))
                                Text(stringResource(R.string.overview_export_select_month))
                            }
                            DropdownMenu(
                                expanded = showPdfMonthPicker,
                                onDismissRequest = { showPdfMonthPicker = false }
                            ) {
                                val currentMonth = LocalDate.now().monthValue
                                val months = if (selectedYear == LocalDate.now().year) {
                                    (1..currentMonth).toList()
                                } else {
                                    (1..12).toList()
                                }
                                months.reversed().forEach { month ->
                                    val monthName = Month.of(month).getDisplayName(TextStyle.FULL, Locale.GERMAN)
                                    DropdownMenuItem(
                                        text = { Text("$monthName $selectedYear") },
                                        onClick = {
                                            showPdfMonthPicker = false
                                            onGeneratePDF(month)
                                        }
                                    )
                                }
                            }
                        }
                    }
                }

                Spacer(Modifier.height(16.dp))

                // CSV Export
                FeatureLockedCard(
                    feature = FeatureGate.CSV_EXPORT,
                    subscriptionManager = subscriptionManager,
                    onShowPaywall = onShowPaywall
                ) {
                    Column {
                        Text(
                            text = stringResource(R.string.overview_export_csv_label),
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Spacer(Modifier.height(4.dp))

                        Row(
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                            modifier = Modifier.horizontalScroll(rememberScrollState())
                        ) {
                            // Month picker
                            var showCsvMonthPicker by remember { mutableStateOf(false) }
                            Box {
                                AssistChip(
                                    onClick = { showCsvMonthPicker = true },
                                    label = { Text(stringResource(R.string.overview_month)) },
                                    leadingIcon = { Icon(Icons.Default.Description, null) }
                                )
                                DropdownMenu(
                                    expanded = showCsvMonthPicker,
                                    onDismissRequest = { showCsvMonthPicker = false }
                                ) {
                                    val currentMonth = LocalDate.now().monthValue
                                    val months = if (selectedYear == LocalDate.now().year) {
                                        (1..currentMonth).toList()
                                    } else {
                                        (1..12).toList()
                                    }
                                    months.reversed().forEach { month ->
                                        val monthName = Month.of(month).getDisplayName(TextStyle.FULL, Locale.GERMAN)
                                        DropdownMenuItem(
                                            text = { Text("$monthName $selectedYear") },
                                            onClick = {
                                                showCsvMonthPicker = false
                                                onGenerateCSV(CSVRange.Month(month))
                                            }
                                        )
                                    }
                                }
                            }

                            // Quarter picker
                            var showQuarterPicker by remember { mutableStateOf(false) }
                            Box {
                                AssistChip(
                                    onClick = { showQuarterPicker = true },
                                    label = { Text(stringResource(R.string.overview_export_quarter)) },
                                    leadingIcon = { Icon(Icons.Default.Description, null) }
                                )
                                DropdownMenu(
                                    expanded = showQuarterPicker,
                                    onDismissRequest = { showQuarterPicker = false }
                                ) {
                                    (1..4).forEach { quarter ->
                                        DropdownMenuItem(
                                            text = { Text("Q$quarter $selectedYear") },
                                            onClick = {
                                                showQuarterPicker = false
                                                onGenerateCSV(CSVRange.Quarter(quarter))
                                            }
                                        )
                                    }
                                }
                            }

                            // Full year
                            AssistChip(
                                onClick = { onGenerateCSV(CSVRange.Year) },
                                label = { Text(stringResource(R.string.overview_export_year, selectedYear)) },
                                leadingIcon = { Icon(Icons.Default.Description, null) }
                            )
                        }
                    }
                }

                Spacer(Modifier.height(16.dp))

                // DATEV Export
                FeatureLockedCard(
                    feature = FeatureGate.DATEV_EXPORT,
                    subscriptionManager = subscriptionManager,
                    onShowPaywall = onShowPaywall
                ) {
                    Column {
                        Text(
                            text = stringResource(R.string.overview_export_datev_label),
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Spacer(Modifier.height(4.dp))

                        var showDatevMonthPicker by remember { mutableStateOf(false) }
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            Box {
                                OutlinedButton(onClick = { showDatevMonthPicker = true }) {
                                    Icon(Icons.Default.Share, null, modifier = Modifier.padding(end = 4.dp))
                                    Text(stringResource(R.string.overview_export_select_month))
                                }
                                DropdownMenu(
                                    expanded = showDatevMonthPicker,
                                    onDismissRequest = { showDatevMonthPicker = false }
                                ) {
                                    val currentMonth = LocalDate.now().monthValue
                                    val months = if (selectedYear == LocalDate.now().year) {
                                        (1..currentMonth).toList()
                                    } else {
                                        (1..12).toList()
                                    }
                                    months.reversed().forEach { month ->
                                        val monthName = Month.of(month).getDisplayName(TextStyle.FULL, Locale.GERMAN)
                                        DropdownMenuItem(
                                            text = { Text("$monthName $selectedYear") },
                                            onClick = {
                                                showDatevMonthPicker = false
                                                onGenerateDATEV(month)
                                            }
                                        )
                                    }
                                }
                            }
                            Text(
                                text = stringResource(R.string.overview_export_datev_beta),
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier
                                    .padding(horizontal = 6.dp, vertical = 2.dp)
                            )
                        }
                    }
                }
            }
        }
    }
}
