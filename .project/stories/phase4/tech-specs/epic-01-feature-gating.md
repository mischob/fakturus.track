# Tech-Spec: EPIC 01 -- Feature-Gating Infrastruktur

## Uebersicht

Das Feature-Gating-System besteht aus 4 Dateien pro Plattform. Es ist bewusst einfach gehalten: ein Enum fuer Tiers, ein Enum fuer Features, ein Manager der beides verbindet.

---

## Tier.swift (iOS)

```swift
enum Tier: Int, Comparable, Codable {
    case free = 0
    case starter = 1
    case pro = 2

    static func < (lhs: Tier, rhs: Tier) -> Bool {
        lhs.rawValue < rhs.rawValue
    }

    /// Product-ID Mapping (StoreKit 2)
    init?(productID: String) {
        switch productID {
        case "starter_monthly": self = .starter
        case "pro_monthly": self = .pro
        default: return nil
        }
    }

    var displayName: String {
        switch self {
        case .free: "Free"
        case .starter: "Starter"
        case .pro: "Pro"
        }
    }
}
```

## Tier.kt (Android)

```kotlin
enum class Tier(val level: Int) : Comparable<Tier> {
    FREE(0),
    STARTER(1),
    PRO(2);

    companion object {
        fun fromProductId(productId: String): Tier? = when (productId) {
            "starter_monthly" -> STARTER
            "pro_monthly" -> PRO
            else -> null
        }
    }
}
```

---

## FeatureGate.swift (iOS)

```swift
enum FeatureGate: CaseIterable {
    // STARTER Features
    case widgets
    case pdfExport
    case csvExport
    case sickDays
    case vacation
    case overtimeDashboard

    // PRO Features
    case datevExport
    case schoolHolidays
    case calendarIntegration

    var requiredTier: Tier {
        switch self {
        case .widgets, .pdfExport, .csvExport,
             .sickDays, .vacation, .overtimeDashboard:
            return .starter
        case .datevExport, .schoolHolidays, .calendarIntegration:
            return .pro
        }
    }

    /// Lokalisierter Feature-Name fuer Paywall
    var displayName: String {
        switch self {
        case .widgets: String(localized: "feature_widgets")
        case .pdfExport: String(localized: "feature_pdf_export")
        case .csvExport: String(localized: "feature_csv_export")
        case .sickDays: String(localized: "feature_sick_days")
        case .vacation: String(localized: "feature_vacation")
        case .overtimeDashboard: String(localized: "feature_overtime_dashboard")
        case .datevExport: String(localized: "feature_datev_export")
        case .schoolHolidays: String(localized: "feature_school_holidays")
        case .calendarIntegration: String(localized: "feature_calendar_integration")
        }
    }
}
```

## FeatureGate.kt (Android)

```kotlin
enum class FeatureGate(
    val requiredTier: Tier,
    val displayNameRes: Int
) {
    // STARTER
    WIDGETS(Tier.STARTER, R.string.feature_widgets),
    PDF_EXPORT(Tier.STARTER, R.string.feature_pdf_export),
    CSV_EXPORT(Tier.STARTER, R.string.feature_csv_export),
    SICK_DAYS(Tier.STARTER, R.string.feature_sick_days),
    VACATION(Tier.STARTER, R.string.feature_vacation),
    OVERTIME_DASHBOARD(Tier.STARTER, R.string.feature_overtime_dashboard),

    // PRO
    DATEV_EXPORT(Tier.PRO, R.string.feature_datev_export),
    SCHOOL_HOLIDAYS(Tier.PRO, R.string.feature_school_holidays),
    CALENDAR_INTEGRATION(Tier.PRO, R.string.feature_calendar_integration);
}
```

---

## SubscriptionManager.swift (iOS)

