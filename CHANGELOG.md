# Changelog

All notable changes to this package will be documented in this file.

The format is based on Keep a Changelog, and this package follows Semantic Versioning.

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
