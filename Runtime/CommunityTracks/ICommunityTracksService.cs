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
        Task<CommunityTrackInfo[]> ListFeedAsync(
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes or replaces a track package for the authenticated player.
        /// </summary>
        Task<CommunityTrackInfo> PublishAsync(
            CommunityTrackPublishRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a published package by track id + content hash (anonymous).
        /// </summary>
        Task<byte[]> GetPackageAsync(
            string trackId,
            string contentHash,
            CancellationToken cancellationToken = default);
    }
}
