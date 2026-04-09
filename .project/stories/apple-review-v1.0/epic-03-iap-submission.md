# EPIC 03: IAP-Produkte zum Review einreichen

## Ziel

Die In-App Purchase Produkte (Starter Yearly, Starter Monthly, Pro Monthly) muessen in App Store Connect zum Review eingereicht werden. Aktuell referenziert die App diese Produkte, sie wurden aber nicht submitted. (Guideline 2.1(b) - App Completeness)

## Abhaengigkeiten

- IAP-Produkte muessen in App Store Connect angelegt sein (bereits vorhanden)
- Screenshots der IAP-Flows (Paywall) muessen hochgeladen werden

---

## Analyse

### Aktuelle Situation

Die App definiert 4 Produkte (siehe `StoreKitManager` / StoreKit Configuration):
- `com.fakturus.track.starter.monthly` (2,99 EUR)
- `com.fakturus.track.starter.yearly` (24,99 EUR)
- `com.fakturus.track.pro.monthly` (3,99 EUR)
- `com.fakturus.track.pro.yearly` (44,99 EUR)

Apple erwaehnt explizit dass **Starter Yearly, Starter Monthly und Pro Monthly** nicht zum Review eingereicht wurden. Das bedeutet:
- Die Produkte existieren in App Store Connect
- Sie wurden aber nicht zusammen mit dem App-Binary submitted
- Eventuell fehlen auch die Review-Screenshots fuer die IAPs

**Pro Yearly** wird nicht erwaehnt -- moeglicherweise wurde nur dieses eine Produkt korrekt eingereicht.

---

## Stories

### ARV1-E03-S01: IAP-Produkte in App Store Connect einreichen

**Als** Entwickler
**moechte ich** alle 4 IAP-Produkte korrekt zum Review einreichen,
**damit** die App vollstaendig geprueft werden kann.

**Plattform**: App Store Connect (Konfiguration)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Alle 4 Subscription-Produkte in App Store Connect pruefen:
  - `com.fakturus.track.starter.monthly` -- Status: "Ready to Submit" oder besser
  - `com.fakturus.track.starter.yearly` -- Status: "Ready to Submit" oder besser
  - `com.fakturus.track.pro.monthly` -- Status: "Ready to Submit" oder besser
  - `com.fakturus.track.pro.yearly` -- Status: "Ready to Submit" oder besser
- [ ] Fuer jedes Produkt:
  - Referenzname gesetzt
  - Preis korrekt konfiguriert
  - Beschreibung (DE + EN) ausgefuellt
  - Subscription Group korrekt zugewiesen
  - **App Review Screenshot hochgeladen** (Pflicht! Screenshot der Paywall wo das Produkt sichtbar ist)
- [ ] Subscription Groups korrekt:
  - Upgrade/Downgrade-Reihenfolge: Pro > Starter
  - Localization fuer DE und EN
- [ ] Alle Produkte werden zusammen mit dem naechsten Binary eingereicht
  - In App Store Connect: Beim Erstellen der neuen Version die IAPs auswaehlen
- [ ] Given der Reviewer oeffnet die Paywall
  When er ein Abo kaufen moechte
  Then sind alle 4 Optionen verfuegbar und funktional (Sandbox-Kauf moeglich)

**Technische Hinweise**:
- App Store Connect > App > In-App Purchases > Manage
- Jedes IAP braucht mindestens einen Screenshot fuer den Review
  - Empfehlung: Screenshot der PaywallView mit dem jeweiligen Produkt sichtbar
  - Screenshot muss die Paywall zeigen, nicht das Ergebnis nach dem Kauf
- Beim Einreichen der neuen App-Version: Unter "In-App Purchases" alle 4 Produkte anhaengen
- Status-Flow: Missing Metadata -> Ready to Submit -> Waiting for Review -> Approved
- **Wichtig**: IAPs koennen nur zusammen mit einem App-Binary reviewed werden, nicht separat

---

### ARV1-E03-S02: Paywall-Screenshot fuer Review erstellen

**Als** Entwickler
**moechte ich** Screenshots der Paywall fuer den IAP-Review bereitstellen,
**damit** Apple die Kauf-Flows pruefen kann.

**Plattform**: App Store Connect
**Geschaetzter Aufwand**: XS

**Akzeptanzkriterien**:
- [ ] Screenshot der Paywall erstellt (iPhone, helle Darstellung)
- [ ] Alle 4 Abo-Optionen sind auf dem Screenshot sichtbar:
  - Starter Monthly (2,99 EUR)
  - Starter Yearly (24,99 EUR)
  - Pro Monthly (3,99 EUR)
  - Pro Yearly (44,99 EUR)
- [ ] Preisinformationen, Laufzeit und Feature-Vergleich sichtbar
- [ ] Links zu Terms und Privacy sichtbar am unteren Rand
- [ ] Screenshot in App Store Connect bei jedem der 4 IAP-Produkte hochgeladen

**Technische Hinweise**:
- Ein einzelner Paywall-Screenshot reicht fuer alle 4 Produkte (gleiche Ansicht)
- Screenshot via Simulator oder physisches Geraet erstellen
- Mindestaufloesung: Geraeteaufloesung (z.B. 1290x2796 fuer iPhone 15 Pro Max)
