using System.Collections.Generic;

namespace BackendSdk.Transport.Core
{
    /// <summary>
    /// Describes the HTTP request that the transport would send.
    /// </summary>
    internal sealed class TransportRequestSnapshot
    {
        internal TransportRequestSnapshot(
            string method,
            string url,
            string payload,
            IReadOnlyDictionary<string, string> headers)
        {
            Method = method ?? string.Empty;
            Url = url ?? string.Empty;
            Payload = payload ?? string.Empty;
            Headers = headers ?? new Dictionary<string, string>();
        }

        internal string Method { get; }

        internal string Url { get; }

        internal string Payload { get; }

        internal IReadOnlyDictionary<string, string> Headers { get; }

        internal bool HasHeader(string name)
        {
            return TryGetHeader(name, out _);
        }

        internal bool TryGetHeader(string name, out string value)
        {
            foreach (var header in Headers)
            {
                if (string.Equals(header.Key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    value = header.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
