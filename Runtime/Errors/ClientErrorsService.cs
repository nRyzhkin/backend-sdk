using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Sends critical client error reports to the backend.
    /// </summary>
    public sealed class ClientErrorsService : IClientErrorsService
    {
        const int MaxMessageLength = 32768;

        /// <inheritdoc />
        public async Task ReportAsync(
            ClientErrorReportRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();

            var message = (request.Message ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message is required.", nameof(request));
            if (message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength);

            var logType = (request.LogType ?? string.Empty).Trim();
            if (!string.Equals(logType, "Error", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(logType, "Exception", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("LogType must be Error or Exception.", nameof(request));
            }

            logType = string.Equals(logType, "Exception", StringComparison.OrdinalIgnoreCase)
                ? "Exception"
                : "Error";

            var body = BuildRequestJson(
                message,
                logType,
                request.Place,
                request.TrackId,
                request.TrackName,
                request.TransportId,
                request.TransportName,
                request.Context);

            await client.PostJsonAsync(BuildPath(client), body, cancellationToken);
        }

        static string BuildRequestJson(
            string message,
            string logType,
            string place,
            string trackId,
            string trackName,
            string transportId,
            string transportName,
            object context)
        {
            var builder = new StringBuilder(256 + message.Length);
            builder.Append("{\"message\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(message));
            builder.Append(",\"logType\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(logType));
            builder.Append(",\"place\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(place ?? string.Empty));
            builder.Append(",\"trackId\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(trackId ?? string.Empty));
            builder.Append(",\"trackName\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(trackName ?? string.Empty));
            builder.Append(",\"transportId\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(transportId ?? string.Empty));
            builder.Append(",\"transportName\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(transportName ?? string.Empty));
            if (context != null)
            {
                builder.Append(",\"context\":");
                builder.Append(AnalyticsParametersJson.SerializeJsonValue(context));
            }

            builder.Append('}');
            return builder.ToString();
        }

        static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();
            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "Error reporting requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        static string BuildPath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/errors/{applicationId}/reports";
        }

        static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }
    }
}
