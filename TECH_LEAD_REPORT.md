# Technical Lead Report

API stabilization pass after initial authentication and storage contract review.

## Iteration Summary

This iteration refined the public SDK contract before backend implementation begins. No networking, JWT, login, or storage behavior was added.

## 1. Files Created

| File | Purpose |
|------|---------|
| `Runtime/BackendNotImplementedException.cs` | SDK-specific exception for unimplemented public operations |

## 2. Files Modified

| File | Change |
|------|--------|
| `Runtime/Auth/AuthService.cs` | Read-only session exposure, internal session mutation methods, `LogoutAsync`, `BackendNotImplementedException` |
| `Runtime/Auth/IAuthService.cs` | Documented read-only session contract, added `LogoutAsync` |
| `Runtime/Auth/PlayerSession.cs` | Documented immutability |
| `Runtime/Storage/StorageService.cs` | Replaced raw `NotImplementedException` with `BackendNotImplementedException` |
| `Runtime/Storage/IStorageService.cs` | Documented stable storage method names |
| `README.md` | Updated public API documentation |
| `TECH_LEAD_REPORT.md` | Updated with this iteration |

## 3. Exactly What Changed

### Read-only session

- `AuthService.Session` is now exposed through a get-only property backed by a private field.
- Game code cannot assign to `Backend.Auth.Session`.
- Internal session mutation is limited to:
  - `AuthService.SetSession(PlayerSession value)`
  - `AuthService.ClearSession()`
- `PlayerSession` remains immutable with get-only properties and no public setters.

### Logout contract

Added to `IAuthService` and `AuthService`:

```csharp
Task LogoutAsync(CancellationToken cancellationToken = default);
```

Current behavior:

- Validates SDK initialization.
- Honors cancellation.
- Throws `BackendNotImplementedException` because logout networking is not implemented yet.

### Storage API naming frozen

Confirmed and documented the stable public storage API:

- `SetAsync<T>()`
- `GetAsync<T>()`
- `DeleteAsync()`

No alternate names such as `SaveAsync`, `LoadAsync`, or `PutAsync` were introduced.

### SDK-specific exceptions

Added:

```csharp
public sealed class BackendNotImplementedException : BackendException
```

Used by unimplemented public auth and storage methods so Unity logs clearly identify SDK-not-implemented failures.

Error code: `not_implemented`.

## 4. Public API Changes

Additive only:

- `BackendNotImplementedException`
- `IAuthService.LogoutAsync()`
- `AuthService.LogoutAsync()`

Clarified behavior:

- `Backend.Auth.Session` is read-only from game code.
- Storage method names are stable public API.

No existing public members were renamed or removed.

## 5. Architectural Decisions Made

1. **Private session field with get-only property**
   - Minimal change that prevents game code from replacing the current session.
   - Keeps future login/logout implementation inside `AuthService`.

2. **`BackendNotImplementedException` inherits `BackendException`**
   - Matches the existing lightweight exception model.
   - Keeps one SDK exception hierarchy instead of introducing a parallel pattern.

3. **Logout added before implementation**
   - Stabilizes the auth lifecycle contract before backend work starts.

## 6. Architectural Concerns Remaining

1. **Development settings in runtime builds**
   - Development credentials are still mirrored into runtime JSON.
   - Parameterless login remains Editor-only, but explicit login can still be called in builds.

2. **Session lifecycle details still undefined**
   - `SetSession` and `ClearSession` exist internally, but login/logout behavior is not implemented.
   - Token refresh remains out of scope.

3. **Storage auth enforcement deferred**
   - Storage still does not require `Backend.Auth.IsAuthenticated` before throwing `BackendNotImplementedException`.
   - This should be enforced when storage networking is implemented.

4. **JsonUtility constraints unchanged**
   - DTO serialization limitations remain the same.

## 7. Breaking API Change Assessment

No breaking public API changes are expected before server implementation, assuming the current contract is accepted:

- Auth method signatures are stable.
- Storage method names are frozen.
- Session exposure is now explicitly read-only, which is a tightening rather than a breaking rename/removal.
- `BackendNotImplementedException` replaces raw `NotImplementedException` in public SDK methods. This is behavior-compatible for callers using `catch (Exception)` but improves log clarity for SDK-specific handling.

## 8. Things Intentionally Left Unimplemented

- Authentication networking
- Logout networking
- Session assignment after successful login
- Token refresh
- Storage networking
- Player/application scoping logic inside storage transport
- Internal auth module under `Runtime/Internal/Modules/Auth`

## 9. Recommendations for Next Iteration

1. Implement `LoginAsync(LoginRequest)` and map the backend response to `PlayerSession` via `SetSession`.
2. Implement `LogoutAsync()` and clear session via `ClearSession`.
3. Enforce authenticated storage access when storage networking begins.
4. Review whether development settings should be excluded from build artifacts.

No broader architectural redesign was introduced in this iteration.
