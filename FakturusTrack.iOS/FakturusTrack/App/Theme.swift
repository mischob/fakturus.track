import SwiftUI
import UIKit

enum Theme {
    // MARK: - Brand Colors

    static let primary = Color(
        light: Color(hex: 0x1A5CFF),
        dark: Color(hex: 0x4D8AFF)
    )

    static let success = Color(
        light: Color(hex: 0x1DB954),
        dark: Color(hex: 0x34D96E)
    )

    static let danger = Color(
        light: Color(hex: 0xE5383B),
        dark: Color(hex: 0xFF6B6B)
    )

    static let warning = Color(hex: 0xF59E0B)

    // MARK: - Semantic Colors

    static let timerActive = success

    static let pause = Color(
        light: Color(hex: 0x8B5CF6),
        dark: Color(hex: 0xA78BFA)
    )

    static let vacation = Color(hex: 0x06B6D4)

    static let sickDay = Color(hex: 0xEF4444)

    static let syncPending = warning

    static let syncDone = success

    static let timerPaused = warning

    static let offlineBanner = warning

    // MARK: - Neutrals

    static let gray50 = Color(
        light: Color(hex: 0xF9FAFB),
        dark: Color(hex: 0x0F1117)
    )

    static let gray100 = Color(
        light: Color(hex: 0xF3F4F6),
        dark: Color(hex: 0x1A1D27)
    )

    static let gray200 = Color(
        light: Color(hex: 0xE5E7EB),
        dark: Color(hex: 0x2D3040)
    )

    static let gray500 = Color(hex: 0x6B7280)

    static let gray700 = Color(hex: 0x374151)

    static let gray900 = Color(
        light: Color(hex: 0x111827),
        dark: Color(hex: 0xF0F1F3)
    )

    // MARK: - Backgrounds & Surfaces

    static let background = gray50
    static let surface = Color(
        light: .white,
        dark: Color(hex: 0x1A1D27)
    )
    static let textPrimary = gray900
    static let textSecondary = gray500
}

// MARK: - Color Helpers

extension Color {
    /// Creates a color from a hex integer (e.g. 0x1A5CFF).
    init(hex: UInt, opacity: Double = 1.0) {
        self.init(
            red: Double((hex >> 16) & 0xFF) / 255.0,
            green: Double((hex >> 8) & 0xFF) / 255.0,
            blue: Double(hex & 0xFF) / 255.0,
            opacity: opacity
        )
    }

    /// Creates a dynamic color that adapts to light/dark mode.
    init(light: Color, dark: Color) {
        self.init(uiColor: UIColor { traits in
            traits.userInterfaceStyle == .dark
                ? UIColor(dark)
                : UIColor(light)
        })
    }
}
