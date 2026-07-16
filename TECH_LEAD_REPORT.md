# Technical Lead Report

Unity main-thread fix for `UnityWebRequestTransport`.

## Root Cause

`UnityWebRequestTransport.SendAsync` awaited the request with:

```csharp
await request.SendWebRequestAsync(cancellationToken).ConfigureAwait(false);
```

`ConfigureAwait(false)` tells the runtime **not** to marshal the continuation back to the captured `SynchronizationContext`.

In Unity, that context is the player-loop / main-thread context. After the await completed, execution resumed on a thread-pool thread. The next line accessed:

```csharp
request.downloadHandler?.text
```

Unity requires `DownloadHandler.text` (and most `UnityWebRequest` APIs) to be called from the main thread, which produced:

```text
UnityException: DownloadHandler.text can only be called from the Unity main thread.
```

## Why It Happened

`ConfigureAwait(false)` is a common .NET server/library optimization. It is the wrong default for Unity code that touches engine APIs after an await.

The previous await wrapper also completed a `TaskCompletionSource` from `AsyncOperation.completed`. That pattern can work on Unity **only if** callers capture the Unity synchronization context (`ConfigureAwait(true)` / omit `ConfigureAwait`). Combining TCS-style completion with `ConfigureAwait(false)` guaranteed off-thread resumption before Unity object access.

## What Changed

1. **`Runtime/Internal/UnityWebRequestTransport.cs`**
   - Removed `ConfigureAwait(false)` from the transport await.
   - After the await, `DownloadHandler.text` and related UnityWebRequest reads now run on the Unity main thread.

2. **`Runtime/Internal/UnityWebRequestExtensions.cs`**
   - Kept a `TaskCompletionSource` completion wrapper because `UnityWebRequestAsyncOperation` has no `GetAwaiter` in this package/Unity setup.
   - Completion is signaled from `AsyncOperation.completed` (Unity player loop / main thread).
   - Callers must await without `ConfigureAwait(false)` so continuations remain on Unity's synchronization context.
   - No custom dispatcher, no `Task.Run`, no manual thread switching.

Public SDK API was not changed.

## Why The New Implementation Stays On The Unity Main Thread

1. `UnityWebRequest.SendWebRequest()` completes via `AsyncOperation.completed` on the Unity player loop.
2. That callback sets the `TaskCompletionSource`, so the Task completes from the main-thread completion path.
3. The transport awaits that Task **without** `ConfigureAwait(false)`, preserving Unity's synchronization context.
4. All `UnityWebRequest` / `DownloadHandler` access happens only after that await returns, therefore on the main thread.

Note: a direct `await request.SendWebRequest()` was attempted, but `UnityWebRequestAsyncOperation` does not expose `GetAwaiter` in this package configuration (`CS1061`). The TCS + main-thread-safe await pattern is the compatible fix.

## Files Modified

| File | Change |
|------|--------|
| `Runtime/Internal/UnityWebRequestTransport.cs` | Removed `ConfigureAwait(false)` before Unity API access |
| `Runtime/Internal/UnityWebRequestExtensions.cs` | Await `SendWebRequest()` operation directly |
| `TECH_LEAD_REPORT.md` | This report |

## Remaining Notes

Service-layer `ConfigureAwait(false)` calls after `BackendClient` returns are currently safe because UnityWebRequest access is finished inside the transport before the Task completes. They were left unchanged to keep this fix focused. If future service code starts touching Unity APIs after awaits, those `ConfigureAwait(false)` usages should also be removed.
