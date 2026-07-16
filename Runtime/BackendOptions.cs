using System;

namespace BackendSdk
{
    /// <summary>
    /// Defines the mutable configuration values used to initialize the Backend SDK.
    /// </summary>
    [Serializable]
    public sealed class BackendOptions
    {
        /// <summary>
        /// The backend server base URL.
        /// </summary>
        public string ServerUrl = string.Empty;

        /// <summary>
        /// The application identifier sent with backend requests.
        /// </summary>
        public string ApplicationId = string.Empty;

        /// <summary>
        /// The request timeout, in seconds.
        /// </summary>
        public int TimeoutSeconds = 30;

        /// <summary>
        /// Indicates whether diagnostic logging is enabled.
        /// </summary>
        public bool EnableLogging;

        /// <summary>
        /// The optional API key reserved for future use.
        /// </summary>
        public string ApiKey = string.Empty;

        /// <summary>
        /// Indicates whether development-mode authentication is enabled.
        /// </summary>
        public bool DevelopmentMode;

        /// <summary>
        /// The provider identifier used when development-mode authentication is enabled.
        /// </summary>
        public string DevelopmentProvider = string.Empty;

        /// <summary>
        /// The external identifier used when development-mode authentication is enabled.
        /// </summary>
        public string DevelopmentExternalId = string.Empty;
    }
}
