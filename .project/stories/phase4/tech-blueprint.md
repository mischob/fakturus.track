# Technischer Gesamtplan Phase 4

## Ziel-Dateistruktur am Ende von Phase 4

### iOS -- Neue Dateien

```
FakturusTrack/
  FakturusTrack/
    Services/
      Subscription/
        SubscriptionManager.swift           E01-S01  Tier-Verwaltung, Feature-Gate Pruefung
        StoreKitManager.swift               E02-S02  StoreKit 2: Products, Purchase, Transactions
        FeatureGate.swift                   E01-S01  Enum aller gated Features + requiredTier
        Tier.swift                          E01-S01  Enum FREE/STARTER/PRO, Comparable

    Features/
      Paywall/
        PaywallView.swift                   E04-S01  Feature-Vergleich, Preis-Anzeige, CTA
        FeatureLockedOverlay.swift          E01-S02  ViewModifier: Lock-Icon + Tap -> Paywall
        PaywallTeaserView.swift             E01-S02  Teaser fuer gesperrte Tabs (Urlaub, Gesamt)

    Resources/
      StoreKit/
        FakturusTrack.storekit             E02-S02  StoreKit Configuration File (Xcode Testing)
```

**Neue Dateien iOS: 7** (+ 1 StoreKit Config)

### iOS -- Modifizierte Dateien

```
FakturusTrack/
  App/
    FakturusTrackApp.swift                  +SubscriptionManager Environment, +Transaction.updates Listener
    ServiceContainer.swift                  +subscriptionManager Property, +StoreKitManager init

  Features/
    TimeTracking/
      TimeTrackingView.swift                +History 365-Tage-Filter im FREE-Tier
      MonthGroup.swift                      +"Aeltere Eintraege mit STARTER" Hinweis
    Vacation/
      VacationScreen.swift                  +PaywallTeaser wenn FREE-Tier
      VacationViewModel.swift               +Read-Only Check bei Downgrade
    Overview/
      OverviewScreen.swift                  +FeatureLockedOverlay auf Export-Buttons + Dashboard
      OverviewViewModel.swift               +Gate-Check vor Export-Generierung
    Settings/
      SettingsView.swift                    +Restore Purchases Button, +Lock auf Schulferien/Kalender
      SettingsViewModel.swift               +restorePurchases(), +SubscriptionManager Dependency

  Shared/
    VacationCalendar.swift                  +Krankheitstag-Kontextmenue nur wenn Tier >= STARTER
```

**Modifizierte Dateien iOS: 12**

---

### Android -- Neue Dateien

```
app/src/main/java/com/fakturus/track/
  services/
    subscription/
      SubscriptionManager.kt               E01-S01  StateFlow<Tier>, isAvailable()
      BillingManager.kt                    E03-S02  BillingClient, Purchase, Acknowledge
      FeatureGate.kt                       E01-S01  Sealed class/Enum aller gated Features
      Tier.kt                              E01-S01  Enum FREE/STARTER/PRO, Comparable

  features/
    paywall/
      PaywallScreen.kt                     E04-S02  BottomSheet: Feature-Vergleich, CTA
      PaywallViewModel.kt                  E04-S02  Preis-Loading aus BillingManager
      FeatureLockedCard.kt                 E01-S03  Composable: Lock-Card + Tap -> Paywall
      PaywallTeaserCard.kt                 E01-S03  Material 3 Teaser fuer gesperrte Bereiche
```

**Neue Dateien Android: 8**

### Android -- Modifizierte Dateien

```
app/src/main/java/com/fakturus/track/
  ServiceContainer.kt                       +subscriptionManager, +billingManager
  MainActivity.kt                           +BillingClient Lifecycle (connect/disconnect)

  features/
    timetracking/
      TimeTrackingScreen.kt                 +History 365-Tage-Filter im FREE-Tier
      MonthGroup.kt                         +"Aeltere Eintraege" Hinweis
    vacation/
      VacationScreen.kt                     +PaywallTeaserCard wenn FREE
      VacationViewModel.kt                  +Read-Only bei Downgrade
    overview/
      OverviewScreen.kt                     +FeatureLockedCard auf Exports + Dashboard
      OverviewViewModel.kt                  +Gate-Check vor Export
    settings/
      SettingsScreen.kt                     +Restore Purchases, +Lock auf Schulferien/Kalender
      SettingsViewModel.kt                  +restorePurchases()

  ui/shared/
    VacationCalendar.kt                     +Krankheitstag nur wenn Tier >= STARTER

app/build.gradle.kts                        +billing-ktx Dependency
```

