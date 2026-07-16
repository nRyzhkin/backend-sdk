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
        /// <remarks>
        /// The session is read-only. Only the SDK can create or replace the current session.
        /// </remarks>
        PlayerSession Session { get; }

        /// <summary>
        /// Gets a value indicating whether the player is currently authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Logs in using development-mode credentials configured in Project Settings.
        /// </summary>
        /// <remarks>
        /// This overload is only available when development mode is enabled and the application is running in the Unity Editor.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token for the login operation.</param>
        /// <returns>A task that completes with the login result.</returns>
        Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs in using the specified provider credentials.
        /// </summary>
        /// <param name="request">The login credentials.</param>
        /// <param name="cancellationToken">A cancellation token for the login operation.</param>
        /// <returns>A task that completes with the login result.</returns>
        Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs out the current player and clears the active session.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token for the logout operation.</param>
        /// <returns>A task that completes when logout finishes.</returns>
        Task LogoutAsync(CancellationToken cancellationToken = default);
    }
}
