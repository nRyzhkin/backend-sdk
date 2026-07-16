using System;
using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Provides player-scoped key/value storage for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Storage is scoped to the current authenticated player and application identifier.
    /// Game code must not pass player identifiers to these methods.
    /// </remarks>
    public sealed class StorageService : IStorageService
    {
        /// <inheritdoc />
        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);
            throw new BackendNotImplementedException("Storage networking is not implemented yet.");
        }

        /// <inheritdoc />
        public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);
            throw new BackendNotImplementedException("Storage networking is not implemented yet.");
        }

        /// <inheritdoc />
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);
            throw new BackendNotImplementedException("Storage networking is not implemented yet.");
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
