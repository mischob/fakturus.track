import UIKit

@MainActor
enum HapticManager {
    private static let impactLight = UIImpactFeedbackGenerator(style: .light)
    private static let impactMedium = UIImpactFeedbackGenerator(style: .medium)
    private static let impactHeavy = UIImpactFeedbackGenerator(style: .heavy)
    private static let selection = UISelectionFeedbackGenerator()
    private static let notification = UINotificationFeedbackGenerator()

    static func timerStart() {
        impactMedium.prepare()
        impactMedium.impactOccurred()
    }

    static func timerStop() {
        impactHeavy.prepare()
        impactHeavy.impactOccurred()
    }

    static func timerPauseResume() {
        impactLight.prepare()
        impactLight.impactOccurred()
    }

    static func sessionFinished() {
        notification.prepare()
        notification.notificationOccurred(.success)
    }

    static func sessionDeleted() {
        notification.prepare()
        notification.notificationOccurred(.warning)
    }

    static func toggle() {
        selection.prepare()
        selection.selectionChanged()
    }

    static func error() {
        notification.prepare()
        notification.notificationOccurred(.error)
    }
}
