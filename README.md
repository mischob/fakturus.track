# Fakturus Track - Zeiterfassung

Zeiterfassungs-App fuer den deutschen Markt mit nativen Mobile-Apps und Web-App.

## Projekte

| Projekt | Technologie | Beschreibung |
|---------|-------------|-------------|
| `Fakturus.Track.Backend` | ASP.NET Core 10, FastEndpoints, PostgreSQL | REST-API unter `api.track.fakturus.com` |
| `Fakturus.Track.WebApp` | Blazor Server, Tailwind CSS | Web-App unter `track.fakturus.com` (Desktop/Tablet) |
| `FakturusTrack.iOS` | SwiftUI, SwiftData | Native iOS App |
| `FakturusTrack.Android` | Jetpack Compose, Room | Native Android App |
| `Fakturus.Track.Frontend` | Blazor WASM (VERALTET) | Wird durch WebApp ersetzt |

## Architektur

```
                    ┌─────────────────────┐
                    │   Azure AD B2C      │
                    │   (Authentifizierung)│
                    └──────────┬──────────┘
                               │
          ┌────────────────────┼────────────────────┐
          │                    │                    │
    ┌─────▼─────┐      ┌──────▼──────┐     ┌──────▼──────┐
    │  iOS App  │      │  Web App    │     │ Android App │
    │  (SwiftUI)│      │  (Blazor    │     │ (Compose)   │
    │           │      │   Server)   │     │             │
    └─────┬─────┘      └──────┬──────┘     └──────┬──────┘
          │                   │                    │
          └───────────────────┼────────────────────┘
                              │ HTTPS
                    ┌─────────▼─────────┐
                    │   Backend API     │
                    │  (FastEndpoints)  │
                    │   PostgreSQL      │
                    └───────────────────┘
```

## Voraussetzungen

- .NET 10 SDK
- Xcode 16+ (fuer iOS)
- Android Studio (fuer Android)
- Docker + Docker Compose
- PostgreSQL (via Docker oder Azure)

## Lokale Entwicklung

### 1. Datenbank starten

```bash
docker-compose up -d
```

PostgreSQL laeuft auf `localhost:5434` (User: admin, Passwort: adminpassword).

### 2. Backend starten

```bash
cd Fakturus.Track.Backend
dotnet run
```

API verfuegbar unter `https://localhost:7067`. Datenbank-Migrationen werden automatisch ausgefuehrt.

### 3. Web-App starten

```bash
cd Fakturus.Track.WebApp
dotnet run
```

Web-App verfuegbar unter `https://localhost:5001` (oder siehe `Properties/launchSettings.json`).

### 4. iOS App

Oeffne `FakturusTrack.iOS/FakturusTrack.xcodeproj` in Xcode. Scheme: FakturusTrack, Destination: iPhone Simulator.

### 5. Android App

Oeffne `FakturusTrack.Android/` in Android Studio. Build und auf Emulator/Geraet deployen.

## Deployment (Produktion)

### Docker-Images bauen

```bash
# Backend API
docker build -f Fakturus.Track.Backend/Dockerfile -t registry.fakturus.com/fakturus-track-api:latest .

# Web-App
docker build -f Fakturus.Track.WebApp/Dockerfile -t registry.fakturus.com/fakturus-track-webapp:latest .
```

### Images pushen

```bash
docker push registry.fakturus.com/fakturus-track-api:latest
docker push registry.fakturus.com/fakturus-track-webapp:latest
```

### Auf Server deployen

```bash
ssh root@91.99.65.63
cd /opt/fakturus-track
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

### Domains

| Domain | Service | Port |
|--------|---------|------|
| `api.track.fakturus.com` | Backend API | 8082 |
| `track.fakturus.com` | Web-App (Blazor Server) | 8092 |

Traefik uebernimmt TLS-Terminierung und Routing.

## API Endpoints

### Authentifiziert (Bearer Token)

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/v1/work-sessions` | Alle Arbeitssitzungen |
| POST | `/v1/work-sessions` | Neue Sitzung erstellen |
| PUT | `/v1/work-sessions/{id}` | Sitzung aktualisieren |
| DELETE | `/v1/work-sessions/{id}` | Sitzung loeschen |
| POST | `/v1/work-sessions/sync` | Sitzungen synchronisieren |
| GET | `/v1/vacation-days` | Urlaubstage |
| POST | `/v1/vacation-days` | Urlaubstag erstellen |
| DELETE | `/v1/vacation-days/{id}` | Urlaubstag loeschen |
| POST | `/v1/vacation-days/sync` | Urlaubstage synchronisieren |
| GET | `/v1/settings` | Benutzereinstellungen |
| PUT | `/v1/settings` | Einstellungen aktualisieren |
| GET | `/v1/overtime-summary` | Ueberstunden-Zusammenfassung |
| GET | `/api/legal/consent` | Consent-Status |
| POST | `/api/legal/consent` | Consent abgeben |
| DELETE | `/api/account` | Konto loeschen |

### Oeffentlich (kein Token)

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/v1/health` | Health-Check |
| GET | `/v1/version` | API-Version |
| GET | `/api/legal/versions` | Rechtsdokument-Versionen |
| GET | `/privacy` | Datenschutzerklaerung |
| GET | `/terms` | Nutzungsbedingungen |
| GET | `/imprint` | Impressum |

## Authentifizierung

Azure AD B2C (Tenant: `fakturus.onmicrosoft.com`).

| Client | Client-ID | Redirect-URI |
|--------|-----------|-------------|
| Backend API | `74fd0ed2-...` | — |
| Web-App | `3fb35bc6-...` | `https://track.fakturus.com/signin-oidc` |
| iOS | `3fb35bc6-...` | `msauth.com.fakturus.track://auth` |
| Android | `3fb35bc6-...` | `msauth://com.fakturus.track/...` |

## Rechtliches

- Datenschutzerklaerung: `https://track.fakturus.com/privacy`
- Nutzungsbedingungen: `https://track.fakturus.com/terms`
- Impressum: `https://track.fakturus.com/imprint`
- VVT: `.project/legal/vvt.md` (intern)

## Projektplanung

| Phase | Beschreibung | Status |
|-------|-------------|--------|
| Phase 1-3 | Native Mobile Apps (iOS + Android) | Abgeschlossen |
| Phase 4 | Feature-Gating, Paywall, Subscription | Abgeschlossen |
| Phase 5 | Legal Compliance (AGB, Datenschutz, Consent) | Implementiert |
| Phase 6 | Web-App (Blazor Server, Desktop-optimiert) | In Entwicklung |

Detaillierte Stories in `.project/stories/`.
