namespace BackendSdk.Internal
{
    /// <summary>
    /// Carries a pre-built JSON payload for read-only POST endpoints that must not use idempotency headers.
    /// </summary>
    internal sealed class ReadOnlyJsonRequestBody
    {
        internal ReadOnlyJsonRequestBody(string json)
        {
            Json = json ?? "{}";
        }

        internal string Json { get; }
    }
}
