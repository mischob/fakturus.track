# ADR-007: Ktor Client statt Retrofit auf Android

## Status
Akzeptiert

## Kontext
Der PO-Plan fuer Android schlaegt Retrofit + OkHttp oder Ktor Client vor. Wir muessen uns entscheiden.

## Entscheidung
Wir verwenden **Ktor Client** statt Retrofit.

## Begruendung

### Retrofit erfordert:
1. Ein Interface mit Annotationen (`@GET`, `@POST`, `@Body`, `@Path`)
2. Eine Converter-Factory (Gson, Moshi, oder kotlinx.serialization)
3. Einen OkHttp Interceptor fuer Auth-Token
4. Builder-Pattern fuer Retrofit-Instanz
5. Call Adapter (Coroutines, oder RxJava)

### Ktor Client erfordert:
1. Ein `HttpClient` mit `ContentNegotiation` Plugin
2. Direkte `client.get/post/put/delete` Aufrufe

### Konkret fuer unsere ~8 Endpunkte:

**Retrofit:**
```kotlin
// Interface
interface TrackApiService {
    @GET("/v1/work-sessions") suspend fun getWorkSessions(): List<WorkSessionDTO>
    @POST("/v1/work-sessions/sync") suspend fun syncWorkSessions(@Body request: SyncRequest): List<WorkSessionDTO>
    // ... 6 weitere
}
// Interceptor
class AuthInterceptor : Interceptor { ... }
// Builder
val retrofit = Retrofit.Builder().baseUrl(...).addConverterFactory(...).client(okHttpClient).build()
val service = retrofit.create(TrackApiService::class.java)
```

**Ktor:**
```kotlin
// Direkt im APIClient
suspend fun getWorkSessions(): List<WorkSessionDTO> = get("/v1/work-sessions")
suspend fun syncWorkSessions(request: SyncRequest): List<WorkSessionDTO> = post("/v1/work-sessions/sync", request)
```

Ktor ist **direkter**: Kein Interface, kein Codegen, kein Interceptor-Pattern. Der APIClient ist eine Klasse mit Methoden. Das ist konsistenter mit dem iOS APIClient (URLSession) und fuer AI-Agenten leichter zu verstehen.

## Konsequenzen
- Ktor Client ist weniger verbreitet als Retrofit (weniger StackOverflow-Antworten)
- Kein Codegen -- aber bei 8 Endpunkten ist das kein Problem
- Konsistenterer Code zwischen iOS und Android
- kotlinx.serialization statt Gson/Moshi (kompilierzeit-sicher)
