# EPIC 01: Feature-Gating Infrastruktur

## Ziel

Ein robustes, plattformuebergreifend konsistentes Feature-Gating-System, das Features basierend auf dem Abo-Tier des Nutzers freischaltet oder sperrt. Das System muss offline-faehig sein (gecachter Tier-Status) und sauber mit der bestehenden App-Architektur integrieren.

## Abhaengigkeiten

- **Phase 3 abgeschlossen**: Alle Features muessen implementiert sein, bevor sie hinter Gates gesetzt werden
- **Keine Backend-Aenderung in Phase 4**: Tier-Status wird ueber Apple/Google Subscription-Status ermittelt, nicht ueber das Backend

## Design-Entscheidungen

**Tier-Verwaltung client-seitig (nicht backend-seitig)**:
- StoreKit 2 (iOS) und Play Billing (Android) liefern den Subscription-Status
- Der Tier wird lokal gecacht (UserDefaults / DataStore), um Offline-Zugriff zu ermoeglichen
- Das Backend wird NICHT um Tier-Information erweitert (YAGNI -- kein Server-seitiges Feature-Gating noetig fuer eine Single-User-App)
- Falls spaeter ein TEAM-Tier kommt, muss diese Entscheidung ggf. revidiert werden

**Graceful Degradation bei Downgrade**:
- Wenn ein Nutzer sein Abo kuendigt, werden Premium-Features gesperrt
- Bereits exportierte PDFs/CSVs bleiben erhalten
- Erfasste Krankheitstage und Urlaubstage bleiben sichtbar (Read-Only), aber neue koennen nicht angelegt werden
- Historie wird auf 30 Tage eingeschraenkt (aeltere Eintraege werden ausgeblendet, nicht geloescht)

---

## Stories

### P4-E01-S01: Feature-Flag-System (Beide Plattformen)

**Als** Entwickler
**moechte ich** ein zentrales Feature-Flag-System,
**damit** ich Features einfach nach Tier ein-/ausschalten kann, ohne jede View einzeln zu aendern.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: Phase 3 abgeschlossen
**Parallelisierbar mit**: P4-E05-*, P4-E06-*, P4-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `FeatureGate` Enum/Sealed Class definiert alle gated Features:
  - `pdfExport`, `csvExport`, `sickDays`, `vacation` (STARTER)
  - `widgets`, `overtimeDashboard` (FREE -- gemaess features.md)
  - `datevExport`, `schoolHolidays`, `calendarIntegration` (PRO)
- [ ] `SubscriptionManager` (Protocol/Interface) mit:
  - `currentTier: Tier` (FREE, STARTER, PRO)
  - `isFeatureAvailable(feature: FeatureGate) -> Bool`
  - `tierDidChange` Publisher/Flow (reaktiv)
- [ ] Tier wird lokal gecacht (UserDefaults / DataStore) fuer Offline-Zugriff
- [ ] Default-Tier ist FREE (bei keinem aktiven Abo)
- [ ] Given ein Nutzer hat STARTER-Tier
  When er `isFeatureAvailable(.pdfExport)` aufruft
  Then gibt die Funktion `true` zurueck
- [ ] Given ein Nutzer hat FREE-Tier
  When er `isFeatureAvailable(.pdfExport)` aufruft
  Then gibt die Funktion `false` zurueck

**Technische Hinweise**:
- iOS: `SubscriptionManager` als `@Observable` class, injiziert via Environment
- Android: `SubscriptionManager` als Hilt Singleton, exponiert `StateFlow<Tier>`
- Tier-Mapping: `FREE` = kein aktives Abo, `STARTER` = Product-ID "starter_monthly", `PRO` = Product-ID "pro_monthly"

---

### P4-E01-S02: iOS Feature-Gate Integration in bestehende Views

**Als** Nutzer im FREE-Tier
**moechte ich** klar sehen welche Features in meinem Tier nicht verfuegbar sind,
**damit** ich verstehe was ich mit einem Upgrade bekomme.

