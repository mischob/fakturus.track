---
name: Marketing Integration Decisions
description: Key decisions from March 2026 marketing analysis integration -- pricing tiers, critical feature gaps, timeline impact
type: project
---

Marketing analysis completed 2026-03-29, integrated into planning same day.

**Three critical gaps identified and added to roadmap:**
1. Pausenerfassung (break tracking) -- legal requirement (ArbZG), added to Phase 1 (+1 week)
2. PDF/CSV Export -- baseline expectation (10/11 competitors have it), added to Phase 2 (+1 week)
3. Krankheitstage (sick days) -- expected feature (6/11 competitors), added to Phase 2

**Why:** Without breaks, the app is not legally compliant. Without export, users cannot prove compliance to authorities. These are non-negotiable for a "compliant" positioning.

**How to apply:** Timeline shifted from 22 to 24 weeks. Store launch now ~06 Sep 2026 (was ~23 Aug). All subsequent milestones shifted +2 weeks.

**Pricing model decided:** Freemium with 4 tiers (FREE/STARTER at 2.99 EUR/PRO at 4.99 EUR/TEAM at 3.99 EUR per user/month). Feature-gating implementation deferred to Phase 4 (launch). During development, all features available.

**Open decisions:**
- Break tracking backend: PauseMinutes field vs separate PauseEntry entity (recommended: simple field first)
- Sick days backend: Separate SickDay entity vs AbsenceDay with type field (recommended: type field for extensibility)
- Payment provider: Apple/Google IAP vs Stripe vs own billing (not decided yet)
- PDF generation: Client-side recommended (fits offline-first)
