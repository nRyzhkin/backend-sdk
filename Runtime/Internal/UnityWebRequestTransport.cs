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
            string authorizationHeader,
            CancellationToken cancellationToken)
        {
            using var request = CreateRequest(verb, path, body, authorizationHeader);

            try
            {
                // Must resume on the Unity main thread before touching DownloadHandler / UnityWebRequest APIs.
                await request.SendWebRequestAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BackendException(
                    "Backend request failed before a response was received.",
                    "transport_failure",
                    true,
                    exception);
            }

            var responseText = request.downloadHandler?.text ?? string.Empty;
            var statusCode = (int)request.responseCode;

            if (settings.EnableLogging)
            {
                Debug.Log($"[Backend SDK] {verb} {request.url} -> {statusCode}");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return default;
                }

                return UnityJsonSerializer.Deserialize<TResponse>(responseText);
            }

            throw CreateException(request, responseText, statusCode);
        }

        private UnityWebRequest CreateRequest<TRequest>(HttpVerb verb, string path, TRequest body, string authorizationHeader)
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

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.SetRequestHeader(AuthorizationHeader, authorizationHeader);
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

        private static BackendException CreateException(UnityWebRequest request, string responseText, int statusCode)
        {
            var isTransient = request.result == UnityWebRequest.Result.ConnectionError
                || statusCode == 408
                || statusCode == 429
                || statusCode >= 500;

            var serverError = ExtractServerError(responseText);
            var message = !string.IsNullOrWhiteSpace(serverError)
                ? serverError
                : !string.IsNullOrWhiteSpace(responseText)
                    ? responseText
                    : !string.IsNullOrWhiteSpace(request.error)
                        ? request.error
                        : "Backend request failed.";

            return new BackendException(
                message,
                "request_failed",
                isTransient,
                null,
                statusCode > 0 ? statusCode : (int?)null,
                serverError);
        }

        private static string ExtractServerError(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            try
            {
                var body = UnityJsonSerializer.Deserialize<ServerErrorBody>(responseText);
                if (body == null)
                {
                    return string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(body.error))
                {
                    return body.error;
                }

                if (!string.IsNullOrWhiteSpace(body.title))
                {
                    return body.title;
                }

                if (!string.IsNullOrWhiteSpace(body.detail))
                {
                    return body.detail;
                }

                if (!string.IsNullOrWhiteSpace(body.message))
                {
                    return body.message;
                }
            }
            catch
            {
                // Fall through and leave ServerError empty when the payload is not JSON.
            }

            return string.Empty;
        }

        [Serializable]
        private sealed class ServerErrorBody
        {
            public string error = string.Empty;
            public string title = string.Empty;
            public string detail = string.Empty;
            public string message = string.Empty;
        }
    }
}
