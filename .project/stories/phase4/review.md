# Devils Advocate Review -- Phase 4: Store-Launch, Feature-Gating & Migration

**Reviewer**: Devils Advocate Agent
**Datum**: 2026-03-29
**Gegenstand**: Gesamte Phase-4-Planung (10 EPICs, 32 Stories, Tech-Specs, Execution Waves)

---

## Gesamturteil: [WARNUNG] Empfohlen mit Aenderungen

Die Planung ist insgesamt solide und gut strukturiert. Feature-Gating ist korrekt spezifiziert, StoreKit 2 / Play Billing sind technisch sauber, die Paywall ist Apple/Google-konform. Aber: Der Zeitplan ist auf Kante genaht mit nur 0.5 Wochen Puffer bei einem kritischen Pfad von 4.5 Wochen. Es gibt mehrere mittelschwere Luecken bei Billing-Version, Acknowledge-Retry, Offline-Tier-Sicherheit und einem unterschaetzten Testing-Aufwand.

---

## Befunde

### [KRITISCH] 1. Zeitplan: 0.5 Wochen Puffer ist keine Reserve

- **Problem**: Der kritische Pfad betraegt 4.5 Wochen bei 5 Wochen Gesamtzeit. Das ergibt 0.5 Wochen Puffer. Eine Apple App Store Review-Ablehnung kostet typischerweise 3-7 Tage (Fix + erneute Einreichung + erneutes Review). Eine **einzige** Ablehnung sprengt den Zeitplan.
- **Risiko**: Ersteinreichungen haben eine hoehere Ablehnungsrate. Haeufige Gruende: Unklare Subscription Terms, fehlende Demo-Daten fuer Reviewer, Metadata-Fehler. Bei einer Zeiterfassungs-App koennte Apple auch nach der Login-Begruendung fragen (Guideline 5.1.1). Google Play Ersteinreichungen dauern bis zu 7 Tage.
- **Erschwerend**: Phase 3 hatte bereits ein aehnliches Scope-Problem (10 EPICs in 4 Wochen -- siehe Phase-3-Review). Wenn Phase 3 auch nur 3 Tage ueberzieht, beginnt Phase 4 unter Druck.
- **Alternative**:
  1. Store-Submission fruehzeitig als "Pre-Submission" einreichen (iOS TestFlight External Testing erfordert ein Beta App Review, das die meisten Review-Probleme frueh aufdeckt). Das ist bereits als E08-S04 geplant -- aber der Wert als Review-Pre-Validation wird nicht explizit genutzt.
  2. Google Play Internal Testing Track (kein Review noetig) fuer fruehes Testing nutzen und erst spaet auf Production wechseln.
  3. **E10 (MAUI-Migration) aus Phase 4 herausnehmen** und als separate Phase 4b nach dem Launch planen. Das schafft 1 Woche echten Puffer auf dem kritischen Pfad, da E10 erst nach E09 beginnt und nicht parallelisierbar ist.

### [KRITISCH] 2. Google Play Billing Library Version: v7.1.1 existiert nicht (Stand Maerz 2026)

- **Problem**: Die Tech-Specs (tech-blueprint.md, tech-specs/epic-03) referenzieren `com.android.billingclient:billing-ktx:7.1.1`. Die letzte veroeffentlichte Version der Billing Library ist v7.0.0 (Release Q1 2026). Version 7.1.1 gibt es nicht.
- **Risiko**: Build-Fehler beim Projektstart. Kleine Korrektur, aber zeigt dass die Spec nicht gegen aktuelle Artefakte validiert wurde.
- **Alternative**: Auf `7.0.0` setzen und im `libs.versions.toml` dokumentieren, dass bei Phase-4-Start die aktuellste 7.x geprueft werden soll.

### [KRITISCH] 3. Acknowledge-Retry auf Android: TODO im Code ist eine Zeitbombe

