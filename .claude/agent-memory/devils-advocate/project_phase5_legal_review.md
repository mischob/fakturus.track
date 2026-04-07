---
name: Phase 5 Legal Compliance Review Findings
description: Key findings from devils-advocate review of Phase 5 (Legal/Consent/Store Compliance) planning -- consent vs contract basis is the critical open question
type: project
---

Phase 5 reviewed 2026-03-31. Overall verdict: recommended with changes.

Critical open question: The consent flow assumes DSGVO Art. 7 (Einwilligung) as legal basis, but data processing is justified via Art. 6 Abs. 1 lit. b (Vertragsdurchfuehrung). These are mutually exclusive approaches. The checkbox wording and the entire revocation flow depend on which legal basis the lawyer confirms.

**Why:** If the wrong legal basis is baked into the UI before legal review, the entire consent screen and revocation flow must be rebuilt.

**How to apply:**
- Block consent UI implementation until legal basis is clarified with lawyer
- Two delete paths (E02-S05 consent revocation + E04-S04 account deletion) should be merged into one
- SemVer for legal docs is YAGNI -- recommend simple integer versioning
- Missing: AVV with Azure (Art. 28), Verzeichnis der Verarbeitungstaetigkeiten (Art. 30), Widerrufsbelehrung for subscriptions
- Inconsistency: AGB say minimum age 16, Google Play target audience says 18+
- Offline consent is an acceptable risk but must be explicitly documented as conscious decision
