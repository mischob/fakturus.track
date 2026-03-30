# Tech-Spec: EPIC 05 -- Android Widget (Glance) + App Shortcuts

## Uebersicht

Jetpack Glance Widget (Small + Medium) mit Quick Actions. DataStore fuer Widget-State. App Shortcuts fuer Long-Press auf App-Icon. Alles im bestehenden `app`-Modul (kein separates Modul).

---

## S01: Widget Setup

### Neue Dependencies (libs.versions.toml)

```toml
[versions]
glance = "1.1.1"

[libraries]
glance-appwidget = { group = "androidx.glance", name = "glance-appwidget", version.ref = "glance" }
glance-material3 = { group = "androidx.glance", name = "glance-material3", version.ref = "glance" }
```

```kotlin
// app/build.gradle.kts
dependencies {
    implementation(libs.glance.appwidget)
    implementation(libs.glance.material3)
}
```

### Widget Metadata: timer_widget_info.xml

```xml
<!-- res/xml/timer_widget_info.xml -->
<?xml version="1.0" encoding="utf-8"?>
<appwidget-provider xmlns:android="http://schemas.android.com/apk/res/android"
    android:minWidth="110dp"
    android:minHeight="110dp"
    android:minResizeWidth="110dp"
    android:minResizeHeight="110dp"
    android:maxResizeWidth="250dp"
    android:maxResizeHeight="110dp"
    android:targetCellWidth="2"
    android:targetCellHeight="2"
    android:resizeMode="horizontal|vertical"
    android:widgetCategory="home_screen"
    android:description="@string/widget_description"
    android:previewLayout="@layout/widget_preview"
    android:updatePeriodMillis="0" />
```

### AndroidManifest.xml Ergaenzung

```xml
<receiver
    android:name=".widget.TimerWidgetReceiver"
    android:exported="true"
    android:label="@string/widget_name">
    <intent-filter>
        <action android:name="android.appwidget.action.APPWIDGET_UPDATE" />
    </intent-filter>
    <meta-data
        android:name="android.appwidget.provider"
        android:resource="@xml/timer_widget_info" />
</receiver>
```

### WidgetStateHelper.kt (NEU)

```kotlin
// widget/WidgetStateHelper.kt
package com.fakturus.track.widget

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.*
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first

val Context.widgetDataStore: DataStore<Preferences> by preferencesDataStore(name = "timer_widget")

object WidgetKeys {
    val IS_TIMER_RUNNING = booleanPreferencesKey("isTimerRunning")
    val TIMER_START_MILLIS = longPreferencesKey("timerStartMillis")
    val IS_PAUSED = booleanPreferencesKey("isPaused")
    val PAUSE_MINUTES = intPreferencesKey("pauseMinutes")
    val TODAY_TOTAL_SECONDS = longPreferencesKey("todayTotalSeconds")
}

object WidgetStateHelper {
    suspend fun writeTimerState(
        context: Context,
        isRunning: Boolean,
        startTimeMillis: Long?,
        isPaused: Boolean,
        pauseMinutes: Int,
        todayTotalSeconds: Long
    ) {
        context.widgetDataStore.edit { prefs ->
            prefs[WidgetKeys.IS_TIMER_RUNNING] = isRunning
            startTimeMillis?.let { prefs[WidgetKeys.TIMER_START_MILLIS] = it }
                ?: prefs.remove(WidgetKeys.TIMER_START_MILLIS)
            prefs[WidgetKeys.IS_PAUSED] = isPaused
            prefs[WidgetKeys.PAUSE_MINUTES] = pauseMinutes
            prefs[WidgetKeys.TODAY_TOTAL_SECONDS] = todayTotalSeconds
        }

        // Widget-Update triggern
        TimerWidget().updateAll(context)
    }

    suspend fun readTimerState(context: Context): TimerWidgetState {
        val prefs = context.widgetDataStore.data.first()
        return TimerWidgetState(
            isRunning = prefs[WidgetKeys.IS_TIMER_RUNNING] ?: false,
            startTimeMillis = prefs[WidgetKeys.TIMER_START_MILLIS],
            isPaused = prefs[WidgetKeys.IS_PAUSED] ?: false,
            pauseMinutes = prefs[WidgetKeys.PAUSE_MINUTES] ?: 0,
            todayTotalSeconds = prefs[WidgetKeys.TODAY_TOTAL_SECONDS] ?: 0
        )
    }
}

data class TimerWidgetState(
    val isRunning: Boolean = false,
    val startTimeMillis: Long? = null,
    val isPaused: Boolean = false,
    val pauseMinutes: Int = 0,
    val todayTotalSeconds: Long = 0
)
```

