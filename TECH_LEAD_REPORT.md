# Technical Lead Report

Player Economy client integration (SDK Iteration 1).

Package version: **0.6.1**

## Test Architecture

**Primary command:** `dotnet test`

Unity Test Runner is **not** used for SDK validation.

| Project | Role |
|---------|------|
| `DotNetTests~/Backend.Sdk.DotNetTests.csproj` | NUnit test entry point (`IsTestProject=true`) |
| `Shared/Backend.Sdk.Runtime/` | Compiles linked `Runtime/**/*.cs` for headless tests |
| `Shared/Backend.Sdk.UnityStubs/` | Minimal `UnityEngine` / `UnityWebRequest` stubs |
| `Shared/Backend.Sdk.Transport.Core/` | Shared transport primitives |
| `Tests~/Economy/*.cs` | Economy tests (linked into DotNetTests) |
| `Tests~/Transport/TransportTestSupport.cs` | Shared fake transport helpers (linked) |

**Test count (verified):** 57 total — 24 transport/header + 33 Economy (`TestCategory=Economy`).

```bash
dotnet restore
dotnet build
dotnet test
dotnet test --list-tests
dotnet test --filter TestCategory=Economy
```

MSB3539 is resolved via root `Directory.Build.props` (artifact paths set before `Microsoft.Common.props`).

---

## 1. Backend Endpoints Used

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/economy/{applicationId}/me` | Player JWT (`[Authorize]`) | Player economy state and active definitions |

**Not exposed in player SDK** (admin-only routes under `/admin/api/applications/{applicationId}/economy/...`).

There is **no separate player definitions endpoint**. `GetDefinitionsAsync()` reads the active catalog from `/me`.

## 2. Public API

```csharp
await Backend.InitializeAsync();
await Backend.Auth.LoginAsync();

var state = await Backend.Economy.GetStateAsync();

long gold = state.GetCurrencyBalance("gold");
bool removeAds = state.HasEntitlement("remove_ads");
long tickets = state.GetEntitlementQuantity("arena_ticket");

var definitions = await Backend.Economy.GetDefinitionsAsync();

await PerformServerAuthoritativeActionAsync();
var refreshed = await Backend.Economy.RefreshAsync();
```

Facade: `Backend.Economy` (`IEconomyService` / `EconomyService`).

## 3. `/me` Contract (verified against backend)

| Question | Answer |
|----------|--------|
| Full active catalog or player-owned only? | **Full active catalog** merged with balances/quantities |
| Currency with balance = 0? | **Yes** |
| Entitlement player does not own? | **Yes** (quantity = 0) |
| Inactive definitions? | **No** |
| `MaxBalance`? | **Yes** |
| `EntitlementKind`? | **Yes** (`"permanent"` / `"consumable"`) |
| `DisplayName`? | **Yes** |
| `Description`? | **No** (not in player response) |

`GetDefinitionsAsync()` is correct — variant A.

## 4. Cache / Session / Cancellation

- Memory cache scoped by `playerId|applicationId|serverUrl`
- Cleared on logout, session change, `ClearCache()`
- Single-flight via `SemaphoreSlim`
- Shared HTTP uses `CancellationToken.None`
- Caller token cancels waiting only
- Stale in-flight responses do not populate cache after session change (`cacheGeneration` guard)

## 5. Economy Test Cases (`dotnet test --filter TestCategory=Economy`)

### JSON / models (17)

- `GetDefinitionsAsync_MapsCurrencyDefinitions`
- `GetDefinitionsAsync_MapsEntitlementKinds`
- `GetStateAsync_MapsCurrencyBalances`
- `GetStateAsync_MapsPermanentEntitlement`
- `GetStateAsync_MapsConsumableEntitlement`
- `GetCurrencyBalance_ReturnsBalance`
- `GetCurrencyBalance_ReturnsZeroForMissingKey`
- `HasEntitlement_ReturnsTrueForPositiveQuantity`
- `HasEntitlement_ReturnsFalseForZeroOrMissing`
- `GetEntitlementQuantity_ReturnsQuantity`
- `Helpers_RejectNullOrEmptyKeys`
- `ParseSnapshot_RejectsUnknownEntitlementKind`
- `MapsLongMaxValue`
- `MapsIntegerAboveDoublePrecisionBoundary`
- `MapsNullMaxBalance`
- `Helpers_AreCaseSensitive`
- `ReturnedState_CannotMutateSdkCache`

### Cache / concurrency / session (15)

- `GetStateAsync_UsesCache`
- `GetStateAsync_ForceRefreshCallsBackendAgain`
- `RefreshAsync_ReplacesCachedState`
- `ClearCache_ForcesReload`
- `GetDefinitionsAsync_UsesSharedCacheWithState`
- `FailedRequest_IsNotCached`
- `Unauthorized_UsesExistingSdkError`
- `Cancellation_PropagatesCorrectly`
- `LogoutOrSessionChange_ClearsEconomyCache`
- `ConcurrentGetStateAsync_UsesSingleFlight`
- `ConcurrentInitialLoads_UseSingleRequest`
- `FailedInitialLoad_AllowsRetry`
- `CancelledCaller_DoesNotCancelSharedLoad`
- `InFlightRequest_FromOldSession_DoesNotPopulateNewSessionCache`
- `SharedLoadCompletion_DoesNotCacheAfterSessionChange`

### Transport header (1)

- `GetStateAsync_UsesAuthenticatedGetWithoutRequestId`

## 6. Validation Results

| Command | Result |
|---------|--------|
| `dotnet build` | Pass, no MSB3539 |
| `dotnet test` | **57/57 passed** |
| `dotnet test --filter TestCategory=Economy` | **33/33 passed** |
| Unity Test Runner | Not used |

## 7. Why Mutation Methods Were Not Added

The Unity client is untrusted. Low-level economy mutations remain admin/internal backend operations. Future gameplay uses intent-based modules (Daily Rewards, Store, Battle Pass).

## 8. Compilable Usage Example

```csharp
using System.Threading.Tasks;
using BackendSdk;
using UnityEngine;

public static class EconomyExample
{
    public static async Task RunAsync()
    {
        await Backend.InitializeAsync();
        await Backend.Auth.LoginAsync();

        var state = await Backend.Economy.GetStateAsync();
        long gold = state.GetCurrencyBalance("gold");
        bool removeAds = state.HasEntitlement("remove_ads");
        long tickets = state.GetEntitlementQuantity("arena_ticket");

        await PerformServerAuthoritativeActionAsync();
        var refreshed = await Backend.Economy.RefreshAsync();
        Debug.Log($"Refreshed at {refreshed.ServerTime:o}");
    }

    private static Task PerformServerAuthoritativeActionAsync() => Task.CompletedTask;
}
```
