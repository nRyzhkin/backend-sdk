using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BackendSdk.Internal
{
    internal sealed class UnityWebRequestTransport : IBackendTransport
    {
        private const string ContentType = "application/json";
        private const string ApplicationIdHeader = "X-Application-Id";
        private const string AuthorizationHeader = "Authorization";

        private readonly BackendSettings settings;

        internal UnityWebRequestTransport(BackendSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<TResponse> SendAsync<TRequest, TResponse>(
            HttpVerb verb,
            string path,
            TRequest body,
            string authorizationToken,
            CancellationToken cancellationToken)
        {
            using var request = CreateRequest(verb, path, body, authorizationToken);

            try
            {
                await request.SendWebRequestAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BackendException("Backend request failed before a response was received.", "transport_failure", true, exception);
            }

            var responseText = request.downloadHandler?.text ?? string.Empty;

            if (settings.EnableLogging)
            {
                Debug.Log($"[Backend SDK] {verb} {request.url}");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                return UnityJsonSerializer.Deserialize<TResponse>(responseText);
            }

            throw CreateException(request, responseText);
        }

        private UnityWebRequest CreateRequest<TRequest>(HttpVerb verb, string path, TRequest body, string authorizationToken)
        {
            var request = new UnityWebRequest(BuildUrl(path), MapMethod(verb))
            {
                timeout = settings.TimeoutSeconds,
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Accept", ContentType);

            if (!string.IsNullOrWhiteSpace(settings.ApplicationId))
            {
                request.SetRequestHeader(ApplicationIdHeader, settings.ApplicationId);
            }

            if (!string.IsNullOrWhiteSpace(authorizationToken))
            {
                request.SetRequestHeader(AuthorizationHeader, authorizationToken);
            }

            var payload = verb == HttpVerb.Get || verb == HttpVerb.Delete
                ? string.Empty
                : UnityJsonSerializer.Serialize(body);

            if (!string.IsNullOrEmpty(payload))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                request.SetRequestHeader("Content-Type", ContentType);
            }

            return request;
        }

        private string BuildUrl(string path)
        {
            var baseUrl = (settings.ServerUrl ?? string.Empty).TrimEnd('/');
            var relativePath = (path ?? string.Empty).TrimStart('/');

            if (string.IsNullOrEmpty(baseUrl))
            {
                return relativePath;
            }

            if (string.IsNullOrEmpty(relativePath))
            {
                return baseUrl;
            }

            return $"{baseUrl}/{relativePath}";
        }

        private static string MapMethod(HttpVerb verb)
        {
            return verb switch
            {
                HttpVerb.Get => UnityWebRequest.kHttpVerbGET,
                HttpVerb.Post => UnityWebRequest.kHttpVerbPOST,
                HttpVerb.Put => UnityWebRequest.kHttpVerbPUT,
                HttpVerb.Delete => UnityWebRequest.kHttpVerbDELETE,
                _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, "Unsupported HTTP verb.")
            };
        }

        private static BackendException CreateException(UnityWebRequest request, string responseText)
        {
            var isTransient = request.result == UnityWebRequest.Result.ConnectionError;
            var message = string.IsNullOrWhiteSpace(responseText)
                ? request.error
                : responseText;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Backend request failed.";
            }

            return new BackendException(message, "request_failed", isTransient);
        }
    }
}
