# EPIC 02: App Store Metadaten EULA-Link

## Ziel

Apple verlangt einen funktionalen Link zu den Nutzungsbedingungen (Terms of Use / EULA) in den App Store Metadaten. Aktuell fehlt dieser Link. (Guideline 3.1.2(c) - Subscriptions)

## Abhaengigkeiten

- Nutzungsbedingungen muessen unter `https://track.fakturus.com/terms` erreichbar sein (bereits implementiert in P4-E07-S02)

---

## Analyse

### Aktuelle Situation

Die App hat bereits:
- Terms of Use gehostet unter `https://track.fakturus.com/terms`
- Link in der Paywall (`PaywallView.swift:179`)
- Link im Consent-Flow (`ConsentView.swift:92`)
- Privacy Policy Link vermutlich in App Store Connect hinterlegt

**Was fehlt**: Der EULA/Terms-Link in den **App Store Connect Metadaten** selbst:
- Entweder als Custom EULA in App Store Connect hinterlegt
- Oder als Link in der App-Beschreibung erwaehnt

### Apple Anforderung (Guideline 3.1.2(c))

Fuer Apps mit Auto-Renewable Subscriptions muessen folgende Links **in den App Store Metadaten** vorhanden sein:
1. Privacy Policy (im Privacy Policy Feld in ASC) -- vermutlich bereits vorhanden
2. Terms of Use / EULA -- entweder als Custom EULA in ASC ODER als Link in der App-Beschreibung

---

## Stories

### ARV1-E02-S01: EULA in App Store Connect hinterlegen

**Als** Product Owner
**moechte ich** den Terms-of-Use-Link in den App Store Metadaten hinterlegen,
**damit** Apple die Subscription-Anforderungen als erfuellt ansieht.

**Plattform**: App Store Connect (Konfiguration)
**Geschaetzter Aufwand**: XS

**Akzeptanzkriterien**:
- [ ] **Option A (empfohlen)**: Custom EULA in App Store Connect hinterlegt:
  - App Store Connect > App > App Information > License Agreement
  - Custom EULA Text eingefuegt ODER Link zu `https://track.fakturus.com/terms` hinterlegt
- [ ] **Option B (alternativ)**: Link in der App-Beschreibung erwaehnt:
  - Am Ende der App-Beschreibung:
    "Nutzungsbedingungen: https://track.fakturus.com/terms"
    "Datenschutz: https://track.fakturus.com/privacy"
- [ ] Link ist funktional und oeffentlich erreichbar (ohne Login)
- [ ] Link fuehrt zu den deutschsprachigen Nutzungsbedingungen
- [ ] Sichergestellt dass auch der Privacy Policy Link im ASC Privacy Policy Feld steht

**Technische Hinweise**:
- Apple akzeptiert sowohl Custom EULA als auch den Standard Apple EULA -- aber bei Standard-EULA muss trotzdem ein Link in der Beschreibung stehen
- Empfehlung: Beides machen (Custom EULA + Link in Beschreibung) fuer maximale Compliance
- Die Nutzungsbedingungen muessen Abo-spezifische Infos enthalten:
  - Automatische Verlaengerung
  - Kuendigungsbedingungen
  - Preis und Laufzeit
- Sicherstellen dass `https://track.fakturus.com/terms` erreichbar ist (SSL, kein 404)
