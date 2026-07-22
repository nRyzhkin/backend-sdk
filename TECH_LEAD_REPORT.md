# Technical Lead Report

Player Profiles client integration (SDK Iteration 1).

Package version: **0.5.0**

## 1. Files Added

| File | Purpose |
|------|---------|
| `Runtime/Profiles/IProfilesService.cs` | Public profiles contract |
| `Runtime/Profiles/ProfilesService.cs` | Thin profiles service facade |
| `Runtime/Profiles/PlayerProfile.cs` | Immutable profile model with `GetPublicData<T>()` |
| `Runtime/Profiles/PlayerProfileBatchResult.cs` | Batch lookup result with ordered collections and lookup helpers |
| `Runtime/Internal/ProfileJson.cs` | Parse/serialize profile wire format |
| `Runtime/Internal/ReadOnlyJsonRequestBody.cs` | Marker for read-only POST bodies without `X-Request-Id` |
| `Tests~/Profiles/PlayerProfileJsonTests.cs` | JSON parsing, validation, and serialization unit tests |

## 2. Files Modified

| File | Change |
|------|--------|
| `Runtime/Backend.cs` | Added `Backend.Profiles` facade |
| `Runtime/Internal/BackendClient.cs` | Added anonymous/PUT JSON helpers; batch uses `ReadOnlyJsonRequestBody` |
| `Runtime/Internal/UnityWebRequestTransport.cs` | Skip `X-Request-Id` for `ReadOnlyJsonRequestBody` |
| `Runtime/Internal/RemoteConfigJson.cs` | Exposed shared JSON helpers as `internal` |
| `Runtime/Internal/AnalyticsParametersJson.cs` | Exposed `SerializeJsonValue` and `QuoteJsonString` |
| `README.md` | Profiles usage, auth matrix, PublicData trust warning |
| `Documentation~/Architecture.md` | Profiles architecture notes |
| `Samples~/GettingStarted/README.md` | Profiles integration sample |
| `CHANGELOG.md` | Version `0.5.0` |
| `package.json` | Version bump |
| `TECH_LEAD_REPORT.md` | This report |

Unchanged by design:

- Write operations (`POST`/`PUT`/`DELETE` with `JsonRequestBody` or DTO bodies) still send `X-Request-Id`
- Auth / Storage / Leaderboards / Analytics / Remote Config service logic
- Public APIs of existing modules

## 3. Public API

```csharp
await Backend.InitializeAsync();
await Backend.Auth.LoginAsync();

var me = await Backend.Profiles.GetMeAsync();

var updated = await Backend.Profiles.UpdateMeAsync(
    "Player One",
    "avatar_03",
    new MyPublicProfileData
    {
        status = "Looking for team",
        level = 12,
        badges = new[] { "founder", "tester" }
    });

var data = updated.GetPublicData<MyPublicProfileData>();

var publicProfile = await Backend.Profiles.GetAsync(updated.UserId);

var batch = await Backend.Profiles.GetBatchAsync(new[] { updated.UserId, Guid.NewGuid() });
if (batch.TryGetProfile(updated.UserId, out var profile))
{
    Debug.Log(profile.DisplayName);
}
```

Interface:

```csharp
Task<PlayerProfile> GetMeAsync(CancellationToken cancellationToken = default);

Task<PlayerProfile> UpdateMeAsync<TPublicData>(
    string displayName,
    string avatarId,
    TPublicData publicData,
    CancellationToken cancellationToken = default);

Task<PlayerProfile> UpdateMeAsync(
    string displayName,
    string avatarId,
    string publicDataJson,
    CancellationToken cancellationToken = default);

Task<PlayerProfile> GetAsync(Guid userId, CancellationToken cancellationToken = default);

Task<PlayerProfileBatchResult> GetBatchAsync(
    IReadOnlyCollection<Guid> userIds,
    CancellationToken cancellationToken = default);
```

Constants:

- `ProfilesService.MaxBatchSize = 100`

## 4. ApplicationId Resolution

- Taken from `Backend.Settings.ApplicationId` via `BackendClient.ApplicationIdOrThrow()`
- Inserted into paths:
  - `GET v1/profiles/{applicationId}/me`
  - `PUT v1/profiles/{applicationId}/me`
  - `GET v1/profiles/{applicationId}/{userPublicId}`
  - `POST v1/profiles/{applicationId}/batch`
- Never accepted from game code

## 5. Authorization Matrix

| Method | Auth | Transport |
|--------|------|-----------|
| `GetMeAsync` | Player JWT required | `GetRawAsync` |
| `UpdateMeAsync` | Player JWT required | `PutJsonAsync` (Bearer + `X-Request-Id`) |
| `GetAsync` | Anonymous | `GetRawAnonymousAsync` |
| `GetBatchAsync` | Anonymous | `PostJsonAnonymousAsync` |

`GetMeAsync` / `UpdateMeAsync` throw `BackendException` (`not_authenticated`) when no session exists.

`GetAsync` / `GetBatchAsync` work after `Backend.InitializeAsync()` without `Auth.LoginAsync()`.

## 6. Backend Wire Format Compatibility

Aligned with `my-backend` Player Profiles Iteration 1 + 2.

