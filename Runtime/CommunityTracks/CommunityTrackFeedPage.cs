namespace BackendSdk
{
    /// <summary>One page of the public community feed.</summary>
    public sealed class CommunityTrackFeedPage
    {
        public CommunityTrackFeedPage(CommunityTrackInfo[] items, string nextCursor)
        {
            Items = items ?? System.Array.Empty<CommunityTrackInfo>();
            NextCursor = nextCursor ?? string.Empty;
        }

        public CommunityTrackInfo[] Items { get; }

        /// <summary>Opaque cursor for the next page; empty when exhausted.</summary>
        public string NextCursor { get; }

        public bool HasMore => !string.IsNullOrEmpty(NextCursor);
    }
}
