# EPIC 08: Consent-Flow & Subscription (Stripe)

## Ziel

Consent-Flow bei erstmaligem Login (AGB-Zustimmung + Datenschutz-Kenntnisnahme) und Stripe-basierte Abo-Verwaltung. Web-Abos muessen kompatibel mit Mobile-Abos (Apple IAP / Google Billing) sein.

---

## Stories

### S01: Consent-Check nach Login
**Als** neuer Benutzer **moechte ich** nach dem Login aufgefordert werden, AGB und Datenschutzerklaerung zu akzeptieren, **damit** die Nutzung rechtlich abgesichert ist.

**Akzeptanzkriterien:**
- [ ] Nach erfolgreichem Login: API-Check `/api/legal/consent`
- [ ] Falls keine Zustimmung vorhanden: Weiterleitung auf Consent-Seite
- [ ] Falls Zustimmung vorhanden: Weiterleitung auf Dashboard
- [ ] Consent-Status wird im App-State gecached (kein API-Call bei jedem Seitenwechsel)

**Aufwand:** S

---

### S02: Consent-Seite
**Als** neuer Benutzer **moechte ich** AGB und Datenschutz lesen und zustimmen, **damit** ich die App nutzen kann.

**Akzeptanzkriterien:**
- [ ] Eigene Route: `/consent` (authentifiziert, aber ohne Sidebar)
- [ ] Zentriertes Layout, aehnlich wie Login
- [ ] Zwei Checkboxen:
  - "Ich stimme den AGB zu." + Link [AGB lesen] (oeffnet /terms in neuem Tab)
  - "Ich habe die Datenschutzerklaerung zur Kenntnis genommen." + Link [Lesen]
- [ ] "Weiter"-Button: Disabled bis beide Checkboxen aktiviert
- [ ] Bei Klick auf "Weiter": API-Call POST `/api/legal/consent`
- [ ] Consent-Daten: Version der AGB + Datenschutzerklaerung, Timestamp
- [ ] Nach Consent: Weiterleitung auf Dashboard
- [ ] Kein "Abbrechen" (ohne Consent keine Nutzung, nur Logout moeglich)

**Aufwand:** M

---

### S03: Re-Consent bei neuen Versionen
**Als** bestehender Benutzer **moechte ich** ueber aktualisierte AGB/Datenschutzerklaerung informiert werden, **damit** ich den neuen Bedingungen zustimmen kann.

**Akzeptanzkriterien:**
- [ ] API-Check vergleicht aktuelle Legal-Version mit Consent-Version
- [ ] Falls neue Version: Consent-Seite wird erneut angezeigt
- [ ] Hinweis auf der Consent-Seite: "Die Nutzungsbedingungen wurden aktualisiert."
- [ ] "Aenderungen anzeigen"-Link (falls Diff verfuegbar)
- [ ] Gleicher Flow wie erstmaliger Consent (S02)

**Aufwand:** S

---

### S04: Stripe Checkout Integration
**Als** Benutzer **moechte ich** ein Abonnement per Kreditkarte oder SEPA abschliessen, **damit** ich alle Features nutzen kann.

**Akzeptanzkriterien:**
- [ ] "Upgrade"-Button in Einstellungen und bei Feature-Gates
- [ ] Stripe Checkout Session wird server-seitig erstellt
- [ ] Redirect zu Stripe Checkout (hosted payment page)
- [ ] Erfolgs-Redirect zurueck zur Web-App (`/settings?subscription=success`)
- [ ] Abbruch-Redirect zurueck zur Web-App (`/settings?subscription=cancelled`)
- [ ] Stripe Webhook verarbeitet Events:
  - `checkout.session.completed` -> Abo aktivieren
  - `invoice.paid` -> Abo verlaengern
  - `customer.subscription.deleted` -> Abo kuendigen
- [ ] Tiers: FREE / STARTER (2.99 EUR) / PRO (4.99 EUR)
- [ ] Zahlungsmethoden: Kreditkarte, SEPA Lastschrift

