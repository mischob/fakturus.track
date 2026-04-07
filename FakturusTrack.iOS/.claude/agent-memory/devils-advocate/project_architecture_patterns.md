---
name: Architecture patterns in FakturusTrack iOS
description: Key architectural decisions and patterns found in existing iOS codebase -- Theme.swift dark mode, project.yml with XcodeGen, TimeTrackingViewModel structure
type: project
---

Observed architecture patterns (2026-03-29):

1. **Theme.swift uses Color(light:dark:) extension** -- already dark-mode-ready. Phase 3 E01 Dark Mode is primarily a color audit, not a rewrite.
2. **project.yml (XcodeGen)** -- single iOS target + test target. Phase 3 adds Widget Extension + watchOS App targets. The project.yml configuration for new targets needs careful validation.
3. **iOS deployment target: 17.0, Swift 6.0** -- enables modern APIs (Interactive Widgets, String Catalogs, contentTransition). No need for iOS 16 fallbacks.
4. **TimeTrackingViewModel is @Observable @MainActor** -- uses SwiftData ModelContext directly. Pause state persisted in UserDefaults for crash recovery. This is the central integration point for Phase 3 features.
5. **Single MSAL dependency** -- no third-party UI/utility libraries. Phase 3 adds Glance (Android only) but no new iOS SPM dependencies.

**Why:** Understanding existing patterns prevents over-engineering or contradicting established conventions.
**How to apply:** When reviewing Phase 3 implementations, verify they follow these patterns rather than introducing competing approaches.
