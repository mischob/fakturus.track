# Preisanalyse: Zeiterfassungs-Software im DACH-Raum

**Erstellt:** 29. Maerz 2026
**Fokus:** Preisvergleich und Preisempfehlung fuer fakturus.track

---

## 1. Preisvergleich der Wettbewerber

### 1.1 Uebersicht: Preise pro User/Monat

| Anbieter | Free-Tier | Einstiegspreis | Mid-Tier | Premium/Enterprise | Abrechnungsmodell |
|----------|-----------|---------------|----------|-------------------|-------------------|
| **Clockodo** | Ja (1 User) | 5,00 EUR | 5 + 2 EUR Plus | 12,00 EUR | Monatl./Jaehrl. (-5%) |
| **Crewmeister** | Nein (14d Test) | 1,40 EUR | 5,20 EUR | 9,00 EUR | Monatl./Jaehrl. (-30% Aktion) |
| **TimeTac** | Nein (30d Test) | 4,90 EUR | - | - | Jaehrl. (Monatl. +20%) |
| **Papershift** | Nein (14d Test) | 6,00 EUR | Auf Anfrage | Auf Anfrage | Individuell |
| **Timebutler** | Nein (6 Wo Test) | 3,80 EUR* | 1,71 EUR** | 1,39 EUR*** | Monatl. Festpreis |
| **ZEP** | Nein (30d Test) | 2,00 EUR (Clock) | 7,00 EUR (Compact) | 20,00 EUR (Pro) | Quartal |
| **Kimai Cloud** | Nein (30d Test) | 2,99 EUR | 3,99 EUR | - | Monatl./Jaehrl. |
| **Toggl Track** | Ja (5 User) | 9,00 EUR | 18,00 EUR | Auf Anfrage | Monatl./Jaehrl. |
| **Clockify** | Ja (unbegrenzt) | 3,99 USD | 5,49 USD | 11,99 USD | Monatl./Jaehrl. |
| **Harvest** | Nein (Test) | 11,00 USD | - | - | Monatl./Jaehrl. (-20%) |

*Timebutler: 19 EUR/Monat fuer bis 5 User = 3,80 EUR/User
**Timebutler: 59,95 EUR/Monat fuer 6-35 User = ca. 1,71 EUR/User bei 35
***Timebutler: 1,39 EUR pro User ab User 36

### 1.2 Preissegmente

| Segment | Preisspanne | Typische Anbieter | Funktionsumfang |
|---------|------------|-------------------|-----------------|
| **Budget** | 0-3 EUR/User/Monat | Crewmeister Basis, ZEP Clock, Kimai Standard | Reine Zeiterfassung, minimale Extras |
| **Standard** | 3-7 EUR/User/Monat | Clockodo Basic, TimeTac, ZEP Compact, Kimai Pro | Zeiterfassung + Abwesenheit + Projekte |
| **Premium** | 7-12 EUR/User/Monat | Clockodo Enterprise, Crewmeister Premium, Toggl Starter | Vollumfang + Reporting + Integrationen |
| **Enterprise** | 12+ EUR/User/Monat | Toggl Premium, ZEP Professional, Personio | Umfangreiche Features + Support + SLA |

### 1.3 Kostenbeispiele fuer typische Team-Groessen

| Team-Groesse | Crewmeister | Clockodo | TimeTac | ZEP Clock | Kimai Cloud |
|--------------|-------------|----------|---------|-----------|-------------|
| 1 User | 5,20 EUR | 0 EUR (Free) | 4,90 EUR | 2,00 EUR | 2,99 EUR |
| 5 User | 26,00 EUR | 25,00 EUR | 24,50 EUR | 10,00 EUR | 14,95 EUR |
| 10 User | 52,00 EUR | 50,00 EUR | 49,00 EUR | 20,00 EUR | 29,90 EUR |
| 25 User | 130,00 EUR | 125,00 EUR | 122,50 EUR | 50,00 EUR | 74,75 EUR |

---

## 2. Analyse der Preismodelle

### 2.1 Gaengige Preismodelle im Markt

| Modell | Beschreibung | Vorteile | Nachteile | Verwendet von |
|--------|-------------|----------|-----------|---------------|
| **Per User/Monat** | Fester Preis pro Nutzer | Kalkulierbar, skaliert | Kann bei vielen Usern teuer werden | Clockodo, TimeTac, Kimai, Toggl |
| **Festpreis-Staffeln** | Fixpreis fuer User-Bereiche | Guenstig bei voller Auslastung | Sprungkosten bei Ueberschreitung | Timebutler |
| **Modularer Preis** | Basis + optionale Module | Flexibel, zahlt nur was man braucht | Komplex, schwer vergleichbar | ZEP, Personio, Papershift |
| **Freemium** | Basis kostenlos, Premium kostenpflichtig | Niedrige Einstiegshuerde | Konvertierung schwierig | Clockodo, Toggl, Clockify |
| **Pauschal** | Ein Preis fuer alles | Einfach, transparent | Kann zu teuer fuer Wenig-Nutzer sein | - |

