# Technical Lead Report

CancellationToken hardening in the transport layer.

## 1. Files Changed

| File | Change |
|------|--------|
| `Runtime/Internal/UnityWebRequestTransport.cs` | Explicit cancellation checks before each attempt and before retry; cancellation logging; ensure `OperationCanceledException` is never wrapped or retried |
| `TECH_LEAD_REPORT.md` | This report |

Unchanged:

- `BackendClient`
- `AuthService`
- `StorageService`
- `LeaderboardsService`
- Public SDK API
- `UnityWebRequestExtensions` (already aborts the in-flight request and completes the Task as canceled)

## 2. How Cancellation Works Now

For every transport `SendAsync` call:

1. **Before each attempt**  
   `cancellationToken.ThrowIfCancellationRequested()` runs before creating/sending the HTTP request.

2. **During the HTTP request**  
   `SendWebRequestAsync` aborts `UnityWebRequest` when the token is canceled and completes the await with `OperationCanceledException` / `TaskCanceledException`.

3. **After a transient failure, before retry**  
   The transport checks the token again. If canceled, it does **not** log a retry and does **not** start another attempt.

4. **During retry delay**  
   `await Task.Delay(RetryDelayMilliseconds, cancellationToken)` is used. If canceled during the wait, `Task.Delay` throws `OperationCanceledException`, which is not caught for wrapping — only logged, then rethrown.

5. **Logging**  
   When `EnableLogging` is on, cancellation logs:

   ```text
   [Backend SDK] Request cancelled. PUT https://.../v1/storage/... RequestId=... Attempt=2
   ```

   RequestId is included only when present (mutating requests).

## 3. Why OperationCanceledException Is Not Wrapped In BackendException

Cancellation is a caller decision, not a backend failure.

Wrapping it in `BackendException` would:

- hide the cancellation signal from game code
- risk treating it as `IsTransient == true`
- trigger unwanted retries

Therefore:

- `SendOnceAsync` catches `OperationCanceledException` before the generic `Exception` handler and rethrows it unchanged
- `SendAsync` catches it only to log (when enabled), then rethrows

## 4. Why Retry Stops Correctly After Cancellation

Retry only happens in:

```csharp
catch (BackendException exception) when (exception.IsTransient && attempt < maxAttempts)
```

`OperationCanceledException` is not a `BackendException`, so it never enters that branch.

Additionally, before delay/next attempt the transport calls `ThrowIfCancellationRequested()`, and `Task.Delay(..., cancellationToken)` itself cancels. Both paths rethrow `OperationCanceledException` after optional logging, so no further HTTP attempt is made.

## 5. Scenario Verification

| Scenario | Expected behavior |
|----------|-------------------|
| Cancel before first attempt | `ThrowIfCancellationRequested` throws; log; no HTTP request; no retry |
| Cancel during HTTP request | request aborted; OCE propagates; log; no retry |
| Cancel during `Task.Delay` between retries | delay throws OCE; log; no next attempt |
| Successful request without cancel | normal success path; no cancellation log |

## 6. Public API Confirmation

No public API changes.

Game code still uses existing service methods and existing optional `CancellationToken` parameters. Callers continue to observe raw `OperationCanceledException` on cancel.
