import Foundation
import SwiftData

@Observable @MainActor
final class OverviewViewModel {
    // MARK: - State

    var selectedYear: Int
    var summary: OvertimeSummaryDTO?
    var isLoading = false
    var isShowingCachedData = false
    var lastUpdated: Date?
    var error: String?

    // Export state
    var isGeneratingPDF = false
    var isGeneratingCSV = false
    var exportedFileURL: URL?
    var exportError: String?

    // MARK: - Dependencies

    private let apiClient: APIClient?
    private var modelContext: ModelContext?

    init(apiClient: APIClient? = nil, modelContext: ModelContext? = nil) {
        self.apiClient = apiClient
        self.modelContext = modelContext
        self.selectedYear = Calendar.current.component(.year, from: Date())
    }

    // MARK: - Load Data

    func loadSummary() async {
        isLoading = true
        error = nil
        defer { isLoading = false }

        // Load cached data first
        if let cached = OvertimeCache.load(year: selectedYear) {
            summary = cached.summary
            lastUpdated = cached.timestamp
            isShowingCachedData = true
        }

        guard let apiClient else {
            // No API client yet (still initializing after login)
            if summary == nil {
                error = "Daten werden geladen..."
            }
            return
        }

        do {
            let result: OvertimeSummaryDTO = try await apiClient.getOvertimeSummary(year: selectedYear)
            summary = result
            isShowingCachedData = false
            lastUpdated = Date()

            // Save to cache
            try? OvertimeCache.save(summary: result, year: selectedYear)
        } catch {
            // Try cache fallback
            if let cached = OvertimeCache.load(year: selectedYear) {
                summary = cached.summary
                lastUpdated = cached.timestamp
                isShowingCachedData = true
            } else {
                self.error = "Uebersicht konnte nicht geladen werden"
                summary = nil
            }
        }
    }

    // MARK: - Year Navigation

    func previousYear() {
        selectedYear -= 1
        Task { await loadSummary() }
    }

    func nextYear() {
        selectedYear += 1
        Task { await loadSummary() }
    }

    // MARK: - Export

    func generatePDF(month: Int, year: Int) async {
        guard let modelContext else { return }
        isGeneratingPDF = true
        exportError = nil
        defer { isGeneratingPDF = false }

        do {
            let sessions = try modelContext.fetch(FetchDescriptor<WorkSession>())
            let vacationDays = try modelContext.fetch(FetchDescriptor<VacationDay>())
            let sickDays = try modelContext.fetch(FetchDescriptor<SickDay>())
            let settings = try modelContext.fetch(FetchDescriptor<UserSettings>()).first
                ?? UserSettings()

            let holidays = HolidayCalculator.holidays(
                bundesland: settings.bundesland, year: year
            )

            guard let pdfData = await PDFReportGenerator.generateMonthlyReport(
                month: month, year: year,
                sessions: sessions, vacationDays: vacationDays,
                sickDays: sickDays, holidays: holidays,
                settings: settings, employeeName: nil
            ) else {
                exportError = "PDF konnte nicht generiert werden"
                return
            }

            let fileName = "Arbeitszeitnachweis_\(year)-\(String(format: "%02d", month)).pdf"
            let tempURL = FileManager.default.temporaryDirectory.appendingPathComponent(fileName)
            try pdfData.write(to: tempURL)
            exportedFileURL = tempURL
        } catch {
            exportError = "Fehler beim Generieren: \(error.localizedDescription)"
        }
    }

    func generateCSV(timeRange: CSVExporter.TimeRange) {
        guard let modelContext else { return }
        isGeneratingCSV = true
        exportError = nil
        defer { isGeneratingCSV = false }

        do {
            let sessions = try modelContext.fetch(FetchDescriptor<WorkSession>())
            let vacationDays = try modelContext.fetch(FetchDescriptor<VacationDay>())
            let sickDays = try modelContext.fetch(FetchDescriptor<SickDay>())
            let settings = try modelContext.fetch(FetchDescriptor<UserSettings>()).first
                ?? UserSettings()

            // Collect holidays for the full range
            let cal = Calendar.current
            let fromYear = cal.component(.year, from: timeRange.from)
            let toYear = cal.component(.year, from: timeRange.to)
            var holidays: [Holiday] = []
            for y in fromYear...toYear {
                holidays.append(contentsOf: HolidayCalculator.holidays(
                    bundesland: settings.bundesland, year: y
                ))
            }

            let csv = CSVExporter.generateCSV(
                sessions: sessions, vacationDays: vacationDays,
                sickDays: sickDays, holidays: holidays,
                settings: settings, timeRange: timeRange
            )

            let tempURL = FileManager.default.temporaryDirectory
                .appendingPathComponent(timeRange.fileName)
            try csv.write(to: tempURL, atomically: true, encoding: .utf8)
            exportedFileURL = tempURL
        } catch {
            exportError = "Fehler beim Generieren: \(error.localizedDescription)"
        }
    }

    // MARK: - Formatting

    static func formatHours(_ totalHours: Double) -> String {
        let isNegative = totalHours < 0
        let absHours = abs(totalHours)
        let hours = Int(absHours)
        let minutes = Int((absHours - Double(hours)) * 60)
        let sign = isNegative ? "-" : (totalHours > 0 ? "+" : "")
        return "\(sign)\(hours):\(String(format: "%02d", minutes))h"
    }
}

// MARK: - Disk Cache

enum OvertimeCache {
    struct CachedSummary: Codable {
        let summary: OvertimeSummaryDTO
        let timestamp: Date
    }

    static func save(summary: OvertimeSummaryDTO, year: Int) throws {
        let cached = CachedSummary(summary: summary, timestamp: Date())
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        let data = try encoder.encode(cached)
        try data.write(to: cacheURL(year: year))
    }

    static func load(year: Int) -> CachedSummary? {
        guard let data = try? Data(contentsOf: cacheURL(year: year)) else { return nil }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try? decoder.decode(CachedSummary.self, from: data)
    }

    private static func cacheURL(year: Int) -> URL {
        FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("overtime_cache_\(year).json")
    }
}
