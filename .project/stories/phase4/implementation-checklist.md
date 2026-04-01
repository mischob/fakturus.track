# Implementation-Checkliste -- Phase 4

Dieses Dokument muss jeder Entwickler / AI-Agent lesen, bevor er eine Phase-4-Story anfaengt. Es ergaenzt die Checklisten aus Phase 1, 2 und 3 um Phase-4-spezifische Hinweise.

---

## 1. Phase-1/2/3-Checklisten gelten weiterhin

Alle Konventionen aus den vorherigen Phasen gelten unveraendert:
- Namenskonventionen, Ordnerstruktur, Code-Stil
- Error Handling, Git-Workflow, Definition of Done
- Dark Mode, Accessibility, Lokalisierung (DE + EN)
- Performance-Budget (Cold Start < 1s, 60fps Scrolling)

**ACHTUNG**: Phase-1/2/3-Features NICHT brechen! Feature-Gating darf bestehende Funktionalitaet nicht zerstoeren. Gesperrte Features muessen weiterhin korrekt funktionieren wenn der Nutzer upgradet.

---

## 2. Feature-Gating Regeln

### Absolute Regeln (nicht verhandelbar)

1. **Gesetzlich vorgeschriebene Features bleiben IMMER kostenlos**:
   - Timer (Start/Stop/Finish) -- ArbZG Zeiterfassungspflicht
   - Pausenerfassung -- ArbZG Pausenpflicht
   - Feiertage -- korrekte Soll-Stunden-Berechnung
   - Basis-History (365 Tage) -- Nachweispflicht
   - Offline-Sync -- Funktionsfaehigkeit ohne Netzwerk

2. **Kein Feature wird komplett versteckt**:
   - Gesperrte Features sind SICHTBAR aber nicht bedienbar
   - Lock-Icon + Tier-Badge zeigen was mit Upgrade verfuegbar ist
   - Dies ist ein bewusster Teaser-Effekt fuer Conversion

3. **Kein Datenverlust bei Tier-Wechsel**:
   - Downgrade: Daten werden ausgeblendet (Read-Only), NICHT geloescht
   - Upgrade: Daten werden sofort wieder verfuegbar
   - Niemals Daten loeschen basierend auf dem Tier

4. **Preise IMMER dynamisch laden**:
   - iOS: `Product.displayPrice` aus StoreKit 2
   - Android: `ProductDetails.subscriptionOfferDetails` aus Play Billing
   - NIEMALS Preise hardcoden (waehrungsabhaengig, aenderbar ohne App-Update)

### Feature-Gate Implementierungs-Pattern

**iOS:**
```swift
// SubscriptionManager.swift
@Observable
class SubscriptionManager {
    var currentTier: Tier = .free

    func isAvailable(_ feature: FeatureGate) -> Bool {
        feature.requiredTier <= currentTier
    }
}

// In Views:
struct ExportSection: View {
    @Environment(SubscriptionManager.self) var subscriptionManager

    var body: some View {
        if subscriptionManager.isAvailable(.pdfExport) {
            PDFExportButton()
        } else {
            FeatureLockedView(feature: .pdfExport)
                .onTapGesture { showPaywall = true }
        }
    }
}
```

**Android:**
```kotlin
// SubscriptionManager.kt
@Singleton
class SubscriptionManager @Inject constructor() {
    private val _tier = MutableStateFlow(Tier.FREE)
    val tier: StateFlow<Tier> = _tier.asStateFlow()

    fun isAvailable(feature: FeatureGate): Boolean =
        feature.requiredTier <= _tier.value
}

// In Composables:
@Composable
fun ExportSection(subscriptionManager: SubscriptionManager = hiltViewModel()) {
    val tier by subscriptionManager.tier.collectAsState()
    if (subscriptionManager.isAvailable(FeatureGate.PDF_EXPORT)) {
        PDFExportButton()
    } else {
        FeatureLockedCard(feature = FeatureGate.PDF_EXPORT)
    }
}
```

---

## 3. In-App Purchase Checkliste

### iOS StoreKit 2

