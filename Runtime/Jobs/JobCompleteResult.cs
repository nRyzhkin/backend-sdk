namespace BackendSdk
{
    /// <summary>
    /// Result of completing a job run.
    /// </summary>
    public sealed class JobCompleteResult
    {
        internal JobCompleteResult(
            JobOffer job,
            long coinsGranted,
            long xpGranted,
            int levelsGained,
            long levelUpCoinsGranted,
            int levelUpBonusTicketsGranted,
            string currencyKey)
        {
            Job = job ?? throw new System.ArgumentNullException(nameof(job));
            CoinsGranted = coinsGranted;
            XpGranted = xpGranted;
            LevelsGained = levelsGained;
            LevelUpCoinsGranted = levelUpCoinsGranted;
            LevelUpBonusTicketsGranted = levelUpBonusTicketsGranted;
            CurrencyKey = currencyKey ?? string.Empty;
        }

        /// <summary>Updated offer after rewards were applied.</summary>
        public JobOffer Job { get; }

        /// <summary>Coins from the complete formula (before level-up extras).</summary>
        public long CoinsGranted { get; }

        /// <summary>XP granted for this complete (BOOSTABLE on server).</summary>
        public long XpGranted { get; }

        /// <summary>How many levels were gained from this complete.</summary>
        public int LevelsGained { get; }

        /// <summary>Extra coins from level-up reward table entries.</summary>
        public long LevelUpCoinsGranted { get; }

        /// <summary>Bonus tickets granted from level-up reward table entries.</summary>
        public int LevelUpBonusTicketsGranted { get; }

        /// <summary>Currency key used for coin grants.</summary>
        public string CurrencyKey { get; }
    }
}
