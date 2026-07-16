namespace BackendSdk.Internal
{
    /// <summary>
    /// Carries idempotency metadata for a single transport-level send, including retries.
    /// </summary>
    internal sealed class RequestContext
    {
        public RequestContext(string requestId)
        {
            RequestId = requestId;
        }

        public string RequestId { get; }

        public int Attempt { get; set; }
    }
}
