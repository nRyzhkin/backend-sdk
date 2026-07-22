using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Provides read-only access to the authenticated player's economy state.
    /// </summary>
    /// <remarks>
    /// The Unity client cannot grant, spend, or set economy resources directly. Rewards and mutations are
    /// performed by authoritative backend modules such as Daily Rewards, Store, or Battle Pass.
    /// </remarks>
    public interface IEconomyService
    {
        /// <summary>
        /// Gets active economy definitions for the configured application.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancels waiting for the operation. Does not cancel the shared in-flight HTTP request started
        /// by another caller.
        /// </param>
        /// <returns>Active currency and entitlement definitions from the player economy endpoint.</returns>
        Task<EconomyDefinitions> GetDefinitionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the authenticated player's economy state.
        /// </summary>
        /// <param name="forceRefresh">When <c>true</c>, bypasses the in-memory cache.</param>
        /// <param name="cancellationToken">
        /// Cancels waiting for the operation. Does not cancel the shared in-flight HTTP request started
        /// by another caller.
        /// </param>
        /// <returns>The current player economy state.</returns>
        Task<PlayerEconomyState> GetStateAsync(
            bool forceRefresh = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes the authenticated player's economy state from the backend.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancels waiting for the operation. Does not cancel the shared in-flight HTTP request started
        /// by another caller.
        /// </param>
        /// <returns>The refreshed player economy state.</returns>
        Task<PlayerEconomyState> RefreshAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears cached definitions and player state.
        /// </summary>
        void ClearCache();
    }
}
