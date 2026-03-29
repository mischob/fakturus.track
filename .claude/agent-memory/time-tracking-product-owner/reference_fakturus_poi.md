---
name: fakturus.poi Architecture Reference
description: Architecture patterns from the sister app fakturus.poi that should be reused for Track native apps
type: reference
---

fakturus.poi is at /Users/mischob/Projects/fakturus.poi with native iOS (Swift/SwiftUI) and Android (Kotlin/Jetpack Compose) apps.

Key patterns to reuse:
- **AuthManager.swift**: MSAL B2C integration with acquireTokenSilently(), domain hints for social login (apple.com, google.com, live.com)
- **APIClient.swift**: URLSession-based with custom PascalCase key decoding, ISO8601 date handling, automatic Bearer token injection
- **LoginView.swift**: Social login buttons (SignInButton component) with per-provider domain hints
- **AppState.swift (@Observable)**: Global app state, tab selection, auth status
- **FakturusPOIApp.swift**: Service initialization pattern -- services created only after auth, ViewModels get services via initializer injection
- **Configuration.swift**: Enum with static properties for B2C config, API URLs

fakturus.poi B2C config (different app registration): ClientId 4f8fbb19-2b03-4684-ab9d-32f1448a08dd, Policy B2C_1_OpenSignUpSignIn
Track B2C config (to reuse): ClientId 3fb35bc6-8825-495e-b0a2-18e00352f968, Policy B2C_1_BetaSignInOnly
