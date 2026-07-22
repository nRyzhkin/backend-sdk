# Technical Lead Report

Remote Config read-only client integration.

## 1. Files Added

| File | Purpose |
|------|---------|
| `Runtime/RemoteConfig/IRemoteConfigService.cs` | Public remote config contract |
| `Runtime/RemoteConfig/RemoteConfigService.cs` | Thin read-only service |
| `Runtime/RemoteConfig/RemoteConfigValue.cs` | Public arbitrary JSON value wrapper (`JsonElement` equivalent) |
| `Runtime/Internal/RemoteConfigJson.cs` | Parse backend JSON and deserialize typed values |
| `Runtime/AssemblyInfo.cs` | `InternalsVisibleTo` for tests |
| `Tests~/RemoteConfig/RemoteConfigJsonTests.cs` | JSON parsing unit tests |
| `Tests~/RemoteConfig/Backend.Sdk.Tests.asmdef` | Editor test assembly |

## 2. Files Modified

| File | Change |
|------|--------|
| `Runtime/Internal/BackendClient.cs` | Added `GetRawAsync` for raw JSON GET responses |
| `Runtime/Internal/UnityJsonSerializer.cs` | Added untyped `Deserialize(string, Type)` for array elements |
| `Runtime/BackendPlaceholders.cs` | Removed remote config placeholder |
| `README.md` | Remote Config usage and constraints |
| `Documentation~/Architecture.md` | Remote Config architecture notes |
| `Samples~/GettingStarted/README.md` | Remote Config example before auth |
| `CHANGELOG.md` | Version `0.4.0` |
| `package.json` | Version bump |
| `TECH_LEAD_REPORT.md` | This report |

Unchanged by design:

- `UnityWebRequestTransport` (204 / empty body already supported)
- Auth / Storage / Leaderboards / Analytics service logic
- Public APIs of existing modules

## 3. Public API

```csharp
await Backend.InitializeAsync();

var apiUrl = await Backend.RemoteConfig.GetAsync<string>("apiUrl");
var maintenance = await Backend.RemoteConfig.GetAsync<bool>("maintenance");

var settings = await Backend.RemoteConfig.GetAsync<GameSettings>("gameSettings");

var value = await Backend.RemoteConfig.GetAsync("cdnUrl");
var cdnUrl = value.As<string>();

var all = await Backend.RemoteConfig.GetAllAsync();
```

Interface:

```csharp
Task<RemoteConfigValue> GetAsync(string key, CancellationToken cancellationToken = default);
Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
Task<Dictionary<string, RemoteConfigValue>> GetAllAsync(CancellationToken cancellationToken = default);
```

`RemoteConfigValue` is used instead of `System.Text.Json.JsonElement` to avoid adding a new dependency and to fit Unity-friendly conventions.

## 4. ApplicationId Resolution

- Taken from `Backend.Settings.ApplicationId` via `BackendClient.ApplicationIdOrThrow()`
- Inserted into paths:
  - `GET v1/remote-config/{applicationId}`
  - `GET v1/remote-config/{applicationId}/{key}`
- Never accepted from game code
- Prevents accidental cross-application reads through the public API

## 5. Authorization

- Remote Config does **not** require `Backend.Auth.LoginAsync()`
- Uses existing `BackendClient.GetRawAsync`
- If a session exists, transport may still attach `Authorization` through the shared client path; backend public endpoints are anonymous
- No Remote Config-specific JWT logic was added

## 6. Backend Wire Format Compatibility

Actual `my-backend` public API returns:

**List**

```json
[
  { "key": "apiUrl", "value": "https://api.example.com" },
  { "key": "maintenance", "value": false }
]
```

**Single entry**

```json
{ "key": "apiUrl", "value": "https://api.example.com" }
```

The SDK unwraps these into game-friendly values.

`RemoteConfigJson` also supports a flat object list response if the backend shape changes later:

```json
{
  "apiUrl": "https://api.example.com",
  "maintenance": false
}
```

## 7. JSON Serialization Approach

Problem:

