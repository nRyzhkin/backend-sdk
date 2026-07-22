namespace BackendSdk.Transport.Core
{
    /// <summary>
    /// Carries a pre-built JSON payload through the transport without re-serializing it.
    /// </summary>
    internal sealed class JsonRequestBody
    {
        internal JsonRequestBody(string json)
        {
            Json = json ?? "{}";
        }

        internal string Json { get; }
    }
}
