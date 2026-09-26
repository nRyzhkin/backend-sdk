using System;

namespace BackendSdk.Internal
{
    [Serializable]
    internal sealed class CommunityTrackItemDto
    {
        public string trackId = string.Empty;
        public string contentHash = string.Empty;
        public string title = string.Empty;
        public string authorId = string.Empty;
        public string authorName = string.Empty;
        public string requiredTransportHash = string.Empty;
        public string requiredTransportName = string.Empty;
        public string previewPngBase64 = string.Empty;
        public string publishedAtUtc = string.Empty;
        public int packageByteLength;
        public int likeCount;
        public int visitorCount;
        public bool likedByMe;
        public bool hidden;
        public string moderationStatus = string.Empty;
        public int reportCount;
    }

    [Serializable]
    internal sealed class CommunityTrackFeedDto
    {
        public CommunityTrackItemDto[] items = Array.Empty<CommunityTrackItemDto>();
        public string nextCursor = string.Empty;
    }

    [Serializable]
    internal sealed class CommunityTrackEngagementDto
    {
        public string trackId = string.Empty;
        public int likeCount;
        public int visitorCount;
        public bool likedByMe;
        public bool hidden;
    }

    [Serializable]
    internal sealed class CommunityTrackReportRequestDto
    {
        public string reason = string.Empty;
    }

    [Serializable]
    internal sealed class CommunityTrackModerationHideRequestDto
    {
        public string reason = string.Empty;
    }

    [Serializable]
    internal sealed class CommunityTrackModerationReportDto
    {
        public string userPublicId = string.Empty;
        public string reason = string.Empty;
        public string createdAt = string.Empty;
        public string updatedAt = string.Empty;
    }

    [Serializable]
    internal sealed class CommunityTrackModerationDto
    {
        public string trackId = string.Empty;
        public string title = string.Empty;
        public string moderationStatus = string.Empty;
        public string hiddenReason = string.Empty;
        public int reportCount;
        public CommunityTrackModerationReportDto[] reports = Array.Empty<CommunityTrackModerationReportDto>();
    }

    [Serializable]
    internal sealed class CommunityTrackPublishRequestDto
    {
        public string contentHash = string.Empty;
        public string title = string.Empty;
        public string authorName = string.Empty;
        public string requiredTransportHash = string.Empty;
        public string requiredTransportName = string.Empty;
        public string previewPngBase64 = string.Empty;
        public string packageBase64 = string.Empty;
    }

    [Serializable]
    internal sealed class CommunityTrackPackageDto
    {
        public string trackId = string.Empty;
        public string contentHash = string.Empty;
        public string packageBase64 = string.Empty;
        public int packageByteLength;
    }

    [Serializable]
    internal sealed class CommunityTrackGhostPutRequestDto
    {
        public string contentHash = string.Empty;
        public double timeSeconds;
        public string ghostBase64 = string.Empty;
        public string transportHash = string.Empty;
        public string transportName = string.Empty;
    }

    [Serializable]
    internal sealed class CommunityTrackGhostItemDto
    {
        public string trackId = string.Empty;
        public string contentHash = string.Empty;
        public string userId = string.Empty;
        public double timeSeconds;
        public string transportHash = string.Empty;
        public string transportName = string.Empty;
        public int ghostByteLength;
        public bool hasGhost = true;
    }

    [Serializable]
    internal sealed class CommunityTrackGhostListDto
    {
        public CommunityTrackGhostItemDto[] items = Array.Empty<CommunityTrackGhostItemDto>();
    }

    [Serializable]
    internal sealed class CommunityTrackGhostPackageDto
    {
        public string trackId = string.Empty;
        public string contentHash = string.Empty;
        public string userId = string.Empty;
        public double timeSeconds;
        public string transportHash = string.Empty;
        public string transportName = string.Empty;
        public string ghostBase64 = string.Empty;
        public int ghostByteLength;
    }
}
