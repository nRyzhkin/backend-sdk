namespace BackendSdk
{
    /// <summary>Metadata for a stored community race ghost (no payload bytes).</summary>
    public sealed class CommunityTrackGhostInfo
    {
        public CommunityTrackGhostInfo(
            string trackId,
            string contentHash,
            string userId,
            double timeSeconds,
            string transportHash,
            string transportName,
            int ghostByteLength,
            bool hasGhost)
        {
            TrackId = trackId ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            UserId = userId ?? string.Empty;
            TimeSeconds = timeSeconds;
            TransportHash = transportHash ?? string.Empty;
            TransportName = transportName ?? string.Empty;
            GhostByteLength = ghostByteLength;
            HasGhost = hasGhost;
        }

        public string TrackId { get; }
        public string ContentHash { get; }
        public string UserId { get; }
        public double TimeSeconds { get; }
        public string TransportHash { get; }
        public string TransportName { get; }
        public int GhostByteLength { get; }
        public bool HasGhost { get; }
    }

    /// <summary>Downloaded community race ghost payload.</summary>
    public sealed class CommunityTrackGhostPackage
    {
        public CommunityTrackGhostPackage(
            string trackId,
            string contentHash,
            string userId,
            double timeSeconds,
            string transportHash,
            string transportName,
            byte[] ghostBytes)
        {
            TrackId = trackId ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            UserId = userId ?? string.Empty;
            TimeSeconds = timeSeconds;
            TransportHash = transportHash ?? string.Empty;
            TransportName = transportName ?? string.Empty;
            GhostBytes = ghostBytes ?? System.Array.Empty<byte>();
        }

        public string TrackId { get; }
        public string ContentHash { get; }
        public string UserId { get; }
        public double TimeSeconds { get; }
        public string TransportHash { get; }
        public string TransportName { get; }
        public byte[] GhostBytes { get; }
    }

    /// <summary>Upload request for a community race ghost (WBGH1 bytes).</summary>
    public sealed class CommunityTrackGhostPutRequest
    {
        public CommunityTrackGhostPutRequest(
            string trackId,
            string contentHash,
            double timeSeconds,
            byte[] ghostBytes)
        {
            TrackId = trackId ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            TimeSeconds = timeSeconds;
            GhostBytes = ghostBytes ?? System.Array.Empty<byte>();
        }

        public string TrackId { get; }
        public string ContentHash { get; }
        public double TimeSeconds { get; }
        public byte[] GhostBytes { get; }
        public string TransportHash { get; set; } = string.Empty;
        public string TransportName { get; set; } = string.Empty;
    }
}
