# Technical Lead Report

Analytics client integration for `POST /v1/analytics/{applicationId}/events`.

## 1. Files Added

| File | Purpose |
|------|---------|
| `Runtime/Analytics/IAnalyticsService.cs` | Public analytics contract |
| `Runtime/Analytics/AnalyticsService.cs` | Thin analytics service implementation |
| `Runtime/Internal/AnalyticsEventRequestDto.cs` | Internal wire contract (`eventName`, `parameters`) |
| `Runtime/Internal/AnalyticsParametersJson.cs` | Builds analytics request JSON for arbitrary parameters |
| `Runtime/Internal/JsonRequestBody.cs` | Passes pre-built JSON through existing transport |

## 2. Files Modified

| File | Change |
|------|--------|
| `Runtime/Internal/BackendClient.cs` | Added `PostJsonAsync` for analytics POST bodies |
| `Runtime/Internal/UnityWebRequestTransport.cs` | Sends `JsonRequestBody` without re-serializing |
| `Runtime/BackendPlaceholders.cs` | Removed analytics placeholder |
| `README.md` | Analytics usage and module list |
| `Documentation~/Architecture.md` | Analytics route and module status |
| `Samples~/GettingStarted/README.md` | Analytics example |
| `CHANGELOG.md` | Version `0.3.0` entry |
| `package.json` | Version bump to `0.3.0` |
| `TECH_LEAD_REPORT.md` | This report |

## 3. Public Analytics API

```csharp
await Backend.InitializeAsync();
await Backend.Auth.LoginAsync();

await Backend.Analytics.TrackAsync(
    "LevelStarted",
    new
    {
        level = 5,
        difficulty = "Hard"
    });

await Backend.Analytics.TrackAsync("TutorialCompleted");
```

Interface:

```csharp
Task TrackAsync(
    string eventName,
    object parameters = null,
    CancellationToken cancellationToken = default);
```

Validation:

- `eventName` required, trimmed, max 128 chars
- invalid `eventName` → `ArgumentException`
- not initialized → `BackendException`
- not authenticated → `BackendException` (`not_authenticated`)

No breaking changes to existing public APIs.

## 4. Integration With BackendClient

`AnalyticsService` flow:

1. Validate `eventName`
2. Require authenticated session
3. Build JSON body via `AnalyticsParametersJson`
4. Call `BackendClient.PostJsonAsync("v1/analytics/{applicationId}/events", json, token)`

`BackendClient` automatically provides:

- `ServerUrl` via transport
- `ApplicationId` in path via `ApplicationIdOrThrow()`
- `Authorization: Bearer <JWT>` via `AuthService`
- timeout, cancellation, retry, and `X-Request-Id` via existing transport

`AnalyticsService` contains no HTTP, retry, or token logic.

## 5. Authorization And ApplicationId

- JWT: reused from `AuthService.GetAuthorizationHeader()` through `BackendClient`
- ApplicationId: inserted into URL path, never accepted from game code
- Backend resolves `UserId` from JWT; game code never passes user identifiers

## 6. 204 No Content Handling

Backend returns `204 No Content`.

Existing transport behavior already supports this:

- empty response body → `default(EmptyResponse)`
- no deserialization attempt on empty payload

`TrackAsync` awaits `PostJsonAsync` and completes normally on 204.

## 7. Retry Behavior

No analytics-specific retry logic was added.

`POST` analytics events use the existing transport retry policy:

- transient failures may retry
- same `X-Request-Id` is reused across retries for one logical call
- constant delay from `RetryDelayMilliseconds`

This is acceptable for collection-phase analytics. Separate queue/batch/idempotency is deferred.

## 8. Why Separate Analytics Idempotency Was Not Added

Out of scope for this iteration:

- local queue
- offline storage
- batch endpoint
- background worker
- analytics-specific deduplication

Current design sends one event per `TrackAsync` call and relies on transport retry only within that call.

## 9. JSON Serializer Limitations

`JsonUtility` cannot embed arbitrary `object` / anonymous parameters inside a DTO.

Minimal adaptation:

- `AnalyticsParametersJson` builds the full request JSON string
- supports:
  - `null` parameters (field omitted; backend stores `{}`)
  - primitives and strings
  - `Dictionary<string, object>` and other `IDictionary`
  - `[Serializable]` DTOs via existing `UnityJsonSerializer`
  - anonymous objects and plain CLR objects via public properties/fields (analytics-scoped reflection)

Limitations:

- no Newtonsoft / System.Text.Json added
- complex nested graphs are not fully generalized
- dictionary values must be JSON-compatible types
- enums serialize as numeric values unless wrapped in a serializable DTO

Auth / Storage / Leaderboards serialization paths were not changed.

## 10. Minimal Transport Change

`UnityWebRequestTransport` now recognizes `JsonRequestBody` and sends its JSON verbatim.

This was required because `JsonUtility` cannot produce `{ "eventName": "...", "parameters": { ... } }` with arbitrary nested parameters.

Change is limited to payload selection; retry, cancellation, auth, and RequestId behavior are unchanged.

## 11. How To Verify In Unity

```csharp
await Backend.InitializeAsync();
await Backend.Auth.LoginAsync();

await Backend.Analytics.TrackAsync(
    "LevelStarted",
    new { level = 5, difficulty = "Hard" });

await Backend.Analytics.TrackAsync("TutorialCompleted");
```

Expected checks:

1. HTTP `204`
2. row appears in backend `AnalyticsEvents`
3. `EventName` matches input
4. `ParametersJson` contains expected JSON for parameterized events
5. `UserId` matches authenticated user
6. `ApplicationId` matches Project Settings
7. event without parameters succeeds
8. unauthenticated call throws `BackendException`
9. backend down → existing `BackendException`
10. Auth / Storage / Leaderboards still work

Enable `Enable Logging` to inspect transport logs including RequestId on POST retries.

## 12. Open Questions For Next Iteration

- Should analytics support fire-and-forget without awaiting?
- Should failed analytics events be queued locally?
- Should batch ingestion be added?
- Should duplicate event suppression exist client-side?
- Should serializer move to a shared non-reflection JSON builder for all modules?

## 13. Public API Confirmation

No breaking changes.

`Backend.Analytics` is available after `Backend.InitializeAsync()` with the same lifecycle as other services.
