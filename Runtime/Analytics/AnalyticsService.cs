using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides analytics operations for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Application identifiers and authorization are handled automatically by the SDK infrastructure.
    /// </remarks>
    public sealed class AnalyticsService : IAnalyticsService
    {
        private const int MaxEventNameLength = 128;

        /// <inheritdoc />
        public async Task TrackAsync(
            string eventName,
            object parameters = null,
            CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateEventName(eventName);

            var body = AnalyticsParametersJson.BuildRequestJson(eventName.Trim(), parameters);

            await client.PostJsonAsync(
                BuildPath(client),
                body,
                cancellationToken).ConfigureAwait(false);
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "Analytics requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildPath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/analytics/{applicationId}/events";
        }

        private static void ValidateEventName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name is required.", nameof(eventName));
            }

            if (eventName.Length > MaxEventNameLength)
            {
                throw new ArgumentException(
                    $"Event name cannot exceed {MaxEventNameLength} characters.",
                    nameof(eventName));
            }
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }
    }
}