**Plattform**: iOS
**Abhaengigkeiten**: P4-E01-S01
**Parallelisierbar mit**: P4-E01-S03
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] **Zeiten-Tab (History)**: Im FREE-Tier nur die letzten 30 Tage sichtbar. Aeltere Eintraege: dezenter Hinweis "Aeltere Eintraege verfuegbar mit STARTER"
- [ ] **Urlaub-Tab**: Im FREE-Tier gesperrt. Tab zeigt Paywall-Teaser mit Beschreibung + Upgrade-Button
- [ ] **Gesamt-Tab**: Ueberstunden-Dashboard im FREE-Tier verfuegbar (gemaess features.md: FREE). Keine Feature-Gate-Einschraenkung.
- [ ] **Export-Bereich (Settings)**: PDF/CSV im FREE-Tier gesperrt, DATEV im FREE+STARTER gesperrt. Jeweils mit Lock-Icon und Tier-Badge
- [ ] **Widgets**: Im FREE-Tier verfuegbar (gemaess features.md: FREE). Keine Feature-Gate-Einschraenkung fuer Widgets.
- [ ] **Krankheitstage**: Im FREE-Tier ist der Long-Press-Kontextmenue-Eintrag "Krankheitstag" nicht sichtbar
- [ ] **Schulferien-Einstellungen**: Im FREE+STARTER gesperrt mit Lock-Icon
- [ ] **Kalender-URL Einstellung**: Im FREE+STARTER gesperrt mit Lock-Icon
- [ ] Kein Feature wird komplett versteckt -- gesperrte Features sind sichtbar aber nicht bedienbar (Teaser-Effekt)
- [ ] Given ein FREE-Nutzer tippt auf ein gesperrtes Feature
  When die Paywall-UI erscheint
  Then zeigt sie den benoetigten Tier und den Preis

**Technische Hinweise**:
- `FeatureLockedOverlay` ViewModifier: `.featureLocked(.pdfExport)` -- zeigt Lock-Icon + Tap-Handler fuer Paywall
- `PaywallTeaser` View fuer gesperrte Tabs (Urlaub, Gesamt)
- Bestehende Views mit `if subscriptionManager.isFeatureAvailable(.feature)` wrappen

---

### P4-E01-S03: Android Feature-Gate Integration in bestehende Views

**Als** Nutzer im FREE-Tier
**moechte ich** klar sehen welche Features in meinem Tier nicht verfuegbar sind,
**damit** ich verstehe was ich mit einem Upgrade bekomme.

**Plattform**: Android
**Abhaengigkeiten**: P4-E01-S01
**Parallelisierbar mit**: P4-E01-S02
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Identische Feature-Gating-Logik wie iOS (siehe P4-E01-S02)
- [ ] Material 3 gesperrte Elemente: Ausgegraut mit Lock-Icon-Badge
- [ ] Gesperrte Tabs zeigen Material 3 Paywall-Teaser-Card
- [ ] Given ein FREE-Nutzer tippt auf ein gesperrtes Feature
  When die Paywall-UI erscheint
  Then zeigt sie den benoetigten Tier und den Preis

**Technische Hinweise**:
- `FeatureLockedModifier` als Compose Modifier
- `PaywallTeaserCard` Composable fuer gesperrte Bereiche
- Bestehende Screens mit `if (subscriptionManager.isFeatureAvailable(feature))` wrappen

---

### P4-E01-S04: Tier-Downgrade Handling (Beide Plattformen)

**Als** Nutzer, der sein Abo gekuendigt hat,
**moechte ich** meine erfassten Daten nicht verlieren,
**damit** ich bei einer erneuten Subscription sofort weiterarbeiten kann.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P4-E01-S02, P4-E01-S03
**Parallelisierbar mit**: P4-E02-*, P4-E03-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Bei Tier-Downgrade von STARTER/PRO zu FREE:
  - Urlaubstage: Sichtbar (Read-Only), neue nicht anlegbar
  - Krankheitstage: Sichtbar (Read-Only), neue nicht anlegbar
  - Exportierte Dateien: Bleiben auf dem Geraet
  - History: Wird auf 30 Tage eingeschraenkt (aeltere ausgeblendet)
  - Laufender Timer: Funktioniert weiter (FREE-Feature)
  - Widgets: Funktionieren weiter (FREE-Feature gemaess features.md)
- [ ] Bei Tier-Upgrade von FREE zu STARTER/PRO:
  - Alle Daten sofort wieder verfuegbar (History komplett, Urlaub/Krank wieder editierbar)
  - Kein erneuter Sync noetig (Daten sind lokal vorhanden)
- [ ] Kein Datenverlust bei Tier-Wechsel (Daten werden nie geloescht)
- [ ] Given ein STARTER-Nutzer kuendigt sein Abo
  When das Abo auslaeuft
  Then wechselt die App auf FREE-Tier
  And zeigt einen freundlichen Hinweis "Ihr Abo ist abgelaufen"
  And alle Daten bleiben erhalten (Read-Only fuer Premium-Features)

**Technische Hinweise**:
- `SubscriptionManager.tierDidChange` Publisher/Flow triggert UI-Aktualisierung
- History-Filter: `sessions.filter { $0.date >= thirtyDaysAgo || subscriptionManager.currentTier >= .starter }`
- Kalender: Bestehende Eintraege mit `.opacity(0.6)` + Lock-Badge anzeigen
