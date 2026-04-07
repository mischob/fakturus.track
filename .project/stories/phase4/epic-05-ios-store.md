# EPIC 05: App Store Vorbereitung (iOS)

## Ziel

Alle erforderlichen Materialien und Metadaten fuer die App Store Einreichung sind vorbereitet. Screenshots, Beschreibungen, Keywords und technische Konfigurationen entsprechen den Apple Review Guidelines.

## Abhaengigkeiten

- **Phase 3 abgeschlossen**: Finale UI fuer Screenshots (Dark Mode, Polish)
- **E07 (Privacy Policy)**: Privacy Policy URL muss stehen

---

## Stories

### P4-E05-S01: App Store Screenshots erstellen

**Als** Product Owner
**moechte ich** professionelle App Store Screenshots,
**damit** potenzielle Nutzer sofort verstehen was die App kann.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 3 (finale UI)
**Parallelisierbar mit**: P4-E06-*, P4-E01-*, P4-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Screenshots fuer 3 Geraeteklassen:
  - iPhone 6.7" (iPhone 15 Pro Max / 16 Pro Max) -- Pflicht
  - iPhone 6.1" (iPhone 15 / 16) -- Pflicht
  - iPad Pro 12.9" (falls iPad-Support, sonst Optional)
- [ ] Mindestens 5 Screenshots pro Geraeteklasse in folgender Reihenfolge:
  1. Timer-Screen (laufende Session mit Live-Timer) -- "Arbeitszeit erfassen. Einfach."
  2. History (Monatsgruppierung, mehrere Eintraege) -- "Alle Zeiten im Ueberblick"
  3. Urlaub-Kalender (mit Feiertagen und Urlaubstagen markiert) -- "Urlaub & Feiertage verwalten"
  4. Gesamt-Tab (Ueberstunden-Dashboard) -- "Ueberstunden auf einen Blick"
  5. Export (PDF-Vorschau oder Export-Dialog) -- "PDF & CSV Export fuer Ihren Steuerberater"
- [ ] Screenshots in DE (primaer) und EN (sekundaer)
- [ ] Jeder Screenshot hat einen kurzen Marketing-Text als Overlay (Headline ueber dem Screenshot)
- [ ] Screenshots zeigen realistische Demo-Daten (keine leeren Screens)
- [ ] Dark Mode Screenshot als optionaler 6. Screenshot

**Technische Hinweise**:
- Xcode UI Tests oder `xcrun simctl` fuer automatisierte Screenshot-Erstellung
- Fastlane `snapshot` + `frameit` fuer automatisierte Frames und Marketing-Texte
- Demo-Daten: 3 Monate Historie mit variierenden Arbeitszeiten, Pausen, Urlaub, Krankheitstagen

---

### P4-E05-S02: App Store Listing (Beschreibung, Keywords)

**Als** Product Owner
**moechte ich** eine optimierte App Store Beschreibung,
**damit** die App in der Store-Suche gut platziert wird und Nutzer zum Download motiviert werden.

**Plattform**: iOS (App Store Connect)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **App-Name**: "Fakturus Track -- Zeiterfassung" (max. 30 Zeichen)
- [ ] **Untertitel**: "Arbeitszeit. Einfach. Ueberall." (max. 30 Zeichen)
- [ ] **Keywords** (max. 100 Zeichen, kommagetrennt):
  - DE: "Zeiterfassung,Arbeitszeit,Stempeluhr,ArbZG,Ueberstunden,Stundenzettel,Arbeitszeitgesetz,DATEV"
  - EN: "time tracking,work hours,timesheet,overtime,punch clock,DATEV,work log"
- [ ] **Beschreibung** (DE + EN):
  - Erster Absatz: Value Proposition (50 Woerter max, wird in Suchergebnissen angezeigt)
  - Feature-Liste mit Bullet Points
  - Freemium-Hinweis ("Kostenlos starten, Premium ab 2,99 EUR/Monat")
  - ArbZG-Konformitaet hervorheben
  - Offline-First erwaehnen
- [ ] **Promotional Text** (max. 170 Zeichen, aenderbar ohne Review):
  - "Jetzt NEU: Fakturus Track -- die native Zeiterfassungs-App fuer Deutschland. ArbZG-konform. Offline-first. DATEV-Export."
- [ ] **Kategorie**: Primaer "Business", Sekundaer "Productivity"
- [ ] **Altersfreigabe**: 4+ (keine bedenklichen Inhalte)
- [ ] **Was ist neu** (Version 1.0):
  - "Die erste Version von Fakturus Track -- nativ fuer iOS. Zeiterfassung, Pausenerfassung, Ueberstunden, Urlaub, Export und mehr."

**Technische Hinweise**:
- ASO (App Store Optimization): Keywords nach Suchvolumen priorisieren
- Beschreibung: Die ersten 167 Zeichen sind am wichtigsten (sichtbar ohne "Mehr")
- Promotional Text kann ohne App Update geaendert werden -- ideal fuer saisonale Anpassungen

---

### P4-E05-S03: App Store Review Compliance (iOS)

**Als** Entwickler
**moechte ich** sicherstellen dass die App den Apple Review Guidelines entspricht,
**damit** die Ersteinreichung ohne Ablehnung durchgeht.

**Plattform**: iOS
**Abhaengigkeiten**: E02 (StoreKit), E04 (Paywall)
**Parallelisierbar mit**: P4-E06-*, P4-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Guideline 3.1.1 (In-App Purchase)**: Alle digitalen Features hinter IAP, kein externer Zahlungslink
- [ ] **Guideline 3.1.2 (Subscriptions)**: Auto-Renewable Subscription korrekt implementiert:
  - Abo-Bedingungen klar kommuniziert
  - Kuendigungshinweis vorhanden
  - Preis dynamisch geladen (nicht hardcoded)
- [ ] **Guideline 5.1.1 (Privacy)**: Privacy Policy URL in App Store Connect hinterlegt
- [ ] **Guideline 5.1.2 (Data Use)**: App Privacy Details ("Nutrition Labels") korrekt ausgefuellt:
  - Daten fuer App-Funktionalitaet: Arbeitszeiten, Urlaubstage (mit User verknuepft)
  - Identifikatoren: Geraete-ID fuer Sync
  - Keine Tracking-Daten, keine Analyse-Daten, keine Werbung
- [ ] **Guideline 2.1 (Performance)**: App startet zuverlaessig, kein Crash im normalen Flow
- [ ] **Guideline 4.0 (Design)**: Native UI, kein WebView fuer Kern-Features
- [ ] **"Restore Purchases" Button** vorhanden (Pflicht fuer Auto-Renewable Subscriptions)
- [ ] **Login**: App muss auch ohne Login nutzbar sein ODER Login muss klar begruendet sein
  - Entscheidung: Anonymer Modus (Offline-only) ist verfuegbar -> Login nicht erzwungen
- [ ] **Demo-Content**: Reviewer kann die App ohne eigene Daten testen (Demo-Modus oder Testaccount)

**Technische Hinweise**:
- App Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- Haeufigste Ablehnungsgruende: Fehlende Restore-Funktion, unklare Abo-Bedingungen, Crash bei Review
- Tipp: "Notes for Reviewers" Feld nutzen -- Testaccount und Feature-Erklaerung hinterlegen
