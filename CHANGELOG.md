# Changelog

All notable changes to this package will be documented in this file.

The format is based on Keep a Changelog, and this package follows Semantic Versioning.

## [0.10.0] - 2026-09-22

### Added

- `Backend.CommunityTracks.DeleteAsync` — author removes their own publication (`DELETE v1/community-tracks/{applicationId}/{trackId}`).

## [0.9.0] - 2026-09-17

### Added

- `Backend.CommunityTracks` ghost APIs: `PutGhostAsync`, `ListGhostsAsync`, `GetGhostAsync` against `v1/community-tracks/{applicationId}/{trackId}/ghosts`.

## [0.8.0] - 2026-09-17

### Added

- `Backend.CommunityTracks` public UGC catalog: `ListFeedAsync`, `PublishAsync`, `GetPackageAsync` against `v1/community-tracks/{applicationId}`.

## [0.7.0] - 2026-09-16

### Added

- `Auth.CreateGuestAsync`, `Auth.LinkAsync`, and `Auth.EnsureSessionAsync` for server-issued guest accounts and platform linking.
- `IGuestCredentialStore` with PlayerPrefs default (`BackendSdk.GuestKey.{applicationId}`).
- `AuthProviders.Guest` and `LoginResult.GuestKey`.
- `PlayerProfile.PublicDataJson` is now public for round-trip profile updates.
- `Auth.LoginByUserIdAsync` and `POST /v1/auth/impersonate` for Editor login by public player id (server flag `Auth:AllowPublicIdLogin`).
- Linking a platform identity to a guest removes the guest credential. If that identity already belongs to another user, guest storage/profiles are merged into that user and the guest account is deleted.
- Login/guest/link accept optional `displayName` and `applicationId` so a placeholder nick (`Player` / `Guest-…`) is filled in at auth time.

### Changed

- Guest login never creates a user; unknown guest keys fail until `POST /v1/auth/guest`.
- Login and guest create are sent without a previous Authorization header.
- Runtime awaits no longer use `ConfigureAwait(false)` so continuations stay on the Unity/WebGL main thread.

## [0.6.1] - 2026-07-22

### Changed

- Economy tests in `Tests~/Economy/` now run through `dotnet test` via `Backend.Sdk.DotNetTests`.
- Added `Shared/Backend.Sdk.Runtime` and `Shared/Backend.Sdk.UnityStubs` for headless runtime compilation.
- MSBuild artifact paths moved to root `Directory.Build.props` (fixes MSB3539).

## [0.6.0] - 2026-07-22

### Added

- Player Economy module with `GetDefinitionsAsync`, `GetStateAsync`, `RefreshAsync`, and `ClearCache`.
- `EconomyDefinitions`, `CurrencyDefinition`, `EntitlementDefinition`, and `EntitlementKind`.
- `PlayerEconomyState` with `GetCurrencyBalance`, `HasEntitlement`, and `GetEntitlementQuantity` helpers.
- `PlayerCurrencyBalance` and `PlayerEntitlement` immutable models.
- Internal `EconomyJson` parser for `GET /v1/economy/{applicationId}/me`.
- In-memory cache with single-flight loading for definitions and player state.
- Economy cache invalidation on logout and session change.
- Editor tests in `Tests~/Economy/` and transport header tests for economy GET.

## [0.5.0] - 2026-07-22

### Added

- Player Profiles module with `GetMeAsync`, `UpdateMeAsync`, `GetAsync`, and `GetBatchAsync`.
- `PlayerProfile` immutable model with typed `GetPublicData<T>()`.
- `PlayerProfileBatchResult` with `Profiles`, `MissingUserIds`, `ByUserId`, and `TryGetProfile`.
- `ProfilesService.MaxBatchSize = 100`.
- Internal `ProfileJson` parser/serializer for profile wire format.
- Anonymous transport helpers on `BackendClient` (`GetRawAnonymousAsync`, `PostJsonAnonymousAsync`).
- `BackendClient.PutJsonAsync` for authenticated PUT with pre-built JSON bodies.
- Editor tests in `Tests~/Profiles/`.

## [0.4.0] - 2026-07-22

### Added

- Remote Config module with `GetAsync`, `GetAsync<T>`, and `GetAllAsync`.
- `RemoteConfigValue` for arbitrary JSON values.
- Internal `RemoteConfigJson` parser for backend wire formats.
- Editor tests in `Tests~/RemoteConfig/`.

## [0.3.0] - 2026-07-22

### Added

- Analytics module with `Backend.Analytics.TrackAsync(eventName, parameters)`.
- Internal analytics JSON builder for arbitrary event parameters.
- `BackendClient.PostJsonAsync` for pre-built JSON POST bodies.

## [0.2.1] - 2026-07-16

### Added

- Transport-level `X-Request-Id` for POST/PUT/DELETE.
- Automatic retry for transient failures with stable RequestId across attempts.
- `RetryCount` and `RetryDelayMilliseconds` settings (defaults: 2 and 500).

## [0.2.0] - 2026-07-16

### Added

- Working Auth networking with session ownership and automatic Bearer token injection.
- Working Storage networking with automatic ApplicationId path insertion.
- Working Leaderboards networking (`SubmitAsync`, `GetTopAsync`, `GetAroundPlayerAsync`).
- `BackendException.StatusCode` and `BackendException.ServerError`.

### Changed

- Initialization now validates Backend URL before creating the transport.

## [0.1.0] - 2026-07-16

### Added

- Initial UPM package scaffold for Backend SDK.
- Core runtime facade, options, settings, exceptions, and request result types.
- Internal UnityWebRequest transport with async/await, JSON serialization, timeout, cancellation, and authorization header support.
- Project Settings integration for configuring backend runtime settings.