---

## S02: Timer-Status Widget

### TimerWidgetReceiver.kt

```kotlin
// widget/TimerWidgetReceiver.kt
package com.fakturus.track.widget

import android.content.Context
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.GlanceAppWidgetReceiver

class TimerWidgetReceiver : GlanceAppWidgetReceiver() {
    override val glanceAppWidget: GlanceAppWidget = TimerWidget()
}
```

### TimerWidget.kt

```kotlin
// widget/TimerWidget.kt
package com.fakturus.track.widget

import android.content.Context
import androidx.compose.runtime.Composable
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.glance.*
import androidx.glance.action.actionStartActivity
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.SizeMode
import androidx.glance.appwidget.provideContent
import androidx.glance.layout.*
import androidx.glance.text.FontWeight
import androidx.glance.text.Text
import androidx.glance.text.TextStyle
import com.fakturus.track.MainActivity
import java.time.Duration
import java.time.Instant

class TimerWidget : GlanceAppWidget() {
    override val sizeMode = SizeMode.Responsive(
        setOf(
            DpSize(110.dp, 110.dp),  // Small (2x2)
            DpSize(250.dp, 110.dp)   // Medium (4x2)
        )
    )

    override suspend fun provideGlance(context: Context, id: GlanceId) {
        val state = WidgetStateHelper.readTimerState(context)

        provideContent {
            val size = LocalSize.current
            GlanceTheme {
                if (size.width >= 250.dp) {
                    MediumTimerWidget(state)
                } else {
                    SmallTimerWidget(state)
                }
            }
        }
    }
}

@Composable
private fun SmallTimerWidget(state: TimerWidgetState) {
    Column(
        modifier = GlanceModifier
            .fillMaxSize()
            .padding(12.dp)
            .background(GlanceTheme.colors.widgetBackground)
            .clickable(actionStartActivity<MainActivity>()),
        verticalAlignment = Alignment.CenterVertically,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        if (state.isRunning && !state.isPaused) {
            // Laufender Timer
            val elapsed = formatElapsed(state.startTimeMillis)
            Text(
                text = elapsed,
                style = TextStyle(
                    fontSize = 24.sp,
                    fontWeight = FontWeight.Bold,
                    color = GlanceTheme.colors.onSurface
                )
            )
            Spacer(modifier = GlanceModifier.height(4.dp))
            Text(
                text = "Laeuft",
                style = TextStyle(
                    fontSize = 12.sp,
                    color = GlanceTheme.colors.secondary
                )
            )
        } else if (state.isPaused) {
            Text(
                text = "Pausiert",
                style = TextStyle(
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold,
                    color = GlanceTheme.colors.onSurface
                )
            )
        } else {
            Text(
                text = "Bereit",
                style = TextStyle(
                    fontSize = 16.sp,
                    color = GlanceTheme.colors.secondary
                )
            )
        }
    }
}

@Composable
private fun MediumTimerWidget(state: TimerWidgetState) {
    Row(
        modifier = GlanceModifier
            .fillMaxSize()
            .padding(12.dp)
            .background(GlanceTheme.colors.widgetBackground),
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Links: Timer-Status
        Column(modifier = GlanceModifier.defaultWeight()) {
            if (state.isRunning && !state.isPaused) {
                Text(
                    text = formatElapsed(state.startTimeMillis),
                    style = TextStyle(fontSize = 28.sp, fontWeight = FontWeight.Bold)
                )
                Text(
                    text = "Seit ${formatStartTime(state.startTimeMillis)}",
                    style = TextStyle(fontSize = 12.sp, color = GlanceTheme.colors.secondary)
                )
            } else if (state.isPaused) {
                Text(text = "Pausiert", style = TextStyle(fontSize = 20.sp, fontWeight = FontWeight.Bold))
                Text(
                    text = "${state.pauseMinutes} Min Pause",
                    style = TextStyle(fontSize = 12.sp, color = GlanceTheme.colors.secondary)
                )
            } else {
                Text(text = "Bereit", style = TextStyle(fontSize = 20.sp, color = GlanceTheme.colors.secondary))
            }
        }

        // Rechts: Tages-Total + Buttons
        Column(horizontalAlignment = Alignment.End) {
            val h = state.todayTotalSeconds / 3600
            val m = (state.todayTotalSeconds % 3600) / 60
            Text(
                text = "${h}:${"%02d".format(m)}h",
                style = TextStyle(fontSize = 18.sp, fontWeight = FontWeight.Bold)
            )
            Text(
                text = "Heute",
                style = TextStyle(fontSize = 10.sp, color = GlanceTheme.colors.secondary)
            )
        }
    }
}

private fun formatElapsed(startTimeMillis: Long?): String {
    if (startTimeMillis == null) return "--:--"
    val elapsed = Duration.between(Instant.ofEpochMilli(startTimeMillis), Instant.now())
    val h = elapsed.toHours()
    val m = elapsed.toMinutes() % 60
    return "${h}:${"%02d".format(m)}"
}

private fun formatStartTime(startTimeMillis: Long?): String {
    if (startTimeMillis == null) return "--:--"
    val instant = Instant.ofEpochMilli(startTimeMillis)
    val time = instant.atZone(java.time.ZoneId.systemDefault()).toLocalTime()
    return "${time.hour}:${"%02d".format(time.minute)}"
}
```

