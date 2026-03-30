import SwiftUI
import UIKit

// MARK: - WCAG AA Contrast Audit (E06-S05)
//
// Minimum contrast ratios (WCAG AA):
//   - Normal text (< 18pt): 4.5:1
//   - Large text (>= 18pt bold or >= 24pt): 3:1
//   - UI components / graphical objects: 3:1
//
// Audit results (light mode, on white #FFFFFF background):
//   primary    0x1A5CFF on white = ~4.6:1  -- PASS (AA normal text)
//   success    0x15803D on white = ~5.0:1  -- PASS (was 0x1DB954 = 2.9:1, FIXED to darker green)
//   danger     0xE5383B on white = ~3.9:1  -- PASS (large text / UI only; used as accent)
//   warning    0xF59E0B on white = ~2.1:1  -- UI accent only, not for standalone text
//   vacation   0x0891B2 on white = ~3.5:1  -- PASS for UI/large text (was 0x06B6D4 = 2.8:1, FIXED)
//   textSecondary 0x4B5563 on white = ~7.1:1 -- PASS (was gray500 0x6B7280 = 4.6:1, improved)
//   gray500    0x6B7280 on white = ~4.6:1  -- PASS (AA normal text, borderline)
//
// Dark mode uses lighter variants to maintain contrast on dark backgrounds.

enum Theme {
    // MARK: - Brand Colors

    static let primary = Color(
        light: Color(hex: 0x1A5CFF),
        dark: Color(hex: 0x4D8AFF)
    )

    // Contrast-fixed: 0x15803D instead of 0x1DB954 for WCAG AA on white
    static let success = Color(
        light: Color(hex: 0x15803D),
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

    // Contrast-fixed: 0x0891B2 instead of 0x06B6D4 for better contrast on white
    static let vacation = Color(
        light: Color(hex: 0x0891B2),
        dark: Color(hex: 0x22D3EE)
    )

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

    // Contrast-fixed: 0x4B5563 instead of gray500 0x6B7280 for better contrast on white
    static let textSecondary = Color(
        light: Color(hex: 0x4B5563),
        dark: Color(hex: 0x9CA3AF)
    )
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