### 2.2 Trends bei der Preisgestaltung

1. **Jahreszahlung wird zum Standard**: Fast alle Anbieter incentivieren Jahreszahlung (5-30% Rabatt)
2. **Free-Tier fuer Freelancer**: Clockodo, Toggl und Clockify bieten kostenlose Einstiegsversionen
3. **Mindest-Vertragslaufzeit steigt**: ZEP ist auf Quartalszahlung umgestiegen
4. **Module statt All-inclusive**: Trend zu modularen Preisen (ZEP, Papershift)
5. **Einstiegspreise sinken**: ZEP Clock bei 2 EUR, Crewmeister Basis bei 1,40 EUR -- Preisdruck von unten

---

## 3. Preisempfehlung fuer fakturus.track

### 3.1 Positionierungsentscheidung

**Empfehlung: Budget-bis-Standard-Segment mit Freemium-Einstieg**

Begruendung:
- fakturus.track ist aktuell funktional im Budget-Segment (vergleichbar ZEP Clock, Kimai Standard)
- Offline-First und native Mobile App rechtfertigen einen leichten Aufpreis gegenueber reinen Web-Loesungen
- Freemium senkt die Einstiegshuerde und ermoeglicht organisches Wachstum
- Die gesetzliche Pflicht treibt Nutzer, die bereit sind zu zahlen -- kein Preiswettbewerb nach unten noetig

### 3.2 Empfohlenes Preismodell

```
+------------------------------------------------------------------+
|                  fakturus.track -- Preismodell                    |
+------------------------------------------------------------------+
|                                                                  |
|  FREE            STARTER          PRO              TEAM          |
|  0 EUR           2,99 EUR         4,99 EUR         ab 3,99 EUR  |
|  1 User          /User/Monat      /User/Monat      /User/Monat  |
|                  (jaehrl.)        (jaehrl.)        (jaehrl.,     |
|                                                    ab 5 User)   |
|                                                                  |
|  - Start/Stop    Alles aus Free   Alles aus        Alles aus    |
|  - 1 Geraet        plus:          Starter plus:    Pro plus:    |
|  - 365 Tage      - Unbegr. Hist.  - Projekte       - Admin-     |
|    Historie      - PDF-Export      - DATEV-Export     Dashboard  |
|  - Ueberstunden  - Kalender-      - Mehrere        - Rollen &   |
|  - Feiertage       Import           Geraete          Rechte     |
|  - Offline       - Schulferien    - Prioritaets-   - Genehmi-   |
|  - Mobile App    - Excel-Export     Support          gungs-WF   |
|                  - Pausen                          - Team-       |
|                  - Urlaub                            Reports     |
|                                                                  |
+------------------------------------------------------------------+
|  Monatliche Zahlung: +25% (3,99 / 6,49 / 4,99 EUR)             |
|  Jaehrliche Zahlung: Wie oben angegeben                          |
+------------------------------------------------------------------+
```

### 3.3 Begruendung der Preispunkte

| Tier | Preis | Benchmark | Begr. |
|------|-------|-----------|-------|
| **Free** | 0 EUR (1 User) | Clockodo Free, Toggl Free | Fuehrt Freelancer an das Produkt heran, Mundpropaganda |
| **Starter** | 2,99 EUR/User/Monat | Kimai Standard (2,99), ZEP Clock (2,00), Crewmeister Basis (1,40) | Leicht ueber dem Tiefstpreis, gerechtfertigt durch Offline + Native App |
| **Pro** | 4,99 EUR/User/Monat | Clockodo Basic (5,00), TimeTac (4,90), Kimai Pro (3,99) | Vergleichbar mit Clockodo/TimeTac, aber mit Offline-USP |
| **Team** | 3,99 EUR/User/Monat (ab 5 User) | Crewmeister Standard (5,20), Clockodo (5,00) | Volumenrabatt ab 5 User, konkurrenzfaehig im Team-Segment |

### 3.4 Umsatzprojektionen (konservativ)

**Annahmen:**
- Launch Q3 2026
- Organisches Wachstum durch SEO/Content und App Store
- Durchschnittlicher Umsatz pro zahlender User: 3,50 EUR/Monat (Mix aus Tiers)
- Conversion Rate Free -> Paid: 5-8%

| Zeitraum | Free User | Paid User | MRR (EUR) | ARR (EUR) |
|----------|-----------|-----------|-----------|-----------|
| Monat 3 | 200 | 15 | 53 | 630 |
| Monat 6 | 600 | 45 | 158 | 1.890 |
| Monat 12 | 1.500 | 120 | 420 | 5.040 |
| Monat 18 | 3.000 | 300 | 1.050 | 12.600 |
| Monat 24 | 5.000 | 500 | 1.750 | 21.000 |

