using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Public community UGC catalog: list feed, publish packages, download packages.
    /// </summary>
    public interface ICommunityTracksService
    {
        /// <summary>
        /// Lists published community tracks for the configured application (anonymous).
        /// </summary>
        Task<CommunityTrackFeedPage> ListFeedAsync(
            int limit = 18,
            string cursor = null,
            bool includeHidden = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ranked search across the public catalog (title, author, fuzzy, layout).
        /// </summary>
        Task<CommunityTrackFeedPage> SearchAsync(
            string query,
            int limit = 18,
            string cursor = null,
            bool includeHidden = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists tracks the authenticated player has liked (visible only).
        /// </summary>
        Task<CommunityTrackInfo[]> ListLikedAsync(
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes or replaces a track package for the authenticated player.
        /// </summary>
        Task<CommunityTrackInfo> PublishAsync(
            CommunityTrackPublishRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the authenticated player's own publication from the catalog (idempotent).
        /// </summary>
        Task DeleteAsync(
            string trackId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a published package by track id + content hash (anonymous).
        /// </summary>
        Task<byte[]> GetPackageAsync(
            string trackId,
            string contentHash,
            CancellationToken cancellationToken = default);

        /// <summary>Likes a track for the authenticated player (idempotent).</summary>
        Task<CommunityTrackEngagement> LikeAsync(
            string trackId,
            CancellationToken cancellationToken = default);

        /// <summary>Removes the authenticated player's like.</summary>
        Task<CommunityTrackEngagement> UnlikeAsync(
            string trackId,
            CancellationToken cancellationToken = default);

        /// <summary>Records a unique visit for the authenticated player (idempotent).</summary>
        Task<CommunityTrackEngagement> RecordVisitAsync(
            string trackId,
            CancellationToken cancellationToken = default);

        /// <summary>Upserts a report for the authenticated player; may soft-hide the track.</summary>
        Task<CommunityTrackEngagement> ReportAsync(
            string trackId,
            CommunityTrackReportReason reason,
            CancellationToken cancellationToken = default);

        /// <summary>Loads moderation details (reports + status) for a community track. Requires moderator.</summary>
        Task<CommunityTrackModeration> GetModerationAsync(
            string trackId,
            CancellationToken cancellationToken = default);

        /// <summary>Hides a community track from the public feed. Requires moderator.</summary>
        Task<CommunityTrackModeration> HideAsModeratorAsync(
            string trackId,
            string reason = null,
            CancellationToken cancellationToken = default);

        /// <summary>Restores a hidden community track to the public feed. Requires moderator.</summary>
        Task<CommunityTrackModeration> UnhideAsModeratorAsync(
            string trackId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads or replaces the authenticated player's race ghost when the time improves.
        /// </summary>
        Task<CommunityTrackGhostInfo> PutGhostAsync(
            CommunityTrackGhostPutRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists ghost metadata for a track revision (anonymous).
        /// </summary>
        Task<CommunityTrackGhostInfo[]> ListGhostsAsync(
            string trackId,
            string contentHash,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a player's race ghost for a track revision (anonymous).
        /// </summary>
        Task<CommunityTrackGhostPackage> GetGhostAsync(
            string trackId,
            string contentHash,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