**Aufwand:** L

---

### S05: Stripe Customer Portal
**Als** Benutzer **moechte ich** mein Abo selbst verwalten (kuendigen, Zahlungsmethode aendern), **damit** ich unabhaengig bin.

**Akzeptanzkriterien:**
- [ ] "Abo verwalten"-Button in Einstellungen
- [ ] Redirect zum Stripe Customer Portal (hosted)
- [ ] Dort moeglich: Kuendigung, Zahlungsmethode aendern, Rechnungshistorie
- [ ] Rueck-Redirect zur Web-App
- [ ] Aktuelle Abo-Info in Einstellungen angezeigt:
  - Tier-Name (FREE/STARTER/PRO)
  - Preis
  - Naechstes Rechnungsdatum
  - Status (aktiv/gekuendigt/ablaufend)

**Aufwand:** M

---

### S06: Feature-Gating
**Als** Benutzer im Free-Tier **moechte ich** sehen welche Features Premium sind, **damit** ich eine informierte Upgrade-Entscheidung treffen kann.

**Akzeptanzkriterien:**
- [ ] Feature-Gating basierend auf Subscription-Tier:
  - FREE: Timer + 30 Tage History
  - STARTER: + Urlaub, Reports (ohne Export)
  - PRO: Alle Features inkl. Export, DATEV
- [ ] Gesperrte Features: Sichtbar aber mit "PRO"-Badge und Overlay
- [ ] Klick auf gesperrtes Feature: Upgrade-Dialog mit Tier-Vergleich
- [ ] Tier-Info via API: `/api/subscription/status`

**Aufwand:** M

---

### S07: Cross-Platform Abo-Synchronisation
**Als** Benutzer **moechte ich** dass mein Abo plattformuebergreifend gilt, **damit** ich nicht doppelt bezahlen muss.

**Akzeptanzkriterien:**
- [ ] Backend speichert Subscription-Status zentral (unabhaengig von Zahlungsquelle)
- [ ] Apple IAP, Google Billing und Stripe schreiben auf das gleiche User-Abo
- [ ] Web-App liest Abo-Status ueber Backend-API
- [ ] Kein direkter Stripe<->Apple/Google Sync (Backend ist Single Source of Truth)
- [ ] Hinweis bei bestehendem Mobile-Abo: "Ihr Abo ueber [Apple/Google] ist aktiv"
- [ ] Kein erneuter Checkout wenn Mobile-Abo aktiv

**Aufwand:** L

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Consent-Check | S | E01-S03, E07 |
| S02 Consent-Seite | M | S01, E07 |
| S03 Re-Consent | S | S02 |
| S04 Stripe Checkout | L | E01-S03, Backend Stripe Endpoints |
| S05 Stripe Portal | M | S04 |
| S06 Feature-Gating | M | S04 |
| S07 Cross-Platform Sync | L | S04, Backend, Mobile Apps |

**Gesamt: ca. 1.5 Wochen** (Consent schnell, Stripe aufwaendiger)

## Backend-Anforderungen (Vorarbeit)

Diese Stories erfordern Backend-Arbeit, die VOR oder PARALLEL zur Web-App-Entwicklung stattfinden muss:

1. **Stripe-Integration im Backend:**
   - Stripe SDK einbinden
   - Checkout Session Endpoint: POST `/api/subscription/checkout`
   - Customer Portal Endpoint: POST `/api/subscription/portal`
   - Webhook Endpoint: POST `/api/subscription/webhook`
   - Subscription Status Endpoint: GET `/api/subscription/status`

2. **Cross-Platform Abo-Modell:**
   - `Subscription`-Entity: UserId, Tier, Source (stripe/apple/google), ExpiresAt, Status
   - Bestehende Apple/Google Endpoints muessen auf das gleiche Modell schreiben
   - Feature-Gate Middleware: Tier-Check bei geschuetzten Endpoints
