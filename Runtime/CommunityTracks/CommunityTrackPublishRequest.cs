namespace BackendSdk
{
    /// <summary>
    /// Publish payload for a community track package.
    /// </summary>
    public sealed class CommunityTrackPublishRequest
    {
        public CommunityTrackPublishRequest(
            string trackId,
            string contentHash,
            string title,
            byte[] packageBytes)
        {
            TrackId = trackId ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            Title = title ?? string.Empty;
            PackageBytes = packageBytes;
        }

        public string TrackId { get; set; }
        public string ContentHash { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string RequiredTransportHash { get; set; } = string.Empty;
        public string RequiredTransportName { get; set; } = string.Empty;
        public string PreviewPngBase64 { get; set; } = string.Empty;
        public byte[] PackageBytes { get; set; }
    }
}
