namespace BackendSdk
{
    /// <summary>
    /// Represents a single leaderboard entry.
    /// </summary>
    public sealed class LeaderboardEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardEntry"/> class.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <param name="value">The score value.</param>
        /// <param name="rank">The 1-based rank.</param>
        public LeaderboardEntry(string playerId, double value, int rank)
        {
            PlayerId = playerId ?? string.Empty;
            Value = value;
            Rank = rank;
        }

        /// <summary>
        /// Gets the player identifier.
        /// </summary>
        public string PlayerId { get; }

        /// <summary>
        /// Gets the score value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Gets the 1-based rank.
        /// </summary>
        public int Rank { get; }
    }
}
