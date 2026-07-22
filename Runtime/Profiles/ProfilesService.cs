using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides player profile operations for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GetMeAsync"/> and <see cref="UpdateMeAsync"/> require an authenticated player session.
    /// <see cref="GetAsync"/> and <see cref="GetBatchAsync"/> are anonymous and do not require sign-in.
    /// </para>
    /// <para>
    /// Public data is client-controlled display data and must not be trusted for authoritative gameplay decisions.
    /// Inventory, currency, purchases, verified achievements, and server rank belong in separate authoritative backend modules.
    /// </para>
    /// </remarks>
    public sealed class ProfilesService : IProfilesService
    {
        /// <summary>
        /// Gets the maximum number of user identifiers allowed in a single batch request.
        /// </summary>
        public const int MaxBatchSize = 100;

        /// <inheritdoc />
        public async Task<PlayerProfile> GetMeAsync(CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();

            var responseJson = await client.GetRawAsync(BuildMePath(client), cancellationToken).ConfigureAwait(false);
            return ProfileJson.ParseProfile(responseJson);
        }

        /// <inheritdoc />
        public async Task<PlayerProfile> UpdateMeAsync<TPublicData>(
            string displayName,
            string avatarId,
            TPublicData publicData,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateDisplayName(displayName);

            var body = ProfileJson.BuildUpdateRequest(displayName, avatarId, publicData);
            var responseJson = await client.PutJsonAsync(BuildMePath(client), body, cancellationToken).ConfigureAwait(false);
            return ProfileJson.ParseProfile(responseJson);
        }

        /// <inheritdoc />
        public async Task<PlayerProfile> UpdateMeAsync(
            string displayName,
            string avatarId,
            string publicDataJson,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateDisplayName(displayName);

            var body = ProfileJson.BuildUpdateRequest(displayName, avatarId, publicDataJson);
            var responseJson = await client.PutJsonAsync(BuildMePath(client), body, cancellationToken).ConfigureAwait(false);
            return ProfileJson.ParseProfile(responseJson);
        }

        /// <inheritdoc />
        public async Task<PlayerProfile> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            var client = Backend.ClientOrThrow();
            var responseJson = await client.GetRawAnonymousAsync(
                BuildPublicPath(client, userId),
                cancellationToken).ConfigureAwait(false);

            return ProfileJson.ParseProfile(responseJson);
        }

        /// <inheritdoc />
        public async Task<PlayerProfileBatchResult> GetBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            var dedupedUserIds = ProfileJson.DedupeUserIdsPreserveOrder(ValidateUserIds(userIds));
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            var client = Backend.ClientOrThrow();
            var body = ProfileJson.BuildBatchRequest(dedupedUserIds);
            var responseJson = await client.PostJsonAnonymousAsync(
                BuildBatchPath(client),
                body,
                cancellationToken).ConfigureAwait(false);

            return ProfileJson.ParseBatchResult(responseJson);
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "This profile operation requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildMePath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/profiles/{applicationId}/me";
        }

        private static string BuildPublicPath(BackendClient client, Guid userId)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            var userPublicId = Uri.EscapeDataString(userId.ToString());
            return $"v1/profiles/{applicationId}/{userPublicId}";
        }

        private static string BuildBatchPath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/profiles/{applicationId}/batch";
        }

        private static IReadOnlyCollection<Guid> ValidateUserIds(IReadOnlyCollection<Guid> userIds)
        {
            if (userIds == null)
            {
                throw new ArgumentNullException(nameof(userIds));
            }

            if (userIds.Count == 0)
            {
                throw new ArgumentException("At least one user ID is required.", nameof(userIds));
            }

            if (userIds.Count > MaxBatchSize)
            {
                throw new ArgumentException(
                    $"At most {MaxBatchSize} user IDs are allowed.",
                    nameof(userIds));
            }

            foreach (var userId in userIds)
            {
                ValidateUserId(userId);
            }

            return userIds;
        }

        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be Guid.Empty.", nameof(userId));
            }
        }

        private static void ValidateDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }
    }
}
