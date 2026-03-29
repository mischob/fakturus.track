---
name: Phase 2 Detailplanung abgeschlossen
description: Phase 2 Stories und EPICs sind vollstaendig spezifiziert -- 6 EPICs, 47 Stories, 6 Execution-Wellen fuer 8.5 Wochen
type: project
---

Phase 2 Detailplanung wurde am 2026-03-29 erstellt.

**Scope**: 6 EPICs (Backend SickDay, Settings, Urlaub-Kalender, Krankheitstage, Gesamt-Tab, Export), 47 Stories, 8.5 Wochen geplant.

**Why:** Phase 2 liefert Feature-Paritaet mit dem Web-Frontend und schliesst die von der Marktanalyse identifizierten Luecken (Export, Krankheitstage). Ohne Phase 2 ist die App nicht store-ready.

**How to apply:** Kritischer Pfad ist 6 Wochen (E02 Settings -> E05 Gesamt -> E06 Export). Der Kalender (E03) ist die komplexeste Einzelkomponente und groesstes Risiko. Maximale Parallelitaet: 3 Straenge gleichzeitig (Backend + iOS + Android), verschiedene Tabs parallel.

**Key decisions:**
- SickDay als separate Entity (kein AbsenceDay mit Typ-Feld) -- YAGNI, kein Breaking Change
- Client-seitige PDF-Generierung (nicht Backend) -- passt zu Offline-First
- Long-Press fuer Krankheitstage-Kontext-Menue (Tap bleibt Urlaub fuer Rueckwaertskompatibilitaet)
- CSV mit Semikolon + Komma-Dezimal (deutscher Excel-Standard)
- Eine zentrale HolidayCalculator-Klasse fuer alle Feiertag-Berechnungen
- Bayern Mariae Himmelfahrt vereinfacht: gilt fuer ganz Bayern (nicht nur katholische Gemeinden)