---

## S03: Widget Quick Actions

### TimerWidgetActions.kt

```kotlin
// widget/TimerWidgetActions.kt
package com.fakturus.track.widget

import android.content.Context
import androidx.glance.GlanceId
import androidx.glance.action.ActionParameters
import androidx.glance.appwidget.action.ActionCallback

class StartTimerAction : ActionCallback {
    override suspend fun onAction(context: Context, glanceId: GlanceId, parameters: ActionParameters) {
        // Widget-Action in Preferences schreiben
        context.widgetDataStore.edit { prefs ->
            prefs[booleanPreferencesKey("widgetAction_start")] = true
        }

        // Sofort Widget-State aktualisieren
        WidgetStateHelper.writeTimerState(
            context = context,
            isRunning = true,
            startTimeMillis = System.currentTimeMillis(),
            isPaused = false,
            pauseMinutes = 0,
            todayTotalSeconds = 0
        )
    }
}

class StopTimerAction : ActionCallback {
    override suspend fun onAction(context: Context, glanceId: GlanceId, parameters: ActionParameters) {
        context.widgetDataStore.edit { prefs ->
            prefs[booleanPreferencesKey("widgetAction_stop")] = true
        }

        WidgetStateHelper.writeTimerState(
            context = context,
            isRunning = false,
            startTimeMillis = null,
            isPaused = false,
            pauseMinutes = 0,
            todayTotalSeconds = 0
        )
    }
}

class PauseResumeAction : ActionCallback {
    override suspend fun onAction(context: Context, glanceId: GlanceId, parameters: ActionParameters) {
        val state = WidgetStateHelper.readTimerState(context)

        if (state.isPaused) {
            context.widgetDataStore.edit { prefs ->
                prefs[booleanPreferencesKey("widgetAction_resume")] = true
            }
        } else {
            context.widgetDataStore.edit { prefs ->
                prefs[booleanPreferencesKey("widgetAction_pause")] = true
            }
        }

        WidgetStateHelper.writeTimerState(
            context = context,
            isRunning = state.isRunning,
            startTimeMillis = state.startTimeMillis,
            isPaused = !state.isPaused,
            pauseMinutes = state.pauseMinutes,
            todayTotalSeconds = state.todayTotalSeconds
        )
    }
}
```

