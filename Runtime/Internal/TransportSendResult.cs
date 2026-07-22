namespace BackendSdk.Internal
{
    /// <summary>
    /// Result returned by the internal transport test hook.
    /// </summary>
    internal sealed class TransportSendResult
    {
        internal string ResponseText { get; set; } = string.Empty;

        internal BackendException ExceptionToThrow { get; set; }
    }
}
