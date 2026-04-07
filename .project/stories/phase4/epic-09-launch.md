# EPIC 09: Launch-Submission & Go-Live

## Ziel

Erfolgreiche Einreichung und Veroeffentlichung der App in Apple App Store und Google Play Store. Koordinierter Launch-Zeitpunkt fuer beide Plattformen.

## Abhaengigkeiten

- **E05 (iOS Store)**: Screenshots, Beschreibung, Review Compliance
- **E06 (Play Store)**: Screenshots, Listing, Data Safety
- **E07 (Privacy/Legal)**: Privacy Policy und Terms of Use URL aktiv
- **E08 (Testing)**: Alle Tests bestanden, Beta-Feedback eingearbeitet

---

## Stories

### P4-E09-S01: iOS App Store Submission

**Als** Product Owner
**moechte ich** die App im Apple App Store einreichen,
**damit** sie nach dem Review-Prozess veroeffentlicht werden kann.

**Plattform**: iOS
**Abhaengigkeiten**: E05, E07, E08
**Parallelisierbar mit**: P4-E09-S02 (Android Submission)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Release-Build mit korrekter Version (1.0.0) und Build-Nummer erstellt
- [ ] Build ueber Xcode oder Transporter an App Store Connect hochgeladen
- [ ] App Store Connect Einreichung:
  - Alle Screenshots hochgeladen (DE + EN)
  - Beschreibung, Keywords, Kategorie ausgefuellt
  - Privacy Policy URL hinterlegt
  - App Privacy Details ("Nutrition Labels") ausgefuellt
  - Preismodell: Kostenlos (mit In-App Kaeufen)
  - Review Notes: Testaccount + Feature-Erklaerung + Hinweis auf Subscription
  - Release-Modus: "Manuell freigeben" (fuer koordinierten Launch)
- [ ] Build zur Review eingereicht
- [ ] Given der Build wird eingereicht
  Then wird der Status "Waiting for Review" angezeigt
  And wir werden bei Status-Aenderung benachrichtigt

**Technische Hinweise**:
- Apple Review dauert typischerweise 24-48 Stunden
- Falls Ablehnung: Rejection Reason lesen, beheben, erneut einreichen
- "Manuell freigeben" waehlen, damit wir iOS + Android gleichzeitig launchen koennen
- Tipp: Freitags NICHT einreichen (Weekend-Reviews sind unzuverlaessiger)

---

### P4-E09-S02: Google Play Store Submission

**Als** Product Owner
**moechte ich** die App im Google Play Store einreichen,
**damit** sie fuer Android-Nutzer verfuegbar wird.

**Plattform**: Android
**Abhaengigkeiten**: E06, E07, E08
**Parallelisierbar mit**: P4-E09-S01 (iOS Submission)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Release-Build (signed APK/AAB) mit korrekter Version erstellt
- [ ] AAB (Android App Bundle) in Google Play Console hochgeladen
- [ ] Production Track Einreichung:
  - Store Listing komplett (DE + EN)
  - Screenshots hochgeladen
  - Data Safety Section ausgefuellt
  - Content Rating ausgefuellt
  - Preis: Kostenlos (mit In-App Kaeufen)
  - Release Notes (DE + EN)
  - "Timed Publishing" aktiviert (fuer koordinierten Launch)
- [ ] Review eingereicht
- [ ] Given die App wird eingereicht
  Then durchlaeuft sie den Google Review-Prozess

**Technische Hinweise**:
- Google Play Review dauert typischerweise wenige Stunden bis 7 Tage (Ersteinreichung laenger)
- AAB statt APK (Pflicht seit 2021)
- "Timed Publishing" oder "Managed Publishing" fuer koordinierten Launch
- Signing Key: Google Play App Signing nutzen (Key wird von Google verwaltet)

---

### P4-E09-S03: Koordinierter Launch

**Als** Product Owner
**moechte ich** beide Apps gleichzeitig veroeffentlichen,
**damit** wir eine einheitliche Launch-Kommunikation fahren koennen.

**Plattform**: Beide
**Abhaengigkeiten**: P4-E09-S01 (iOS Review bestanden), P4-E09-S02 (Android Review bestanden)
**Parallelisierbar mit**: Keine
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Beide Apps haben den Review-Prozess bestanden
- [ ] Launch-Datum festgelegt (idealerweise Dienstag oder Mittwoch)
- [ ] iOS: "Developer Release" in App Store Connect druecken
- [ ] Android: "Go Live" in Google Play Console
- [ ] Beide Apps innerhalb von max. 2 Stunden oeffentlich verfuegbar
- [ ] App Store / Play Store URLs funktionieren und sind erreichbar
- [ ] Download + Installation auf echtem Geraet verifiziert (nicht nur Emulator)
- [ ] Given beide Apps sind approved
  When der Launch-Termin erreicht ist
  Then werden beide Apps gleichzeitig freigeschaltet

**Technische Hinweise**:
- iOS: App Store propagation dauert ~1 Stunde nach Release
- Android: Play Store propagation dauert wenige Minuten bis Stunden
- Backup-Plan: Falls ein Store ablehnt, den anderen nicht alleine launchen (einheitliche Kommunikation)
