import SwiftData
import Foundation

@Model
final class WorkSession {
    @Attribute(.unique) var id: UUID
    var userId: String
    var date: Date
    var startTime: Date
    var stopTime: Date?
    var pauseMinutes: Int
    var calendarEventId: String?
    var createdAt: Date
    var updatedAt: Date
    var syncedAt: Date?
    var isPendingSync: Bool
    var isSynced: Bool
    var isFinished: Bool

    init(
        id: UUID = UUID(),
        userId: String = "",
        date: Date = Date(),
        startTime: Date = Date(),
        stopTime: Date? = nil,
        pauseMinutes: Int = 0,
        calendarEventId: String? = nil,
        isPendingSync: Bool = true,
        isSynced: Bool = false,
        isFinished: Bool = false
    ) {
        self.id = id
        self.userId = userId
        self.date = date
        self.startTime = startTime
        self.stopTime = stopTime
        self.pauseMinutes = pauseMinutes
        self.calendarEventId = calendarEventId
        self.createdAt = Date()
        self.updatedAt = Date()
        self.isPendingSync = isPendingSync
        self.isSynced = isSynced
        self.isFinished = isFinished
    }

    // MARK: - Computed

    var isRunning: Bool { !isFinished && stopTime == nil }

    /// Gross duration in seconds (nil if no stop time and not actively running today)
    var duration: TimeInterval? {
        if let stop = stopTime {
            return stop.timeIntervalSince(startTime)
        }
        // Only calculate live duration for actively running sessions (started today)
        if isRunning && Calendar.current.isDateInToday(startTime) {
            return Date().timeIntervalSince(startTime)
        }
        return nil
    }

    /// Net duration in seconds (minus pause), nil if duration unknown
    var netDuration: TimeInterval? {
        guard let dur = duration else { return nil }
        return max(0, dur - Double(pauseMinutes * 60))
    }

    /// Net duration in minutes, 0 if unknown
    var netDurationMinutes: Int {
        Int((netDuration ?? 0) / 60)
    }

    /// Whether this session has a calculable duration
    var hasDuration: Bool { duration != nil }

    /// Grouping key "März 2026"
    var monthKey: String { date.monthYearString }

    // MARK: - DTO Conversion

    func toDTO() -> WorkSessionSyncItem {
        WorkSessionSyncItem(
            id: id.uuidString,
            date: ISO8601DateFormatter.dateOnly.string(from: date),
            startTime: startTime.ISO8601Format(),
            stopTime: stopTime?.ISO8601Format(),
            pauseMinutes: pauseMinutes
        )
    }

    func update(from dto: WorkSessionDTO) {
        if let d = ISO8601DateFormatter.dateOnly.date(from: dto.date) { date = d }
        if let t = ISO8601Helper.parse(dto.startTime) { startTime = t }
        // Only overwrite stopTime when the server explicitly returns a value.
        // If the server sent a value but parsing failed, keep the local stopTime
        // instead of clobbering the user's edit with nil.
        if let dtoStop = dto.stopTime {
            if let parsed = ISO8601Helper.parse(dtoStop) {
                stopTime = parsed
            }
            // parse failure: leave stopTime untouched
        } else {
            stopTime = nil
        }
        pauseMinutes = dto.pauseMinutes ?? 0
        updatedAt = Date()
        syncedAt = Date()
        isPendingSync = false
        isSynced = true
        // Server-sourced sessions are committed entries — keep them in the
        // history list even if stopTime is missing (matches `init(from:)`).
        isFinished = true
    }

    convenience init(from dto: WorkSessionDTO) {
        self.init(
            id: UUID(uuidString: dto.id) ?? UUID(),
            userId: dto.userId ?? "",
            date: ISO8601DateFormatter.dateOnly.date(from: dto.date) ?? Date(),
            startTime: ISO8601Helper.parse(dto.startTime) ?? Date(),
            stopTime: dto.stopTime.flatMap { ISO8601Helper.parse($0) },
            pauseMinutes: dto.pauseMinutes ?? 0,
            isPendingSync: false,
            isSynced: true,
            isFinished: true
        )
        self.syncedAt = Date()
    }
}

// MARK: - ISO8601 Helpers

/// Tolerant ISO8601 parser. The .NET backend may serialize `DateTime`
/// values with a fractional-seconds component (e.g. `2026-04-15T17:00:00.1234567Z`),
/// which the default `ISO8601DateFormatter` rejects. Try both variants.
enum ISO8601Helper {
    nonisolated(unsafe) private static let withFractional: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f
    }()

    nonisolated(unsafe) private static let plain: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime]
        return f
    }()

    static func parse(_ input: String) -> Date? {
        if let date = withFractional.date(from: input) { return date }
        return plain.date(from: input)
    }
}

extension ISO8601DateFormatter {
    nonisolated(unsafe) static let dateOnly: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withFullDate]
        return f
    }()
}
