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

        internal string ApplicationIdOrThrow()
        {
            if (string.IsNullOrWhiteSpace(Settings.ApplicationId))
            {
                throw new BackendException(
                    "Application ID is not configured. Set it in Project Settings > Backend.",
                    "missing_application_id");
            }

            return Settings.ApplicationId;
        }

        internal Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<object, TResponse>(
                HttpVerb.Get,
                path,
                null,
                ResolveAuthorizationHeader(),
                cancellationToken);
        }

        internal Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<TRequest, TResponse>(
                HttpVerb.Post,
                path,
                body,
                ResolveAuthorizationHeader(),
                cancellationToken);
        }

        internal Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<TRequest, TResponse>(
                HttpVerb.Put,
                path,
                body,
                ResolveAuthorizationHeader(),
                cancellationToken);
        }

        internal Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<object, EmptyResponse>(
                HttpVerb.Delete,
                path,
                null,
                ResolveAuthorizationHeader(),
                cancellationToken);
        }

        internal Task PostJsonAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<JsonRequestBody, EmptyResponse>(
                HttpVerb.Post,
                path,
                new JsonRequestBody(jsonBody),
                ResolveAuthorizationHeader(),
                cancellationToken);
        }

        internal Task<string> GetRawAsync(string path, CancellationToken cancellationToken = default)
        {
            return GetAsync<string>(path, cancellationToken);
        }

        internal Task<string> GetRawAnonymousAsync(string path, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<object, string>(
                HttpVerb.Get,
                path,
                null,
                null,
                cancellationToken);
        }

        internal Task<string> PostJsonAnonymousAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<ReadOnlyJsonRequestBody, string>(
                HttpVerb.Post,
                path,
                new ReadOnlyJsonRequestBody(jsonBody),
                null,
                cancellationToken);
        }

        internal Task<string> PutJsonAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            return transport.SendAsync<JsonRequestBody, string>(
                HttpVerb.Put,
                path,
                new JsonRequestBody(jsonBody),
                ResolveAuthorizationHeader(),
                cancellationToken);
        }

        private static string ResolveAuthorizationHeader()
        {
            return Backend.Auth.GetAuthorizationHeader();
        }
    }
}
