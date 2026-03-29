---
name: Native App Strategy Decision
description: Strategic decision to build native iOS (Swift/SwiftUI) and Android (Kotlin/Jetpack Compose) apps to replace MAUI Blazor Hybrid app
type: project
---

Decision: Build native iOS and Android apps to replace the existing MAUI/Blazor Hybrid mobile app.

**Why:** The MAUI app works but lacks professional quality (WebView-based UI, incomplete screens for Urlaub and Settings). Native apps provide better performance, platform-native UX, and we have proven experience from fakturus.poi (already in stores with Swift/SwiftUI and Kotlin/Jetpack Compose).

**How to apply:** All mobile development planning should target native platforms. The existing backend (ASP.NET Core + FastEndpoints + PostgreSQL) requires no changes -- both apps use the same API v1 endpoints. Auth uses the same Azure B2C tenant (fakturus.onmicrosoft.com) with the existing app registration (ClientId: 3fb35bc6-8825-495e-b0a2-18e00352f968). Plan documents are in `.project/plan/`, design documents in `.project/design/`.
