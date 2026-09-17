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
    }

    [Serializable]
    internal sealed class CommunityTrackFeedDto
    {
        public CommunityTrackItemDto[] items = Array.Empty<CommunityTrackItemDto>();
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
}
