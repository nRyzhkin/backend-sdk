using System;
using System.Collections.Generic;

namespace BackendSdk
{
    /// <summary>
    /// Represents the result of a batch profile lookup.
    /// </summary>
    public sealed class PlayerProfileBatchResult
    {
        private readonly Dictionary<Guid, PlayerProfile> byUserId;

        internal PlayerProfileBatchResult(
            IReadOnlyList<PlayerProfile> profiles,
            IReadOnlyList<Guid> missingUserIds)
        {
            Profiles = profiles ?? Array.Empty<PlayerProfile>();
            MissingUserIds = missingUserIds ?? Array.Empty<Guid>();

            byUserId = new Dictionary<Guid, PlayerProfile>(Profiles.Count);
            for (var i = 0; i < Profiles.Count; i++)
            {
                var profile = Profiles[i];
                if (!byUserId.ContainsKey(profile.UserId))
                {
                    byUserId[profile.UserId] = profile;
                }
            }
        }

        /// <summary>
        /// Gets the profiles returned by the backend, in response order.
        /// </summary>
        public IReadOnlyList<PlayerProfile> Profiles { get; }

        /// <summary>
        /// Gets user identifiers that were not found, in response order.
        /// </summary>
        public IReadOnlyList<Guid> MissingUserIds { get; }

        /// <summary>
        /// Gets a lookup of returned profiles by user identifier.
        /// </summary>
        public IReadOnlyDictionary<Guid, PlayerProfile> ByUserId => byUserId;

        /// <summary>
        /// Attempts to get a profile for the specified user identifier.
        /// </summary>
        /// <param name="userId">The user identifier to look up.</param>
        /// <param name="profile">When this method returns, contains the profile if found.</param>
        /// <returns><c>true</c> when a profile was found; otherwise <c>false</c>.</returns>
        public bool TryGetProfile(Guid userId, out PlayerProfile profile)
        {
            return byUserId.TryGetValue(userId, out profile);
        }
    }
}
