# Marketing-Integration Changelog

**Erstellt:** 29. Maerz 2026
**Basis:** Marktanalyse, Preisanalyse und Feature-Vergleichsmatrix aus `/marketing/`
**Ziel:** Marketing-Erkenntnisse in die bestehende technische Planung einarbeiten

---

## Zusammenfassung

Die Marktanalyse hat drei kritische Luecken identifiziert, die vor dem Store-Launch geschlossen werden muessen:

1. **Pausenerfassung** -- gesetzliche Pflicht (ArbZG), 7/11 Wettbewerber bieten es
2. **PDF/Excel-Export** -- Basisanforderung, 10/11 Wettbewerber bieten es
3. **Krankheitstage** -- erwartete Basisfunktion, 6/11 Wettbewerber bieten es

Zusaetzlich liefert die Marktanalyse eine klare Positionierung, ein Preismodell und Umsatzprojektionen, die in die Planung eingeflossen sind.

**Auswirkung auf die Timeline:** +3 Wochen (von 22 auf 25 Wochen), Store-Launch verschiebt sich von Ende August auf Anfang Oktober 2026. Aufschluesselung: +1.5 Wochen Pausen (Phase 1), +1.5 Wochen Export/Krankheitstage (Phase 2), +1 Woche Feature-Gating (Phase 4). AI-gestuetzte Entwicklung kompensiert teilweise.

---

## Geaenderte Dokumente

### 1. features.md -- Feature-Liste

**Was geaendert:**
- Freemium-Tier-Markierungen ([FREE], [STARTER], [PRO], [TEAM]) ergaenzt mit Verweis auf Preismodell
- **Neuer Abschnitt 2.2b: Pausenerfassung** -- als P1 Must-Have in Phase 1 aufgenommen
  - Pause-Button waehrend laufender Session
  - Manuelle Pauseneingabe bei nachtraeglicher Erfassung
  - Nettoarbeitszeit-Berechnung
  - ArbZG-Pausenhinweise (6h/9h)
  - Backend-Aenderung erforderlich (PauseMinutes-Feld)
- **Neuer Abschnitt 5b: Reporting & Export**
  - 5b.1: PDF-Monatsreport (P2, STARTER)
  - 5b.2: Excel/CSV-Export (P2, STARTER)
  - 5b.3: DATEV-Export (P3, PRO)
- **Neuer Abschnitt 5c: Krankheitstage** (P2, STARTER)
- **MoSCoW-Matrix aktualisiert:**
  - Must Have: Pausenerfassung hinzugefuegt
  - Should Have: PDF-Report, CSV-Export, Krankheitstage hinzugefuegt
  - Could Have: DATEV-Export, einfache Projektzuordnung, Audit-Log hinzugefuegt
  - Won't Have: Schichtplanung und Rechnungsstellung explizit aufgenommen

**Begruendung:** Die Marktanalyse zeigt, dass Pausenerfassung gesetzliche Pflicht ist und PDF-Export eine Grunderwartung. Ohne diese Features waere fakturus.track nicht wettbewerbsfaehig und nicht gesetzeskonform.

---

### 2. roadmap.md -- Zeitplan

**Was geaendert:**
- **Phase 1 erweitert:** Von 8 auf 10.5 Wochen (+1.5 Wochen fuer Pausenerfassung)
  - Neuer Meilenstein 1.5: Pausenerfassung (Woche 9-10.5)
  - Backend-Aenderung, UI-Integration, ArbZG-Hinweise
- **Phase 2 erweitert:** Von 6 auf 8.5 Wochen (+1.5 Wochen fuer Export + Krankheitstage)
  - Meilenstein 2.2 um Krankheitstage erweitert (Kalender, Sync, Backend-Entity)
  - Meilenstein 2.3 um PDF/CSV-Export erweitert
  - Phase 2 Abschlusskriterien ergaenzt (Export, Krankheitstage)
- **Phase 4 erweitert:** Von 4 auf 5 Wochen (+1 Woche fuer Feature-Gating)
  - Neuer Meilenstein 4.0: Feature-Gating Implementation (In-App-Purchase / Subscription)
- **Alle Meilenstein-Daten um 3 Wochen verschoben:**
  - Phase 3 Start: Mitte August statt Juli
  - Phase 4 Launch: 04. Oktober statt 23. August
  - MAUI-Abloesung: 11. Oktober statt 30. August
