using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the authenticated player entry and nearby leaderboard entries.
    /// </summary>
    public sealed class LeaderboardAroundResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardAroundResult"/> class.
        /// </summary>
        /// <param name="me">The authenticated player's entry.</param>
        /// <param name="around">Nearby entries including the authenticated player.</param>
        public LeaderboardAroundResult(LeaderboardEntry me, LeaderboardEntry[] around)
        {
            Me = me ?? throw new ArgumentNullException(nameof(me));
            Around = around ?? Array.Empty<LeaderboardEntry>();
        }

        /// <summary>
        /// Gets the authenticated player's entry.
        /// </summary>
        public LeaderboardEntry Me { get; }

        /// <summary>
        /// Gets nearby entries including the authenticated player.
        /// </summary>
        public LeaderboardEntry[] Around { get; }
    }
}
