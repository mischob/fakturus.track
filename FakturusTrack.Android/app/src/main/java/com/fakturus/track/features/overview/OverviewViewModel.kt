package com.fakturus.track.features.overview

import android.content.Context
import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.fakturus.track.models.AppDatabase
import com.fakturus.track.models.OvertimeSummaryDTO
import com.fakturus.track.models.UserSettingsEntity
import com.fakturus.track.services.api.APIClient
import com.fakturus.track.services.export.CSVExporter
import com.fakturus.track.services.export.DATEVExporter
import com.fakturus.track.services.export.PDFReportGenerator
import com.fakturus.track.util.HolidayCalculator
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import java.io.File
import java.time.Instant
import java.time.LocalDate
import java.time.YearMonth

data class OverviewUiState(
    val selectedYear: Int = LocalDate.now().year,
    val summary: OvertimeSummaryDTO? = null,
    val isLoading: Boolean = false,
    val error: String? = null,
    val isShowingCachedData: Boolean = false,
    val lastUpdated: String? = null,
    val isExporting: Boolean = false,
    val exportedFile: File? = null,
    val exportMimeType: String? = null
)

class OvertimeCache(private val context: Context) {
    @Serializable
    data class CachedSummary(
        val summary: OvertimeSummaryDTO,
        val timestamp: String
    )

    private val json = Json { ignoreUnknownKeys = true }

    fun save(summary: OvertimeSummaryDTO, year: Int) {
        try {
            val cached = CachedSummary(summary, Instant.now().toString())
            val file = File(context.filesDir, "overtime_cache_$year.json")
            file.writeText(json.encodeToString(CachedSummary.serializer(), cached))
        } catch (e: Exception) {
            Log.w("OvertimeCache", "Failed to save cache", e)
        }
    }

    fun load(year: Int): CachedSummary? {
        val file = File(context.filesDir, "overtime_cache_$year.json")
        if (!file.exists()) return null
        return try {
            json.decodeFromString(CachedSummary.serializer(), file.readText())
        } catch (_: Exception) {
            null
        }
    }
}

