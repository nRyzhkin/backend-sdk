# Architecture

## Design Principles

Backend SDK is structured around a simple rule: game code consumes high-level services, while backend transport stays internal.

That rule drives the current architecture:

- `Backend` is the public composition root.
- Public service facades provide stable API entry points.
- `BackendClient` and the transport layer stay internal.
- Configuration is edited in the Unity Editor and mirrored into a runtime resource.

## Runtime Layers

### Public Facade

The public facade is intentionally small:

- `Backend`
- `BackendOptions`
- `BackendSettings`
- `BackendException`
- `RequestResult<T>`

Future modules should extend the facade through service-specific methods, not through generic transport APIs.

### Internal Infrastructure

The internal infrastructure handles:

- Request construction
- JSON serialization
- Authorization headers
- Timeout handling
- Cancellation
- Future retry composition

By hiding these details behind `IBackendTransport`, new modules can share one networking implementation without coupling gameplay code to HTTP.

## Extension Strategy

When adding a new module:

1. Keep the public entry point under `Backend.<Service>`.
2. Put transport-facing logic behind internal classes.
3. Keep request and response DTOs close to the module that owns them.
4. Avoid broad shared abstractions until multiple modules truly need them.
5. Add retries or other cross-cutting behavior as transport decorators, not as public API features.

## Editor Settings Flow

The editor settings page writes project-level configuration into:

- `ProjectSettings/BackendSdkSettings.json`

It also mirrors the runtime subset into:

- `Assets/Resources/BackendSdkSettings.json`

This keeps the authoring experience simple while allowing `Backend.InitializeAsync()` to load configuration in play mode and in builds.
