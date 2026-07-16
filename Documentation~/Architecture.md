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
6. Service facades (`Auth`, `Storage`, `Leaderboards`, ...) reuse the shared client.

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

Services never implement RequestId or Retry. Future modules (Analytics, Friends, Chat, Purchases) automatically inherit this behavior by calling `BackendClient`.

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
- Wire-format DTOs for camelCase JSON
- `UnityJsonSerializer`

## Implemented Modules

- Auth
- Storage
- Leaderboards

## Extension Strategy

1. Add a public facade under `Backend.<Service>`.
2. Keep wire DTOs internal.
3. Route all networking through `BackendClient`.
4. Return domain models, never transport objects.
5. Keep ApplicationId, Authorization, RequestId, and Retry inside the SDK.

## Remaining Work

- Analytics, Friends, Remote Config, Inventory, Daily Rewards
- Token refresh
- Offline / cross-session delivery (optional future layer on top of current transport retry)
