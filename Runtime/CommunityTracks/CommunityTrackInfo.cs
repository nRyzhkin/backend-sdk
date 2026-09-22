namespace BackendSdk
{
    /// <summary>
    /// Public catalog entry for a community track (no package bytes).
    /// </summary>
    public sealed class CommunityTrackInfo
    {
        public CommunityTrackInfo(
            string trackId,
            string contentHash,
            string title,
            string authorId,
            string authorName,
            string requiredTransportHash,
            string requiredTransportName,
            string previewPngBase64,
            string publishedAtUtc,
            int packageByteLength,
            int likeCount = 0,
            int visitorCount = 0,
            bool likedByMe = false)
        {
            TrackId = trackId ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            Title = title ?? string.Empty;
            AuthorId = authorId ?? string.Empty;
            AuthorName = authorName ?? string.Empty;
            RequiredTransportHash = requiredTransportHash ?? string.Empty;
            RequiredTransportName = requiredTransportName ?? string.Empty;
            PreviewPngBase64 = previewPngBase64 ?? string.Empty;
            PublishedAtUtc = publishedAtUtc ?? string.Empty;
            PackageByteLength = packageByteLength;
            LikeCount = likeCount < 0 ? 0 : likeCount;
            VisitorCount = visitorCount < 0 ? 0 : visitorCount;
            LikedByMe = likedByMe;
        }

        public string TrackId { get; }
        public string ContentHash { get; }
        public string Title { get; }
        public string AuthorId { get; }
        public string AuthorName { get; }
        public string RequiredTransportHash { get; }
        public string RequiredTransportName { get; }
        public string PreviewPngBase64 { get; }
        public string PublishedAtUtc { get; }
        public int PackageByteLength { get; }
        public int LikeCount { get; }
        public int VisitorCount { get; }
        public bool LikedByMe { get; }
    }
}
