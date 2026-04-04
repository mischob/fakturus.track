---
name: Phase 1 Epic 11 Offline-Login Review
description: Review findings for E11 Offline-Login -- AppStartCoordinator violates ADR-002/006, OfflineSession has unnecessary PII, 6 stories should be 4
type: project
---

Epic 11 (Offline-Login & Session-Persistierung) reviewed 2026-04-04. Overall verdict: recommended with changes.

**Critical findings (red):**

1. AppStartCoordinator is a new Coordinator class that violates ADR-002 (flat architecture) and ADR-006 (no UseCase/Orchestrator classes). Resolve logic should live directly on AuthManager as `resolveStartState()`.

2. OfflineSession stores PII (email, displayName, loginProvider) unnecessarily. Only userId + lastSuccessfulAuth are needed for the offline decision. Extra fields increase attack surface and add undocumented Art. 30 processing activity.

**Important findings (yellow):**

3. S05 (expiry warning at day 12/14) is YAGNI -- practically no user will be 12+ days continuously offline. S04 already handles the actual expiry case.

4. `acquireTokenSilently()` in offline path may trigger network call with long timeout despite NetworkMonitor saying "offline". Must use forceRefresh=false or skip MSAL entirely when offline.

5. S02 (fast network detection) is not a real story -- NetworkMonitor from E04 already provides this. Should merge into S03.

**Recommendation:** Reduce from 6 to 4 stories (drop S02 into S03, drop S05). Eliminate AppStartCoordinator. Minimize OfflineSession fields.

**How to apply:** When implementing E11, verify that no new Coordinator/Orchestrator classes are created. AuthManager is the right home for start-state resolution. When reviewing OfflineSession implementation, check that only minimal non-PII fields are stored in Keychain/EncryptedSharedPreferences.
