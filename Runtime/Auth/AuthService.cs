using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;
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
        public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
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

            var client = Backend.ClientOrThrow();
            var body = new LoginRequestDto
            {
                provider = request.Provider.Trim(),
                externalId = request.ExternalId.Trim()
            };

            var response = await client.PostAsync<LoginRequestDto, LoginResponseDto>(
                "v1/auth/login",
                body,
                cancellationToken).ConfigureAwait(false);

            if (response == null || string.IsNullOrWhiteSpace(response.accessToken))
            {
                throw new BackendException("Login response did not include an access token.", "invalid_login_response");
            }

            var expiration = ParseExpiration(response.expiresAt);
            var playerSession = new PlayerSession(
                response.userId,
                response.accessToken,
                expiration,
                body.provider,
                body.externalId,
                true);

            SetSession(playerSession);
            return new LoginResult(playerSession);
        }

        /// <inheritdoc />
        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ClearSession();
            return Task.CompletedTask;
        }

        internal string GetAuthorizationHeader()
        {
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                return null;
            }

            return "Bearer " + session.AccessToken;
        }

        internal void SetSession(PlayerSession value)
        {
            session = value;
        }

        internal void ClearSession()
        {
            session = null;
        }

        private static DateTime ParseExpiration(string expiresAt)
        {
            if (DateTime.TryParse(
                    expiresAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                return parsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : parsed.ToUniversalTime();
            }

            return DateTime.UtcNow;
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
