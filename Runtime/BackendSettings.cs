using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the immutable configuration currently used by the Backend SDK.
    /// </summary>
    [Serializable]
    public sealed class BackendSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackendSettings"/> class.
        /// </summary>
        /// <param name="serverUrl">The backend server base URL.</param>
        /// <param name="applicationId">The application identifier.</param>
        /// <param name="timeoutSeconds">The request timeout, in seconds.</param>
        /// <param name="enableLogging">Whether diagnostic logging is enabled.</param>
        /// <param name="apiKey">The optional API key reserved for future use.</param>
        /// <param name="developmentMode">Whether development-mode authentication is enabled.</param>
        /// <param name="developmentProvider">The provider used in development mode.</param>
        /// <param name="developmentExternalId">The external identifier used in development mode.</param>
        public BackendSettings(
            string serverUrl,
            string applicationId,
            int timeoutSeconds,
            bool enableLogging,
            string apiKey,
            bool developmentMode,
            string developmentProvider,
            string developmentExternalId)
        {
            ServerUrl = serverUrl ?? string.Empty;
            ApplicationId = applicationId ?? string.Empty;
            TimeoutSeconds = Math.Max(1, timeoutSeconds);
            EnableLogging = enableLogging;
            ApiKey = apiKey ?? string.Empty;
            DevelopmentMode = developmentMode;
            DevelopmentProvider = developmentProvider ?? string.Empty;
            DevelopmentExternalId = developmentExternalId ?? string.Empty;
        }

        /// <summary>
        /// Gets the backend server base URL.
        /// </summary>
        public string ServerUrl { get; }

        /// <summary>
        /// Gets the application identifier sent with backend requests.
        /// </summary>
        public string ApplicationId { get; }

        /// <summary>
        /// Gets the request timeout, in seconds.
        /// </summary>
        public int TimeoutSeconds { get; }

        /// <summary>
        /// Gets a value indicating whether diagnostic logging is enabled.
        /// </summary>
        public bool EnableLogging { get; }

        /// <summary>
        /// Gets the optional API key reserved for future use.
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Gets a value indicating whether development-mode authentication is enabled.
        /// </summary>
        public bool DevelopmentMode { get; }

        /// <summary>
        /// Gets the provider identifier used when development-mode authentication is enabled.
        /// </summary>
        public string DevelopmentProvider { get; }

        /// <summary>
        /// Gets the external identifier used when development-mode authentication is enabled.
        /// </summary>
        public string DevelopmentExternalId { get; }

        internal BackendOptions ToOptions()
        {
            return new BackendOptions
            {
                ServerUrl = ServerUrl,
                ApplicationId = ApplicationId,
                TimeoutSeconds = TimeoutSeconds,
                EnableLogging = EnableLogging,
                ApiKey = ApiKey,
                DevelopmentMode = DevelopmentMode,
                DevelopmentProvider = DevelopmentProvider,
                DevelopmentExternalId = DevelopmentExternalId
            };
        }

        internal static BackendSettings FromOptions(BackendOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new BackendSettings(
                options.ServerUrl,
                options.ApplicationId,
                options.TimeoutSeconds,
                options.EnableLogging,
                options.ApiKey,
                options.DevelopmentMode,
                options.DevelopmentProvider,
                options.DevelopmentExternalId);
        }
    }
}
