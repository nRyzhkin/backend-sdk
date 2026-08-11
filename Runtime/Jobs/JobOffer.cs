using System;

namespace BackendSdk
{
    /// <summary>
    /// A player-facing job offer snapshot from the server.
    /// </summary>
    public sealed class JobOffer
    {
        internal JobOffer(
            string jobId,
            string displayName,
            int level,
            long xp,
            long xpToNext,
            int freeTickets,
            int bonusTickets,
            int freeTicketMax,
            DateTime? freeTicketsFullAtUtc,
            DateTime? nextFreeTicketAtUtc,
            long rewardPreview,
            bool hasActiveRun,
            bool canStart,
            bool canComplete,
            DateTime serverTimeUtc)
        {
            JobId = jobId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Level = level;
            Xp = xp;
            XpToNext = xpToNext;
            FreeTickets = freeTickets;
            BonusTickets = bonusTickets;
            FreeTicketMax = freeTicketMax;
            FreeTicketsFullAtUtc = freeTicketsFullAtUtc;
            NextFreeTicketAtUtc = nextFreeTicketAtUtc;
            RewardPreview = rewardPreview;
            HasActiveRun = hasActiveRun;
            CanStart = canStart;
            CanComplete = canComplete;
            ServerTimeUtc = serverTimeUtc;
        }

        /// <summary>Stable job id (e.g. cleaner, pizza).</summary>
        public string JobId { get; }

        /// <summary>Display name from admin config.</summary>
        public string DisplayName { get; }

        /// <summary>Current job level (1–max).</summary>
        public int Level { get; }

        /// <summary>XP progress within the current level.</summary>
        public long Xp { get; }

        /// <summary>XP required to reach the next level (0 at max).</summary>
        public long XpToNext { get; }

        /// <summary>Free tickets currently available.</summary>
        public int FreeTickets { get; }

        /// <summary>Bonus tickets (spent before free; uncapped).</summary>
        public int BonusTickets { get; }

        /// <summary>Configured free-ticket cap.</summary>
        public int FreeTicketMax { get; }

        /// <summary>UTC when free tickets are expected to be full, if regenerating.</summary>
        public DateTime? FreeTicketsFullAtUtc { get; }

        /// <summary>UTC when the next free ticket regenerates (null at cap).</summary>
        public DateTime? NextFreeTicketAtUtc { get; }

        /// <summary>Coins preview for the next completion.</summary>
        public long RewardPreview { get; }

        /// <summary>True when this job already has an unfinished run.</summary>
        public bool HasActiveRun { get; }

        /// <summary>True when the player may call start.</summary>
        public bool CanStart { get; }

        /// <summary>True when the player may call complete.</summary>
        public bool CanComplete { get; }

        /// <summary>Server clock at response time (UTC).</summary>
        public DateTime ServerTimeUtc { get; }
    }
}
