# Architecture

## Design Principles

Backend SDK is a game-facing API, not a REST wrapper.

- Game code consumes high-level services.
- HTTP, JSON, JWT, URLs, ApplicationId, RequestId, and Retry stay inside the package.
- `Backend` is the public composition root.
- `BackendClient` and `UnityWebRequestTransport` stay internal.

## Initialization Flow

1. Editor settings are saved to `ProjectSettings/BackendSdkSettings.json`.
2. Settings are mirrored to `Assets/Resources/BackendSdkSettings.json`.
3. `Backend.InitializeAsync()` loads that resource.
4. Runtime caches Backend URL, Application ID, and retry settings in `Backend.Settings`.
5. Transport and `BackendClient` are created once.
6. Service facades (`Auth`, `Storage`, `Leaderboards`, `Analytics`, `RemoteConfig`, `Profiles`, `Economy`, ...) reuse the shared client.

## Authentication Flow

1. `AuthService.LoginAsync` validates input or development settings.
2. Request is sent through `BackendClient` to `POST /v1/auth/login`.
3. Response is mapped to an immutable `PlayerSession`.
4. `AuthService` stores the session privately.
5. `LogoutAsync` clears the local session.

## Automatic Token Injection

- `AuthService` owns the access token.
- `BackendClient` calls `AuthService.GetAuthorizationHeader()`.
- Transport sets `Authorization: Bearer <token>` when present.
- Storage and Leaderboards never receive tokens as parameters.

## Automatic ApplicationId Usage

- Application ID comes from Project Settings.
- Storage paths: `/v1/storage/{applicationId}/{key}`
- Leaderboard paths: `/v1/leaderboards/{applicationId}/{leaderboardName}`
- Analytics path: `/v1/analytics/{applicationId}/events`
- Remote Config paths: `/v1/remote-config/{applicationId}` and `/v1/remote-config/{applicationId}/{key}`
- Profile paths: `/v1/profiles/{applicationId}/me`, `/v1/profiles/{applicationId}/{userPublicId}`, `/v1/profiles/{applicationId}/batch`
- Economy path: `/v1/economy/{applicationId}/me`
- Game APIs never accept ApplicationId arguments.

## RequestId And Retry (Transport Layer)

Owned exclusively by `UnityWebRequestTransport`:

1. For `POST` / `PUT` / `DELETE`, generate `Guid.NewGuid().ToString("N")` once per logical call.
2. Store it in internal `RequestContext`.
3. Send header `X-Request-Id` on every attempt of that call.
4. GET requests never receive a RequestId.
5. On `BackendException` with `IsTransient == true`, retry up to `RetryCount` additional times.
6. Retries reuse the same RequestId and use a constant `RetryDelayMilliseconds`.
7. Retry exists only for the duration of one method call. No offline queue.

Services never implement RequestId or Retry. Future modules automatically inherit this behavior by calling `BackendClient`.

## Remote Config

- Read-only Game API
- Anonymous (no login required)
- No caching in iteration 1
- Each `GetAsync` / `GetAllAsync` performs a fresh HTTP GET
- Backend wire format `{ key, value }` is unwrapped into `RemoteConfigValue`
- Arbitrary JSON supported through `RemoteConfigValue` and `GetAsync<T>()`

## Player Profiles

- `GetMeAsync` / `UpdateMeAsync` require Player JWT
- `GetAsync` / `GetBatchAsync` are anonymous (explicit `null` authorization on transport)
- `GetMeAsync` relies on backend lazy profile creation; no separate create API in the SDK
- `UpdateMeAsync` is a full replace (`displayName`, `avatarId`, `publicData`) via `PUT` with transport RequestId + retry
- `GetBatchAsync` validates 1–100 IDs before local dedupe, rejects `Guid.Empty`, dedupes preserving first occurrence order
- `PublicData` stored as raw JSON fragment; typed access via `PlayerProfile.GetPublicData<T>()`
- `PublicData` is client-controlled display data only — not authoritative for inventory, currency, purchases, achievements, or rank

## Player Economy

- Read-only player SDK — no grant, spend, set, consume, or revoke operations
- `GetDefinitionsAsync` / `GetStateAsync` / `RefreshAsync` require Player JWT
- `/me` returns the full active catalog merged with player balances/quantities; inactive definitions are excluded
- No separate player definitions endpoint; `GetDefinitionsAsync` reads the active catalog from `/me`
- In-memory cache with single-flight loading; cleared on logout, session change, and `ClearCache()`
- Stale in-flight responses do not populate cache after session change (generation guard)
- Caller `CancellationToken` cancels waiting only; shared HTTP load uses `CancellationToken.None`
- SDK validation: `dotnet test` (57 tests). Economy subset: `dotnet test --filter TestCategory=Economy`
- `PlayerEconomyState` helpers return `0` / `false` for missing resources (not an error)
- Admin economy routes under `/admin/api/applications/{applicationId}/economy/...` are intentionally excluded

## Runtime Layers

### Public Facade

- `Backend`
- `BackendOptions` / `BackendSettings`
- `BackendException`
- Domain services and models

### Internal Infrastructure

- `BackendClient`
- `IBackendTransport`
- `UnityWebRequestTransport`
- `RequestContext`
- `RemoteConfigJson`
- `EconomyJson`
- Wire-format DTOs for camelCase JSON
- `UnityJsonSerializer`

## Implemented Modules

- Auth
- Storage
- Leaderboards
- Analytics
- Remote Config
- Player Profiles
- Player Economy

## Extension Strategy

1. Add a public facade under `Backend.<Service>`.
2. Keep wire DTOs internal.
3. Route all networking through `BackendClient`.
4. Return domain models, never transport objects.
5. Keep ApplicationId, Authorization, RequestId, and Retry inside the SDK.

## Remaining Work

- Analytics queue / batch / offline delivery
- Remote Config caching / background refresh
- Friends, Inventory, Daily Rewards
- Token refresh
- Offline / cross-session delivery (optional future layer on top of current transport retry)
