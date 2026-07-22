using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides read-only access to the authenticated player's economy state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The backend is the source of truth for balances and entitlements. This module does not expose
    /// low-level grant, spend, set, consume, or revoke operations.
    /// </para>
    /// <para>
    /// Definitions and player state are cached in memory. Call <see cref="RefreshAsync"/> after
    /// server-authoritative gameplay actions complete.
    /// </para>
    /// <para>
    /// In-flight shared loads use <see cref="CancellationToken.None"/> for the transport call so one
    /// caller's cancellation does not abort the shared HTTP request for other waiters. The caller's
    /// <paramref name="cancellationToken"/> cancels waiting only.
    /// </para>
    /// </remarks>
    public sealed class EconomyService : IEconomyService
    {
        private readonly SemaphoreSlim loadGate = new SemaphoreSlim(1, 1);

        private PlayerEconomyState cachedState;
        private EconomyDefinitions cachedDefinitions;
        private string cachedSessionKey;
        private int cacheGeneration;

        /// <inheritdoc />
        public async Task<EconomyDefinitions> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            var snapshot = await LoadSnapshotAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
            return snapshot.Definitions;
        }

        /// <inheritdoc />
        public async Task<PlayerEconomyState> GetStateAsync(
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await LoadSnapshotAsync(forceRefresh, cancellationToken).ConfigureAwait(false);
            return snapshot.State;
        }

        /// <inheritdoc />
        public Task<PlayerEconomyState> RefreshAsync(CancellationToken cancellationToken = default)
        {
            return GetStateAsync(forceRefresh: true, cancellationToken);
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            cacheGeneration++;
            cachedState = null;
            cachedDefinitions = null;
            cachedSessionKey = null;
        }

        private async Task<EconomyJson.EconomySnapshot> LoadSnapshotAsync(
            bool forceRefresh,
            CancellationToken cancellationToken)
        {
            GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            InvalidateCacheIfSessionChanged();

            if (!forceRefresh && cachedState != null && cachedDefinitions != null)
            {
                return new EconomyJson.EconomySnapshot(cachedState, cachedDefinitions);
            }

            await loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                InvalidateCacheIfSessionChanged();

                if (!forceRefresh && cachedState != null && cachedDefinitions != null)
                {
                    return new EconomyJson.EconomySnapshot(cachedState, cachedDefinitions);
                }

                var generationAtStart = cacheGeneration;
                var sessionKeyAtStart = BuildSessionCacheKey();

                var client = Backend.ClientOrThrow();
                var responseJson = await client.GetRawAsync(
                    BuildMePath(client),
                    CancellationToken.None).ConfigureAwait(false);

                var snapshot = EconomyJson.ParseSnapshot(responseJson);

                if (generationAtStart == cacheGeneration
                    && string.Equals(sessionKeyAtStart, BuildSessionCacheKey(), StringComparison.Ordinal))
                {
                    cachedState = snapshot.State;
                    cachedDefinitions = snapshot.Definitions;
                    cachedSessionKey = sessionKeyAtStart;
                }

                return snapshot;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                ClearCache();
                throw;
            }
            finally
            {
                loadGate.Release();
            }
        }

        private void InvalidateCacheIfSessionChanged()
        {
            var currentSessionKey = BuildSessionCacheKey();
            if (!string.IsNullOrEmpty(cachedSessionKey)
                && !string.Equals(cachedSessionKey, currentSessionKey, StringComparison.Ordinal))
            {
                ClearCache();
            }
        }

        private static string BuildSessionCacheKey()
        {
            var session = Backend.Auth.Session;
            var settings = Backend.Settings;
            var applicationId = settings?.ApplicationId ?? string.Empty;
            var serverUrl = settings?.ServerUrl ?? string.Empty;
            if (session == null || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                return string.Empty;
            }

            return session.PlayerId + "|" + applicationId + "|" + serverUrl;
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "Economy requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildMePath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/economy/{applicationId}/me";
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }
    }
}
