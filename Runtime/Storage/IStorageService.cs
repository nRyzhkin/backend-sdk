using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Defines the public player-scoped key/value storage contract.
    /// </summary>
    /// <remarks>
    /// The public method names <c>SetAsync</c>, <c>GetAsync</c>, and <c>DeleteAsync</c> are stable SDK API.
    /// </remarks>
    public interface IStorageService
    {
        /// <summary>
        /// Stores a value for the authenticated player under the specified key.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The storage key.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes when the value has been stored.</returns>
        Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a value for the authenticated player under the specified key.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The storage key.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes with the stored value.</returns>
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a value for the authenticated player under the specified key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes when the value has been deleted.</returns>
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    }
}
