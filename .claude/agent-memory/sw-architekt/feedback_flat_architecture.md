---
name: Flache Architektur bevorzugt
description: AI-optimierte Architektur ohne Clean Architecture, Hilt, UseCases oder Repository Pattern
type: feedback
---

Architektur-Entscheidungen fuer Fakturus Track: Flache 2-Schichten statt Clean Architecture. Kein Hilt (manuelle DI). Keine UseCases. Kein Repository Pattern ueber Room/SwiftData. Ktor statt Retrofit.

**Why:** Die App hat 4 Screens und 8 API-Endpunkte. Clean Architecture wuerde 5+ Dateien pro Flow erzeugen. AI-Agenten verlieren den Kontext ueber viele Abstraktionsschichten. Dokumentiert in ADRs 002, 004, 006, 007.

**How to apply:** Bei Code-Reviews und Feature-Implementierung darauf achten, dass keine unnuetzen Schichten eingefuehrt werden. ViewModels sprechen direkt mit DB und APIClient. Feature-basierte Ordnerstruktur (nicht technisch-geschichtet).
