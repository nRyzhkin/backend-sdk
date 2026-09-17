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
                item.packageByteLength);
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