- **Problem**: In `BillingManager.kt` (tech-specs/epic-03, Zeile 175) steht:
  ```
  // TODO: In WorkManager-Job verschieben fuer Retry
  ```
  Das Acknowledge eines Kaufs ist **Pflicht innerhalb von 3 Tagen**. Ohne Acknowledge erstattet Google den Kauf automatisch. Ein TODO hier ist inakzeptabel.
- **Risiko**: Wenn `acknowledgePurchase()` fehlschlaegt (Netzwerkfehler, BillingClient disconnected) und kein Retry existiert, verliert der Nutzer sein Abo nach 3 Tagen. Das ist ein Umsatzverlust UND eine schlechte UX.
- **Alternative**: WorkManager-basierter Retry **muss** Teil der E03-S02 Story sein, nicht ein TODO. Akzeptanzkriterium hinzufuegen: "Bei fehlgeschlagenem Acknowledge wird ein WorkManager OneTimeWorkRequest mit exponential backoff erstellt."

### [BEDENKEN] 4. Offline-Tier-Caching in UserDefaults/SharedPreferences ist manipulierbar

- **Problem**: Der Tier wird in `UserDefaults` (iOS) bzw. `SharedPreferences` (Android) als Integer gecacht. Ein technisch versierter Nutzer kann den Wert manuell aendern und Premium-Features freischalten.
- **Risiko**: Gering fuer eine B2B-Zeiterfassungs-App (die Zielgruppe ist nicht technikaffin). Aber: Jailbroken/Rooted Geraete machen das trivial. Der Export (PDF, CSV, DATEV) wird dann serverseitig nicht validiert -- ein FREE-Nutzer koennte lokal Exports generieren, da die Daten komplett lokal vorhanden sind.
- **Kontext**: Die bewusste Entscheidung "kein Server-seitiges Feature-Gating" (YAGNI) ist im tech-blueprint dokumentiert und fuer eine Single-User-App vertretbar. Aber das Risiko sollte explizit akzeptiert und dokumentiert werden.
- **Alternative**: Kein sofortiger Handlungsbedarf, aber in der README oder einer ADR (Architecture Decision Record) dokumentieren: "Client-seitiges Gating ist bewusst. Manipulation ist moeglich, wird aber fuer den aktuellen Markt akzeptiert." Spaeter bei Bedarf: Exportvorgaenge serverseitig pruefen (API liefert nur Daten wenn Tier stimmt).

### [BEDENKEN] 5. Feature-Gating Tier-Zuordnung hat Widersprueche zu features.md

- **Problem 1**: In `features.md` steht Ueberstunden-Dashboard als `[FREE]` in der MoSCoW-Matrix ("Should Have Phase 2: Ueberstunden-Dashboard [FREE]"). In der Phase-4 README und Tier-Tabelle steht es als `STARTER`. Das ist ein Widerspruch.
- **Problem 2**: In `features.md` steht "Widgets (iOS + Android) [FREE]" unter Phase 3 Could Have. In der Phase-4 Tier-Tabelle sind Widgets als `STARTER` eingeordnet.
- **Problem 3**: In `features.md` steht "Kalender-Import [STARTER]" unter Phase 2 Should Have. In Phase 4 wird Kalender-Integration als `PRO` eingeordnet. Sind das dasselbe Feature oder unterschiedliche?
- **Risiko**: Verwirrung bei der Implementierung. Wenn ein Agent `features.md` als Referenz nutzt und ein anderer die Phase-4 README, implementieren sie unterschiedliche Gates.
- **Alternative**: `features.md` als Single Source of Truth fuer Tier-Zuordnungen aktualisieren. Die Phase-4 README Tier-Tabelle muss exakt mit features.md uebereinstimmen. **Entscheidung treffen**: Ist das Ueberstunden-Dashboard FREE oder STARTER? Sind Widgets FREE oder STARTER? Die Antwort hat direkte Auswirkung auf den Umsatz und das ArbZG-Argument.

