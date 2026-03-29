# Fakturus Track - Android

Android-App fuer Zeiterfassung mit Jetpack Compose und Material 3.

## Tech Stack

- **Kotlin 2.1** mit KSP
- **Jetpack Compose** mit Material 3
- **Room** fuer lokale Datenbank
- **Ktor Client** fuer HTTP
- **MSAL Android** fuer Azure AD B2C Auth
- **kotlinx.serialization** fuer JSON
- **WorkManager** fuer Background Sync

## Build

Oeffne das Projekt in Android Studio (Ladybug+) und fuehre einen Gradle Sync durch.

```
./gradlew assembleDebug
```

## Struktur

```
app/src/main/java/com/fakturus/track/
  FakturusTrackApp.kt      -- Application-Klasse
  MainActivity.kt           -- Single Activity, Compose Host
  ServiceContainer.kt       -- Service-Lifecycle (manuelle DI)
  Configuration.kt          -- B2C-Config, API-URLs
  services/                  -- Auth, API, Sync, Network
  features/                  -- Auth, TimeTracking, Overtime, Vacation, Settings
  models/                    -- Room Entities, DTOs, AppDatabase
  ui/theme/                  -- Material 3 Theme, Colors, Typography
  util/                      -- DateFormatting
```
