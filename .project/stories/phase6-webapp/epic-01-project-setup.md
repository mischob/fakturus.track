# EPIC 01: Projekt-Setup & Infrastruktur

## Ziel

Neues Blazor Server Projekt aufsetzen (oder bestehendes WASM-Projekt umbauen), mit Auth, Tailwind CSS, CI/CD und Deployment-Pipeline. Am Ende dieser Epic ist eine leere, authentifizierte Shell unter `track.fakturus.com` erreichbar.

## Entscheidung: Neu vs. Umbau

**Empfehlung: Neues Projekt** (`Fakturus.Track.WebApp/`), da:
- Blazor Server vs. WASM sind fundamental unterschiedliche Hosting-Modelle
- Das bestehende WASM-Projekt hat Offline-Logik (LocalStorage, Service Worker) die fuer Server nicht relevant ist
- Sauberer Start ohne technische Schulden aus dem alten Projekt
- Services/Models koennen aus dem alten Projekt uebernommen werden

---

## Stories

### S01: Blazor Server Projekt erstellen
**Als** Entwickler **moechte ich** ein neues Blazor Server Projekt mit .NET 8 aufsetzen, **damit** die Web-App eine saubere Grundlage hat.

**Akzeptanzkriterien:**
- [ ] Neues Projekt `Fakturus.Track.WebApp` in der Solution
- [ ] .NET 8 Blazor Server Template
- [ ] Projekt kompiliert und startet lokal
- [ ] `Program.cs` mit Basis-Konfiguration (Logging, DI, Routing)
- [ ] Health-Check Endpoint unter `/health`

**Aufwand:** S

---

### S02: Tailwind CSS Integration
**Als** Entwickler **moechte ich** Tailwind CSS mit dem bestehenden Design-System integrieren, **damit** das Styling konsistent mit den Design-Vorgaben ist.

**Akzeptanzkriterien:**
- [ ] Tailwind CSS via PostCSS Build-Pipeline (npm/pnpm)
- [ ] `tailwind.config.js` mit Farben aus Design-System (Primary #1A5CFF, Success, Danger, etc.)
- [ ] Inter Font eingebunden (Google Fonts oder selbst-gehostet)
- [ ] Dark Mode Support via `class`-Strategie (nicht `media`)
- [ ] CSS wird bei Aenderungen automatisch rebuilt (Watch-Modus)
- [ ] Produktions-Build mit Purging (minimale CSS-Groesse)

**Aufwand:** S

---

### S03: Azure AD B2C Authentifizierung
**Als** Benutzer **moechte ich** mich mit meinem bestehenden Konto (Google oder E-Mail) anmelden, **damit** ich die gleichen Daten wie in der Mobile-App sehe.

**Akzeptanzkriterien:**
- [ ] MSAL-Integration fuer Blazor Server (Microsoft.Identity.Web)
- [ ] Login via Azure AD B2C (gleicher Tenant: fakturus B2C)
- [ ] Google und E-Mail/Passwort als Identity Provider (kein Apple Sign-In im Web)
- [ ] JWT Access Token wird nach Login an Backend-API Calls angehaengt
- [ ] Logout-Funktionalitaet
- [ ] Nicht-authentifizierte Nutzer werden auf Login-Seite umgeleitet
- [ ] Legal Pages (/privacy, /terms, /imprint) sind OHNE Login erreichbar
- [ ] Token-Refresh funktioniert (Silent Refresh)

**Aufwand:** M

---

### S04: API-Client Service
**Als** Entwickler **moechte ich** einen typsicheren API-Client fuer das Backend, **damit** die Web-App Daten laden und speichern kann.

**Akzeptanzkriterien:**
- [ ] HttpClient mit Base-URL `api.track.fakturus.com`
- [ ] Automatische JWT Bearer Token Injection
- [ ] API-Client Interfaces (aus bestehendem Frontend uebernehmen):
  - `IWorkSessionsApiClient`
  - `IVacationApiClient`
  - `ISettingsApiClient`
  - `ICalendarApiClient`
  - `ISchoolHolidayApiClient`
- [ ] Error Handling (401 -> Re-Login, 500 -> Toast-Meldung)
- [ ] Request/Response DTOs (aus bestehendem Projekt uebernehmen)

**Aufwand:** M

---

### S05: CI/CD Pipeline
**Als** Entwickler **moechte ich** eine automatische Build- und Deploy-Pipeline, **damit** Aenderungen automatisch auf `track.fakturus.com` deployed werden.

**Akzeptanzkriterien:**
- [ ] GitHub Actions Workflow: Build -> Test -> Deploy
- [ ] Deployment auf Azure App Service (Linux Container oder App Service direkt)
- [ ] Tailwind CSS Build im CI-Schritt
- [ ] Staging-Umgebung: `staging.track.fakturus.com`
- [ ] Production-Deployment nur ueber manuellen Trigger oder Tag
- [ ] DNS-Konfiguration: `track.fakturus.com` -> Azure App Service
- [ ] SSL/TLS Zertifikat (Azure Managed Certificate oder Let's Encrypt)

**Aufwand:** M

---

### S06: Shared Models & Utilities
**Als** Entwickler **moechte ich** die bestehenden Models und Utilities wiederverwenden, **damit** kein Code dupliziert wird.

**Akzeptanzkriterien:**
- [ ] Shared Project oder NuGet-Referenz fuer DTOs (WorkSession, VacationDay, UserSettings, etc.)
- [ ] Formatierungs-Utilities (Zeitformatierung HH:MM, Datum dd.MM.yyyy)
- [ ] Feiertags-Berechnung (oder API-Call zum Backend)
- [ ] ArbZG-Konstanten (6h, 9h, 10h Schwellen)

**Aufwand:** S

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Blazor Server Projekt | S | -- |
| S02 Tailwind CSS | S | S01 |
| S03 Azure AD B2C Auth | M | S01 |
| S04 API-Client Service | M | S01, S03 |
| S05 CI/CD Pipeline | M | S01 |
| S06 Shared Models | S | S01 |

**Gesamt: ca. 1 Woche** (S01+S02+S06 parallel, dann S03+S04+S05 parallel)
