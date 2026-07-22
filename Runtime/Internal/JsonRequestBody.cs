namespace BackendSdk.Internal
{
    /// <summary>
    /// Carries a pre-built JSON payload through the existing transport without re-serializing it.
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
