# Changelog

All notable changes to this package will be documented in this file.

The format is based on Keep a Changelog, and this package follows Semantic Versioning.

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
