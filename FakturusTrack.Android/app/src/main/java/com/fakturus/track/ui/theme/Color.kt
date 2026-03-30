package com.fakturus.track.ui.theme

import androidx.compose.ui.graphics.Color

// Primary
val Primary = Color(0xFF1A5CFF)
val PrimaryDark = Color(0xFF4D8AFF)

// Success / Timer Active -- Improved contrast for WCAG AA on white background
val Success = Color(0xFF15803D)       // Was 0xFF1DB954 -- darker green for better contrast on light
val SuccessDark = Color(0xFF34D96E)
val TimerRunning = Success
val TimerRunningDark = SuccessDark

// Danger
val Danger = Color(0xFFE5383B)
val DangerDark = Color(0xFFFF6B6B)

// Warning
val Warning = Color(0xFFF59E0B)

// Pause / Holiday
val PauseColor = Color(0xFF8B5CF6)
val PauseColorDark = Color(0xFFA78BFA)

// Vacation -- Improved contrast for WCAG AA on white background
val Vacation = Color(0xFF0891B2)       // Was 0xFF06B6D4 -- darker cyan for better contrast on light

// Sick Day
val SickDay = Color(0xFFEF4444)

// Timer states
val TimerPaused = Warning
val TimerStopped = Color(0xFFFF8C00)

// Sync
val SyncPending = Warning
val SyncDone = Success

// Offline
val OfflineBanner = Warning

// Grays - Light
val Gray50 = Color(0xFFF9FAFB)
val Gray100 = Color(0xFFF3F4F6)
val Gray200 = Color(0xFFE5E7EB)
val Gray500 = Color(0xFF6B7280)
val Gray600 = Color(0xFF4B5563)       // Added for better secondary text contrast
val Gray700 = Color(0xFF374151)
val Gray900 = Color(0xFF111827)

// Grays - Dark
val Gray50Dark = Color(0xFF0F1117)
val Gray100Dark = Color(0xFF1A1D27)
val Gray200Dark = Color(0xFF2D3040)
val Gray900Dark = Color(0xFFF0F1F3)
