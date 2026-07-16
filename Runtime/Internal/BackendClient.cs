using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk.Internal
{
    internal sealed class BackendClient
    {
        private readonly IBackendTransport transport;

        internal BackendClient(BackendSettings settings, IBackendTransport transport)
        {
            Settings = settings;
            this.transport = transport;
        }

        internal BackendSettings Settings { get; }

        internal Task InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        internal Task<TResponse> GetAsync<TResponse>(string path, string authorizationToken = null, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<object, TResponse>(
                HttpVerb.Get,
                path,
                null,
                authorizationToken,
                cancellationToken);
        }

        internal Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, string authorizationToken = null, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<TRequest, TResponse>(
                HttpVerb.Post,
                path,
                body,
                authorizationToken,
                cancellationToken);
        }

        internal Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, string authorizationToken = null, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<TRequest, TResponse>(
                HttpVerb.Put,
                path,
                body,
                authorizationToken,
                cancellationToken);
        }

        internal Task<TResponse> DeleteAsync<TResponse>(string path, string authorizationToken = null, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<object, TResponse>(
                HttpVerb.Delete,
                path,
                null,
                authorizationToken,
                cancellationToken);
        }
    }
}