- **Marktanalyse-Kontext ergaenzt:** Hinweis auf Zeiterfassungspflicht 2026 und Uebergangsfristen
- **Marktanalyse-basierte Erfolgsmetriken hinzugefuegt:** Free User, Paid User, MRR, Break-Even

**Begruendung:** Die +3 Wochen sind ein akzeptabler Trade-off fuer gesetzeskonforme und wettbewerbsfaehige Features. AI-gestuetzte Entwicklung kompensiert teilweise, aber Pausen und Krankheitstage erfordern substantielle Backend-Aenderungen. Der Launch-Termin Anfang Oktober 2026 liegt immer noch frueh im Zeitfenster der Zeiterfassungspflicht.

---

### 3. design-system.md -- Design-System

**Was geaendert:**
- **Neuer Abschnitt "Markenpositionierung"** am Dokumentanfang hinzugefuegt
  - Tagline: "Arbeitszeit erfassen. Einfach. Ueberall."
  - Value Proposition aus Marktanalyse
  - Kern-Differenzierung (Offline-First, Sofort startklar, Deutschland-optimiert, Native)
  - Primaere Zielgruppe definiert
- **Zwei neue Spezialfarben:**
  - `sick-day` (#EF4444) -- fuer Krankheitstage im Kalender
  - `pause` (#F59E0B) -- fuer Pausenindikator in Timer und History

**Begruendung:** Die Markenpositionierung aus der Marktanalyse gibt dem Design eine klare Richtung. Sie bestaetigt die bestehende Design-Philosophie ("Werkzeug, nicht Social Network") und macht die Zielgruppe explizit. Die neuen Farben sind fuer die neuen Features (Krankheitstage, Pausenerfassung) notwendig.

---

### 4. screens.md -- Bildschirm-Wireframes

**Was geaendert:**
- **Login Screen:** Tagline aktualisiert auf "Arbeitszeit erfassen. Einfach. Ueberall."
- **Zeiten Screen (laufende Session):**
  - Pause-Button hinzugefuegt ([Pause] [Stop] [Fertig] statt [Stop] [Fertig])
  - Pausenanzeige in der ActiveSessionCard ("Pause: 30 min")
  - History-Zeilen zeigen Pausendauer ("P30") und Nettodauer
- **Urlaub Screen:**
  - Kalender-Legende um "Krank" erweitert (roter Punkt)
- **Session Detail Sheet:**
  - Neues Feld "Pause (Minuten)" hinzugefuegt
  - Dauer aufgesplittet in "Brutto" und "Netto"
- **Einstellungen Screen:**
  - Neue Sektion "EXPORT" mit "PDF-Monatsreport" und "CSV-Export"

**Begruendung:** Die Wireframes muessen die neuen Features (Pause, Krankheitstage, Export) abbilden, damit Entwickler eine klare visuelle Spezifikation haben.

---

### 5. ux-flows.md -- UX-Flows

**Was geaendert:**
- **Neuer Flow 2b: Pause erfassen**
  - Echtzeit-Pause waehrend laufender Session (Tap Pause -> Tap Weiter)
  - Manuelle Pauseneingabe bei nachtraeglicher Erfassung
  - ArbZG-Pausenhinweis bei 6h (30min) und 9h (45min)
- **Neuer Flow 8b: Monatsreport exportieren**
  - PDF-Monatsreport mit Vorschau und Share-Sheet
  - CSV-Export mit Zeitraumauswahl
- **Neuer Flow 8c: Krankheitstage erfassen**
  - Long-Press auf Arbeitstag fuer Kontext-Menue (Urlaub/Krank)
  - Design-Entscheidung dokumentiert (Long-Press statt zyklisches Tippen)

**Begruendung:** Neue Features benoetigen klare Benutzerszenarien. Die Flows definieren das erwartete Verhalten und helfen bei der Implementierung.

---

### 6. components.md -- UI-Komponenten

**Was geaendert:**
- **ActiveSessionCard:** Neuer "Paused"-Zustand mit gelbem Indikator, Pause- und Weiter-Buttons, onPause/onResume Callbacks
- **SessionRow:** Layout um Pausenanzeige ("P30") erweitert, Trailing zeigt jetzt Nettodauer
- **VacationCalendar:** Neuer Tages-Typ "Krankheitstag" (roter Hintergrund-Kreis), neues Prop `sickDays` und `onSetSickDay`
- **SessionDetailSheet:** Neues Feld "Pause (Min)", Dauer aufgesplittet in Brutto/Netto

**Begruendung:** Die Komponentenspezifikationen muessen die neuen Features abbilden, damit iOS- und Android-Entwickler identische Funktionalitaet implementieren.

---

## Was NICHT geaendert wurde (und warum)

### Technische Architektur
Die Marketing-Erkenntnisse erfordern keine Architektur-Aenderungen. Die bestehende 2-Schichten-Architektur, SyncEngine und Offline-First-Strategie bleiben unveraendert. Pausenerfassung erfordert lediglich eine DTO-Erweiterung (PauseMinutes-Feld).

### Auth-Konzept
Keine Aenderung -- die Freemium-Preisstruktur wird ueber Backend-Logik oder einen separaten Subscription-Service gehandhabt, nicht ueber die Authentifizierung.

### Migrations-Strategie
Keine Aenderung -- die MAUI-Migration ist unabhaengig von den Marketing-Erkenntnissen.

### iOS/Android-spezifische Plaene
Keine direkten Aenderungen an ios-plan.md und android-plan.md. Die neuen Features (Pause, Export, Krankheitstage) ergeben sich aus features.md und roadmap.md. Die plattformspezifischen Technologieentscheidungen bleiben bestehen.

### Devils Advocate Review Befunde
Die Befunde aus review.md bleiben unveraendert bestehen und muessen weiterhin vor Implementierungsstart adressiert werden. Die Marketing-Integration aendert nichts an den identifizierten technischen Inkonsistenzen.

---

## Offene Fragen / Entscheidungen

### 1. Pausenerfassung: Backend-Design
- **Option A:** `PauseMinutes: int` als einfaches Feld auf WorkSession
- **Option B:** Separate `PauseEntry`-Entity (Start/Ende pro Pause, ermoeglicht mehrere Pausen)
- **Empfehlung:** Option A fuer Phase 1 (einfacher, schneller), spaeter Migration zu Option B falls noetig

### 2. Krankheitstage: Backend-Design
- **Option A:** Neue `SickDay`-Entity (analog VacationDay)
- **Option B:** `AbsenceDay`-Entity mit Typ-Feld (Urlaub/Krank/Sonstig)
- ~~**Empfehlung:** Option B -- zukunftssicherer, ermoeglicht spaeter weitere Abwesenheitstypen (Bildungsurlaub, Sonderurlaub)~~
- **Entscheidung: Option A (separate SickDay-Entity)**
  - Begruendung: Kein Breaking Change an der bestehenden VacationDay-Entity/API. YAGNI -- weitere Abwesenheitstypen (Bildungsurlaub etc.) sind aktuell nicht geplant und koennen spaeter bei Bedarf ergaenzt werden. Separate Entity haelt Sync-Logik einfach und parallel zu VacationDay.

### 3. Freemium Feature-Gating: Implementierung
- Die Preisstruktur aus der Preisanalyse definiert 4 Tiers (FREE/STARTER/PRO/TEAM)
- Feature-Gating muss technisch implementiert werden (Backend-Check oder In-App-Purchase)
- **Entscheidung ausstehend:** Zahlungsanbieter (Apple/Google IAP vs. Stripe vs. eigener Billing-Service)
- **Empfehlung:** Fuer Phase 1-2 keine Feature-Gating-Implementierung. Alle Features verfuegbar. Preismodell ab Phase 4 (Launch) implementieren.

### 4. PDF-Report: Generierung
- **Option A:** Client-seitig in der App generieren (PDFKit iOS / Android PDF API)
- **Option B:** Server-seitig generieren und als Download bereitstellen
- **Empfehlung:** Option A -- passt zum Offline-First-Ansatz, keine Backend-Aenderung noetig

---

## Marketing-Dokumente Referenz

| Dokument | Kern-Erkenntnis |
|----------|----------------|
| marktanalyse.md | Positionierung, SWOT, Wettbewerbsanalyse, Feature-Empfehlungen |
| preisanalyse.md | Freemium-Preismodell (FREE/STARTER/PRO/TEAM), Umsatzprojektionen |
| feature-matrix.md | Detaillierter Feature-Vergleich mit 11 Wettbewerbern, Luecken-Analyse |
