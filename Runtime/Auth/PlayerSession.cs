using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the authenticated player session returned by the backend.
    /// </summary>
    /// <remarks>
    /// This type is immutable. Game code can read session values but cannot modify them.
    /// </remarks>
    public sealed class PlayerSession
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSession"/> class.
        /// </summary>
        /// <param name="playerId">The backend player identifier.</param>
        /// <param name="accessToken">The access token for authenticated requests.</param>
        /// <param name="expiration">The UTC expiration time of the access token.</param>
        /// <param name="provider">The authentication provider identifier.</param>
        /// <param name="externalId">The external player identifier from the provider.</param>
        /// <param name="authenticated">Whether the session is authenticated.</param>
        public PlayerSession(
            string playerId,
            string accessToken,
            DateTime expiration,
            string provider,
            string externalId,
            bool authenticated)
        {
            PlayerId = playerId ?? string.Empty;
            AccessToken = accessToken ?? string.Empty;
            Expiration = expiration;
            Provider = provider ?? string.Empty;
            ExternalId = externalId ?? string.Empty;
            Authenticated = authenticated;
        }

        /// <summary>
        /// Gets the backend player identifier.
        /// </summary>
        public string PlayerId { get; }

        /// <summary>
        /// Gets the access token for authenticated requests.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the UTC expiration time of the access token.
        /// </summary>
        public DateTime Expiration { get; }

        /// <summary>
        /// Gets the authentication provider identifier.
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// Gets the external player identifier from the provider.
        /// </summary>
        public string ExternalId { get; }

        /// <summary>
        /// Gets a value indicating whether the session is authenticated.
        /// </summary>
        public bool Authenticated { get; }
    }
}