### [BEDENKEN] 6. StoreKitManager wird als Singleton in ServiceContainer initialisiert -- aber StoreKit 2 braucht keinen Login

- **Problem**: Der tech-blueprint zeigt StoreKit-Konfiguration in `onLogin()`. Aber StoreKit 2 funktioniert ausschliesslich ueber die Apple-ID -- es braucht keinen App-Login. Wenn ein Nutzer die App im "Anonymen Modus" (offline ohne Login, laut features.md verfuegbar) nutzt, wird `onLogin()` nie aufgerufen, und StoreKit wird nie konfiguriert.
- **Risiko**: Ein Nutzer der die App ohne Login nutzt, kann kein Abo kaufen. Das ist moeglicherweise gewollt (Abo braucht Sync, Sync braucht Login), aber es wird nirgends explizit entschieden oder dokumentiert.
- **Alternative**: Entscheidung dokumentieren. Empfehlung: StoreKit-Initialisierung bei App-Start (nicht erst bei Login), aber Kauf-Button nur zeigen wenn auch Login vorhanden ist. Grund: `Transaction.updates` Listener sollte auch im Nicht-Login-Zustand laufen, damit ein Abo das ueber Apple-ID-Einstellungen gekauft wurde, erkannt wird.

### [BEDENKEN] 7. Paywall-UI: `storeKitManager` ist `@State private` und wird nicht injiziert

- **Problem**: In `PaywallView.swift` (tech-specs/epic-04) ist `storeKitManager` als `@State private var storeKitManager: StoreKitManager?` deklariert mit dem Kommentar "via ServiceContainer". Aber es gibt keine Zuweisung, keine Environment-Injection, keine Initialization. Die PaywallView hat keinen Zugriff auf den StoreKitManager.
- **Risiko**: Die Paywall kann keine Produkte laden und keinen Kauf starten. Build-Fehler oder Runtime-nil-Crash.
- **Alternative**: StoreKitManager ueber `@Environment` injizieren (analog zu SubscriptionManager) oder als Parameter an PaywallView uebergeben. Dieses Pattern muss in der Tech-Spec korrigiert werden.

### [BEDENKEN] 8. Testing-Matrix: Nur Simulator, keine realen IAP-Tests spezifiziert

- **Problem**: Die Testmatrix (tech-specs/epic-08) listet hauptsaechlich Simulatoren. StoreKit Sandbox-Testing auf dem Simulator ist limitiert (kein echtes Zahlungs-UI, kein Sandbox-Account-Login). Play Billing License Testing funktioniert NUR auf echten Geraeten mit signiertem Build.
- **Risiko**: IAP-Bugs die nur auf echten Geraeten auftreten (z.B. Sandbox-Account nicht korrekt eingerichtet, BillingClient Connection-Probleme auf bestimmten OEM-Skins) werden erst beim Launch gefunden.
- **Alternative**: Die Testmatrix listet zwar "Physisches Geraet" als eine Zeile, aber die konkreten IAP-Tests (Starter kaufen, Pro kaufen, Restore) sind nicht explizit als "nur auf physischem Geraet" markiert. Empfehlung: IAP-Tests als eigene Kategorie "Nur echtes Geraet" kennzeichnen und sicherstellen dass mindestens 1 iOS + 1 Android physisches Geraet verfuegbar ist.

### [BEDENKEN] 9. MAUI Migration: `HasPendingSyncsAsync()` wird als existent angenommen

- **Problem**: Die Tech-Spec (epic-10) und epic-10-maui-migration.md referenzieren `SyncService.HasPendingSyncsAsync()` mit dem Kommentar "existiert bereits (laut migration.md)". Im aktuellen Git-Status sehen wir `Fakturus.Track.Mobile/Services/Offline/SyncService.cs` als modifiziert -- aber nicht ob die Methode tatsaechlich existiert.
- **Risiko**: Gering, da die Methode entweder existiert oder trivial zu implementieren ist. Aber Specs sollten keine Annahmen ueber existierenden Code machen ohne Verifizierung.
- **Alternative**: Vor Sprint-Start mit `grep` verifizieren ob `HasPendingSyncsAsync` in SyncService.cs existiert.

