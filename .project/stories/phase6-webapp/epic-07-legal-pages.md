# EPIC 07: Legal Pages

## Ziel

Oeffentlich zugaengliche Legal Pages (/privacy, /terms, /imprint) als echte Blazor-Seiten. Diese Seiten muessen ohne Login erreichbar sein und sind sowohl fuer SEO als auch fuer App-Store-Compliance relevant.

---

## Stories

### S01: Legal Page Layout
**Als** Besucher **moechte ich** die Legal Pages in einem lesbaren Layout sehen, **damit** ich die rechtlichen Informationen gut lesen kann.

**Akzeptanzkriterien:**
- [ ] Eigenes Layout (NICHT das Sidebar-Layout der authentifizierten App)
- [ ] Header: Fakturus Logo (links) + "Anmelden" / "Starten"-Button (rechts)
- [ ] Content: Zentriert, max-width 800px
- [ ] Typografie: 18px Font, 1.8 Line-Height (optimiert fuer Lesbarkeit)
- [ ] Footer: Querlinks zu den drei Legal Pages + Copyright
- [ ] Responsive: Funktioniert auch auf Mobile (die Links werden aus Apps geoeffnet)

**Aufwand:** S

---

### S02: Datenschutzerklaerung (/privacy)
**Als** Besucher **moechte ich** die Datenschutzerklaerung unter `/privacy` lesen, **damit** ich weiss wie meine Daten verarbeitet werden.

**Akzeptanzkriterien:**
- [ ] Route: `/privacy` (oeffentlich, kein Auth)
- [ ] Inhalt wird aus dem Backend geladen (bestehende HTML: `privacy.html`)
- [ ] Alternative: HTML-Inhalt direkt in die Blazor-Seite einbetten
- [ ] Seitentitel: "Datenschutzerklaerung -- fakturus.track"
- [ ] Meta-Tags fuer SEO (title, description)
- [ ] Korrekte Darstellung aller HTML-Elemente (Listen, Tabellen, Links)

**Aufwand:** S

---

### S03: AGB / Nutzungsbedingungen (/terms)
**Als** Besucher **moechte ich** die AGB unter `/terms` lesen, **damit** ich die Nutzungsbedingungen kenne.

**Akzeptanzkriterien:**
- [ ] Route: `/terms` (oeffentlich, kein Auth)
- [ ] Inhalt aus bestehender `terms.html`
- [ ] Seitentitel: "Nutzungsbedingungen -- fakturus.track"
- [ ] Meta-Tags fuer SEO

**Aufwand:** S

---

### S04: Impressum (/imprint)
**Als** Besucher **moechte ich** das Impressum unter `/imprint` lesen, **damit** ich den Anbieter identifizieren kann.

**Akzeptanzkriterien:**
- [ ] Route: `/imprint` (oeffentlich, kein Auth)
- [ ] Inhalt aus bestehender `imprint.html`
- [ ] Seitentitel: "Impressum -- fakturus.track"
- [ ] Meta-Tags fuer SEO

**Aufwand:** S

---

### S05: Legal Content Versionierung
**Als** Betreiber **moechte ich** Legal-Inhalte aktualisieren koennen, **damit** ich auf Rechtsaenderungen reagieren kann.

**Akzeptanzkriterien:**
- [ ] Legal-Inhalte werden ueber die bestehende Legal API geladen (/api/legal/versions)
- [ ] Fallback: Statische HTML-Dateien, falls API nicht erreichbar
- [ ] "Stand: {Datum}" wird oben auf jeder Seite angezeigt
- [ ] Bei neuer Version: Consent-Flow wird fuer bestehende Nutzer erneut getriggert

**Aufwand:** M

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Legal Layout | S | E01 |
| S02 Datenschutz | S | S01 |
| S03 AGB | S | S01 |
| S04 Impressum | S | S01 |
| S05 Content Versionierung | M | S01, Backend Legal API |

**Gesamt: ca. 0.5 Wochen** (S01-S04 sehr schnell, S05 etwas aufwaendiger)