**Profile response**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationId": "test-game",
  "displayName": "Player",
  "avatarId": null,
  "publicData": { "status": "Online" },
  "createdAt": "2026-07-22T12:00:00Z",
  "updatedAt": "2026-07-22T12:05:00Z"
}
```

**Update request**

```json
{
  "displayName": "Player One",
  "avatarId": "avatar_03",
  "publicData": { "status": "Looking for team" }
}
```

**Batch request / response**

```json
{ "userIds": ["guid-1", "guid-2"] }
{ "profiles": [ ... ], "missingUserIds": ["guid-2"] }
```

## 7. JSON Serialization Approach

- `ProfileJson.ParseProfile` stores `publicData` as a raw JSON fragment
- `PlayerProfile.GetPublicData<T>()` uses `RemoteConfigJson.DeserializeValue<T>`
- `ProfileJson.BuildUpdateRequest` uses `AnalyticsParametersJson.SerializeJsonValue` so `publicData` is a native JSON object
- Raw JSON overload embeds a validated JSON object directly
- `avatarId: null` preserved
- `DateTime` parsed as UTC via `DateTimeStyles.RoundtripKind`
- Malformed GUID/JSON throws `BackendException` (`profile_deserialization_failed`)

No new public JSON DOM type. `RemoteConfigValue` was not reused to avoid semantic coupling.

## 8. Lazy Profile Creation

- `GetMeAsync` calls `GET /v1/profiles/{applicationId}/me`
- Backend creates the profile when missing
- SDK does not expose a separate create operation

## 9. Update Semantics

- `PUT /me` fully replaces `displayName`, `avatarId`, and `publicData`
- One `X-Request-Id` per `UpdateMeAsync` call, reused across transient retries
- `CancellationToken` honored on every attempt through existing transport

## 10. Batch Semantics

SDK-side validation before HTTP:

| Rule | Exception |
|------|-----------|
| `userIds == null` | `ArgumentNullException` |
| empty collection | `ArgumentException` |
| more than 100 IDs (before dedupe) | `ArgumentException` |
| `Guid.Empty` | `ArgumentException` |

Local dedupe removes duplicates while preserving first-occurrence order.

Response collections are never null. `TryGetProfile` and `ByUserId` support leaderboard enrichment.

## 11. Error Handling

| Case | Behavior |
|------|----------|
| SDK not initialized | `BackendException` (`backend_not_initialized`) |
| Unauthenticated protected calls | `BackendException` (`not_authenticated`) |
| Missing public profile | `BackendException` with `StatusCode = 404` |
| Backend validation error | `BackendException` with status code and server message |
| Malformed profile JSON | `BackendException` (`profile_deserialization_failed`) |
| Invalid batch arguments | `ArgumentException` / `ArgumentNullException` |
| Cancellation | `OperationCanceledException` |

No separate `ProfilesException`.

## 12. PublicData Trust Warning

PublicData is client-controlled display data and must not be trusted for authoritative gameplay decisions.

Real inventory, currency, purchases, verified achievements, and server rank belong in separate authoritative backend modules.

## 13. Retry / RequestId

| Operation | Verb | X-Request-Id | Retry |
|-----------|------|--------------|-------|
| `GetMeAsync` | GET | No | Transient read retry |
| `UpdateMeAsync` | PUT | Yes | Transient write retry, stable RequestId |
| `GetAsync` | GET | No | Transient read retry |
| `GetBatchAsync` | POST | **No** | Transient read retry (no idempotency) |

### X-Request-Id audit (Profiles batch)

**Finding:** before the fix, `UnityWebRequestTransport.CreateRequestContext` generated a RequestId for every non-GET verb, so `GetBatchAsync` incorrectly sent `X-Request-Id` on `POST /profiles/batch`.

**Path traced:**

`ProfilesService.GetBatchAsync` → `BackendClient.PostJsonAnonymousAsync` → `UnityWebRequestTransport.SendAsync(POST, ReadOnlyJsonRequestBody, …)` → `CreateRequest` → `SetRequestHeader("X-Request-Id", …)` only when `context.RequestId` is set.

**Fix:** `PostJsonAnonymousAsync` wraps the body in `ReadOnlyJsonRequestBody`. Transport treats this marker like GET: `RequestContext.RequestId = null`, so the header is not sent. `UpdateMeAsync` and other write operations are unchanged (`JsonRequestBody` / DTO bodies still get RequestId).

**Confirmed for `GetBatchAsync`:**

- Uses anonymous POST via `PostJsonAnonymousAsync`
- `X-Request-Id` is **not** sent
- No idempotency semantics; POST chosen only because batch user ID lists exceed practical GET URL limits
- Transient network retry may still occur via the shared transport loop (same as GET), but without RequestId reuse

## 14. Not Implemented

- Profile search, explicit create, PATCH/merge, admin API
- Avatar upload, friends, inventory, achievements, rank
- Leaderboard auto-enrichment, local cache, offline fallback
- ETag, polling, profile change events
- New parallel HTTP infrastructure

## 15. Tests

`Tests~/Profiles/PlayerProfileJsonTests.cs` covers:

1. Profile response parsing
2. Nested PublicData types
3. `GetPublicData<T>`
4. Batch response order and lookup
5. Validation rules
6. Update/batch request serialization
7. Local dedupe

No mock HTTP transport was added.

## 16. Unity Verification Checklist

1. `Backend.Profiles` available after initialization
2. `GetMeAsync` requires login; lazy create on backend
3. `UpdateMeAsync` sends native JSON `publicData` object
4. `GetAsync` / `GetBatchAsync` work without login
5. Batch max 100, rejects `Guid.Empty`, dedupes locally
6. `404` / `401` map to `BackendException`
7. Existing modules still work
8. Package version `0.5.0`

## 17. Public API Confirmation

No breaking changes. Additive API only:

- `Backend.Profiles`
- `IProfilesService`
- `ProfilesService`
- `PlayerProfile`
- `PlayerProfileBatchResult`