```swift
import Foundation
import Observation

@Observable @MainActor
final class SubscriptionManager {
    private(set) var currentTier: Tier = .free

    private let tierCacheKey = "cached_subscription_tier"

    init() {
        // Gecachten Tier laden (Offline-Faehigkeit)
        let cached = UserDefaults.standard.integer(forKey: tierCacheKey)
        currentTier = Tier(rawValue: cached) ?? .free
    }

    func isAvailable(_ feature: FeatureGate) -> Bool {
        feature.requiredTier <= currentTier
    }

    /// Wird von StoreKitManager aufgerufen bei Kauf/Verlaengerung/Kuendigung
    func updateTier(_ newTier: Tier) {
        currentTier = newTier
        UserDefaults.standard.set(newTier.rawValue, forKey: tierCacheKey)
    }

    /// History-Filter: nur 30 Tage im FREE-Tier
    var historyDateLimit: Date? {
        guard currentTier < .starter else { return nil }
        return Calendar.current.date(byAdding: .day, value: -30, to: Date())
    }
}
```

## SubscriptionManager.kt (Android)

```kotlin
class SubscriptionManager(private val context: Context) {
    private val prefs = context.getSharedPreferences("subscription", Context.MODE_PRIVATE)
    private val _tier = MutableStateFlow(loadCachedTier())
    val tier: StateFlow<Tier> = _tier.asStateFlow()

    fun isAvailable(feature: FeatureGate): Boolean =
        feature.requiredTier <= _tier.value

    fun updateTier(newTier: Tier) {
        _tier.value = newTier
        prefs.edit().putInt("cached_tier", newTier.level).apply()
    }

    val historyDateLimit: LocalDate?
        get() = if (_tier.value < Tier.STARTER) {
            LocalDate.now().minusDays(30)
        } else null

    private fun loadCachedTier(): Tier {
        val level = prefs.getInt("cached_tier", 0)
        return Tier.entries.firstOrNull { it.level == level } ?: Tier.FREE
    }
}
```

---

## FeatureLockedOverlay.swift (iOS ViewModifier)

```swift
struct FeatureLockedOverlay: ViewModifier {
    let feature: FeatureGate
    @Environment(SubscriptionManager.self) private var subscriptionManager
    @State private var showPaywall = false

    func body(content: Content) -> some View {
        if subscriptionManager.isAvailable(feature) {
            content
        } else {
            content
                .disabled(true)
                .overlay(alignment: .topTrailing) {
                    Image(systemName: "lock.fill")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(6)
                        .background(.ultraThinMaterial, in: Circle())
                        .padding(4)
                }
                .opacity(0.6)
                .onTapGesture {
                    showPaywall = true
                }
                .sheet(isPresented: $showPaywall) {
                    PaywallView(highlightedFeature: feature)
                }
        }
    }
}

extension View {
    func featureLocked(_ feature: FeatureGate) -> some View {
        modifier(FeatureLockedOverlay(feature: feature))
    }
}
```

## FeatureLockedCard.kt (Android Composable)

```kotlin
@Composable
fun FeatureLockedCard(
    feature: FeatureGate,
    subscriptionManager: SubscriptionManager,
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit
) {
    var showPaywall by remember { mutableStateOf(false) }
    val tier by subscriptionManager.tier.collectAsState()

    if (feature.requiredTier <= tier) {
        content()
    } else {
        Box(
            modifier = modifier
                .alpha(0.6f)
                .clickable { showPaywall = true }
        ) {
            content()
            // Lock Badge oben rechts
            Icon(
                imageVector = Icons.Default.Lock,
                contentDescription = null,
                modifier = Modifier
                    .align(Alignment.TopEnd)
                    .padding(4.dp)
                    .size(16.dp),
                tint = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }

    if (showPaywall) {
        PaywallBottomSheet(
            highlightedFeature = feature,
            onDismiss = { showPaywall = false }
        )
    }
}
```

---

## PaywallTeaserView.swift (iOS) -- fuer gesperrte Tabs

