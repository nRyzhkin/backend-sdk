using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Transport.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace BackendSdk.Internal
{
    internal sealed class UnityWebRequestTransport : IBackendTransport
    {
        internal static Func<TransportSendInvocation, Task<TransportSendResult>> TestSendOnceHandler;

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
            var context = TransportRequestBuilder.CreateRequestContext(verb, body);

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
            if (TestSendOnceHandler != null)
            {
                var testResult = await TestSendOnceHandler(new TransportSendInvocation
                {
                    Verb = verb,
                    Path = path,
                    Body = body,
                    AuthorizationHeader = authorizationHeader,
                    Context = context,
                    Settings = settings
                }).ConfigureAwait(false);

                if (testResult.ExceptionToThrow != null)
                {
                    throw testResult.ExceptionToThrow;
                }

                if (string.IsNullOrWhiteSpace(testResult.ResponseText))
                {
                    return default;
                }

                return UnityJsonSerializer.Deserialize<TResponse>(testResult.ResponseText);
            }

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
            var snapshot = TransportRequestBuilder.Build(
                settings,
                verb,
                path,
                body,
                authorizationHeader,
                context);

            var request = new UnityWebRequest(snapshot.Url, snapshot.Method)
            {
                timeout = settings.TimeoutSeconds,
                downloadHandler = new DownloadHandlerBuffer()
            };

            foreach (var header in snapshot.Headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(snapshot.Payload))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(snapshot.Payload));
            }

            return request;
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
            return TransportRequestBuilder.Build(settings, HttpVerb.Get, path, null, null, new RequestContext(null)).Url;
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
