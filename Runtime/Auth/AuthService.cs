using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BackendSdk
{
    /// <summary>
    /// Provides authentication operations for the Backend SDK.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        private PlayerSession session;

        /// <inheritdoc />
        public PlayerSession Session => session;

        /// <inheritdoc />
        public bool IsAuthenticated => session?.Authenticated == true;

        /// <inheritdoc />
        public Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var settings = Backend.Settings;
            if (!settings.DevelopmentMode)
            {
                throw new BackendException(
                    "Parameterless login requires development mode to be enabled in Project Settings.",
                    "development_mode_disabled");
            }

            if (!Application.isEditor)
            {
                throw new BackendException(
                    "Parameterless login is only available in the Unity Editor.",
                    "editor_only");
            }

            var request = new LoginRequest
            {
                Provider = settings.DevelopmentProvider,
                ExternalId = settings.DevelopmentExternalId
            };

            return LoginAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                throw new BackendException("Login provider is required.", "invalid_login_request");
            }

            if (string.IsNullOrWhiteSpace(request.ExternalId))
            {
                throw new BackendException("Login external identifier is required.", "invalid_login_request");
            }

            throw new BackendNotImplementedException("Authentication networking is not implemented yet.");
        }

        /// <inheritdoc />
        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            throw new BackendNotImplementedException("Authentication logout is not implemented yet.");
        }

        internal void SetSession(PlayerSession value)
        {
            session = value;
        }

        internal void ClearSession()
        {
            session = null;
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
