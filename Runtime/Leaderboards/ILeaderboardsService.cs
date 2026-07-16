using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Defines the public leaderboards service contract.
    /// </summary>
    public interface ILeaderboardsService
    {
        /// <summary>
        /// Submits a score for the authenticated player.
        /// </summary>
        /// <param name="leaderboardName">The leaderboard name.</param>
        /// <param name="value">The score value.</param>
        /// <param name="sortMode">How scores are ranked for this leaderboard.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes with the submit result.</returns>
        Task<LeaderboardSubmitResult> SubmitAsync(
            string leaderboardName,
            double value,
            SortMode sortMode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the top entries for a leaderboard.
        /// </summary>
        /// <param name="leaderboardName">The leaderboard name.</param>
        /// <param name="limit">The maximum number of entries to return.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes with the top entries.</returns>
        Task<LeaderboardEntry[]> GetTopAsync(
            string leaderboardName,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the authenticated player's entry and nearby ranks.
        /// </summary>
        /// <param name="leaderboardName">The leaderboard name.</param>
        /// <param name="range">How many entries above and below the player to include.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task that completes with the surrounding entries.</returns>
        Task<LeaderboardAroundResult> GetAroundPlayerAsync(
            string leaderboardName,
            int range = 5,
            CancellationToken cancellationToken = default);
    }
}
