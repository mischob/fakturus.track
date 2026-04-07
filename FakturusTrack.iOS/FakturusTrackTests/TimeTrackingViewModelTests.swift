import Testing
import SwiftData
import Foundation

@testable import FakturusTrack

@MainActor
struct TimeTrackingViewModelTests {

    private func makeContext() -> ModelContext {
        let container = PersistenceManager.testContainer()
        return ModelContext(container)
    }

    @Test func startSession_createsNewSession() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()

        #expect(vm.activeSession != nil)
        #expect(vm.activeSession?.isFinished == false)
        #expect(vm.activeSession?.isPendingSync == true)
    }

    @Test func stopSession_setsStopTime() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()
        #expect(vm.activeSession?.stopTime == nil)

        vm.stopSession()

        #expect(vm.activeSession?.stopTime != nil)
    }

    @Test func finishSession_marksFinished() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()
        #expect(vm.activeSession != nil)

        vm.finishSession()

        #expect(vm.activeSession == nil)

        // Verify session is marked finished in DB
        let descriptor = FetchDescriptor<WorkSession>(
            predicate: #Predicate { $0.isFinished }
        )
        let finished = try? context.fetch(descriptor)
        #expect(finished?.count == 1)
        #expect(finished?.first?.isFinished == true)
    }

    @Test func pauseSession_setsIsPaused() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()
        #expect(vm.isPaused == false)

        vm.pauseSession()

        #expect(vm.isPaused == true)
        #expect(vm.currentPauseStart != nil)
    }

    @Test func resumeSession_calculatesPauseMinutes() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()
        vm.pauseSession()

        // Wait briefly so pause duration > 0
        try? await Task.sleep(for: .milliseconds(100))

        vm.resumeSession()

        #expect(vm.isPaused == false)
        // ceil(>0s / 60) == 1 minute minimum
        #expect(vm.accumulatedPauseMinutes >= 1)
    }

    @Test func finishSession_endsOpenPause() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()
        vm.pauseSession()
        #expect(vm.isPaused == true)

        vm.finishSession()

        #expect(vm.isPaused == false)
        #expect(vm.currentPauseStart == nil)
        #expect(vm.accumulatedPauseMinutes == 0)
    }

    @Test func deleteSession_removesFromDB() async {
        let context = makeContext()
        let vm = TimeTrackingViewModel(modelContext: context)

        vm.startSession()
        let session = vm.activeSession!

        vm.deleteSession(session)

        #expect(vm.activeSession == nil)

        let descriptor = FetchDescriptor<WorkSession>()
        let remaining = try? context.fetch(descriptor)
        #expect(remaining?.isEmpty == true)
    }

    @Test func netDuration_calculatesCorrectly() async {
        // 9 hours gross, 30 minutes pause -> 8.5 hours net
        let start = Date()
        let stop = start.addingTimeInterval(9 * 3600) // 9 hours later

        let session = WorkSession(
            date: start,
            startTime: start,
            stopTime: stop,
            pauseMinutes: 30,
            isPendingSync: false,
            isSynced: false,
            isFinished: true
        )

        // Gross duration = 9h = 32400s
        let grossSeconds = session.duration
        #expect(abs(grossSeconds - 32400) < 1)

        // Net duration = 9h - 30min = 8.5h = 30600s
        let netSeconds = session.netDuration
        #expect(abs(netSeconds - 30600) < 1)

        // Net minutes = 510 min
        #expect(session.netDurationMinutes == 510)
    }
}
