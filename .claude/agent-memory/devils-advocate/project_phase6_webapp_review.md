---
name: Phase 6 Web-App Review Findings
description: Key findings from devils-advocate review of Phase 6 (Blazor Server Web-App) -- timer persistence, scope realism, consent legal basis, token cache, abo sync are critical
type: project
---

Phase 6 full review 2026-03-31. Overall verdict: recommended with changes.

**Critical findings (🔴):**

1. Timer-State in Blazor Circuit is volatile -- circuit disconnect loses running timer. Timer-Start MUST persist to backend immediately. E03-S01 missing this acceptance criterion. Check if native apps already have ActiveSession backend concept.

2. 54 Stories in 7-8 weeks unrealistic for solo dev. "Parallelisierung" doesn't exist for one person. Stripe alone can take 2-3 weeks. Recommend: MVP = E01-E03, E07, E08 Consent only (~20 stories, 5-6 weeks). Stripe/Reports/Vacation as Phase 6b.

3. Consent legal basis (Art. 6 lit. b vs Art. 7) still unresolved from Phase 5 review. E08 S01-S03 should be blocked until legal review confirms approach.

**Important findings (🟡):**

4. `AddInMemoryTokenCaches()` in Program.cs loses all tokens on Azure App Service restart. Either use `AddDistributedTokenCaches()` or document re-login as accepted tradeoff.

5. Cross-Platform Abo-Sync (E08-S07) vastly underestimated at "L" (~2-3 days). Three payment providers, race conditions, grace period harmonization (Apple 16d, Google varies, Stripe immediate). Should be separate phase, not bundled.

6. `@((MarkupString)legalContent)` is XSS vector. Acceptable for single-dev trusted content but should be documented as conscious decision.

7. Session timeout and token lifetime not specified in auth docs.

**Architecture decisions validated as correct:**
- Blazor Server over WASM/United -- correct for always-online web app
- Single ApiClient over Refit -- correct for ~5 endpoints server-side
- No Fluxor/Redux -- Scoped services + CascadingValue sufficient
- Stripe Server-Side Checkout -- no PCI scope, no Stripe.js needed
- New project over WASM retrofit -- clean break is right call
- Hard-cut migration -- only sane approach for solo dev

**Inconsistencies found:**
- E01-S04 specifies 5 interfaces (IWorkSessionsApiClient etc.) but architecture says single ApiClient without interfaces
- stripe.js listed in directory structure but text says it's not needed
- Keyboard Shortcuts listed as "Could Have" in MoSCoW but appear as hard acceptance criteria in E03 stories
- E02-S07 cookie for dismiss contradicts "no client-side state" philosophy

**How to apply:** Timer persistence is the #1 implementation risk. Scope reduction is #2 priority. When reviewing E03 implementation, verify backend persistence on timer start. Do not start Consent UI until legal basis confirmed.
