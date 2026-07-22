namespace BackendSdk.Transport.Core
{
    /// <summary>
    /// Carries idempotency metadata for a single transport-level send, including retries.
    /// </summary>
    /// <remarks>
    /// <see cref="RequestId"/> is used as the idempotency key exposed as <c>X-Request-Id</c> on write operations.
    /// It is also appended to transport logs. Correlation logging for read-only requests is a future improvement.
    /// </remarks>
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
