using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides leaderboard operations for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Application identifiers are inserted automatically. Game code must not pass ApplicationId.
    /// </remarks>
    public sealed class LeaderboardsService : ILeaderboardsService
    {
        /// <inheritdoc />
        public async Task<LeaderboardSubmitResult> SubmitAsync(
            string leaderboardName,
            double value,
            SortMode sortMode,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateLeaderboardName(leaderboardName);

            var body = new LeaderboardSubmitRequestDto
            {
                value = value,
                sortMode = (int)sortMode
            };

            var response = await client.PutAsync<LeaderboardSubmitRequestDto, LeaderboardSubmitResponseDto>(
                BuildPath(client, leaderboardName),
                body,
                cancellationToken).ConfigureAwait(false);

            return new LeaderboardSubmitResult(response?.value ?? value, response?.rank ?? 0);
        }

        /// <inheritdoc />
        public async Task<LeaderboardEntry[]> GetTopAsync(
            string leaderboardName,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateLeaderboardName(leaderboardName);

            if (limit < 1)
            {
                limit = 1;
            }

            var client = Backend.ClientOrThrow();
            var path = $"{BuildPath(client, leaderboardName)}?limit={limit}";
            var response = await client.GetAsync<LeaderboardTopResponseDto>(path, cancellationToken).ConfigureAwait(false);

            return MapEntries(response?.entries);
        }

        /// <inheritdoc />
        public async Task<LeaderboardAroundResult> GetAroundPlayerAsync(
            string leaderboardName,
            int range = 5,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateLeaderboardName(leaderboardName);

            if (range < 0)
            {
                range = 0;
            }
            else if (range > 50)
            {
                range = 50;
            }

            var path = $"{BuildPath(client, leaderboardName)}/me?range={range}";
            var response = await client.GetAsync<LeaderboardAroundResponseDto>(path, cancellationToken).ConfigureAwait(false);

            if (response?.me == null)
            {
                throw new BackendException(
                    "Leaderboard entry for the current player was not found.",
                    "leaderboard_entry_not_found",
                    statusCode: 404);
            }

            return new LeaderboardAroundResult(
                MapEntry(response.me),
                MapEntries(response.around));
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "This leaderboard operation requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildPath(BackendClient client, string leaderboardName)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            var name = Uri.EscapeDataString(leaderboardName);
            return $"v1/leaderboards/{applicationId}/{name}";
        }

        private static LeaderboardEntry[] MapEntries(LeaderboardEntryDto[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<LeaderboardEntry>();
            }

            var mapped = new LeaderboardEntry[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                mapped[i] = MapEntry(entries[i]);
            }

            return mapped;
        }

        private static LeaderboardEntry MapEntry(LeaderboardEntryDto entry)
        {
            return new LeaderboardEntry(
                entry?.userId ?? string.Empty,
                entry?.value ?? 0d,
                entry?.rank ?? 0);
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }

        private static void ValidateLeaderboardName(string leaderboardName)
        {
            if (string.IsNullOrWhiteSpace(leaderboardName))
            {
                throw new BackendException("Leaderboard name is required.", "invalid_leaderboard_name");
            }
        }
    }
}
