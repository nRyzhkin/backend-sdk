using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk.Internal
{
    internal interface IBackendTransport
    {
        Task<TResponse> SendAsync<TRequest, TResponse>(
            HttpVerb verb,
            string path,
            TRequest body,
            string authorizationHeader,
            CancellationToken cancellationToken);
    }
}
