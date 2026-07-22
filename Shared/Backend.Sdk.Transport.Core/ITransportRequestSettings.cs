namespace BackendSdk.Transport.Core
{
    internal interface ITransportRequestSettings
    {
        string ServerUrl { get; }

        string ApplicationId { get; }
    }
}
