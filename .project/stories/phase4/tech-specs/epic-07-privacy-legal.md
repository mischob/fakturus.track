# Tech-Spec: EPIC 07 -- Privacy Policy & Rechtliches

## Uebersicht

Statische Webseiten unter track.fakturus.com. Keine App-Code-Aenderungen noetig -- die Links sind bereits in SettingsView/SettingsScreen vorhanden (Phase 3, E10-S01).

Einzige App-Aenderung: Paywall muss Terms + Privacy verlinken (siehe EPIC 04).

---

## URLs

| Dokument | URL | Sprache |
|----------|-----|---------|
| Datenschutzerklaerung | https://track.fakturus.com/privacy | DE (primaer), EN Toggle |
| Nutzungsbedingungen | https://track.fakturus.com/terms | DE (primaer), EN Toggle |
| Impressum | https://track.fakturus.com/imprint | DE |

---

## Datenschutzerklaerung -- Kernpunkte

### Verantwortlicher
- Name, Adresse, Kontakt (E-Mail)

### Erhobene Daten
| Daten | Zweck | Rechtsgrundlage |
|-------|-------|-----------------|
| Name, E-Mail | Account/Login | Art. 6(1)(b) DSGVO -- Vertragsdurchfuehrung |
| Arbeitszeiten, Pausen | Zeiterfassung | Art. 6(1)(b) DSGVO |
| Urlaubstage, Krankheitstage | Verwaltung | Art. 6(1)(b) DSGVO |
| Geraete-ID | Synchronisation | Art. 6(1)(f) DSGVO -- berechtigtes Interesse |
| Crash-Reports (anonym) | Fehlerbehebung | Art. 6(1)(f) DSGVO |

### Datenempfaenger
- Azure Germany (Hosting, EU-Rechenzentrum)
- Azure B2C (Authentifizierung, EU)
- Sentry (Crash-Reports, EU, optional Opt-In)
- Apple/Google (In-App-Purchase Zahlungsabwicklung -- wir erhalten KEINE Zahlungsdaten)

### Kein Drittland-Transfer
Alle Server in EU (Azure Germany Region).

### Speicherdauer
- Solange Konto aktiv
- 30 Tage nach Account-Loeschung

### Betroffenenrechte
- Auskunft (Art. 15)
- Berichtigung (Art. 16)
- Loeschung (Art. 17)
- Einschraenkung (Art. 18)
- Datenportabilitaet (Art. 20)
- Widerspruch (Art. 21)

### Kein Tracking
- Keine Werbe-SDKs
- Keine Analytics-SDKs (kein Firebase Analytics, kein Google Analytics)
- Keine Datenweitergabe an Dritte
- Crash-Reporting (Sentry) nur anonymisiert, optional Opt-In

---

## Nutzungsbedingungen -- Kernpunkte

- Geltungsbereich: App + zugehoerige Cloud-Dienste
- Abo-Bedingungen: Auto-Renewal, Kuendigung ueber Apple-ID/Google Play
- Erstattung: Ueber Apple/Google (nicht direkt)
- Haftungsausschluss: Keine Gewaehr fuer Steuerberatung, DATEV-Export ist Beta
- Nutzungsrechte: Persoenliche, nicht-uebertragbare Lizenz
- Konto-Loeschung: Auf Anfrage per E-Mail oder In-App (spaeter)
- Aenderungsvorbehalt: Wir koennen Bedingungen aendern, Nutzer werden informiert
- Anwendbares Recht: Deutsches Recht
- Gerichtsstand: [Firmensitz]

---

## Impressum (TMG-Pflicht)

- Name und Anschrift des Anbieters
- Kontakt (E-Mail, Telefon optional)
- USt-IdNr. (falls vorhanden)
- Verantwortlich fuer Inhalt: Name

---

## Bestehende App-Integration

Die Links sind bereits implementiert (Phase 3):

**iOS (SettingsView.swift, Zeile 136-140)**:
```swift
Link(String(localized: "settings_privacy"),
     destination: URL(string: "https://track.fakturus.com/privacy")!)
Link(String(localized: "settings_imprint"),
     destination: URL(string: "https://track.fakturus.com/imprint")!)
```

**Android (SettingsScreen.kt, Zeile 338-357)**:
```kotlin
ListItem(
    headlineContent = { Text(stringResource(R.string.settings_privacy)) },
    modifier = Modifier.clickable {
        uriHandler.openUri("https://track.fakturus.com/privacy")
    }
)
```

### Fehlende Links (muessen in Phase 4 ergaenzt werden)

1. **Paywall**: Terms + Privacy Links unten (Apple/Google Pflicht bei Subscriptions)
   - Implementiert in PaywallView.swift / PaywallScreen.kt (siehe EPIC 04)

2. **Nutzungsbedingungen** Link in Settings fehlt noch
   - Ergaenzen analog zu Privacy/Impressum Link

---

## Hosting

Statische HTML-Seiten auf bestehender fakturus.com Infrastruktur. Kein CMS noetig.

Optionen:
- Einfache HTML-Dateien im bestehenden Webserver
- Markdown -> HTML via Static Site Generator (Hugo, Jekyll)
- Azure Static Web App

Die URLs muessen **oeffentlich erreichbar sein** (ohne Login). Apple und Google pruefen die Erreichbarkeit.
