import Foundation

extension TimeInterval {
    /// "03:42:18" (for running timer display)
    var formattedHHMMSS: String {
        let total = Int(self)
        let h = total / 3600
        let m = (total % 3600) / 60
        let s = total % 60
        return String(format: "%02d:%02d:%02d", h, m, s)
    }

    /// "8:30h" (for duration display) or "0h" for zero
    var formattedHHMM: String {
        let total = Int(self) / 60 // total minutes
        let h = total / 60
        let m = total % 60
        return m > 0 ? "\(h):\(String(format: "%02d", m))h" : "\(h)h"
    }
}
