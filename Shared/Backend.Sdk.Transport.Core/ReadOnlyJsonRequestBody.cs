namespace BackendSdk.Transport.Core
{
    /// <summary>
    /// Marks a POST request as semantically read-only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ReadOnlyJsonRequestBody"/> suppresses the idempotency header (<c>X-Request-Id</c>)
    /// but does not disable transient retries.
    /// </para>
    /// <para>
    /// This type is internal, not part of the public SDK API, and must only be used for read-only JSON POST
    /// endpoints such as profile batch lookup. It does not affect JSON serialization or authorization.
    /// </para>
    /// </remarks>
    internal sealed class ReadOnlyJsonRequestBody
    {
        internal ReadOnlyJsonRequestBody(string json)
        {
            Json = json ?? "{}";
        }

        internal string Json { get; }
    }
}
