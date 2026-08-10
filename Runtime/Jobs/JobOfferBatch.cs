using System;
using System.Collections.Generic;

namespace BackendSdk
{
    /// <summary>
    /// Batch of job offers for the authenticated player.
    /// </summary>
    public sealed class JobOfferBatch
    {
        internal JobOfferBatch(IReadOnlyList<JobOffer> jobs, DateTime serverTimeUtc)
        {
            Jobs = jobs ?? Array.Empty<JobOffer>();
            ServerTimeUtc = serverTimeUtc;
        }

        /// <summary>Enabled jobs ordered by admin sort order.</summary>
        public IReadOnlyList<JobOffer> Jobs { get; }

        /// <summary>Server clock at response time (UTC).</summary>
        public DateTime ServerTimeUtc { get; }
    }
}
