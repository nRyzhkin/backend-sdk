using System;

namespace BackendSdk
{
    /// <summary>Community track moderation snapshot for in-game moderators.</summary>
    public sealed class CommunityTrackModeration
    {
        internal CommunityTrackModeration(
            string trackId,
            string title,
            string moderationStatus,
            string hiddenReason,
            int reportCount,
            CommunityTrackModerationReport[] reports)
        {
            TrackId = trackId ?? string.Empty;
            Title = title ?? string.Empty;
            ModerationStatus = moderationStatus ?? string.Empty;
            HiddenReason = hiddenReason ?? string.Empty;
            ReportCount = reportCount < 0 ? 0 : reportCount;
            Reports = reports ?? Array.Empty<CommunityTrackModerationReport>();
        }

        public string TrackId { get; }
        public string Title { get; }
        public string ModerationStatus { get; }
        public string HiddenReason { get; }
        public int ReportCount { get; }
        public CommunityTrackModerationReport[] Reports { get; }

        public bool IsHidden =>
            !string.IsNullOrEmpty(ModerationStatus) &&
            !string.Equals(ModerationStatus, "Visible", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Single report row shown to moderators.</summary>
    public sealed class CommunityTrackModerationReport
    {
        internal CommunityTrackModerationReport(
            string userPublicId,
            string reason,
            DateTime createdAt,
            DateTime updatedAt)
        {
            UserPublicId = userPublicId ?? string.Empty;
            Reason = reason ?? string.Empty;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public string UserPublicId { get; }
        public string Reason { get; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
    }
}
