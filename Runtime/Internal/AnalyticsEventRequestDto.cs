using System;

namespace BackendSdk.Internal
{
    /// <summary>
    /// Internal analytics request shape for POST /v1/analytics/{applicationId}/events.
    /// </summary>
    [Serializable]
    internal sealed class AnalyticsEventRequestDto
    {
        public string eventName = string.Empty;
        public string parametersJson;
    }
}