```swift
struct PaywallTeaserView: View {
    let feature: FeatureGate
    let title: String
    let description: String
    @State private var showPaywall = false

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "lock.shield")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)

            Text(title)
                .font(.title2.bold())

            Text(description)
                .font(.body)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal, 32)

            Button {
                showPaywall = true
            } label: {
                Text(String(localized: "paywall_upgrade_button"))
                    .font(.headline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .padding(.horizontal, 48)

            Text("\(String(localized: "paywall_required_tier")): \(feature.requiredTier.displayName)")
                .font(.caption)
                .foregroundStyle(.tertiary)

            Spacer()
        }
        .sheet(isPresented: $showPaywall) {
            PaywallView(highlightedFeature: feature)
        }
    }
}
```

---

## Integration in bestehende Views

### TimeTrackingView.swift -- History-Filter

```swift
// In groupedByMonth computed property:
private var filteredSessions: [WorkSession] {
    guard let limit = subscriptionManager.historyDateLimit else {
        return sessions // Kein Limit fuer STARTER+
    }
    return sessions.filter { $0.date >= limit }
}

// Nach der History-Liste, wenn Filter aktiv:
if subscriptionManager.historyDateLimit != nil {
    PaywallTeaserView(
        feature: .csvExport, // Trigger fuer STARTER-Upgrade
        title: String(localized: "history_older_entries"),
        description: String(localized: "history_upgrade_hint")
    )
    .listRowBackground(Color.clear)
}
```

### VacationScreen.swift -- Gesperrter Tab

```swift
var body: some View {
    if subscriptionManager.isAvailable(.vacation) {
        vacationContent // bestehender Code
    } else {
        PaywallTeaserView(
            feature: .vacation,
            title: String(localized: "vacation_locked_title"),
            description: String(localized: "vacation_locked_description")
        )
    }
}
```

### OverviewScreen.swift -- Export-Section mit Gates

```swift
// PDF Export Button:
Button { /* ... */ }
    .featureLocked(.pdfExport)

// CSV Export Buttons:
Button { /* ... */ }
    .featureLocked(.csvExport)

// DATEV Export Button:
Button { /* ... */ }
    .featureLocked(.datevExport)
```

### SettingsView.swift -- Schulferien + Kalender-URL

```swift
// Schulferien Section:
Section(String(localized: "settings_school_holidays")) {
    Button { showSchoolHolidays = true } label: { /* ... */ }
        .featureLocked(.schoolHolidays)
}

// Kalender-URL (falls vorhanden):
// .featureLocked(.calendarIntegration) auf das TextField/Button

// NEU: Restore Purchases + Abo-Status
Section(String(localized: "settings_subscription")) {
    HStack {
        Text(String(localized: "settings_current_tier"))
        Spacer()
        Text(subscriptionManager.currentTier.displayName)
            .foregroundStyle(.secondary)
    }

    Button(String(localized: "settings_restore_purchases")) {
        Task { await vm.restorePurchases() }
    }
}
```

---

## Tier-Downgrade Handling

### Urlaubstage bei Downgrade (Read-Only)

```swift
// VacationViewModel.swift
func toggleVacationDay(date: Date) {
    guard subscriptionManager.isAvailable(.vacation) else {
        // Zeige Paywall statt Toggle
        showPaywall = true
        return
    }
    // ... bestehende Toggle-Logik ...
}
```

### Krankheitstage bei Downgrade

```swift
// VacationCalendar.swift -- Long-Press Kontextmenue
if subscriptionManager.isAvailable(.sickDays) {
    Button("Krankheitstag") { vm.toggleSickDay(date: date) }
}
// Kein else -- Menue-Eintrag wird einfach nicht angezeigt
```

### Bestehende Daten bei Downgrade

Daten werden NIE geloescht. Bei FREE-Tier:
- Urlaubstage: Sichtbar mit `opacity(0.6)` + Lock-Badge, nicht editierbar
- Krankheitstage: Sichtbar mit `opacity(0.6)`, nicht editierbar
- History > 30 Tage: Ausgeblendet (nicht in Query-Ergebnis)
- Exportierte Dateien: Bleiben auf dem Geraet

Bei Upgrade: Sofort alles wieder verfuegbar. Kein Sync noetig.
