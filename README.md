# Backend SDK

Backend SDK is a lightweight Unity Package Manager package that provides a game-facing API for backend-powered features.

Game code talks to services. It never thinks about HTTP, JSON, JWT, URLs, or ApplicationId.

## Goals

- Unity 6 compatible
- UPM-first package layout
- No third-party dependencies
- Async/await-based runtime APIs
- UnityWebRequest transport
- Lightweight, readable architecture
- Easy to extend with new modules

## Public API Philosophy

```csharp
using BackendSdk;

await Backend.InitializeAsync();
await Backend.Auth.LoginAsync();
await Backend.Storage.SetAsync("Save", save);
await Backend.Leaderboards.SubmitAsync("highscore", 1200, SortMode.Descending);
await Backend.Analytics.TrackAsync("LevelStarted", new { level = 5, difficulty = "Hard" });
```

Not:

```csharp
Backend.Post(...);
Backend.Get(...);
```

## Quick Start

1. Open `Project Settings > Backend`.
2. Set Backend URL, Application ID, and optional development credentials.
3. Initialize and use services:

```csharp
await Backend.InitializeAsync();

var apiUrl =
    await Backend.RemoteConfig.GetAsync<string>("apiUrl");

var cdnUrl =
    await Backend.RemoteConfig.GetAsync<string>("cdnUrl");

var maintenance =
    await Backend.RemoteConfig.GetAsync<bool>("maintenance");

// Editor development flow
await Backend.Auth.LoginAsync();

// Or explicit provider credentials from platform SDKs
await Backend.Auth.LoginAsync(new LoginRequest
{
    Provider = "crazygames",
    ExternalId = crazyGamesUserId
});

await Backend.Storage.SetAsync("Save", save);
var save = await Backend.Storage.GetAsync<MySave>("Save");

await Backend.Leaderboards.SubmitAsync("highscore", 1500, SortMode.Descending);
var top = await Backend.Leaderboards.GetTopAsync("highscore", limit: 50);
var around = await Backend.Leaderboards.GetAroundPlayerAsync("highscore", range: 5);

await Backend.Analytics.TrackAsync(
    "LevelStarted",
    new
    {
        level = 5,
        difficulty = "Hard"
    });

await Backend.Analytics.TrackAsync("TutorialCompleted");
```

## Initialization Flow

`Backend.InitializeAsync()`:

1. Loads Project Settings from the runtime JSON resource.
2. Caches Backend URL and Application ID in `Backend.Settings`.
3. Creates the internal transport and `BackendClient`.
4. Makes service facades ready for use.
5. Runs only once.

## Authentication Flow

1. Game calls `Backend.Auth.LoginAsync()` or `LoginAsync(LoginRequest)`.
2. SDK posts credentials to the backend.
3. `AuthService` stores `PlayerSession` with access token and expiration.
4. Later requests automatically include `Authorization: Bearer <AccessToken>`.
5. Game code never passes tokens.

`Backend.Auth.Session` is read-only. Only the SDK can create or clear the session.

`LogoutAsync()` clears the local session.

## Automatic Token Injection

`BackendClient` asks `AuthService` for the current authorization header on every request.

Services such as Storage and Leaderboards never assemble tokens themselves.

## Automatic ApplicationId Usage

Configured Application ID is inserted into Storage, Leaderboards, and Analytics URLs automatically.

Game code never passes ApplicationId.

## RequestId And Retry

Mutating requests (`POST`, `PUT`, `DELETE`) automatically receive an `X-Request-Id` header.

GET requests do not.

If a request fails with a transient error (`BackendException.IsTransient == true`), the transport retries it with the **same** RequestId.

Defaults:

- `RetryCount = 2` (first attempt + 2 retries)
- `RetryDelayMilliseconds = 500` (constant delay)

Retry lives only in `UnityWebRequestTransport`. Auth, Storage, and Leaderboards do not implement retry logic.

Game code stays unchanged:

