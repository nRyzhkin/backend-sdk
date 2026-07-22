using BackendSdk.Transport.Core;

namespace BackendSdk.DotNetTests
{
    internal sealed class TestTransportSettings : ITransportRequestSettings
    {
        internal TestTransportSettings(string serverUrl, string applicationId)
        {
            ServerUrl = serverUrl;
            ApplicationId = applicationId;
        }

        public string ServerUrl { get; }

        public string ApplicationId { get; }
    }
}