### [HINWEIS] 10. Kein Jahres-Abo geplant -- verschenktes Umsatzpotenzial

- **Problem**: Es werden nur monatliche Abos angeboten (`starter_monthly`, `pro_monthly`). Kein Jahres-Abo mit Rabatt. Die meisten erfolgreichen Subscription-Apps bieten beides an, wobei das Jahres-Abo typischerweise 15-20% guenstiger pro Monat ist und hoehere Retention erzielt.
- **Risiko**: Kein unmittelbares Risiko, aber: Nutzer die sich fuer ein Abo entscheiden, bevorzugen oft Jahres-Abos. Fehlendes Jahres-Abo senkt die Conversion Rate.
- **Alternative**: Fuer den Launch ist nur monatlich vertretbar (YAGNI -- erst Marktdaten sammeln). Aber: In der Subscription Group sollte Platz fuer spaetere Erweiterung sein. Das ist bei StoreKit 2 Subscription Groups und Play Billing Base Plans automatisch gegeben. **Kein Handlungsbedarf jetzt**, aber als Post-Launch Feature notieren.

### [HINWEIS] 11. Sentry Opt-In vs. Always-On: DSGVO-Entscheidung getroffen aber nicht konsistent

- **Problem**: In tech-specs/epic-08 wird die Entscheidung "Option A: immer aktiv" getroffen. In der Privacy Policy (tech-specs/epic-07) steht: "Crashlytics/Sentry: Falls verwendet, Opt-In-Moeglichkeit dokumentieren". Das sind zwei widerspruechliche Aussagen.
- **Risiko**: Apple Review ist hier unkritisch (Crashes sind unter "Diagnostics" akzeptiert). Aber die DSGVO-Argumentation muss konsistent sein: Wenn "immer aktiv" mit berechtigtem Interesse (Art. 6(1)(f)) begruendet wird, dann muss die Privacy Policy das auch so darstellen -- nicht als Opt-In.
- **Alternative**: Privacy Policy anpassen: "Anonymisierte Crash-Reports werden automatisch erfasst (Rechtsgrundlage: berechtigtes Interesse, Art. 6(1)(f) DSGVO). Es werden keine personenbezogenen Daten uebermittelt." Den Satz mit "Opt-In-Moeglichkeit" entfernen.

### [HINWEIS] 12. Android SubscriptionManager verwendet Hilt-Annotation in Epic-Story, aber nicht in Tech-Spec

- **Problem**: In der implementation-checklist.md steht "Android: SubscriptionManager als Hilt Singleton". In der Tech-Spec wird er als einfache Klasse mit `context` Parameter implementiert, ohne Hilt `@Singleton` oder `@Inject`. Der ServiceContainer erstellt ihn manuell.
- **Risiko**: Inkonsistenz, aber da der ServiceContainer als manueller DI-Container fungiert (kein Hilt im Projekt), ist die Tech-Spec korrekt und die Checklist falsch.
- **Alternative**: Checklist-Text korrigieren: "Android: SubscriptionManager als Singleton im ServiceContainer" statt "Hilt Singleton". Das existierende Android-Projekt verwendet kein Hilt (ServiceContainer ist manuell).

---

## Positives

Die Planung hat mehrere Staerken die ausdruecklich anzuerkennen sind:

