# Tech-Spec: EPIC 09 -- Launch-Submission & Go-Live

## Uebersicht

Kein App-Code. Reine Store-Submission und koordinierter Launch.

---

## iOS App Store Submission

### Build vorbereiten

```bash
# Version setzen
# Xcode: Target -> General -> Version: 1.0.0, Build: 1

# Archive erstellen
# Xcode: Product -> Archive

# Oder via CLI:
xcodebuild archive \
  -scheme FakturusTrack \
  -archivePath build/FakturusTrack.xcarchive \
  -destination "generic/platform=iOS"

# Upload via Transporter App oder Xcode Organizer
```

### App Store Connect Checkliste

- [ ] Build hochgeladen und verarbeitet
- [ ] Version 1.0.0 angelegt
- [ ] Screenshots: DE + EN, alle Geraeteklassen
- [ ] Beschreibung: DE + EN
- [ ] Keywords: DE + EN
- [ ] Kategorie: Business / Productivity
- [ ] Altersfreigabe: 4+
- [ ] Preismodell: Kostenlos mit In-App Kaeufen
- [ ] Privacy Policy URL
- [ ] App Privacy Details ("Nutrition Labels")
- [ ] In-App Purchases: starter_monthly + pro_monthly verknuepft
- [ ] Review Information: Testaccount + Anleitung
- [ ] Release-Modus: **Manuell freigeben** (fuer koordinierten Launch)
- [ ] Einreichung: **NICHT an einem Freitag**

### Typische Review-Dauer

- Ersteinreichung: 24-48 Stunden (manchmal bis 7 Tage)
- Bei Ablehnung: Fix + erneute Einreichung (weitere 24-48h)
- Expedited Review: Nur fuer kritische Bugs nach Launch

---

## Google Play Submission

### Build vorbereiten

```bash
# Version setzen
# build.gradle.kts: versionCode = 1, versionName = "1.0.0"

# Release AAB erstellen
./gradlew bundleRelease

# Ergebnis: app/build/outputs/bundle/release/app-release.aab
```

### Play Console Checkliste

- [ ] AAB hochgeladen (Production Track)
- [ ] Store Listing: DE + EN
- [ ] Screenshots: Phone (+ optional Tablet)
- [ ] Feature Graphic (1024x500)
- [ ] Kurzbeschreibung + Vollstaendige Beschreibung
- [ ] Kategorie: Business
- [ ] Data Safety Section vollstaendig
- [ ] Content Rating (IARC)
- [ ] Target Audience: 18+
- [ ] Ads Declaration: Keine Werbung
- [ ] Privacy Policy URL
- [ ] In-App Products: starter_monthly + pro_monthly aktiv
- [ ] Managed Publishing: **Aktiviert** (fuer koordinierten Launch)
- [ ] Release Notes: DE + EN

### Typische Review-Dauer

- Ersteinreichung: Wenige Stunden bis 7 Tage
- Meist schneller als Apple (< 24h)

---

## Koordinierter Launch

### Ablauf

1. Beide Apps zur Review einreichen (parallel, gleicher Tag)
2. Warten bis BEIDE approved sind
3. Launch-Datum festlegen: **Dienstag oder Mittwoch** (beste Store-Sichtbarkeit)
4. Am Launch-Tag:
   - iOS: "Developer Release" in App Store Connect druecken
   - Android: "Go Live" in Play Console
   - Propagation abwarten (~1-2 Stunden)
   - Download auf echtem Geraet testen

### Timing

```
Tag X-7:  Beide Apps einreichen
Tag X-3:  Erwartete Approval (Puffer fuer Verzoegerung)
Tag X:    Koordinierter Launch (Di/Mi)
Tag X+1:  Monitoring (Crashes, Reviews, Downloads)
Tag X+7:  Erste Auswertung
```

### Backup-Plan

Falls ein Store ablehnt:
- Den anderen Store NICHT alleine launchen
- Fix implementieren, erneut einreichen
- Beide zusammen launchen (einheitliche Kommunikation)

Ausnahme: Wenn Apple ablehnt und Google bereits approved ist, kann der Google-Launch bis zu 5 Tage verzoegert werden (Managed Publishing haelt den Build).

---

## Post-Launch Monitoring (erste 24h)

| Was pruefen | Tool | Schwellwert |
|-------------|------|-------------|
| Crash-Free Rate | Sentry Dashboard | >= 99.5% |
| Download-Zahlen | App Store Connect / Play Console | > 0 (funktioniert) |
| IAP-Funktionalitaet | Testkauf mit echtem Geld | Kauf + Erstattung |
| Bewertungen | Store Dashboards | Keine 1-Stern Reviews |
| Backend-Stabilitaet | Azure Dashboard | HTTP 500 Rate < 0.1% |
| Sync-Erfolgsrate | Backend Logs | >= 99% |

---

## Rollback-Optionen

### Stufe 1: Hotfix (< 24h)

- Bug identifizieren, Fix implementieren
- iOS: Neuen Build einreichen, ggf. Expedited Review
- Android: Staged Rollout auf 10% zuruecksetzen

### Stufe 2: Feature-Disable (< 1h)

- Wenn ein spezifisches Feature Crashes verursacht:
- Feature-Gate temporaer hochsetzen (z.B. auf `Tier.pro + 1` = niemand hat Zugriff)
- Oder: Compile-Time Flag + Hotfix-Build

### Stufe 3: Rollback (letztes Mittel)

- iOS: Vorherigen Build in App Store Connect aktivieren
- Android: Staged Rollout auf 0%, vorherigen Build promoten
- Vorherige App-Version als Fallback verfuegbar
