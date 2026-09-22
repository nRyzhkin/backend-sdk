using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Community UGC catalog operations for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Application identifiers are inserted automatically. Game code must not pass ApplicationId.
    /// </remarks>
    public sealed class CommunityTracksService : ICommunityTracksService
    {
        /// <inheritdoc />
        public async Task<CommunityTrackInfo[]> ListFeedAsync(
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            if (limit < 1)
            {
                limit = 1;
            }
            else if (limit > 200)
            {
                limit = 200;
            }

            var client = Backend.ClientOrThrow();
            var path = $"{BuildFeedPath(client)}?limit={limit}";
            var response = await client.GetAsync<CommunityTrackFeedDto>(path, cancellationToken);
            return MapItems(response?.items);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackInfo[]> ListLikedAsync(
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();

            if (limit < 1)
            {
                limit = 1;
            }
            else if (limit > 200)
            {
                limit = 200;
            }

            var path = $"{BuildFeedPath(client)}/liked?limit={limit}";
            var response = await client.GetAsync<CommunityTrackFeedDto>(path, cancellationToken);
            return MapItems(response?.items);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackInfo> PublishAsync(
            CommunityTrackPublishRequest request,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                throw new BackendException("Publish request is required.", "invalid_publish_request");
            }

            ValidateTrackId(request.TrackId);
            ValidateContentHash(request.ContentHash);

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new BackendException("Title is required.", "invalid_title");
            }

            if (request.PackageBytes == null || request.PackageBytes.Length == 0)
            {
                throw new BackendException("Package bytes are required.", "missing_package");
            }

            var body = new CommunityTrackPublishRequestDto
            {
                contentHash = request.ContentHash.Trim(),
                title = request.Title.Trim(),
                authorName = request.AuthorName ?? string.Empty,
                requiredTransportHash = request.RequiredTransportHash ?? string.Empty,
                requiredTransportName = request.RequiredTransportName ?? string.Empty,
                previewPngBase64 = request.PreviewPngBase64 ?? string.Empty,
                packageBase64 = Convert.ToBase64String(request.PackageBytes)
            };

            var path = BuildTrackPath(client, request.TrackId);
            var response = await client.PutAsync<CommunityTrackPublishRequestDto, CommunityTrackItemDto>(
                path,
                body,
                cancellationToken);

            return MapItem(response);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            string trackId,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);

            var path = BuildTrackPath(client, trackId);
            await client.DeleteAsync(path, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<byte[]> GetPackageAsync(
            string trackId,
            string contentHash,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);
            ValidateContentHash(contentHash);

            var client = Backend.ClientOrThrow();
            var path =
                $"{BuildTrackPath(client, trackId)}/package?contentHash={Uri.EscapeDataString(contentHash.Trim())}";
            var response = await client.GetAsync<CommunityTrackPackageDto>(path, cancellationToken);

            if (response == null || string.IsNullOrEmpty(response.packageBase64))
            {
                throw new BackendException(
                    "Community track package was not found.",
                    "community_track_package_not_found",
                    statusCode: 404);
            }

            try
            {
                return Convert.FromBase64String(response.packageBase64);
            }
            catch (FormatException)
            {
                throw new BackendException(
                    "Community track package payload was not valid base64.",
                    "invalid_package_base64");
            }
        }

        /// <inheritdoc />
        public async Task<CommunityTrackEngagement> LikeAsync(
            string trackId,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);

            var path = $"{BuildTrackPath(client, trackId)}/like";
            var response = await client.PutAsync<object, CommunityTrackEngagementDto>(
                path,
                new object(),
                cancellationToken);
            return MapEngagement(response);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackEngagement> UnlikeAsync(
            string trackId,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);

            var path = $"{BuildTrackPath(client, trackId)}/like";
            var response = await client.DeleteAsync<CommunityTrackEngagementDto>(path, cancellationToken);
            return MapEngagement(response);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackEngagement> RecordVisitAsync(
            string trackId,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);

            var path = $"{BuildTrackPath(client, trackId)}/visit";
            var response = await client.PostAsync<object, CommunityTrackEngagementDto>(
                path,
                new object(),
                cancellationToken);
            return MapEngagement(response);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackEngagement> ReportAsync(
            string trackId,
            CommunityTrackReportReason reason,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);

            var path = $"{BuildTrackPath(client, trackId)}/report";
            var body = new CommunityTrackReportRequestDto { reason = reason.ToString() };
            var response = await client.PostAsync<CommunityTrackReportRequestDto, CommunityTrackEngagementDto>(
                path,
                body,
                cancellationToken);
            return MapEngagement(response);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackGhostInfo> PutGhostAsync(
            CommunityTrackGhostPutRequest request,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                throw new BackendException("Ghost put request is required.", "invalid_ghost_request");
            }

            ValidateTrackId(request.TrackId);
            ValidateContentHash(request.ContentHash);

            if (request.GhostBytes == null || request.GhostBytes.Length < 8)
            {
                throw new BackendException("Ghost bytes are required.", "missing_ghost");
            }

            if (request.TimeSeconds < 0d || double.IsNaN(request.TimeSeconds) || double.IsInfinity(request.TimeSeconds))
            {
                throw new BackendException("Ghost time is invalid.", "invalid_time");
            }

            var body = new CommunityTrackGhostPutRequestDto
            {
                contentHash = request.ContentHash.Trim(),
                timeSeconds = request.TimeSeconds,
                ghostBase64 = Convert.ToBase64String(request.GhostBytes),
                transportHash = request.TransportHash ?? string.Empty,
                transportName = request.TransportName ?? string.Empty
            };

            var path = $"{BuildTrackPath(client, request.TrackId)}/ghosts";
            var response = await client.PutAsync<CommunityTrackGhostPutRequestDto, CommunityTrackGhostItemDto>(
                path,
                body,
                cancellationToken);

            return MapGhostItem(response);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackGhostInfo[]> ListGhostsAsync(
            string trackId,
            string contentHash,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);
            ValidateContentHash(contentHash);

            var client = Backend.ClientOrThrow();
            var path =
                $"{BuildTrackPath(client, trackId)}/ghosts?contentHash={Uri.EscapeDataString(contentHash.Trim())}";
            var response = await client.GetAsync<CommunityTrackGhostListDto>(path, cancellationToken);
            return MapGhostItems(response?.items);
        }

        /// <inheritdoc />
        public async Task<CommunityTrackGhostPackage> GetGhostAsync(
            string trackId,
            string contentHash,
            string userId,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateTrackId(trackId);
            ValidateContentHash(contentHash);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new BackendException("User id is required.", "invalid_user_id");
            }

            var client = Backend.ClientOrThrow();
            var path =
                $"{BuildTrackPath(client, trackId)}/ghosts/{Uri.EscapeDataString(userId.Trim())}" +
                $"?contentHash={Uri.EscapeDataString(contentHash.Trim())}";
            var response = await client.GetAsync<CommunityTrackGhostPackageDto>(path, cancellationToken);

            if (response == null || string.IsNullOrEmpty(response.ghostBase64))
            {
                throw new BackendException(
                    "Community track ghost was not found.",
                    "community_track_ghost_not_found",
                    statusCode: 404);
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(response.ghostBase64);
            }
            catch (FormatException)
            {
                throw new BackendException(
                    "Community track ghost payload was not valid base64.",
                    "invalid_ghost_base64");
            }

            return new CommunityTrackGhostPackage(
                response.trackId,
                response.contentHash,
                response.userId,
                response.timeSeconds,
                response.transportHash,
                response.transportName,
                bytes);
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "This community tracks operation requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildFeedPath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/community-tracks/{applicationId}";
        }

        private static string BuildTrackPath(BackendClient client, string trackId)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            var id = Uri.EscapeDataString(trackId);
            return $"v1/community-tracks/{applicationId}/{id}";
        }

        private static CommunityTrackInfo[] MapItems(CommunityTrackItemDto[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CommunityTrackInfo>();
            }

            var mapped = new CommunityTrackInfo[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                mapped[i] = MapItem(items[i]);
            }

            return mapped;
        }

        private static CommunityTrackInfo MapItem(CommunityTrackItemDto item)
        {
            if (item == null)
            {
            return new CommunityTrackInfo(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0);
            }

            return new CommunityTrackInfo(
                item.trackId,
                item.contentHash,
                item.title,
                item.authorId,
                item.authorName,
                item.requiredTransportHash,
                item.requiredTransportName,
                item.previewPngBase64,
                item.publishedAtUtc,
                item.packageByteLength,
                item.likeCount,
                item.visitorCount,
                item.likedByMe);
        }

        static CommunityTrackEngagement MapEngagement(CommunityTrackEngagementDto item)
        {
            if (item == null)
            {
                return new CommunityTrackEngagement(string.Empty, 0, 0, false);
            }

            return new CommunityTrackEngagement(
                item.trackId,
                item.likeCount,
                item.visitorCount,
                item.likedByMe,
                item.hidden);
        }

        static CommunityTrackGhostInfo[] MapGhostItems(CommunityTrackGhostItemDto[] items)
        {
            if (items == null || items.Length == 0)
                return Array.Empty<CommunityTrackGhostInfo>();

            var mapped = new CommunityTrackGhostInfo[items.Length];
            for (var i = 0; i < items.Length; i++)
                mapped[i] = MapGhostItem(items[i]);
            return mapped;
        }

        static CommunityTrackGhostInfo MapGhostItem(CommunityTrackGhostItemDto item)
        {
            if (item == null)
            {
                return new CommunityTrackGhostInfo(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0d,
                    string.Empty,
                    string.Empty,
                    0,
                    false);
            }

            return new CommunityTrackGhostInfo(
                item.trackId,
                item.contentHash,
                item.userId,
                item.timeSeconds,
                item.transportHash,
                item.transportName,
                item.ghostByteLength,
                item.hasGhost || item.ghostByteLength > 0);
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }

        private static void ValidateTrackId(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                throw new BackendException("Track id is required.", "invalid_track_id");
            }
        }

        private static void ValidateContentHash(string contentHash)
        {
            if (string.IsNullOrWhiteSpace(contentHash))
            {
                throw new BackendException("Content hash is required.", "invalid_content_hash");
            }
        }
    }
}