```csharp
await Backend.Storage.SetAsync("Save", save);
await Backend.Leaderboards.SubmitAsync("highscore", 1500, SortMode.Descending);
```

## Implemented Modules

### Auth

- `LoginAsync()`
- `LoginAsync(LoginRequest)`
- `LogoutAsync()`
- `Session`
- `IsAuthenticated`

### Storage

- `SetAsync<T>(key, value)`
- `GetAsync<T>(key)`
- `DeleteAsync(key)`

### Leaderboards

- `SubmitAsync(leaderboardName, value, sortMode)`
- `GetTopAsync(leaderboardName, limit = 100)`
- `GetAroundPlayerAsync(leaderboardName, range = 5)`

### Analytics

- `TrackAsync(eventName, parameters = null)`

```csharp
await Backend.Analytics.TrackAsync(
    "LevelStarted",
    new
    {
        level = 5
    });

await Backend.Analytics.TrackAsync("TutorialCompleted");
```

Requires authentication. ApplicationId and JWT are added automatically.

### Remote Config

- `GetAsync(key)` → `RemoteConfigValue`
- `GetAsync<T>(key)`
- `GetAllAsync()`

```csharp
await Backend.InitializeAsync();

var apiUrl = await Backend.RemoteConfig.GetAsync<string>("apiUrl");
var maintenance = await Backend.RemoteConfig.GetAsync<bool>("maintenance");

var settings = await Backend.RemoteConfig.GetAsync<GameSettings>("gameSettings");

var config = await Backend.RemoteConfig.GetAllAsync();
var cdnUrl = config["cdnUrl"].As<string>();
```

Remote Config:

- does **not** require authentication
- is available after `Backend.InitializeAsync()`
- is read-only on the Game API
- is managed through the backend Admin Panel
- supports arbitrary JSON values
- must **not** store secrets, passwords, or private API keys
- uses Application ID from SDK configuration automatically

## Current Public Surface

- `Backend`
- `BackendOptions`
- `BackendSettings`
- `BackendException`
- `BackendNotImplementedException`
- `RequestResult<T>`
- Authentication: `IAuthService`, `AuthService`, `LoginRequest`, `LoginResult`, `PlayerSession`
- Storage: `IStorageService`, `StorageService`
- Leaderboards: `ILeaderboardsService`, `LeaderboardsService`, `SortMode`, `LeaderboardEntry`, `LeaderboardSubmitResult`, `LeaderboardAroundResult`
- Analytics: `IAnalyticsService`, `AnalyticsService`
- Remote Config: `IRemoteConfigService`, `RemoteConfigService`, `RemoteConfigValue`
- Placeholder facades: Friends, Inventory

## Error Handling

HTTP failures become `BackendException` with:

- `Message`
- `StatusCode`
- `ServerError`
- `ErrorCode`
- `IsTransient`

`UnityWebRequest` is never exposed to game code.

## Project Settings

`Project Settings > Backend` stores:

- Backend URL
- Application ID
- Request Timeout
- Enable Logging
- Retry Count
- Retry Delay (ms)
- Development Mode
- Development Provider
- Development External ID

Development mode allows parameterless `Backend.Auth.LoginAsync()` in the Unity Editor.

## Folder Structure

```text
Runtime/
|- Auth/
|- Storage/
|- Leaderboards/
|- Analytics/
|- RemoteConfig/
|- Internal/
|- Backend.cs
|- BackendException.cs
|- ...
Editor/
Documentation~/
Samples~/
```

## Remaining TODOs

- Analytics queue / batch / offline delivery
- Remote Config caching / background refresh
- Friends
- Remote Config
- Inventory
- Daily Rewards
- Token refresh
- Server-side logout
- Offline queue / retry across app restarts (explicitly out of scope for current transport retry)

## Samples and Documentation

- `Documentation~/Architecture.md`
- `Samples~/GettingStarted/README.md`
- `TECH_LEAD_REPORT.md`
