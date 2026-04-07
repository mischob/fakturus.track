---
name: Phase 4 Review Key Findings
description: Critical findings from Phase 4 (Store-Launch) planning review -- timeline risk, billing version, acknowledge retry, tier-assignment contradictions
type: project
---

Phase 4 (Store-Launch, Feature-Gating & Migration) review completed 2026-03-29. Key findings:

1. **Timeline too tight**: 0.5 weeks buffer on 4.5-week critical path. One App Store rejection blows the schedule. Recommended extracting E10 (MAUI Migration) to Phase 4b for 1.5 weeks buffer.
2. **Billing Library v7.1.1 doesn't exist**: Specs reference non-existent version. Must use actual latest 7.x.
3. **Android Acknowledge-Retry is a TODO**: BillingManager.kt has a TODO for WorkManager retry on failed acknowledge. Must be a concrete acceptance criterion, not a post-hoc TODO.
4. **Tier-assignment contradicts features.md**: Overtime Dashboard (FREE vs STARTER), Widgets (FREE vs STARTER), Calendar Import vs Calendar Integration (STARTER vs PRO) -- needs resolution.
5. **PaywallView StoreKitManager not injected**: Tech-spec has `@State private var` that is never assigned. Build error or nil crash.
6. **StoreKit initialized in onLogin() but works without login**: Anonymous mode users can never purchase. Decision needs explicit documentation.
7. **Sentry Opt-In vs Always-On inconsistent** between Privacy Policy spec and Testing spec.
8. **No Hilt in Android project**: implementation-checklist references "Hilt Singleton" but ServiceContainer is manual DI.

**Why:** Phase 4 is the final gate before public launch. These issues could cause App Store rejection, revenue loss (acknowledge failure), or implementation confusion (tier contradictions).
**How to apply:** Before Phase 4 sprint starts, resolve items 1-5. Items 6-8 can be resolved during sprint.
