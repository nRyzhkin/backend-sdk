using System;

namespace BackendSdk.Internal
{
    [Serializable]
    internal sealed class LoginRequestDto
    {
        public string provider = string.Empty;
        public string externalId = string.Empty;
    }

    [Serializable]
    internal sealed class LoginResponseDto
    {
        public string userId = string.Empty;
        public string accessToken = string.Empty;
        public string expiresAt = string.Empty;
    }
}
