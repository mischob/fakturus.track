# EPIC 07: Privacy Policy & Rechtliches

## Ziel

Alle rechtlich erforderlichen Dokumente (Datenschutzerklaerung, Nutzungsbedingungen, Impressum) sind erstellt, gehostet und in der App verlinkt. Voraussetzung fuer beide Store-Einreichungen.

## Abhaengigkeiten

- **Keine technischen Abhaengigkeiten**: Kann sofort gestartet werden
- Inhaltlich: Feature-Liste muss final sein (welche Daten werden erhoben)

---

## Stories

### P4-E07-S01: Datenschutzerklaerung (Privacy Policy)

**Als** Nutzer
**moechte ich** eine transparente Datenschutzerklaerung lesen koennen,
**damit** ich weiss welche Daten erhoben werden und wie sie verarbeitet werden.

**Plattform**: Web (gehostet unter track.fakturus.com/privacy)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Datenschutzerklaerung auf Deutsch (primaer) und Englisch verfuegbar
- [ ] Gehostet unter `https://track.fakturus.com/privacy` (oder aehnliche URL)
- [ ] DSGVO-konformer Inhalt:
  - Verantwortlicher (Name, Adresse, Kontakt)
  - Welche Daten werden erhoben (Name, E-Mail, Arbeitszeiten, Pausen, Urlaub, Krankheitstage)
  - Zweck der Datenverarbeitung (Zeiterfassung, Synchronisation)
  - Rechtsgrundlage (Art. 6 Abs. 1 lit. b DSGVO -- Vertragsdurchfuehrung)
  - Speicherdauer (solange Konto aktiv + 30 Tage nach Loeschung)
  - Datenempfaenger (Azure Germany fuer Hosting, Azure B2C fuer Auth, kein Drittland-Transfer ausserhalb EU)
  - Betroffenenrechte (Auskunft, Berichtigung, Loeschung, Widerspruch, Datenportabilitaet)
  - Keine Tracking-SDKs, keine Werbung, keine Datenweitergabe
  - Crashlytics/Sentry: Falls verwendet, Opt-In-Moeglichkeit dokumentieren
  - In-App-Purchase: Zahlungsdaten werden von Apple/Google verarbeitet, nicht von uns
- [ ] URL funktioniert und ist oeffentlich erreichbar (ohne Login)
- [ ] URL in App verlinkt (Einstellungen > Rechtliches > Datenschutz)

**Technische Hinweise**:
- Statische HTML-Seite oder Markdown -> HTML auf bestehender fakturus.com Infrastruktur
- DSGVO-Generator als Startpunkt, dann manuell anpassen
- Apple und Google verlangen oeffentlich erreichbare URL (nicht in-App-only)

---

### P4-E07-S02: Nutzungsbedingungen (Terms of Use)

**Als** Nutzer
**moechte ich** die Nutzungsbedingungen einsehen koennen,
**damit** ich weiss unter welchen Bedingungen ich die App nutze.

**Plattform**: Web (gehostet unter track.fakturus.com/terms)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Nutzungsbedingungen auf Deutsch und Englisch
- [ ] Gehostet unter `https://track.fakturus.com/terms`
- [ ] Inhalt:
  - Geltungsbereich (App und zugehoerige Dienste)
  - Abo-Bedingungen (Verlaengerung, Kuendigung, Erstattung via Apple/Google)
  - Haftungsausschluss (keine Gewaehr fuer Steuerberatung, DATEV-Export ist Beta)
  - Nutzungsrechte und -pflichten
  - Konto-Loeschung
  - Aenderungsvorbehalt
  - Anwendbares Recht (deutsches Recht) und Gerichtsstand
- [ ] Verlinkt in der App (Einstellungen > Rechtliches > Nutzungsbedingungen)
- [ ] Verlinkt in der Paywall (Apple/Google Pflicht bei Subscriptions)

**Technische Hinweise**:
- Apple Review Guidelines 3.1.2: "clearly identify" subscription terms
- Nutzungsbedingungen und Datenschutz muessen BEIDE in der Paywall verlinkt sein

---

### P4-E07-S03: Impressum & App-Info

**Als** Nutzer in Deutschland
**moechte ich** ein Impressum und App-Informationen sehen koennen,
**damit** die App den deutschen Telemedien-Anforderungen entspricht.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: Phase 3 E10 (App-Einstellungen Screen vorhanden)
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] In den App-Einstellungen unter "Rechtliches":
  - Datenschutzerklaerung (oeffnet Safari/Chrome mit Privacy Policy URL)
  - Nutzungsbedingungen (oeffnet Browser mit Terms URL)
  - Impressum (In-App oder Browser)
  - Open-Source-Lizenzen (falls relevante OSS-Bibliotheken genutzt)
- [ ] Impressum enthaelt (TMG-Pflicht):
  - Name und Anschrift des Anbieters
  - Kontakt (E-Mail)
  - USt-IdNr. (falls vorhanden)
- [ ] Given ein Nutzer oeffnet Einstellungen > Rechtliches > Datenschutz
  Then oeffnet sich der Systembrowser mit der Privacy Policy URL

**Technische Hinweise**:
- iOS: `Link(destination: URL)` oder `UIApplication.shared.open(url)`
- Android: `Intent(Intent.ACTION_VIEW, Uri.parse(url))`
- Open-Source-Lizenzen: iOS hat `Settings.bundle` mit Acknowledgements, Android hat `oss-licenses-plugin`
