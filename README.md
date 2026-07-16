# Backend SDK

Backend SDK is a lightweight Unity Package Manager package that provides a clean, high-level foundation for backend-powered game features.

The package is intentionally designed so game code talks to services, not transport primitives. HTTP, serialization, request plumbing, and future retry policies stay inside the package.

## Goals

- Unity 6 compatible
- UPM-first package layout
- No third-party dependencies
- Async/await-based runtime APIs
- UnityWebRequest transport
- Lightweight, readable architecture
- Easy to extend with new modules

## Public API Philosophy

Game code should depend on domain services:

```csharp
using BackendSdk;

await Backend.InitializeAsync();
```

It should not call low-level HTTP methods such as `Get`, `Post`, `Put`, or `Delete`.

This package keeps transport concerns internal so future services such as authentication, storage, leaderboards, analytics, friends, remote config, inventory, and daily rewards can evolve without leaking networking details into gameplay code.

## Current Public Surface

- `Backend`
- `BackendOptions`
- `BackendSettings`
- `BackendException`
- `BackendNotImplementedException`
- `RequestResult<T>`
- Authentication:
  - `IAuthService`
  - `AuthService`
  - `LoginRequest`
  - `LoginResult`
  - `PlayerSession`
- Storage:
  - `IStorageService`
  - `StorageService`
- Placeholder service facades:
  - `Backend.Leaderboards`
  - `Backend.Analytics`
  - `Backend.RemoteConfig`
  - `Backend.Friends`
  - `Backend.Inventory`

## Authentication

Authentication is provider-agnostic. The SDK only understands `Provider` and `ExternalId`; platform-specific SDKs remain in game code.

```csharp
await Backend.InitializeAsync();

// Editor development flow
var devLogin = await Backend.Auth.LoginAsync();

// Explicit provider flow
var login = await Backend.Auth.LoginAsync(new LoginRequest
{
    Provider = "crazygames",
    ExternalId = crazyGamesUserId
});

var session = Backend.Auth.Session;
var isAuthenticated = Backend.Auth.IsAuthenticated;

await Backend.Auth.LogoutAsync();
```

`Backend.Auth.Session` is read-only. Game code can inspect the current session but cannot assign or modify it.

The parameterless `LoginAsync()` overload is only available when development mode is enabled and the game is running in the Unity Editor. It uses the development credentials configured in Project Settings.

Networking for authentication is not implemented yet. The public contract is established and methods throw `BackendNotImplementedException` once request validation completes.

## Storage

Storage is player-scoped key/value storage. Values are always scoped to the current authenticated player and application identifier. Game code must never pass a player identifier.

The public method names are stable:

- `SetAsync<T>()`
- `GetAsync<T>()`
- `DeleteAsync()`

```csharp
await Backend.Storage.SetAsync("Save", save);

var save = await Backend.Storage.GetAsync<MySave>("Save");

await Backend.Storage.DeleteAsync("Save");
```

Storage networking is not implemented yet. The public contract is established and methods throw `BackendNotImplementedException` after validation.

## Architecture

The package is split into a small public facade and an internal infrastructure layer.

### Public Layer

- `Backend` is the only runtime entry point.
- Service placeholders are stable facade types intended to grow over time.
- Public result and exception types expose SDK-level outcomes, not HTTP status codes or raw `UnityWebRequest` objects.

### Internal Layer

- `BackendClient` coordinates SDK infrastructure and future modules.
- `IBackendTransport` defines the internal request contract.
- `UnityWebRequestTransport` owns request construction, headers, serialization, timeout, cancellation, and response handling.
- `RuntimeSettingsLoader` loads runtime configuration generated from the Project Settings page.

This makes future retry support straightforward: add a transport decorator or policy layer around `IBackendTransport` without changing the public API.

## Folder Structure

```text
Backend SDK/
|- package.json
|- README.md
|- CHANGELOG.md
|- LICENSE.md
|- Runtime/
|  |- Backend.Runtime.asmdef
|  |- Backend.cs
|  |- BackendOptions.cs
|  |- BackendSettings.cs
|  |- BackendException.cs
|  |- RequestResult.cs
|  |- BackendPlaceholders.cs
|  |- Auth/
|  |  |- LoginRequest.cs
|  |  |- LoginResult.cs
|  |  |- PlayerSession.cs
|  |  |- IAuthService.cs
|  |  `- AuthService.cs
|  |- Storage/
|  |  |- IStorageService.cs
|  |  `- StorageService.cs
|  `- Internal/
|     |- BackendClient.cs
|     |- IBackendTransport.cs
|     |- HttpVerb.cs
|     |- RuntimeSettingsLoader.cs
|     |- UnityJsonSerializer.cs
|     |- UnityWebRequestExtensions.cs
|     `- UnityWebRequestTransport.cs
|- Editor/
|  |- Backend.Editor.asmdef
|  |- BackendProjectSettings.cs
|  `- BackendSettingsProvider.cs
|- Documentation~/
`- Samples~/
```

## Project Settings

The package adds a Project Settings page at `Project/Backend`.

It stores:

- Backend URL
- Application ID
- Request Timeout
- Enable Logging
- Development Mode
- Development Provider
- Development External ID

When saved, the editor mirrors those values into a runtime JSON resource at `Assets/Resources/BackendSdkSettings.json`, which allows `Backend.InitializeAsync()` to work without manual plumbing.

Development mode is intended for local Editor workflows. When enabled, `Backend.Auth.LoginAsync()` can authenticate using the configured development provider credentials without requiring game code to supply them.

## Adding Future Modules

Keep new modules consistent with the current shape:

1. Add a public facade type in `Runtime/` if the module needs a user-facing entry point.
2. Add internal request/response DTOs and service implementation code under `Runtime/Internal/Modules/<ModuleName>/` or a similarly focused folder.
3. Route backend communication through `BackendClient` and `IBackendTransport`.
4. Return SDK-level models, `RequestResult<T>`, or domain-specific results rather than raw transport data.
5. Only expose gameplay-meaningful methods on `Backend.<Service>`.

Example direction for authentication:

```csharp
await Backend.InitializeAsync();
var login = await Backend.Auth.LoginAsync(new LoginRequest
{
    Provider = "crazygames",
    ExternalId = crazyGamesUserId
});
```

Not:

```csharp
await Backend.Post<AuthRequest, AuthResponse>("auth/anonymous", request);
```

## Coding Conventions

- Prefer small, explicit classes over generic abstractions.
- Keep transport code internal.
- Use async/await instead of callbacks.
- Pass `CancellationToken` through all infrastructure and service operations.
- Favor immutable runtime settings and mutable initialization options.
- Use XML documentation on public types.
- Avoid reflection, service locators, and DI frameworks.

## Serialization Notes

The initial implementation uses Unity's built-in `JsonUtility` to avoid third-party dependencies and keep AOT behavior predictable.

As new modules are added, request and response DTOs should be designed to work cleanly with `JsonUtility`:

- Mark DTOs with `[System.Serializable]`
- Prefer fields or simple properties that Unity serialization supports
- Avoid complex polymorphic payloads in the core package

## Samples and Documentation

- `Documentation~/Architecture.md` describes the package structure and extension strategy.
- `Samples~/GettingStarted/README.md` shows the intended initialization flow.

## Status

This package provides the core infrastructure plus public contracts for authentication and storage.

Implemented:

- SDK initialization and internal transport scaffold
- Authentication public API and development-mode configuration
- Storage public API contract

Not yet implemented:

- Authentication networking
- Storage networking
- Leaderboards, analytics, friends, remote config, inventory, and daily rewards
