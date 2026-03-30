# Tech-Spec: EPIC 08 -- Final Testing & QA

## Uebersicht

Kein neuer Feature-Code. Crash-Monitoring-Integration (Sentry) und systematisches Testen aller Features inkl. Feature-Gating Edge Cases.

---

## Sentry Integration

### iOS

**Dependency (SPM)**:
```
https://github.com/getsentry/sentry-cocoa (Version 8.x)
```

**Integration in FakturusTrackApp.swift**:
```swift
import Sentry

@main
struct FakturusTrackApp: App {
    init() {
        SentrySDK.start { options in
            options.dsn = Configuration.sentryDSN
            options.debug = false
            options.tracesSampleRate = 0 // Kein Performance Monitoring
            options.attachStacktrace = true
            options.enableAutoSessionTracking = true
            // DSGVO: Keine User-ID mitsenden
            options.beforeSend = { event in
                event.user = nil
                return event
            }
        }
    }
    // ...
}
```

**dSYM Upload (Xcode Build Phase)**:
```bash
# Run Script Phase in Xcode (nach Build):
if [ "$CONFIGURATION" = "Release" ]; then
    sentry-cli upload-dif "$DWARF_DSYM_FOLDER_PATH"
fi
```

### Android

**Dependency (Gradle)**:
```toml
# libs.versions.toml
[versions]
sentry = "7.8.0"

[libraries]
sentry-android = { group = "io.sentry", name = "sentry-android", version.ref = "sentry" }
```

**Integration in FakturusTrackApp.kt**:
```kotlin
class FakturusTrackApp : Application() {
    override fun onCreate() {
        super.onCreate()

        SentryAndroid.init(this) { options ->
            options.dsn = Configuration.sentryDSN
            options.isDebug = BuildConfig.DEBUG
            options.tracesSampleRate = 0.0 // Kein Performance Monitoring
            options.isEnableAutoSessionTracking = true
            // DSGVO: Keine User-ID
            options.beforeSend = SentryOptions.BeforeSendCallback { event, _ ->
                event.user = null
                event
            }
        }

        serviceContainer = ServiceContainer(this)
    }
}
```

**ProGuard Mapping Upload (Gradle Plugin)**:
```kotlin
// app/build.gradle.kts
plugins {
    id("io.sentry.android.gradle") version "4.x"
}

sentry {
    org.set("fakturus")
    projectName.set("fakturus-track-android")
    autoUploadProguardMapping.set(true)
}
```

### Opt-In Strategie

Option A (empfohlen fuer Launch): Crash-Reporting immer aktiv, in Privacy Policy dokumentiert.
Option B (strenger DSGVO): Opt-In Dialog beim ersten Start.

Entscheidung: **Option A** -- anonymisierte Crash-Reports ohne User-Bezug fallen unter berechtigtes Interesse (Art. 6(1)(f) DSGVO). In der Privacy Policy transparent dokumentieren.

---

## Testmatrix

### Geraete

| Plattform | Geraet / Emulator | OS-Version |
|-----------|-------------------|------------|
| iOS | iPhone 15 Pro (Simulator) | iOS 17 |
| iOS | iPhone 16 Pro (Simulator) | iOS 18 |
| iOS | Physisches Geraet (fuer StoreKit Sandbox) | iOS 17+ |
| Android | Pixel 7 (Emulator) | Android 13 (API 33) |
| Android | Pixel 8 (Emulator) | Android 14 (API 34) |
| Android | Physisches Geraet (fuer License Testing) | Android 13+ |

### Test-Szenarien Feature-Gating

