---
name: Phase 6 Web-App Planning
description: Decision to build Blazor Server web-app at track.fakturus.com replacing WASM frontend, 8 EPICs, ~7-8 weeks
type: project
---

Phase 6: Web-App (Blazor Server) planned on 2026-03-31. Replaces veraltetes Blazor WASM Frontend.

Key decisions:
- New project `Fakturus.Track.WebApp` (not refactoring existing WASM)
- Blazor Server (not WASM) for simpler state management, no offline needed
- Hosting: track.fakturus.com (separate from api.track.fakturus.com)
- Auth: Azure AD B2C (same tenant as mobile), Google + E-Mail only (no Apple Sign-In on web)
- Payment: Stripe (not App Store IAP), must be cross-platform compatible
- Desktop-first with sidebar navigation, tablet as secondary target
- No mobile layout (<768px) -- redirect to native apps
- Legal pages (/privacy, /terms, /imprint) public without login

8 EPICs, 48 stories, ~7-8 weeks with parallelization.
MVP: E01 Setup + E02 Layout + E03 Timer/History + E07 Legal + E08 Consent

**Why:** Desktop users (office workers, freelancers, employers doing exports) need browser access. Also enables Stripe payments (30% less fees than App Store) and proper legal page hosting.

**How to apply:** When making decisions about the web-app, reference the design doc at .project/design/web-app-design.md and stories at .project/stories/phase6-webapp/. Cross-platform abo sync is the highest-risk item -- requires backend work before E08.
