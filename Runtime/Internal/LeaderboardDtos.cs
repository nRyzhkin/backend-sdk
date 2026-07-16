using System;

namespace BackendSdk.Internal
{
    [Serializable]
    internal sealed class LeaderboardSubmitRequestDto
    {
        public double value;
        public int sortMode;
    }

    [Serializable]
    internal sealed class LeaderboardSubmitResponseDto
    {
        public double value;
        public int rank;
    }

    [Serializable]
    internal sealed class LeaderboardEntryDto
    {
        public string userId = string.Empty;
        public double value;
        public int rank;
    }

    [Serializable]
    internal sealed class LeaderboardTopResponseDto
    {
        public LeaderboardEntryDto[] entries = Array.Empty<LeaderboardEntryDto>();
    }

    [Serializable]
    internal sealed class LeaderboardAroundResponseDto
    {
        public LeaderboardEntryDto me = new LeaderboardEntryDto();
        public LeaderboardEntryDto[] around = Array.Empty<LeaderboardEntryDto>();
    }
}