| Szenario | Erwartetes Verhalten | iOS | Android |
|----------|---------------------|-----|---------|
| FREE: Timer starten/stoppen | Funktioniert | [ ] | [ ] |
| FREE: History > 30 Tage | Aeltere ausgeblendet, Upgrade-Hinweis | [ ] | [ ] |
| FREE: Urlaub-Tab oeffnen | PaywallTeaser angezeigt | [ ] | [ ] |
| FREE: Gesamt-Tab oeffnen | Dashboard-Cards gesperrt | [ ] | [ ] |
| FREE: Export antippen | Lock-Icon, Paywall oeffnet sich | [ ] | [ ] |
| FREE: Krankheitstag (Long-Press) | Menue-Eintrag nicht sichtbar | [ ] | [ ] |
| FREE: Schulferien-Settings | Lock-Icon, Paywall bei Tap | [ ] | [ ] |
| STARTER: Alle STARTER-Features | Freigeschaltet, funktional | [ ] | [ ] |
| STARTER: DATEV-Export | Gesperrt (PRO) | [ ] | [ ] |
| PRO: Alle Features | Alles freigeschaltet | [ ] | [ ] |
| Upgrade FREE->STARTER | Sofort freigeschaltet, Lock weg | [ ] | [ ] |
| Upgrade STARTER->PRO | Sofort freigeschaltet | [ ] | [ ] |
| Downgrade PRO->FREE | Daten Read-Only, History 30 Tage | [ ] | [ ] |
| Offline + STARTER | Gecachter Tier, Features verfuegbar | [ ] | [ ] |
| App-Neustart + Abo | Tier korrekt wiederhergestellt | [ ] | [ ] |
| Restore Purchases | Abo wiederhergestellt nach Reinstall | [ ] | [ ] |

### Test-Szenarien Kauf-Flow

| Szenario | iOS (Sandbox) | Android (License Testing) |
|----------|---------------|--------------------------|
| Starter kaufen | [ ] | [ ] |
| Pro kaufen | [ ] | [ ] |
| Kauf abbrechen | [ ] | [ ] |
| Upgrade Starter->Pro | [ ] | [ ] |
| Abo kuendigen (warten auf Ablauf) | [ ] | [ ] |
| Kauf waehrend offline | [ ] (pending) | [ ] |
| App in Background waehrend Kauf | [ ] | [ ] |

### Regressions-Test Phase 1-3

Vollstaendige Checkliste in epic-08-final-testing.md (P4-E08-S02). Kurzfassung:

- [ ] Login (Apple, Google, E-Mail)
- [ ] Timer: Start -> Pause -> Weiter -> Stop -> Fertig
- [ ] Manuelle Session, Bearbeiten, Loeschen
- [ ] History: Monatsgruppierung, Scrollen
- [ ] Offline: Session erstellen -> Netzwerk an -> Sync
- [ ] Pull-to-Refresh
- [ ] Settings: Alle Felder aenderbar, Sync
- [ ] Urlaub: Setzen/Entfernen, Feiertage
- [ ] Krankheitstag: Setzen/Entfernen
- [ ] Ueberstunden-Dashboard: Korrekte Berechnung
- [ ] PDF/CSV/DATEV Export
- [ ] Dark Mode
- [ ] Widgets (Timer starten via Widget)
- [ ] Lokalisierung (DE + EN)
- [ ] VoiceOver/TalkBack

---

## Performance-Ziele

| Metrik | Ziel | Mess-Tool |
|--------|------|-----------|
| Cold Start | < 1 Sekunde | Instruments / Android Profiler |
| History Scrolling | 60fps bei 200+ Eintraegen | Instruments / GPU Monitor |
| Tab-Wechsel | < 200ms | Instruments / Systrace |
| Memory | < 50MB (iOS), < 80MB (Android) | Allocations / Memory Profiler |
| App-Groesse | < 30MB Download | App Thinning / Play Console |
| Crash-Free Rate | >= 99.5% | Sentry Dashboard |

---

## Beta-Distribution

### iOS TestFlight

1. Archive in Xcode erstellen
2. Upload zu App Store Connect (Xcode oder Transporter)
3. External Testing Group erstellen
4. Beta App Review abwarten (1-2 Tage)
5. Oeffentlichen Link oder E-Mail-Einladung verteilen
6. "Was ist neu" Text fuer Tester

### Android Internal Testing

1. Signed AAB erstellen
2. Upload zu Google Play Console -> Internal Testing Track
3. Tester-Liste (Google-Accounts) konfigurieren
4. Opt-In Link verteilen (kein Review noetig)

### Feedback-Kanal

- TestFlight: In-App Feedback (Screenshot + Text)
- Android: E-Mail an support@fakturus.com
- Mindestens 10 externe Tester, 1 Woche Beta-Phase