### Medium Widget mit Buttons (Ergaenzung in TimerWidget.kt)

```kotlin
// Im MediumTimerWidget -- Button-Zeile:
Row(modifier = GlanceModifier.fillMaxWidth(), horizontalAlignment = Alignment.End) {
    if (state.isRunning) {
        Button(
            text = if (state.isPaused) "Weiter" else "Pause",
            onClick = actionRunCallback<PauseResumeAction>()
        )
        Spacer(modifier = GlanceModifier.width(4.dp))
        Button(
            text = "Stop",
            onClick = actionRunCallback<StopTimerAction>()
        )
    } else {
        Button(
            text = "Start",
            onClick = actionRunCallback<StartTimerAction>()
        )
    }
}
```

### Integration in TimeTrackingViewModel.kt

```kotlin
// Am Ende von startSession():
viewModelScope.launch {
    val context = /* Application Context */
    WidgetStateHelper.writeTimerState(
        context, isRunning = true,
        startTimeMillis = Instant.parse(session.startTime).toEpochMilli(),
        isPaused = false, pauseMinutes = 0, todayTotalSeconds = 0
    )
}
```

**Hinweis**: Der Application Context wird ueber den `ServiceContainer` verfuegbar gemacht oder direkt im ViewModel uebergeben (analog zu bestehendem `prefs: SharedPreferences?` Parameter).

---

## S04: App Shortcuts

### shortcuts.xml

```xml
<!-- res/xml/shortcuts.xml -->
<shortcuts xmlns:android="http://schemas.android.com/apk/res/android">
    <shortcut
        android:shortcutId="start_timer"
        android:enabled="true"
        android:shortcutShortLabel="@string/shortcut_start_timer"
        android:shortcutLongLabel="@string/shortcut_start_timer_long"
        android:icon="@drawable/ic_play">
        <intent
            android:action="com.fakturus.track.ACTION_START_TIMER"
            android:targetPackage="com.fakturus.track"
            android:targetClass="com.fakturus.track.MainActivity" />
    </shortcut>
    <shortcut
        android:shortcutId="view_history"
        android:enabled="true"
        android:shortcutShortLabel="@string/shortcut_view_history"
        android:icon="@drawable/ic_history">
        <intent
            android:action="com.fakturus.track.ACTION_VIEW_HISTORY"
            android:targetPackage="com.fakturus.track"
            android:targetClass="com.fakturus.track.MainActivity" />
    </shortcut>
</shortcuts>
```

### AndroidManifest.xml

```xml
<!-- In <activity android:name=".MainActivity"> -->
<meta-data
    android:name="android.app.shortcuts"
    android:resource="@xml/shortcuts" />

<!-- Intent-Filter fuer Shortcut-Actions -->
<intent-filter>
    <action android:name="com.fakturus.track.ACTION_START_TIMER" />
    <category android:name="android.intent.category.DEFAULT" />
</intent-filter>
<intent-filter>
    <action android:name="com.fakturus.track.ACTION_VIEW_HISTORY" />
    <category android:name="android.intent.category.DEFAULT" />
</intent-filter>
```

### MainActivity.kt -- Intent verarbeiten

```kotlin
// In onCreate():
when (intent?.action) {
    "com.fakturus.track.ACTION_START_TIMER" -> {
        // Auto-Start Timer nach Login
        // Flag setzen, das vom TimeTrackingViewModel gelesen wird
    }
    "com.fakturus.track.ACTION_VIEW_HISTORY" -> {
        // Navigation zum Zeiten-Tab (Default)
    }
}
```
