---
name: Phase 4 Store-Launch Planung
description: Phase 4 Detailplanung erstellt am 2026-03-29 -- 10 EPICs, 32 Stories, 5 Wellen, Feature-Gating als kritischer Pfad
type: project
---

Phase 4 (Store-Launch) Detailplanung abgeschlossen am 2026-03-29.

**Scope:** Feature-Gating (FREE/STARTER/PRO), StoreKit 2 + Play Billing, App Store + Play Store Vorbereitung, Final QA, Launch, MAUI-Migration.

**Umfang:** 10 EPICs, 32 Stories, 5 Ausfuehrungswellen, geschaetzte Dauer 5 Wochen (Woche 24-28, Mitte September bis Mitte Oktober 2026).

**Kritischer Pfad:** E01 Feature-Gating (1 Wo) -> E02/E03 IAP (1 Wo) -> E04 Paywall (0.5 Wo) -> E08 Testing (1.5 Wo) -> E09 Launch (0.5 Wo) = 4.5 Wochen.

**Wichtige Entscheidungen:**
- Tier-Verwaltung rein client-seitig (StoreKit 2 / Play Billing), kein Backend-Tier-Check
- Gesetzliche Features (Timer, Pausen, Feiertage) muessen im FREE-Tier bleiben (ArbZG)
- Gesperrte Features werden sichtbar aber nicht bedienbar gehalten (Teaser-Effekt)
- Kein Datenverlust bei Tier-Wechsel (Downgrade = Read-Only, nicht loeschen)
- Sentry statt Firebase Crashlytics empfohlen (DSGVO, "keine Tracking-SDKs" Versprechen)
- Koordinierter Launch beider Plattformen (nicht iOS-only voraus)

**Why:** Phase 4 ist die letzte Phase vor oeffentlichem Launch. Das Zeitfenster der Zeiterfassungspflicht 2026 (Uebergangsfristen bis 2028) soll genutzt werden.

**How to apply:** Bei Feature-Gating-Implementierung immer pruefen ob ein Feature gesetzlich vorgeschrieben ist (dann FREE). Preise nie hardcoden. Store-Vorbereitung parallel zur Entwicklung starten.
