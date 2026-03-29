import Foundation
import Observation

@Observable
final class AppState {
    var isAuthenticated: Bool = false
    var isOnline: Bool = true
    var isSyncing: Bool = false
    var selectedTab: Int = 0
}
