package com.fakturus.track.features.timetracking

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material.icons.filled.Sync
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fakturus.track.R
import com.fakturus.track.ServiceContainer
import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.ui.shared.ArbZGBanner
import com.fakturus.track.ui.shared.SyncStatusIndicator
import com.fakturus.track.util.DateFormatting
import java.time.Duration
import java.time.Instant
import java.time.LocalDate

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TimeTrackingScreen(services: ServiceContainer) {
    val context = LocalContext.current
    val viewModel: TimeTrackingViewModel = viewModel(
        factory = TimeTrackingViewModelFactory(services, context)
    )
    val activeSession by viewModel.activeSession.collectAsState()
    val sessions by viewModel.sessions.collectAsState(initial = emptyList())
    val isPaused by viewModel.isPaused.collectAsState()
    val isSyncing by viewModel.isSyncing.collectAsState()
    val syncLastError by viewModel.syncLastError.collectAsState()
    val currentPauseStart by viewModel.currentPauseStart.collectAsState()
    val accumulatedPauseMinutes by viewModel.accumulatedPauseMinutes.collectAsState()
    val hasShown6h by viewModel.hasShown6h.collectAsState()
    val hasShown9h by viewModel.hasShown9h.collectAsState()
    val hasShown10h by viewModel.hasShown10h.collectAsState()

    // Calculate net work minutes for ArbZG banner
    val netWorkMinutes = activeSession?.let { session ->
        val start = Instant.parse(session.startTime)
        val totalMinutes = Duration.between(start, Instant.now()).toMinutes()
        val currentPauseMinutes = if (isPaused && currentPauseStart != null) {
            Duration.between(currentPauseStart, Instant.now()).toMinutes()
        } else 0L
        totalMinutes - accumulatedPauseMinutes - currentPauseMinutes
    } ?: 0L

    // Pending sync count for SyncStatusIndicator
    val pendingCount = sessions.count { it.isPendingSync }

    var selectedSession by remember { mutableStateOf<WorkSessionEntity?>(null) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.times_tab_title)) },
                actions = {
                    IconButton(onClick = {
                        viewModel.createManualSession { session ->
                            selectedSession = session
                        }
                    }) {
                        Icon(
                            Icons.Default.Add,
                            contentDescription = "Manuelle Nacherfassung"
                        )
                    }
                    SyncStatusIndicator(
                        isSyncing = isSyncing,
                        pendingCount = pendingCount,
                        lastError = syncLastError,
                        onSync = { viewModel.triggerSync() }
                    )
                }
            )
        }
    ) { padding ->
        val finishedSessions = sessions.filter { it.isFinished }
        val grouped = finishedSessions
            .groupBy { it.monthKey }
            .entries
            .sortedByDescending { entry -> entry.value.maxOf { it.date } }
            .associate { it.key to it.value }

        PullToRefreshBox(
            isRefreshing = isSyncing,
            onRefresh = { viewModel.refresh() },
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                // ArbZG Banner
                if (activeSession != null) {
                    item(key = "arbzg_banner") {
                        ArbZGBanner(
                            netWorkMinutes = netWorkMinutes,
                            pauseMinutes = accumulatedPauseMinutes,
                            hasShown6h = hasShown6h,
                            hasShown9h = hasShown9h,
                            hasShown10h = hasShown10h,
                            onShown6h = { viewModel.markShown6h() },
                            onShown9h = { viewModel.markShown9h() },
                            onShown10h = { viewModel.markShown10h() }
                        )
                    }
                }

                item(key = "active_session_card") {
                    ActiveSessionCard(
                        session = activeSession,
                        isPaused = isPaused,
                        currentPauseStart = currentPauseStart,
                        accumulatedPauseMinutes = accumulatedPauseMinutes,
                        onStart = viewModel::startSession,
                        onStop = viewModel::stopSession,
                        onFinish = viewModel::finishSession,
                        onPause = viewModel::pauseSession,
                        onResume = viewModel::resumeSession,
                        onSave = { d, s, e, p -> viewModel.updateSession(d, s, e, p) },
                        onDelete = {
                            activeSession?.let { viewModel.deleteSession(it) }
                        }
                    )
                }

                if (finishedSessions.isEmpty()) {
                    item(key = "empty_state") {
                        EmptyState()
                    }
                } else {
                    grouped.forEach { (month, monthSessions) ->
                        item(key = "month_$month") {
                            MonthGroup(
                                monthName = month,
                                sessions = monthSessions,
                                isCurrentMonth = month == DateFormatting.formatMonthYear(LocalDate.now()),
                                onDeleteSession = { viewModel.deleteSession(it) },
                                onSelectSession = { selectedSession = it }
                            )
                        }
                    }
                }
            }
        }
    }

    // Detail Sheet
    selectedSession?.let { session ->
        SessionDetailSheet(
            session = session,
            onDismiss = { selectedSession = null },
            onSave = { d, s, e, p ->
                viewModel.updateFinishedSession(session, d, s, e, p)
                selectedSession = null
            },
            onDelete = {
                viewModel.deleteSession(session)
                selectedSession = null
            }
        )
    }
}

@Composable
private fun EmptyState() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(vertical = 48.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Icon(
            Icons.Default.Schedule,
            contentDescription = null,
            modifier = Modifier.size(48.dp),
            tint = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(Modifier.height(12.dp))
        Text(
            text = "Noch keine Eintraege",
            style = MaterialTheme.typography.titleMedium
        )
        Spacer(Modifier.height(4.dp))
        Text(
            text = "Starten Sie Ihre erste Arbeitssitzung!",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}
