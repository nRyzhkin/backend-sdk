using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Defines the public remote config service contract.
    /// </summary>
    /// <remarks>
    /// Remote config is read-only, does not require authentication, and uses the configured Application ID automatically.
    /// </remarks>
    public interface IRemoteConfigService
    {
        /// <summary>
        /// Gets a remote config value as an arbitrary JSON value.
        /// </summary>
        /// <param name="key">The config key.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The remote config value.</returns>
        Task<RemoteConfigValue> GetAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a remote config value deserialized to the requested type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="key">The config key.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The remote config value.</returns>
        Task<T> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all remote config values for the configured application.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A dictionary of config keys and values.</returns>
        Task<Dictionary<string, RemoteConfigValue>> GetAllAsync(
            CancellationToken cancellationToken = default);
    }
}
