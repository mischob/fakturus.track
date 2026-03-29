# ADR-001: Native Apps statt Cross-Platform

## Status
Akzeptiert

## Kontext
Die bestehende MAUI/Blazor-Hybrid App funktioniert, wirkt aber nicht professionell genug. Wir muessen entscheiden ob die naechste Generation nativ (Swift/Kotlin) oder Cross-Platform (Flutter, React Native, MAUI) wird.

## Entscheidung
Wir entwickeln **separate native Apps** in Swift (iOS) und Kotlin (Android).

## Begruendung
1. **Bewaehrte Grundlage**: fakturus.poi beweist, dass wir native Apps erfolgreich entwickeln und maintainen koennen
2. **Wiederverwendung**: Auth-System (MSAL), API-Client Pattern, CI/CD Pipeline von fakturus.poi koennen 1:1 adaptiert werden
3. **UX-Qualitaet**: Plattform-native Patterns (Swipe, Haptics, Animationen) sind nativ besser als in Cross-Platform
4. **Performance**: Native UI ist spuerbar schneller als WebView-basiert (MAUI Blazor)
5. **AI-Entwicklung**: Swift und Kotlin Code ist fuer AI-Agenten gut verstaendlich, jede Plattform isoliert bearbeitbar
6. **App Store**: Native Apps haben bessere Akzeptanz und niedrigere Ablehnungsraten

## Konsequenzen
- Doppelte UI-Implementierung (iOS + Android)
- Sync-Logik muss auf beiden Plattformen identisch implementiert werden
- Zwei Code-Repositories/Targets statt einem
- Hoehere Entwicklungskosten, aber bessere Qualitaet