class OverviewViewModel(
    private val apiClient: APIClient?,
    private val cache: OvertimeCache,
    private val database: AppDatabase? = null,
    private val context: Context? = null
) : ViewModel() {

    private val _uiState = MutableStateFlow(OverviewUiState())
    val uiState: StateFlow<OverviewUiState> = _uiState.asStateFlow()

    init {
        loadOvertimeSummary(_uiState.value.selectedYear)
    }

    fun selectYear(year: Int) {
        _uiState.value = _uiState.value.copy(selectedYear = year)
        loadOvertimeSummary(year)
    }

    fun refresh() {
        loadOvertimeSummary(_uiState.value.selectedYear)
    }

    fun clearExport() {
        _uiState.value = _uiState.value.copy(exportedFile = null, exportMimeType = null)
    }

    fun generatePDF(month: Int, year: Int) {
        val ctx = context ?: return
        val db = database ?: return

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isExporting = true)
            try {
                val file = withContext(Dispatchers.IO) {
                    val ym = YearMonth.of(year, month)
                    val from = ym.atDay(1).toString()
                    val to = ym.atEndOfMonth().toString()
                    val sessions = db.workSessionDao().let { dao ->
                        dao.getPendingSessions().plus(dao.getSyncedSessions())
                            .distinctBy { it.id }
                            .filter { it.date >= from && it.date <= to }
                    }
                    val vacationDays = db.vacationDayDao().getAll().filter {
                        it.date >= from && it.date <= to
                    }
                    val sickDays = db.sickDayDao().getAll().filter {
                        it.date >= from && it.date <= to
                    }
                    val settings = db.userSettingsDao().getSettingsOnce() ?: UserSettingsEntity(userId = "")
                    val holidays = HolidayCalculator.holidays(settings.bundesland, year)

                    PDFReportGenerator(ctx).generateMonthlyReport(
                        month = month, year = year,
                        sessions = sessions, vacationDays = vacationDays,
                        sickDays = sickDays, holidays = holidays,
                        settings = settings, employeeName = null
                    )
                }

                _uiState.value = _uiState.value.copy(
                    isExporting = false,
                    exportedFile = file,
                    exportMimeType = "application/pdf"
                )
            } catch (e: Exception) {
                Log.e("OverviewViewModel", "PDF generation failed", e)
                _uiState.value = _uiState.value.copy(isExporting = false)
            }
        }
    }

    fun generateCSV(csvRange: CSVRange) {
        val db = database ?: return
        val ctx = context ?: return

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isExporting = true)
            try {
                val year = _uiState.value.selectedYear
                val (from, to, fileName) = when (csvRange) {
                    is CSVRange.Month -> {
                        val ym = YearMonth.of(year, csvRange.month)
                        Triple(ym.atDay(1), ym.atEndOfMonth(),
                            "Arbeitszeiten_${year}-${"%02d".format(csvRange.month)}.csv")
                    }
                    is CSVRange.Quarter -> {
                        val startMonth = (csvRange.quarter - 1) * 3 + 1
                        val endMonth = startMonth + 2
                        Triple(
                            YearMonth.of(year, startMonth).atDay(1),
                            YearMonth.of(year, endMonth).atEndOfMonth(),
                            "Arbeitszeiten_${year}-Q${csvRange.quarter}.csv"
                        )
                    }
                    is CSVRange.Year -> {
                        Triple(
                            LocalDate.of(year, 1, 1),
                            LocalDate.of(year, 12, 31),
                            "Arbeitszeiten_${year}.csv"
                        )
                    }
                }

                val fromStr = from.toString()
                val toStr = to.toString()

                val file = withContext(Dispatchers.IO) {
                    val sessions = db.workSessionDao().let { dao ->
                        dao.getPendingSessions().plus(dao.getSyncedSessions())
                            .distinctBy { it.id }
                            .filter { it.date >= fromStr && it.date <= toStr }
                    }
                    val vacationDays = db.vacationDayDao().getAll().filter {
                        it.date >= fromStr && it.date <= toStr
                    }
                    val sickDays = db.sickDayDao().getAll().filter {
                        it.date >= fromStr && it.date <= toStr
                    }
                    val settings = db.userSettingsDao().getSettingsOnce() ?: UserSettingsEntity(userId = "")

                    val holidays = (from.year..to.year).flatMap { y ->
                        HolidayCalculator.holidays(settings.bundesland, y)
                    }.filter { it.date in from..to }

                    val csv = CSVExporter.generateCSV(
                        sessions = sessions, vacationDays = vacationDays,
                        sickDays = sickDays, holidays = holidays,
                        settings = settings, from = from, to = to
                    )

                    val f = File(ctx.cacheDir, fileName)
                    f.writeText(csv, Charsets.UTF_8)
                    f
                }

                _uiState.value = _uiState.value.copy(
                    isExporting = false,
                    exportedFile = file,
                    exportMimeType = "text/csv"
                )
            } catch (e: Exception) {
                Log.e("OverviewViewModel", "CSV generation failed", e)
                _uiState.value = _uiState.value.copy(isExporting = false)
            }
        }
    }

    fun generateDATEV(month: Int) {
        val db = database ?: return
        val ctx = context ?: return

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isExporting = true)
            try {
                val year = _uiState.value.selectedYear
                val ym = YearMonth.of(year, month)
                val from = ym.atDay(1).toString()
                val to = ym.atEndOfMonth().toString()

                val file = withContext(Dispatchers.IO) {
                    val sessions = db.workSessionDao().let { dao ->
                        dao.getPendingSessions().plus(dao.getSyncedSessions())
                            .distinctBy { it.id }
                            .filter { it.date >= from && it.date <= to }
                    }
                    val vacationDays = db.vacationDayDao().getAll().filter {
                        it.date >= from && it.date <= to
                    }
                    val sickDays = db.sickDayDao().getAll().filter {
                        it.date >= from && it.date <= to
                    }
                    val settings = db.userSettingsDao().getSettingsOnce()
                        ?: UserSettingsEntity(userId = "")

                    val personalNumber = settings.personalNumber ?: ""

                    val datev = DATEVExporter.generateExport(
                        month = month, year = year,
                        sessions = sessions, vacationDays = vacationDays,
                        sickDays = sickDays, settings = settings,
                        personalNumber = personalNumber
                    )

                    val fileName = DATEVExporter.fileName(month, year)
                    val f = File(ctx.cacheDir, fileName)
                    f.writeText(datev, Charsets.UTF_8)
                    f
                }

                _uiState.value = _uiState.value.copy(
                    isExporting = false,
                    exportedFile = file,
                    exportMimeType = "text/csv"
                )
            } catch (e: Exception) {
                Log.e("OverviewViewModel", "DATEV export failed", e)
                _uiState.value = _uiState.value.copy(isExporting = false)
            }
        }
    }

    private fun loadOvertimeSummary(year: Int) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)

            try {
                val summary = withContext(Dispatchers.IO) {
                    apiClient?.getOvertimeSummary(year)
                }
                if (summary != null) {
                    cache.save(summary, year)
                    _uiState.value = _uiState.value.copy(
                        summary = summary,
                        isLoading = false,
                        isShowingCachedData = false,
                        lastUpdated = Instant.now().toString()
                    )
                } else {
                    loadFromCache(year)
                }
            } catch (e: Exception) {
                Log.w("OverviewViewModel", "API call failed, loading from cache", e)
                loadFromCache(year)
            }
        }
    }

    private fun loadFromCache(year: Int) {
        val cached = cache.load(year)
        if (cached != null) {
            _uiState.value = _uiState.value.copy(
                summary = cached.summary,
                isLoading = false,
                isShowingCachedData = true,
                lastUpdated = cached.timestamp,
                error = null
            )
        } else {
            _uiState.value = _uiState.value.copy(
                summary = null,
                isLoading = false,
                error = "Uebersicht konnte nicht geladen werden"
            )
        }
    }
}

sealed class CSVRange {
    data class Month(val month: Int) : CSVRange()
    data class Quarter(val quarter: Int) : CSVRange()
    data object Year : CSVRange()
}
