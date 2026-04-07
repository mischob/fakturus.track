# EPIC 02: In-App Purchase iOS (StoreKit 2)

## Ziel

Integration von Apple StoreKit 2 fuer Auto-Renewable Subscriptions. Nutzer koennen STARTER- und PRO-Abos direkt in der App abschliessen, verwalten und wiederherstellen.

## Abhaengigkeiten

- **E01 (Feature-Gating)**: SubscriptionManager und Tier-System muessen stehen
- **App Store Connect**: Produkte muessen in App Store Connect angelegt sein (Product IDs, Preise, Subscription Groups)

---

## Stories

### P4-E02-S01: App Store Connect Produkt-Setup

**Als** Product Owner
**moechte ich** die Subscription-Produkte in App Store Connect korrekt konfigurieren,
**damit** StoreKit 2 die Produkte laden und Kaeufe verarbeiten kann.

**Plattform**: iOS (App Store Connect Konfiguration)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle anderen Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Subscription Group "fakturus_track_premium" erstellt
- [ ] Produkt "starter_monthly" (Auto-Renewable, 2,99 EUR/Monat) angelegt
  - Anzeigename DE: "Fakturus Track Starter"
  - Anzeigename EN: "Fakturus Track Starter"
  - Beschreibung DE: "PDF/CSV-Export, Urlaub & Krankheitstage, Widgets, Ueberstunden-Dashboard"
  - Beschreibung EN: "PDF/CSV export, vacation & sick days, widgets, overtime dashboard"
- [ ] Produkt "pro_monthly" (Auto-Renewable, 4,99 EUR/Monat) angelegt
  - Anzeigename DE: "Fakturus Track Pro"
  - Anzeigename EN: "Fakturus Track Pro"
  - Beschreibung DE: "Alle Starter-Features plus DATEV-Export, Schulferien, Kalender-Integration"
  - Beschreibung EN: "All Starter features plus DATEV export, school holidays, calendar integration"
- [ ] Upgrade/Downgrade-Regeln konfiguriert (Starter -> Pro = Upgrade, sofortiger Wechsel)
- [ ] Sandbox-Testaccounts angelegt (mindestens 3: FREE, STARTER, PRO)
- [ ] Review Information ausgefuellt (Testaccount-Credentials fuer Apple Review)

**Technische Hinweise**:
- Subscription Group erlaubt automatische Upgrades/Downgrades
- Preisstaffel: Tier 3 (2,99 EUR) fuer Starter, Tier 5 (4,99 EUR) fuer Pro
- Free Trial: Initial NICHT konfigurieren (erst nach Launch-Daten evaluieren)

---

### P4-E02-S02: StoreKit 2 Integration

**Als** Nutzer
**moechte ich** ein Abo direkt in der App abschliessen koennen,
**damit** ich Premium-Features ohne Umweg freischalten kann.

**Plattform**: iOS
**Abhaengigkeiten**: P4-E01-S01 (SubscriptionManager), P4-E02-S01 (Produkte in ASC)
**Parallelisierbar mit**: P4-E03-* (Android Billing)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] `StoreKitManager.swift` implementiert:
  - `fetchProducts()` -> laedt Starter + Pro Produkte
  - `purchase(product:)` -> startet Kauf-Flow
  - `listenForTransactions()` -> Transaction.updates Listener
  - `restorePurchases()` -> `AppStore.sync()`
  - `currentEntitlement()` -> aktueller Abo-Status
- [ ] `SubscriptionManager` wird von `StoreKitManager` gefuettert:
  - Bei neuem Kauf: Tier aktualisieren
  - Bei Abo-Verlaengerung: Tier bestaetigen
  - Bei Abo-Ablauf: Tier auf FREE setzen
- [ ] Transaction Listener startet bei App-Start und reagiert auf:
  - Neue Kaeufe
  - Verlaengerungen
  - Kuendigungen
  - Revocations
- [ ] StoreKit Configuration File fuer Xcode-Testing vorhanden
- [ ] Given ein FREE-Nutzer kauft das STARTER-Abo
  When die Transaktion erfolgreich ist
  Then wechselt der Tier sofort auf STARTER
  And alle STARTER-Features werden freigeschaltet
- [ ] Given ein Nutzer hat ein aktives STARTER-Abo
  When er die App oeffnet (auch nach Neustart)
  Then erkennt StoreKit 2 das aktive Abo
  And der Tier ist korrekt auf STARTER gesetzt

**Technische Hinweise**:
- StoreKit 2 mit `Product.SubscriptionInfo` fuer Abo-Status
- `Transaction.currentEntitlements` fuer aktiven Abo-Check beim App-Start
- `Transaction.updates` als AsyncSequence fuer Echtzeit-Updates
- `.storeKitError` Handling: `.userCancelled`, `.pending`, `.unknown`
- Sandbox-Testing: `StoreKit.Configuration` File fuer lokale Tests ohne App Store Connect
- `Transaction.finish()` IMMER aufrufen nach Verarbeitung

---

### P4-E02-S03: Restore Purchases (iOS)

**Als** Nutzer, der die App neu installiert hat,
**moechte ich** mein bestehendes Abo wiederherstellen koennen,
**damit** ich nicht erneut bezahlen muss.

**Plattform**: iOS
**Abhaengigkeiten**: P4-E02-S02
**Parallelisierbar mit**: P4-E03-* (Android)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] "Kaeufe wiederherstellen" Button in den Einstellungen
- [ ] `AppStore.sync()` wird aufgerufen
- [ ] Erfolgsmeldung: "Abo wiederhergestellt. Tier: [STARTER/PRO]"
- [ ] Fehlermeldung wenn kein Abo gefunden: "Kein aktives Abo gefunden."
- [ ] Loading-Indikator waehrend der Wiederherstellung
- [ ] Given ein Nutzer mit aktivem STARTER-Abo installiert die App neu
  When er auf "Kaeufe wiederherstellen" tippt
  Then wird sein STARTER-Abo wiederhergestellt
  And alle Features sind sofort verfuegbar

**Technische Hinweise**:
- StoreKit 2: `AppStore.sync()` ist der korrekte Weg (nicht `SKPaymentQueue.restoreCompletedTransactions()`)
- Button laut Apple Review Guidelines Pflicht ("Restore Purchases" muss vorhanden sein)
- Kann auch automatisch beim App-Start via `Transaction.currentEntitlements` pruefen
