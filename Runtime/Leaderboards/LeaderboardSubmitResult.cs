namespace BackendSdk
{
    /// <summary>
    /// Represents the result of submitting a leaderboard score.
    /// </summary>
    public sealed class LeaderboardSubmitResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardSubmitResult"/> class.
        /// </summary>
        /// <param name="value">The stored score value.</param>
        /// <param name="rank">The player's current rank.</param>
        public LeaderboardSubmitResult(double value, int rank)
        {
            Value = value;
            Rank = rank;
        }

        /// <summary>
        /// Gets the stored score value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Gets the player's current rank.
        /// </summary>
        public int Rank { get; }
    }
}