- [ ] `Transaction.updates` Listener laeuft ab App-Start (in `App.init` oder `AppDelegate`)
- [ ] `Transaction.finish()` wird IMMER aufgerufen nach Verarbeitung
- [ ] `Transaction.currentEntitlements` beim App-Start pruefen (Abo-Status wiederherstellen)
- [ ] StoreKit Configuration File fuer lokale Tests vorhanden
- [ ] Sandbox-Testing funktioniert (Sandbox-Account in Settings > App Store)
- [ ] "Restore Purchases" Button vorhanden (Apple Review Pflicht!)
- [ ] Abo-Bedingungen in Paywall klar kommuniziert (Preis, Verlaengerung, Kuendigung)
- [ ] Keine externen Zahlungslinks (Apple Guideline 3.1.1)

### Android Play Billing

- [ ] `BillingClient` wird bei `Activity.onCreate` verbunden und bei `onDestroy` getrennt
- [ ] `purchase.acknowledge()` wird innerhalb von 3 Tagen aufgerufen (sonst automatische Erstattung!)
- [ ] `queryPurchasesAsync` beim App-Start (Abo-Status wiederherstellen)
- [ ] `PurchasesUpdatedListener` verarbeitet alle Response-Codes korrekt
- [ ] BillingClient Retry bei `SERVICE_DISCONNECTED`
- [ ] Pending Purchases aktiviert: `.enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())`
- [ ] ProGuard/R8 Rules fuer Billing Library konfiguriert

---

## 3b. Crash-Reporting (Sentry) -- DSGVO Opt-In Pflicht

Crash-Reporting via Sentry ist **Opt-In** (nicht Opt-Out). Der User muss in den Settings aktiv zustimmen. Default: **deaktiviert**.

- [ ] Sentry SDK wird beim App-Start geladen, aber **NICHT aktiviert** bis der User zustimmt
- [ ] In Settings: Toggle "Crash-Berichte senden" -- Default: **AUS**
- [ ] Erklaerungstext: "Hilft uns, Fehler zu finden und die App zu verbessern. Es werden keine persoenlichen Daten uebermittelt."
- [ ] Opt-In-Status wird in UserDefaults/DataStore gespeichert
- [ ] Bei Opt-In: `SentrySDK.start(...)` bzw. `Sentry.init(...)` aufrufen
- [ ] Bei Opt-Out: Sentry deaktivieren, keine Daten mehr senden
- [ ] DSGVO-Konformitaet: Crash-Daten enthalten keine personenbezogenen Daten (keine User-IDs, keine E-Mail-Adressen in Breadcrumbs)
- [ ] Privacy Policy muss Crash-Reporting erwaehnen (Verweis auf E07)

---

## 4. Store-Richtlinien Checkliste

### Apple App Store Review Guidelines

| Guideline | Anforderung | Status |
|-----------|-------------|--------|
| 3.1.1 | In-App Purchase fuer digitale Features | [ ] |
| 3.1.2 | Subscription Terms klar kommuniziert | [ ] |
| 3.1.2(a) | Auto-Renewable: Kuendigungshinweis | [ ] |
| 5.1.1 | Privacy Policy URL hinterlegt | [ ] |
| 5.1.2 | App Privacy Details ausgefuellt | [ ] |
| 2.1 | Performance: Kein Crash im Review-Flow | [ ] |
| 4.0 | Native UI (kein WebView fuer Kern-Features) | [ ] |
| 2.3 | Accurate Metadata (Screenshots zeigen echte App) | [ ] |
| 3.2.2 | No "Restore Purchases" = Ablehnung | [ ] |

### Google Play Policies

| Policy | Anforderung | Status |
|--------|-------------|--------|
| Payments | Google Play Billing fuer In-App Kaeufe | [ ] |
| Data Safety | Data Safety Section vollstaendig | [ ] |
| Content Rating | IARC Fragebogen ausgefuellt | [ ] |
| Metadata | Store Listing korrekt und aktuell | [ ] |
| Target API | targetSdk >= 34 (aktuelles Minimum) | [ ] |
| App Signing | Google Play App Signing aktiviert | [ ] |
| AAB | Android App Bundle (nicht APK) | [ ] |

---

## 5. Phase-4-spezifische Ordner

### Neue Dateien