- `JsonUtility` cannot deserialize arbitrary JSON trees or nested `object` fields.

Solution:

- `BackendClient.GetRawAsync` returns raw response text
- `RemoteConfigJson` parses:
  - backend entry array / wrapped entry
  - optional flat object map
  - primitive JSON values without double-encoding
- `GetAsync<T>` uses:
  - custom primitive parsing for `string`, `bool`, numeric types
  - `JsonUtility` for `[Serializable]` DTO objects
  - minimal array support for primitive arrays

Examples:

- JSON `"https://cdn.example.com"` → `GetAsync<string>` → `https://cdn.example.com`
- JSON `100` → `GetAsync<int>` → `100`
- JSON `{ "android": "...", "ios": "..." }` → `GetAsync<AssetUrls>`

Deserialization failures throw `BackendException` with error code `remote_config_deserialization_failed` and context:

- ApplicationId
- key
- target type

## 8. Error Handling

| Case | Behavior |
|------|----------|
| SDK not initialized | `BackendException` (`backend_not_initialized`) |
| Missing ApplicationId | `BackendException` (`missing_application_id`) |
| Invalid key argument | `ArgumentException` |
| Missing config key | backend `404` → existing `BackendException` with `StatusCode = 404` |
| Backend unavailable | existing transport / `BackendException` |
| Cancellation | existing `OperationCanceledException` propagation |
| Type conversion failure | `BackendException` (`remote_config_deserialization_failed`) |

No separate Remote Config error system was introduced.

## 9. 204 / Empty Body

Not applicable for Remote Config GET responses.

Existing transport behavior remains used for other modules:

- empty successful body → `default(TResponse)`

## 10. Retry

No Remote Config-specific retry logic.

GET requests:

- do not send `X-Request-Id`
- use existing transport transient retry rules when applicable

## 11. Caching

Explicitly **not** implemented:

- persistent cache
- PlayerPrefs
- disk cache
- offline storage
- background refresh

Each `GetAsync` / `GetAllAsync` performs a fresh HTTP GET.

## 12. Iteration 1 Limitations

Not implemented:

- Admin API in SDK
- write/update/delete from game code
- caching
- versioning / rollback / draft states
- environment switching
- encryption / secrets handling beyond documentation warnings

Remote Config values are public data only. Do not store secrets.

JSON limitations:

- complex polymorphic graphs are not fully generalized
- array support is best-effort for primitive arrays and `[Serializable]` element types
- dictionaries inside DTOs still follow `JsonUtility` constraints

## 13. Tests

Added `Tests~/RemoteConfig/RemoteConfigJsonTests.cs` (Editor / `UNITY_INCLUDE_TESTS`):

- backend entry array parsing
- flat object parsing
- wrapped entry value extraction
- string without double-encoding
- number / bool parsing
- object DTO parsing

Run via Unity Test Runner after importing the package with Test Framework enabled.

## 14. Unity Verification Checklist

1. `Backend.InitializeAsync()` works
2. `Backend.RemoteConfig` is available
3. Remote Config works without `Auth.LoginAsync()`
4. `GetAsync<string>` works
5. `GetAsync<int>` works
6. `GetAsync<bool>` works
7. `GetAsync<T>` works for `[Serializable]` DTO
8. nested object works
9. array values work for supported types
10. `GetAllAsync()` returns all entries
11. missing key returns `404` / `BackendException`
12. backend down uses existing transport errors
13. cancellation works
14. Auth still works
15. Storage still works
16. Leaderboards still work
17. Analytics still works
18. `ApplicationId = game-1` only reads `game-1` data

## 15. Open Questions For Next Iteration

- Should Remote Config add in-memory session cache?
- Should typed getters support `Dictionary<string, object>` directly?
- Should list endpoint be normalized on backend to a flat object?
- Should Remote Config expose metadata such as `updatedAt` in a future admin-only shape?

## 16. Public API Confirmation

No breaking changes to existing modules.

Additive API only:

- `Backend.RemoteConfig`
- `IRemoteConfigService`
- `RemoteConfigService`
- `RemoteConfigValue`
