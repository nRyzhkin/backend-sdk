using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the outcome of a login operation.
    /// </summary>
    public sealed class LoginResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginResult"/> class.
        /// </summary>
        /// <param name="session">The authenticated player session.</param>
        public LoginResult(PlayerSession session, string guestKey = null)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            GuestKey = string.IsNullOrWhiteSpace(guestKey) ? null : guestKey.Trim();
        }

        /// <summary>
        /// Gets the authenticated player session.
        /// </summary>
        public PlayerSession Session { get; }

        /// <summary>
        /// Gets the server-issued guest credential when this session is a guest. Persist via
        /// <see cref="IGuestCredentialStore"/> (the SDK does this automatically).
        /// </summary>
        public string GuestKey { get; }
    }
}
