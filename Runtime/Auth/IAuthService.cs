using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Defines the public authentication service contract.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Gets the current player session, if one exists.
        /// </summary>
        PlayerSession Session { get; }

        /// <summary>
        /// Gets a value indicating whether the player is currently authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Logs in using development-mode credentials configured in Project Settings.
        /// Editor-only when development mode is enabled.
        /// </summary>
        Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs in using provider credentials. Guest logins restore an existing server-issued guest
        /// and do not create accounts — use <see cref="CreateGuestAsync"/> for that.
        /// </summary>
        Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a server-issued guest account and persists the guest key locally.
        /// </summary>
        Task<LoginResult> CreateGuestAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs in as an existing player by public user id. Requires Auth:AllowPublicIdLogin on the server.
        /// </summary>
        Task<LoginResult> LoginByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Links a platform identity to the authenticated player (typically after guest play).
        /// </summary>
        Task<LoginResult> LinkAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores or creates a session: stored guest, new guest, and optional platform link/login.
        /// </summary>
        Task<LoginResult> EnsureSessionAsync(
            LoginRequest platform = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the local session. Does not delete the stored guest key.
        /// </summary>
        Task LogoutAsync(CancellationToken cancellationToken = default);
    }
}
