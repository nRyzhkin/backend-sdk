using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Defines the public analytics service contract.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Tracks an analytics event for the authenticated player.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="parameters">Optional event parameters serialized as a JSON object.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes when the event has been sent.</returns>
        Task TrackAsync(
            string eventName,
            object parameters = null,
            CancellationToken cancellationToken = default);
    }
}
