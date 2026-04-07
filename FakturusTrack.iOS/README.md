# FakturusTrack iOS

Native iOS app for Fakturus Track time tracking.

## Requirements

- Xcode 16+
- iOS 17.0+ deployment target
- Swift 6.0

## Setup

1. Open Xcode and create a new iOS App project:
   - Product Name: **FakturusTrack**
   - Bundle Identifier: **com.fakturus.track**
   - Interface: **SwiftUI**
   - Language: **Swift**
   - Minimum Deployment: **iOS 17.0**

2. Replace the generated source files with the files from `FakturusTrack/`.

3. Configure project settings:
   - Swift Language Version: **6.0** (Strict Concurrency)
   - Add SPM dependency: `https://github.com/AzureAD/microsoft-authentication-library-for-objc` (MSAL)

4. Configure entitlements:
   - Keychain Sharing: `com.fakturus.track`
   - Background Modes: Background fetch

5. Configure Info.plist:
   - `CFBundleURLSchemes`: `msauth.com.fakturus.track` (for MSAL redirect)
   - `NSAppTransportSecurity` > `NSExceptionDomains` > `localhost` with `NSExceptionAllowsInsecureHTTPLoads: true` (Debug only)

6. Build and run on Simulator.

## Project Structure

```
FakturusTrack/
  App/              -- Entry point, AppState, ServiceContainer, Configuration, Theme
  Models/           -- SwiftData models + DTOs (E03)
  Services/         -- Auth, API, Sync, Network (E02/E04/E07)
  Features/         -- Auth, TimeTracking, Shell (E02/E05/E06)
  Shared/           -- Shared UI components (E10)
  Extensions/       -- Date, TimeInterval extensions
  Resources/        -- Assets, Localizable
```
