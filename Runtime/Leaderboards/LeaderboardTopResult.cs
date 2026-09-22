namespace BackendSdk
{
    /// <summary>Top leaderboard page plus optional authenticated "me" entry.</summary>
    public sealed class LeaderboardTopResult
    {
        public LeaderboardTopResult(LeaderboardEntry[] entries, LeaderboardEntry me = null)
        {
            Entries = entries ?? System.Array.Empty<LeaderboardEntry>();
            Me = me;
        }

        public LeaderboardEntry[] Entries { get; }

        /// <summary>Current player's entry when authenticated and scored; otherwise null.</summary>
        public LeaderboardEntry Me { get; }
    }
}