**iOS:**
```
Services/
  Subscription/
    SubscriptionManager.swift    -- Tier-Verwaltung, Feature-Gates
    StoreKitManager.swift        -- StoreKit 2 Integration
    FeatureGate.swift            -- Enum mit allen gated Features
    Tier.swift                   -- FREE, STARTER, PRO

Features/
  Paywall/
    PaywallView.swift            -- Paywall-Screen
    PaywallViewModel.swift       -- Preis-Loading, Feature-Vergleich
    FeatureLockedView.swift      -- Lock-Overlay fuer gesperrte Features
    PaywallTeaserView.swift      -- Teaser fuer gesperrte Tabs
```

**Android:**
```
services/
  subscription/
    SubscriptionManager.kt
    BillingManager.kt
    FeatureGate.kt
    Tier.kt

features/
  paywall/
    PaywallScreen.kt
    PaywallViewModel.kt
    FeatureLockedCard.kt
    PaywallTeaserCard.kt
```

---

## 6. Testumgebungen

### iOS Sandbox Testing

- Sandbox-Accounts in App Store Connect > Users and Access > Sandbox > Testers
- Mindestens 3 Accounts: `free@test.fakturus.com`, `starter@test.fakturus.com`, `pro@test.fakturus.com`
- Sandbox-Abo laeuft alle 5 Minuten ab (zum schnellen Testen)
- Auf Geraet: Settings > App Store > Sandbox Account

### Android License Testing

- In Google Play Console > Settings > License testing
- Test-Gmail-Accounts hinzufuegen
- Testkaeufe: Werden NICHT berechnet
- `BillingClient` erkennt License Tester automatisch

---

## 7. Launch-Tag Checkliste

Am Tag des Launches muessen folgende Dinge geprueft/erledigt sein:

- [ ] Beide Apps im jeweiligen Store verfuegbar (Download testen)
- [ ] In-App Purchase funktioniert mit echtem Geld (Testkauf + sofortige Erstattung)
- [ ] Privacy Policy URL erreichbar
- [ ] Sentry Dashboard zeigt keine Crashes (nur fuer Nutzer die Crash-Reporting aktiviert haben)
- [ ] Backend laeuft stabil (Health-Check)
- [ ] Release Notes in beiden Stores aktuell
- [ ] Monitoring: App Store Connect + Play Console Dashboard im Auge behalten (24h)

---

## 8. Rollback-Plan

Falls nach dem Launch kritische Probleme auftreten:

### Stufe 1: Hotfix (< 24h)
- Bug identifizieren, Fix implementieren, neuen Build einreichen
- iOS: Expedited Review beantragen (fuer kritische Bugs)
- Android: Staged Rollout auf 10% zuruecksetzen

### Stufe 2: Feature-Disable (< 1h)
- Wenn ein spezifisches Feature Crashes verursacht:
  - Feature-Gate fuer das Feature temporaer auf PRO+1 setzen (niemand hat Zugriff)
  - Oder: Serverseitig ueber Remote Config deaktivieren (falls implementiert)

### Stufe 3: Rollback (letztes Mittel)
- iOS: Vorherige Build-Version in App Store Connect aktivieren
- Android: Staged Rollout auf 0% setzen, vorherigen Build promoten
- Vorherige App-Version als Fallback verfuegbar

---

## 9. Definition of Done (Phase 4 Story)

Eine Phase-4-Story ist "Done" wenn:

- [ ] Alle Akzeptanzkriterien aus dem EPIC-Dokument erfuellt
- [ ] Phase-1/2/3-Features funktionieren weiterhin (Regressions-Check)
- [ ] Feature-Gating: Gesperrte Features zeigen Lock-UI, freigeschaltete funktionieren normal
- [ ] IAP: Sandbox-Kauf funktioniert (kein echter Kauf noetig in Entwicklung)
- [ ] Code kompiliert ohne Warnungen
- [ ] Dark Mode funktioniert (falls UI-Story)
- [ ] Strings in Lokalisierungsdateien (DE + EN)
- [ ] Accessibility Labels vorhanden (falls UI-Story)
- [ ] Kein Datenverlust bei Tier-Wechsel verifiziert
