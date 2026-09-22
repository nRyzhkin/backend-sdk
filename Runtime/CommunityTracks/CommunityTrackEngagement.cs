namespace BackendSdk
{
    /// <summary>Engagement counters returned by like / unlike / visit / report.</summary>
    public sealed class CommunityTrackEngagement
    {
        public CommunityTrackEngagement(
            string trackId,
            int likeCount,
            int visitorCount,
            bool likedByMe,
            bool hidden = false)
        {
            TrackId = trackId ?? string.Empty;
            LikeCount = likeCount < 0 ? 0 : likeCount;
            VisitorCount = visitorCount < 0 ? 0 : visitorCount;
            LikedByMe = likedByMe;
            Hidden = hidden;
        }

        public string TrackId { get; }
        public int LikeCount { get; }
        public int VisitorCount { get; }
        public bool LikedByMe { get; }
        public bool Hidden { get; }
    }

    /// <summary>Fixed report reasons for community tracks (matches server enum).</summary>
    public enum CommunityTrackReportReason
    {
        Inappropriate = 0,
        Broken = 1,
        Spam = 2,
        Copyright = 3
    }
}