**Modifizierte Dateien Android: 13**

---

## Neue Dependencies

### iOS

Keine neuen SPM Dependencies. StoreKit 2 ist ein System-Framework.

```swift
// Xcode Capabilities hinzufuegen:
// Target -> Signing & Capabilities -> + In-App Purchase
```

### Android

```toml
# gradle/libs.versions.toml
[versions]
billing = "7.0.0"

[libraries]
billing-ktx = { group = "com.android.billingclient", name = "billing-ktx", version.ref = "billing" }
```

```kotlin
// app/build.gradle.kts
dependencies {
    implementation(libs.billing.ktx)
}
```

### ProGuard/R8 Ergaenzung (Android)

```proguard
# Google Play Billing
-keep class com.android.vending.billing.** { *; }
-keep class com.android.billingclient.** { *; }
```

---

## Datei-Entstehung pro Welle

### Welle 1: Feature-Gating Infra + Store-Vorbereitung (Woche 24-25)

| Story | iOS Dateien (NEU) | iOS Dateien (MOD) | Android Dateien (NEU) | Android Dateien (MOD) |
|-------|-------------------|--------------------|-----------------------|----------------------|
| E01-S01 | Tier.swift, FeatureGate.swift, SubscriptionManager.swift | ServiceContainer.swift, FakturusTrackApp.swift | Tier.kt, FeatureGate.kt, SubscriptionManager.kt | ServiceContainer.kt |
| E01-S02 | FeatureLockedOverlay.swift, PaywallTeaserView.swift | TimeTrackingView.swift, MonthGroup.swift, VacationScreen.swift, OverviewScreen.swift, SettingsView.swift, VacationCalendar.swift | -- | -- |
| E01-S03 | -- | -- | FeatureLockedCard.kt, PaywallTeaserCard.kt | TimeTrackingScreen.kt, MonthGroup.kt, VacationScreen.kt, OverviewScreen.kt, SettingsScreen.kt, VacationCalendar.kt |
| E01-S04 | -- | VacationViewModel.swift, OverviewViewModel.swift, SettingsViewModel.swift | -- | VacationViewModel.kt, OverviewViewModel.kt, SettingsViewModel.kt |
| E08-S01 | -- | (Sentry SDK Integration) | -- | (Sentry SDK Integration) |

### Welle 2: In-App Purchase + Paywall (Woche 25-26)

| Story | iOS Dateien (NEU) | iOS Dateien (MOD) | Android Dateien (NEU) | Android Dateien (MOD) |
|-------|-------------------|--------------------|-----------------------|----------------------|
| E02-S02 | StoreKitManager.swift, FakturusTrack.storekit | SubscriptionManager.swift, FakturusTrackApp.swift | -- | -- |
| E02-S03 | -- | SettingsView.swift (+Restore Button) | -- | -- |
| E03-S02 | -- | -- | BillingManager.kt | SubscriptionManager.kt, MainActivity.kt, build.gradle.kts |
| E03-S03 | -- | -- | -- | SettingsScreen.kt (+Restore Button) |
| E04-S01 | PaywallView.swift | -- | -- | -- |
| E04-S02 | -- | -- | PaywallScreen.kt, PaywallViewModel.kt | -- |
| E04-S03 | -- | (Erfolgs-Animation + Haptic) | -- | (Erfolgs-Animation + Haptic) |

### Welle 3: Testing (Woche 26-27)

Keine neuen Dateien. Modifikationen nur Bug-Fixes.

### Welle 4: Launch (Woche 27)

Keine Code-Aenderungen. Store-Submission und Go-Live.

---

## Abhaengigkeitsdiagramm (Phase 4 Dateien)

