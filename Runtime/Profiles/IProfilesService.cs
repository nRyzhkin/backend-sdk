using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Provides player profile operations for the Backend SDK.
    /// </summary>
    public interface IProfilesService
    {
        /// <summary>
        /// Gets the authenticated player's profile. The backend lazily creates a profile when missing.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The current player's profile.</returns>
        Task<PlayerProfile> GetMeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Fully replaces the authenticated player's display name, avatar, and public data.
        /// </summary>
        /// <typeparam name="TPublicData">The public data type to serialize.</typeparam>
        /// <param name="displayName">The new display name.</param>
        /// <param name="avatarId">The new avatar identifier, or <c>null</c> to clear it.</param>
        /// <param name="publicData">The new public data object.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The updated profile.</returns>
        Task<PlayerProfile> UpdateMeAsync<TPublicData>(
            string displayName,
            string avatarId,
            TPublicData publicData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fully replaces the authenticated player's display name, avatar, and public data.
        /// </summary>
        /// <param name="displayName">The new display name.</param>
        /// <param name="avatarId">The new avatar identifier, or <c>null</c> to clear it.</param>
        /// <param name="publicDataJson">The new public data as a JSON object string.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The updated profile.</returns>
        Task<PlayerProfile> UpdateMeAsync(
            string displayName,
            string avatarId,
            string publicDataJson,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a public profile by user identifier. Does not require authentication.
        /// </summary>
        /// <param name="userId">The public user identifier.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The public profile.</returns>
        Task<PlayerProfile> GetAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets public profiles for multiple user identifiers. Does not require authentication.
        /// </summary>
        /// <param name="userIds">The user identifiers to look up.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>Found profiles and missing user identifiers.</returns>
        Task<PlayerProfileBatchResult> GetBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default);
    }
}
