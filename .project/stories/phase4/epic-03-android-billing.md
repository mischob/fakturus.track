# EPIC 03: In-App Purchase Android (Google Play Billing)

## Ziel

Integration der Google Play Billing Library (v7.0.0) fuer Auto-Renewable Subscriptions. Nutzer koennen STARTER- und PRO-Abos direkt in der App abschliessen, verwalten und wiederherstellen.

## Abhaengigkeiten

- **E01 (Feature-Gating)**: SubscriptionManager und Tier-System muessen stehen
- **Google Play Console**: Produkte muessen in der Play Console angelegt sein (Product IDs, Preise, Base Plans)

---

## Stories

### P4-E03-S01: Google Play Console Produkt-Setup

**Als** Product Owner
**moechte ich** die Subscription-Produkte in der Google Play Console korrekt konfigurieren,
**damit** die Play Billing Library die Produkte laden und Kaeufe verarbeiten kann.

**Plattform**: Android (Google Play Console Konfiguration)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle anderen Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Subscription "starter_monthly" angelegt:
  - Base Plan: Monatlich, 2,99 EUR
  - Titel DE: "Fakturus Track Starter"
  - Titel EN: "Fakturus Track Starter"
  - Beschreibung: Identisch mit iOS (siehe E02-S01)
- [ ] Subscription "pro_monthly" angelegt:
  - Base Plan: Monatlich, 4,99 EUR
  - Titel DE: "Fakturus Track Pro"
  - Titel EN: "Fakturus Track Pro"
- [ ] Upgrade/Downgrade-Regeln:
  - Starter -> Pro: `CHARGE_PRORATED_PRICE` (sofortiger Wechsel, anteiliger Preis)
  - Pro -> Starter: `DEFERRED` (Wechsel am Ende der Abrechnungsperiode)
- [ ] License Testing: Entwickler-Konten als Tester hinzugefuegt

**Technische Hinweise**:
- Google Play erfordert Base Plans (seit Billing Library v5)
- Offers (z.B. Free Trial) koennen spaeter hinzugefuegt werden
- Preis muss in allen relevanten Laendern gesetzt werden (DACH: EUR)

---

### P4-E03-S02: Google Play Billing Integration

**Als** Nutzer
**moechte ich** ein Abo direkt in der App abschliessen koennen,
**damit** ich Premium-Features ohne Umweg freischalten kann.

**Plattform**: Android
**Abhaengigkeiten**: P4-E01-S01 (SubscriptionManager), P4-E03-S01 (Produkte in Play Console)
**Parallelisierbar mit**: P4-E02-* (iOS StoreKit)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `BillingManager.kt` implementiert:
  - `BillingClient` Initialisierung und Connection-Management
  - `queryProducts()` -> laedt Starter + Pro Produkte (QueryProductDetailsParams)
  - `launchPurchaseFlow(productDetails, activity)` -> startet Kauf
  - `acknowledgePurchase(purchase)` -> bestaetigt Kauf (Pflicht!)
  - `queryPurchases()` -> aktuelle Abos pruefen
- [ ] `PurchasesUpdatedListener` verarbeitet:
  - `BillingResponseCode.OK` -> Kauf erfolgreich, Tier aktualisieren
  - `BillingResponseCode.USER_CANCELED` -> Kein Fehler anzeigen
  - `BillingResponseCode.ITEM_ALREADY_OWNED` -> Tier setzen (bereits gekauft)
  - Andere Fehler -> Nutzer-Meldung
- [ ] BillingClient Reconnection bei Verbindungsverlust
- [ ] `purchase.acknowledge()` wird nach jedem erfolgreichen Kauf aufgerufen (innerhalb 3 Tage Pflicht, sonst Erstattung)
- [ ] Wenn `acknowledgePurchase()` fehlschlaegt, wird ein WorkManager-Retry mit exponential Backoff gestartet (max 3 Versuche). Kauf darf NICHT unbestaetigt bleiben.
- [ ] Given ein FREE-Nutzer kauft das STARTER-Abo
  When die Transaktion erfolgreich ist
  Then wechselt der Tier sofort auf STARTER
  And alle STARTER-Features werden freigeschaltet
- [ ] Given ein Nutzer hat ein aktives STARTER-Abo
  When er die App oeffnet
  Then erkennt die Billing Library das aktive Abo
  And der Tier ist korrekt gesetzt

**Technische Hinweise**:
- Billing Library v7.0.0 (`com.android.billingclient:billing-ktx:7.0.0`)
- `BillingClient.newBuilder(context).enablePendingPurchases().setListener(listener).build()`
- `ProductType.SUBS` fuer Subscriptions
- `queryPurchasesAsync(QueryPurchasesParams.newBuilder().setProductType(ProductType.SUBS).build())`
- Acknowledge ist Pflicht -- nicht vergessen!
- BillingClient muss bei `Activity.onDestroy()` disconnected werden

---

### P4-E03-S03: Restore Purchases (Android)

**Als** Nutzer, der die App neu installiert hat,
**moechte ich** mein bestehendes Abo wiederherstellen koennen,
**damit** ich nicht erneut bezahlen muss.

**Plattform**: Android
**Abhaengigkeiten**: P4-E03-S02
**Parallelisierbar mit**: P4-E02-* (iOS)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] "Kaeufe wiederherstellen" Button in den Einstellungen
- [ ] `BillingClient.queryPurchasesAsync()` wird aufgerufen
- [ ] Erfolgsmeldung: "Abo wiederhergestellt. Tier: [STARTER/PRO]"
- [ ] Fehlermeldung wenn kein Abo gefunden: "Kein aktives Abo gefunden."
- [ ] Loading-Indikator waehrend der Wiederherstellung
- [ ] Given ein Nutzer mit aktivem PRO-Abo installiert die App neu
  When er auf "Kaeufe wiederherstellen" tippt
  Then wird sein PRO-Abo wiederhergestellt

**Technische Hinweise**:
- Android: `queryPurchasesAsync` liefert alle aktiven Subscriptions
- Automatische Wiederherstellung beim App-Start via `queryPurchasesAsync` ist empfohlen
- Expliziter "Restore" Button trotzdem anbieten (analog iOS, konsistente UX)
