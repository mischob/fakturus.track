# EPIC 04: Paywall-UI & Upgrade-Flow

## Ziel

Eine ansprechende, conversion-optimierte Paywall, die Nutzer beim Versuch ein gesperrtes Feature zu nutzen zum Upgrade motiviert. Die Paywall muss plattform-nativ wirken, die Tier-Vorteile klar kommunizieren und den Kauf-Flow nahtlos integrieren.

## Abhaengigkeiten

- **E01 (Feature-Gating)**: Tier-System und Feature-Flags
- **E02 (StoreKit 2)**: iOS Purchase-Flow
- **E03 (Play Billing)**: Android Purchase-Flow

---

## Stories

### P4-E04-S01: iOS Paywall Screen

**Als** FREE-Nutzer
**moechte ich** eine uebersichtliche Darstellung der Abo-Optionen sehen,
**damit** ich eine informierte Kaufentscheidung treffen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P4-E02-S02 (StoreKit Integration)
**Parallelisierbar mit**: P4-E04-S02 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Paywall-Screen als Sheet (Half-Sheet oder Full-Screen, je nach Kontext):
  - Header: "Mehr aus Fakturus Track herausholen"
  - Feature-Vergleich: Drei Spalten (FREE / STARTER / PRO) mit Checkmarks
  - Preis-Anzeige: Dynamisch aus StoreKit 2 (NICHT hardcoded)
  - CTA-Buttons: "Starter abonnieren" / "Pro abonnieren"
  - "Kaeufe wiederherstellen" Link unten
  - Nutzungsbedingungen + Datenschutz Links (Apple Pflicht)
  - Abo-Hinweis: "Abo verlaengert sich automatisch. Kuendigung jederzeit ueber die Apple-ID-Einstellungen."
- [ ] Feature-Vergleich zeigt kontextabhaengig das relevante Feature hervorgehoben:
  - Nutzer tippt auf Export -> Export-Zeile ist hervorgehoben
  - Nutzer tippt auf DATEV -> DATEV-Zeile ist hervorgehoben + PRO Badge
- [ ] Dark Mode kompatibel
- [ ] Animierter Uebergang beim Oeffnen
- [ ] Given ein FREE-Nutzer oeffnet die Paywall ueber ein gesperrtes Feature
  When der Paywall-Screen erscheint
  Then ist das relevante Feature visuell hervorgehoben
  And die Preise werden dynamisch aus dem App Store geladen

**Technische Hinweise**:
- `SubscriptionStoreView` (iOS 17+) als Alternative zu Custom-Paywall pruefen -- bietet Apple-Standard-UI, ist aber weniger anpassbar
- Empfehlung: Custom Paywall fuer bessere Conversion und Branding-Kontrolle
- Preise mit `Product.displayPrice` anzeigen (lokalisiert durch Apple)
- `.sheet(isPresented:)` oder `.fullScreenCover` je nach Trigger
- **PaywallView braucht Zugriff auf StoreKitManager**: Injection via `@Environment(StoreKitManager.self)`. StoreKitManager muss in der Environment-Kette bereitgestellt werden (`.environment(services.storeKitManager)`). Damit werden Produkte+Preise geladen und Kaeufe ausgeloest.

---

### P4-E04-S02: Android Paywall Screen

**Als** FREE-Nutzer
**moechte ich** eine uebersichtliche Darstellung der Abo-Optionen sehen,
**damit** ich eine informierte Kaufentscheidung treffen kann.

**Plattform**: Android
**Abhaengigkeiten**: P4-E03-S02 (Play Billing Integration)
**Parallelisierbar mit**: P4-E04-S01 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Paywall als BottomSheet oder eigener Screen:
  - Identischer Inhalt wie iOS (Feature-Vergleich, Preise, CTAs)
  - Material 3 Design (Cards, Typography, Color System)
  - Preise dynamisch aus Play Billing (nicht hardcoded)
  - "Kaeufe wiederherstellen" Link
  - Nutzungsbedingungen + Datenschutz Links
  - Abo-Hinweis: "Abo verlaengert sich automatisch. Kuendigung jederzeit ueber Google Play."
- [ ] Feature-Hervorhebung kontextabhaengig (identisch mit iOS)
- [ ] Dark Mode kompatibel
- [ ] Given ein FREE-Nutzer oeffnet die Paywall
  Then werden Preise dynamisch geladen
  And der Kauf-Flow wird ueber Google Play abgewickelt

**Technische Hinweise**:
- `ModalBottomSheet` (Compose) oder `NavHost`-Route
- Preise mit `ProductDetails.subscriptionOfferDetails.pricingPhases` formatieren
- `BillingFlowParams` mit `setProductDetailsParamsList` fuer den Kauf
- **PaywallScreen braucht Zugriff auf BillingManager**: Wird als Parameter uebergeben (`billingManager: BillingManager`). Damit werden Produkte+Preise geladen und Kaeufe via `launchPurchaseFlow()` ausgeloest.

---

### P4-E04-S03: Upgrade-Erfolgs-Flow (Beide Plattformen)

**Als** Nutzer, der gerade ein Abo abgeschlossen hat,
**moechte ich** sofort sehen dass die Premium-Features freigeschaltet sind,
**damit** ich sicher bin dass mein Kauf erfolgreich war.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P4-E04-S01, P4-E04-S02
**Parallelisierbar mit**: Keine (abschliessende Story)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Nach erfolgreichem Kauf:
  - Paywall schliesst sich
  - Konfetti-Animation oder Success-Checkmark (dezent, nicht uebertrieben)
  - Toast/Banner: "Willkommen bei Fakturus Track [STARTER/PRO]!"
  - Das Feature, das den Paywall-Flow ausgeloest hat, oeffnet sich sofort
- [ ] Alle gesperrten UI-Elemente aktualisieren sich sofort (Lock-Icons verschwinden)
- [ ] Given ein FREE-Nutzer schliesst ein STARTER-Abo ab
  When die Transaktion bestaetigt ist
  Then schliesst sich die Paywall
  And eine Erfolgsmeldung erscheint
  And das zuvor gesperrte Feature ist sofort nutzbar

**Technische Hinweise**:
- iOS: `SubscriptionManager.tierDidChange` Publisher triggert UI-Update
- Android: `SubscriptionManager.tier` StateFlow triggert Recomposition
- Haptic Feedback: `.notification(.success)` bei Kauf-Erfolg