**Optimistisches Szenario** (staerkeres Marketing, Partnerschaften):
- Monat 24: 1.500 Paid User, MRR 5.250 EUR, ARR 63.000 EUR

### 3.5 Monetarisierungs-Strategie

#### Phase 1: Nutzer gewinnen (0-6 Monate)
- Free-Tier grosszuegig gestalten (365 Tage Historie, Offline, Mobile)
- Kein Druck zum Upgrade -- Qualitaet des Produkts soll ueberzeugen
- Fokus auf App Store Reviews und organische Sichtbarkeit

#### Phase 2: Konvertieren (6-12 Monate)
- Export-Features (PDF, Excel, DATEV) als Paid-Trigger
- Projekt-Zuordnung als Pro-Feature
- Team-Features ab 2+ User als natuerlicher Upgrade-Pfad

#### Phase 3: Expandieren (12-24 Monate)
- Jeder Free User, der auf Starter upgradet, bringt mind. 36 EUR/Jahr
- Team-Paket fuer KMU mit 10-25 Mitarbeitern (Zielwert: 50-100 EUR/Monat pro Unternehmen)
- Steuerberater-Partner-Programm (DATEV-Integration als Tueroeffner)

---

## 4. Preispolitik -- Empfehlungen

### 4.1 Dos

- **Transparente Preise auf der Website**: Keine "Preis auf Anfrage"-Strategie (anders als Personio/Papershift) -- KMU und Freelancer wollen sofort sehen, was es kostet
- **Jaehrliche Zahlung incentivieren**: 25% guenstiger bei Jahreszahlung (Standard im Markt)
- **Kostenloser Testzeitraum fuer Paid-Tiers**: 14 Tage Pro-Trial kostenlos
- **Einfache Preisstruktur**: Maximal 3-4 Tiers, kein modulares Chaos
- **EUR-Preise fuer DACH**: Keine USD-Preise im DACH-Markt

### 4.2 Don'ts

- **Nicht auf 0 EUR konkurrieren**: Clockify bietet kostenlos fuer unbegrenzte User -- dieses Rennen ist nicht zu gewinnen
- **Keine versteckten Kosten**: Keine Setup-Gebuehren, keine Mindestvertragslaufzeiten initial
- **Nicht zu guenstig starten**: Unter 2 EUR/User signalisiert "minderwertig" und laesst keinen Spielraum fuer Support-Kosten
- **Keine Enterprise-Pricing-Strategie**: Passt nicht zur Zielgruppe (Freelancer/Kleinst-KMU)

---

## 5. Break-Even-Analyse

### Kostenstruktur (geschaetzt, Cloud-basiert)

| Kostenposition | Monatlich | Details |
|---------------|-----------|---------|
| Cloud-Hosting (Hetzner) | 30-80 EUR | PostgreSQL + App Server |
| Azure AD B2C | 0-50 EUR | Erste 50.000 Auth kostenlos |
| Domain & SSL | 5 EUR | - |
| App Store Gebuehren | 8-25 EUR | Apple Developer (99 USD/Jahr), Google (25 USD einmalig) |
| **Gesamt Fixkosten** | **ca. 50-160 EUR/Monat** | |

### Break-Even bei verschiedenen Szenarien

| Szenario | Fixkosten/Monat | Durchschn. Umsatz/User | Break-Even User |
|----------|----------------|----------------------|-----------------|
| Minimal (nur Hosting) | 50 EUR | 3,50 EUR | 15 zahlende User |
| Standard | 100 EUR | 3,50 EUR | 29 zahlende User |
| Mit Marketing | 300 EUR | 3,50 EUR | 86 zahlende User |

**Fazit:** Der Break-Even ist bereits mit 15-30 zahlenden Nutzern erreichbar -- ein realistisches Ziel innerhalb der ersten 3-6 Monate nach Launch.

---

## Quellen

- [Clockodo Preise](https://www.taxandbytes.de/tools/zeiterfassung/clockodo/preise)
- [Crewmeister Preise](https://crewmeister.com/de/preise)
- [TimeTac Pricing](https://www.timetac.com/en/pricing/)
- [Timebutler Pricing](https://timebutler.com/prices/)
- [ZEP Preise](https://www.zep.de/en/prices)
- [Kimai Pricing](https://www.kimai.org/en/pricing)
- [Toggl Track Pricing - OMR Reviews](https://omr.com/en/reviews/product/toggl-track/pricing)
- [Clockify Pricing](https://clockify.me/pricing)
- [Harvest Pricing](https://www.getharvest.com/pricing)
- [Papershift Erfahrungen - trusted.de](https://trusted.de/papershift)
