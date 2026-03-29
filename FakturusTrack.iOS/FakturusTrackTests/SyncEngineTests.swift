import Testing
import SwiftData
import Foundation

@testable import FakturusTrack

@MainActor
struct SyncEngineTests {

    private func makeContext() -> ModelContext {
        let container = PersistenceManager.testContainer()
        return ModelContext(container)
    }

    @Test func workSession_toDTO_convertsCorrectly() async {
        let id = UUID()
        let date = Date()
        let startTime = date
        let stopTime = date.addingTimeInterval(3600)

        let session = WorkSession(
            id: id,
            userId: "user1",
            date: date,
            startTime: startTime,
            stopTime: stopTime,
            pauseMinutes: 15,
            isPendingSync: true,
            isSynced: false,
            isFinished: true
        )

        let dto = session.toDTO()

        #expect(dto.id == id.uuidString)
        #expect(dto.date == ISO8601DateFormatter.dateOnly.string(from: date))
        #expect(dto.startTime == startTime.ISO8601Format())
        #expect(dto.stopTime == stopTime.ISO8601Format())
        #expect(dto.pauseMinutes == 15)
    }

    @Test func workSession_updateFromDTO_mergesCorrectly() async {
        let context = makeContext()
        let session = WorkSession(
            date: Date(),
            startTime: Date(),
            isPendingSync: true,
            isSynced: false,
            isFinished: false
        )
        context.insert(session)
        try? context.save()

        let newDate = "2026-03-15"
        let newStart = "2026-03-15T08:00:00Z"
        let newStop = "2026-03-15T17:00:00Z"

        let dto = WorkSessionDTO(
            id: session.id.uuidString,
            userId: "user1",
            date: newDate,
            startTime: newStart,
            stopTime: newStop,
            pauseMinutes: 30,
            createdAt: nil,
            updatedAt: nil,
            syncedAt: nil
        )

        session.update(from: dto)

        #expect(session.pauseMinutes == 30)
        #expect(session.isSynced == true)
        #expect(session.isPendingSync == false)
        #expect(session.stopTime != nil)
        #expect(session.isFinished == true)
    }

    @Test func vacationDay_toDTO_convertsCorrectly() async {
        let id = UUID()
        let date = Date()

        let day = VacationDay(
            id: id,
            userId: "user1",
            date: date,
            isPendingSync: true,
            isSynced: false
        )

        let dto = day.toDTO()

        #expect(dto.id == id.uuidString)
        #expect(dto.date == ISO8601DateFormatter.dateOnly.string(from: date))
        #expect(dto.createdAt != nil)
        #expect(dto.updatedAt != nil)
    }

    @Test func netDuration_withPause_subtractsCorrectly() async {
        // 3600s gross - 30min pause = 1800s net
        let start = Date()
        let stop = start.addingTimeInterval(3600) // 1 hour

        let session = WorkSession(
            date: start,
            startTime: start,
            stopTime: stop,
            pauseMinutes: 30,
            isPendingSync: false,
            isSynced: false,
            isFinished: true
        )

        // Gross = 3600s
        #expect(abs(session.duration - 3600) < 1)

        // Net = 3600 - 1800 = 1800s
        #expect(abs(session.netDuration - 1800) < 1)

        // Net minutes = 30
        #expect(session.netDurationMinutes == 30)
    }
}
