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
        private const string RequestIdHeader = "X-Request-Id";

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
            var maxAttempts = 1 + settings.RetryCount;
            var context = CreateRequestContext(verb);

            BackendException lastTransientError = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                context.Attempt = attempt;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    return await SendOnceAsync<TRequest, TResponse>(
                        verb,
                        path,
                        body,
                        authorizationHeader,
                        context,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    LogCancelled(verb, path, context);
                    throw;
                }
                catch (BackendException exception) when (exception.IsTransient && attempt < maxAttempts)
                {
                    lastTransientError = exception;

                    try
                    {
                        // Do not start another retry if cancellation was requested after the failed attempt.
                        cancellationToken.ThrowIfCancellationRequested();
                        LogRetry(verb, path, context, maxAttempts);
                        await DelayBeforeRetryAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        LogCancelled(verb, path, context);
                        throw;
                    }
                }
            }

            throw lastTransientError
                ?? new BackendException("Backend request failed after retries.", "request_failed", true);
        }

        private async Task<TResponse> SendOnceAsync<TRequest, TResponse>(
            HttpVerb verb,
            string path,
            TRequest body,
            string authorizationHeader,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            using var request = CreateRequest(verb, path, body, authorizationHeader, context);

            try
            {
                // Must resume on the Unity main thread before touching DownloadHandler / UnityWebRequest APIs.
                await request.SendWebRequestAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Never wrap cancellation as BackendException and never treat it as transient.
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
                var requestIdSuffix = string.IsNullOrEmpty(context.RequestId)
                    ? string.Empty
                    : $" RequestId={context.RequestId}";
                Debug.Log($"[Backend SDK] {verb} {request.url} -> {statusCode}{requestIdSuffix}");
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

        private UnityWebRequest CreateRequest<TRequest>(
            HttpVerb verb,
            string path,
            TRequest body,
            string authorizationHeader,
            RequestContext context)
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

            if (!string.IsNullOrEmpty(context.RequestId))
            {
                request.SetRequestHeader(RequestIdHeader, context.RequestId);
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

        private static RequestContext CreateRequestContext(HttpVerb verb)
        {
            if (verb == HttpVerb.Get)
            {
                return new RequestContext(null);
            }

            return new RequestContext(Guid.NewGuid().ToString("N"));
        }

        private async Task DelayBeforeRetryAsync(CancellationToken cancellationToken)
        {
            if (settings.RetryDelayMilliseconds <= 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }

            await Task.Delay(settings.RetryDelayMilliseconds, cancellationToken);
        }

        private void LogRetry(HttpVerb verb, string path, RequestContext context, int maxAttempts)
        {
            if (!settings.EnableLogging)
            {
                return;
            }

            var relativePath = string.IsNullOrWhiteSpace(path) ? "/" : "/" + path.TrimStart('/');
            var requestIdSuffix = string.IsNullOrEmpty(context.RequestId)
                ? string.Empty
                : $" RequestId={context.RequestId}";

            Debug.Log(
                $"[Backend SDK] Retry {context.Attempt + 1}/{maxAttempts} {verb} {relativePath}{requestIdSuffix}");
        }

        private void LogCancelled(HttpVerb verb, string path, RequestContext context)
        {
            if (!settings.EnableLogging)
            {
                return;
            }

            var url = BuildUrl(path);
            var requestIdSuffix = string.IsNullOrEmpty(context.RequestId)
                ? string.Empty
                : $" RequestId={context.RequestId}";

            Debug.Log(
                $"[Backend SDK] Request cancelled. {verb} {url}{requestIdSuffix} Attempt={context.Attempt}");
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
            var isTransient = IsTransientFailure(request, statusCode);

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

        private static bool IsTransientFailure(UnityWebRequest request, int statusCode)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return true;
            }

            var error = request.error ?? string.Empty;
            if (error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("dns", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("name resolution", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("cannot resolve", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return statusCode == 408
                || statusCode == 429
                || statusCode >= 500;
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