```
Tier.swift/kt
    |
    +---> FeatureGate (jedes Feature hat requiredTier)
    +---> SubscriptionManager (currentTier State)

FeatureGate.swift/kt
    |
    +---> SubscriptionManager.isAvailable(feature)
    +---> FeatureLockedOverlay / FeatureLockedCard (zeigt welches Tier noetig)
    +---> PaywallView / PaywallScreen (hervorgehobenes Feature)

SubscriptionManager.swift/kt
    |
    +---> StoreKitManager / BillingManager (fuettert Tier-Updates)
    +---> ServiceContainer (wird dort initialisiert)
    +---> ALLE Feature-Views (Gate-Checks)
    +---> Paywall (aktueller Tier fuer Preis-Anzeige)

StoreKitManager.swift (iOS)
    |
    +---> SubscriptionManager (Tier-Updates nach Kauf/Verlaengerung/Kuendigung)
    +---> PaywallView (Product.displayPrice, purchase-Aufruf)
    +---> FakturusTrackApp (Transaction.updates Listener bei App-Start)
    +---> SettingsView (AppStore.sync() fuer Restore)

BillingManager.kt (Android)
    |
    +---> SubscriptionManager (Tier-Updates)
    +---> PaywallScreen (ProductDetails, launchPurchaseFlow)
    +---> MainActivity (BillingClient Lifecycle)
    +---> SettingsScreen (queryPurchasesAsync fuer Restore)

PaywallView.swift / PaywallScreen.kt
    |
    +---> FeatureLockedOverlay / FeatureLockedCard (wird von dort geoeffnet)
    +---> PaywallTeaserView / PaywallTeaserCard (wird von dort geoeffnet)
    +---> SettingsView / SettingsScreen (manueller Aufruf)
```

---

## ServiceContainer Erweiterung

### iOS

```swift
@Observable @MainActor
final class ServiceContainer {
    // Bestehend:
    let appState = AppState()
    let authManager = AuthManager()
    let networkMonitor = NetworkMonitor()
    private(set) var apiClient: APIClient?
    private(set) var syncEngine: SyncEngine?

    // NEU Phase 4:
    let subscriptionManager = SubscriptionManager()
    let storeKitManager = StoreKitManager()

    init() {
        // Phase 4: StoreKit bei App-Start initialisieren (NICHT erst bei Login!)
        Task {
            await storeKitManager.configure(subscriptionManager: subscriptionManager)
            await storeKitManager.checkCurrentEntitlements()
        }
    }
}
```

### Android

```kotlin
class ServiceContainer(private val context: Context) {
    // Bestehend:
    val authManager by lazy { AuthManager(context) }
    val networkMonitor by lazy { NetworkMonitor(context) }
    val database by lazy { /* ... */ }
    var apiClient: APIClient? = null
    var syncEngine: SyncEngine? = null

    // NEU Phase 4:
    val subscriptionManager = SubscriptionManager(context)
    val billingManager = BillingManager(context, subscriptionManager)

    init {
        // Phase 4: Billing bei App-Start initialisieren (NICHT erst bei Login!)
        billingManager.startConnection()
    }

    fun onLogout() {
        // ... bestehender Code ...
        billingManager.endConnection()
    }
}
```

---

## Integration in FakturusTrackApp (iOS)

```swift
@main
struct FakturusTrackApp: App {
    @State private var services = ServiceContainer()

    var body: some Scene {
        WindowGroup {
            // ... bestehender Code ...
        }
        // NEU Phase 4: SubscriptionManager in Environment
        .environment(services.subscriptionManager)
        .task {
            // Transaction Listener fuer StoreKit 2
            await services.storeKitManager.listenForTransactions()
        }
    }
}
```

---

## Xcode Capabilities

Phase 4 erfordert eine zusaetzliche Capability:

```
Target: FakturusTrack
  Signing & Capabilities:
    + In-App Purchase            <-- NEU
    Keychain Sharing             (bestehend)
    Background Modes             (bestehend)
    App Groups                   (bestehend, Phase 3)
```

---

## Keine Backend-Aenderungen

Phase 4 ist vollstaendig client-seitig. Der Tier-Status wird ausschliesslich ueber Apple/Google Subscription-Status ermittelt. Das Backend wird NICHT um Tier-Information erweitert.

Begruendung: Fakturus Track ist eine Single-User-App. Server-seitiges Feature-Gating waere YAGNI. Falls spaeter ein TEAM-Tier kommt, muss diese Entscheidung revidiert werden.

---

## Schema-Migrationen

Keine Datenbank-Migrationen noetig. Der Tier-Status wird in UserDefaults (iOS) / SharedPreferences (Android) gecacht, nicht in der DB.

Begruendung: Der Tier ist transient -- er kommt von Apple/Google und wird nur fuer Offline-Zugriff lokal gecacht. Er gehoert nicht ins Datenmodell.
