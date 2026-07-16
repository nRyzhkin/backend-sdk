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
        public LoginResult(PlayerSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Gets the authenticated player session.
        /// </summary>
        public PlayerSession Session { get; }
    }
}
