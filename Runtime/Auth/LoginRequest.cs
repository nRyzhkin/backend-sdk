using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the credentials used to authenticate a player with the backend.
    /// </summary>
    [Serializable]
    public sealed class LoginRequest
    {
        /// <summary>
        /// The authentication provider identifier.
        /// </summary>
        public string Provider = string.Empty;

        /// <summary>
        /// The external player identifier provided by the authentication provider.
        /// </summary>
        public string ExternalId = string.Empty;

        /// <summary>
        /// Optional display name from the platform (or a chosen nick). Applied when the profile is still a placeholder.
        /// </summary>
        public string DisplayName = string.Empty;
    }
}
