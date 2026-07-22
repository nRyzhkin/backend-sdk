using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides read-only remote config operations for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Remote config does not require authentication. Application ID is taken from SDK configuration automatically.
    /// Values are managed through the backend Admin Panel and must not contain secrets.
    /// </remarks>
    public sealed class RemoteConfigService : IRemoteConfigService
    {
        /// <inheritdoc />
        public async Task<RemoteConfigValue> GetAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var client = GetClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);

            var applicationId = client.ApplicationIdOrThrow();
            var responseJson = await client.GetRawAsync(BuildEntryPath(client, key), cancellationToken).ConfigureAwait(false);
            var valueJson = RemoteConfigJson.ExtractValueJson(responseJson, applicationId, key);
            return new RemoteConfigValue(valueJson);
        }

        /// <inheritdoc />
        public async Task<T> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
        {
            var value = await GetAsync(key, cancellationToken).ConfigureAwait(false);
            return RemoteConfigJson.DeserializeValue<T>(value.RawJson, Backend.Settings.ApplicationId, key);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, RemoteConfigValue>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var client = GetClient();
            cancellationToken.ThrowIfCancellationRequested();

            var applicationId = client.ApplicationIdOrThrow();
            var responseJson = await client.GetRawAsync(BuildAllPath(client), cancellationToken).ConfigureAwait(false);
            return RemoteConfigJson.ParseAll(responseJson, applicationId);
        }

        private static BackendClient GetClient()
        {
            EnsureInitialized();
            return Backend.ClientOrThrow();
        }

        private static string BuildAllPath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/remote-config/{applicationId}";
        }

        private static string BuildEntryPath(BackendClient client, string key)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            var escapedKey = Uri.EscapeDataString(key);
            return $"v1/remote-config/{applicationId}/{escapedKey}";
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Remote config key is required.", nameof(key));
            }
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
