using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides player-scoped key/value storage for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Storage is scoped to the current authenticated player and application identifier.
    /// Game code must not pass player identifiers or application identifiers to these methods.
    /// </remarks>
    public sealed class StorageService : IStorageService
    {
        /// <inheritdoc />
        public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);

            var body = new StorageValueDto
            {
                value = SerializeValue(value)
            };

            await client.PutAsync<StorageValueDto, StorageValueDto>(
                BuildPath(client, key),
                body,
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);

            var response = await client.GetAsync<StorageValueDto>(
                BuildPath(client, key),
                cancellationToken).ConfigureAwait(false);

            return DeserializeValue<T>(response?.value);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);

            await client.DeleteAsync(BuildPath(client, key), cancellationToken).ConfigureAwait(false);
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "Storage requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildPath(BackendClient client, string key)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            var escapedKey = Uri.EscapeDataString(key);
            return $"v1/storage/{applicationId}/{escapedKey}";
        }

        private static string SerializeValue<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string stringValue)
            {
                return stringValue;
            }

            return UnityJsonSerializer.Serialize(value);
        }

        private static T DeserializeValue<T>(string raw)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)(raw ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return default;
            }

            return UnityJsonSerializer.Deserialize<T>(raw);
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new BackendException("Storage key is required.", "invalid_storage_key");
            }
        }
    }
}
