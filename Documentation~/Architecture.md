# Architecture

## Design Principles

Backend SDK is a game-facing API, not a REST wrapper.

- Game code consumes high-level services.
- HTTP, JSON, JWT, URLs, and ApplicationId stay inside the package.
- `Backend` is the public composition root.
- `BackendClient` and `UnityWebRequestTransport` stay internal.

## Initialization Flow

1. Editor settings are saved to `ProjectSettings/BackendSdkSettings.json`.
2. Settings are mirrored to `Assets/Resources/BackendSdkSettings.json`.
3. `Backend.InitializeAsync()` loads that resource.
4. Runtime caches Backend URL and Application ID in `Backend.Settings`.
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
5. Keep ApplicationId and Authorization inside the SDK.

## Remaining Work

- Analytics, Friends, Remote Config, Inventory, Daily Rewards
- Retry policies
- RequestId
- Token refresh