1. **Feature-Gating ist korrekt entworfen**: Die Trennung von `Tier`, `FeatureGate` und `SubscriptionManager` ist sauber, einfach und testbar. Kein Over-Engineering.
2. **YAGNI konsequent angewandt**: Kein Server-seitiges Feature-Gating, keine Datenbank-Migration, kein Remote Config -- alles bewusst und begruendet vermieden.
3. **ArbZG-Konformitaet im FREE-Tier**: Timer, Pausen, Feiertage bleiben kostenlos. Das ist rechtlich notwendig und als "nicht verhandelbar" markiert.
4. **Daten werden nie geloescht**: Downgrade-Handling ist korrekt (Read-Only, nicht loeschen). Das schuetzt vor Datenverlust und macht Re-Subscription attraktiv.
5. **Dynamische Preise**: `Product.displayPrice` / `ProductDetails.subscriptionOfferDetails` statt hardcodierter Preise. Apple/Google-konform.
6. **Restore Purchases Button**: Explizit geplant und als Apple-Review-Pflicht markiert.
7. **Paywall hat Terms + Privacy Links**: Apple Guideline 3.1.2 Konformitaet.
8. **Store-Vorbereitung parallel zu Feature-Gating**: Korrekte Nutzung der Unabhaengigkeit.
9. **Rollback-Plan mit 3 Stufen**: Durchdacht, inkl. Feature-Disable ueber Tier-Manipulation.
10. **Koordinierter Launch**: "Manuell freigeben" / "Managed Publishing" fuer synchronen Release.
11. **MAUI 4-Wochen Parallelbetrieb**: Sichere Migration mit Fallback.

---

## Empfehlung

### Vor Sprint-Start (Prio 1 -- Blockierend)

1. **Tier-Zuordnung Widersprueche aufloesen**: Ueberstunden-Dashboard (FREE oder STARTER?), Widgets (FREE oder STARTER?), Kalender-Import vs. Kalender-Integration -- Entscheidung in `features.md` UND Phase-4 README synchronisieren.
2. **Billing Library Version korrigieren**: `7.1.1` -> aktuelle 7.x Version in allen Docs.
3. **Acknowledge-Retry**: TODO in BillingManager.kt durch konkreten WorkManager-Retry ersetzen. Als Akzeptanzkriterium in E03-S02 aufnehmen.
4. **PaywallView StoreKitManager-Injection fixen**: Tech-Spec korrigieren -- `@State private var` kann nicht funktionieren.

### Vor Sprint-Start (Prio 2 -- Wichtig)

5. **E10 (MAUI-Migration) aus Phase 4 herausloesen**: Als Phase 4b nach dem Launch planen. Schafft 1 Woche echten Puffer. Die MAUI-Migration ist ohnehin erst nach dem Launch relevant.
6. **IAP-Tests als "Nur echtes Geraet" in Testmatrix markieren**.
7. **Sentry Opt-In/Always-On in Privacy Policy konsistent machen**.
8. **Hilt-Referenz aus implementation-checklist entfernen** (Projekt nutzt kein Hilt).

### Waehrend Sprint (Prio 3 -- Nice to Have)

9. **Offline-Tier-Manipulation als akzeptiertes Risiko dokumentieren** (ADR oder README-Absatz).
10. **StoreKit-Initialisierung unabhaengig von Login pruefen** -- Transaction.updates Listener sollte immer laufen.
11. **Jahres-Abo als Post-Launch Feature notieren**.
12. **`HasPendingSyncsAsync()` Existenz in SyncService.cs vor E10-Start verifizieren**.

---

## Zusammenfassung Schweregrade

| Schweregrad | Anzahl | Handlungsbedarf |
|-------------|--------|-----------------|
| KRITISCH    | 3      | Muss vor Sprint-Start geloest werden |
| BEDENKEN    | 6      | Sollte vor oder waehrend Sprint geloest werden |
| HINWEIS     | 3      | Kann spaeter adressiert werden |

**Risiko-Einschaetzung Zeitplan**: Bei Behebung der kritischen Punkte und Herausloesung von E10 steigt der Puffer auf 1.5 Wochen -- ausreichend fuer eine App Store Ablehnung. Ohne diese Anpassung ist das Risiko eines verpassten Launch-Termins **hoch**.
