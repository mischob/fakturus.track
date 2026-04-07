---
name: Phase 3 Review Key Findings
description: Critical findings from Phase 3 planning review -- scope risk, widget race condition, DATEV validation gap, watchOS complexity
type: project
---

Phase 3 (Polish & Erweiterungen) review completed 2026-03-29. Key findings:

1. **Scope too large for 4 weeks**: 10 EPICs, 45 Stories. Recommended deferring Apple Watch (E02) or Live Activity (E04) to post-launch.
2. **Widget Quick Actions have a race condition**: Timer start time is wrong when app is opened later. AppIntent.perform() should execute directly, not via App Group flags.
3. **DATEV export format not validated**: The spec assumes a simplified CSV format that may not match actual DATEV Lodas/Lohn&Gehalt import requirements. External validation with a Steuerberater needed BEFORE implementation.
4. **TimeTrackingViewModel becoming a God Object**: 4+ EPICs add calls to each timer method. Suggested observer pattern or notifyExtensions() consolidation.
5. **SharedDefaults.swift imports WidgetKit**: Will fail on watchOS target. Needs #if os(iOS) guard.

**Why:** These findings address risks that could delay the Q3/Q4 2026 store launch.
**How to apply:** Reference these when reviewing Phase 3 implementation PRs. Watch for the widget race condition and DATEV format issues specifically.
