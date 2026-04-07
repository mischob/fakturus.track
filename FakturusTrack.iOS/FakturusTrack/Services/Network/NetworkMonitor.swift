import Network
import Observation

@Observable @MainActor
final class NetworkMonitor: @unchecked Sendable {
    var isConnected = true

    private let monitor = NWPathMonitor()
    private let queue = DispatchQueue(label: "NetworkMonitor")
    private var onBecameOnline: (() -> Void)?
    private var debounceTask: Task<Void, Never>?

    init() {
        monitor.pathUpdateHandler = { [weak self] path in
            Task { @MainActor in
                guard let self else { return }
                let wasOffline = !self.isConnected
                self.isConnected = path.status == .satisfied

                if wasOffline && self.isConnected {
                    // Debounce: 2 Sekunden warten bevor Callback
                    self.debounceTask?.cancel()
                    self.debounceTask = Task {
                        try? await Task.sleep(nanoseconds: 2_000_000_000)
                        guard !Task.isCancelled, self.isConnected else { return }
                        self.onBecameOnline?()
                    }
                }
            }
        }
        monitor.start(queue: queue)
    }

    func setOnBecameOnline(_ handler: @escaping () -> Void) {
        onBecameOnline = handler
    }

    deinit {
        monitor.cancel()
    }
}
