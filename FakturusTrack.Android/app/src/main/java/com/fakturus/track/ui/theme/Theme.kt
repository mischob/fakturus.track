package com.fakturus.track.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightColorScheme = lightColorScheme(
    primary = Primary,
    onPrimary = Gray50,
    primaryContainer = Primary.copy(alpha = 0.12f),
    onPrimaryContainer = Primary,
    secondary = PauseColor,
    onSecondary = Gray50,
    secondaryContainer = PauseColor.copy(alpha = 0.12f),
    onSecondaryContainer = PauseColor,
    tertiary = Vacation,
    onTertiary = Gray50,
    error = Danger,
    onError = Gray50,
    errorContainer = Danger.copy(alpha = 0.12f),
    onErrorContainer = Danger,
    background = Gray50,
    onBackground = Gray900,
    surface = Gray50,
    onSurface = Gray900,
    surfaceVariant = Gray100,
    onSurfaceVariant = Gray700,
    outline = Gray200,
    outlineVariant = Gray100
)

private val DarkColorScheme = darkColorScheme(
    primary = PrimaryDark,
    onPrimary = Gray900,
    primaryContainer = PrimaryDark.copy(alpha = 0.12f),
    onPrimaryContainer = PrimaryDark,
    secondary = PauseColorDark,
    onSecondary = Gray900,
    secondaryContainer = PauseColorDark.copy(alpha = 0.12f),
    onSecondaryContainer = PauseColorDark,
    tertiary = Vacation,
    onTertiary = Gray900,
    error = DangerDark,
    onError = Gray900,
    errorContainer = DangerDark.copy(alpha = 0.12f),
    onErrorContainer = DangerDark,
    background = Gray50Dark,
    onBackground = Gray900Dark,
    surface = Gray50Dark,
    onSurface = Gray900Dark,
    surfaceVariant = Gray100Dark,
    onSurfaceVariant = Gray500,
    outline = Gray200Dark,
    outlineVariant = Gray100Dark
)

@Composable
fun FakturusTrackTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    // Dynamic color explicitly disabled -- use own branding
    // Status bar appearance is handled by enableEdgeToEdge() in MainActivity
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
