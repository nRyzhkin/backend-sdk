using BackendSdk.Transport.Core;

namespace BackendSdk.Internal
{
    /// <summary>
    /// Captures one transport send attempt for internal test hooks.
    /// </summary>
    internal sealed class TransportSendInvocation
    {
        internal HttpVerb Verb { get; set; }

        internal string Path { get; set; }

        internal object Body { get; set; }

        internal string AuthorizationHeader { get; set; }

        internal RequestContext Context { get; set; }

        internal BackendSettings Settings { get; set; }
    }
}
