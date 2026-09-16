using System;
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

        /// <summary>
        /// Gets or sets where server-issued guest keys are persisted. Defaults to PlayerPrefs.
        /// </summary>
        public IGuestCredentialStore GuestCredentials { get; set; } = new PlayerPrefsGuestCredentialStore();

        /// <summary>
        /// Editor-only account field (public user id or guest key). Separate from the guest key store.
        /// </summary>
        public IGuestCredentialStore EditorLogin { get; set; } =
            new PlayerPrefsGuestCredentialStore(PlayerPrefsGuestCredentialStore.EditorLoginPrefix);

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
            var json = AuthJson.BuildLoginRequest(request.Provider.Trim(), request.ExternalId.Trim());
            var responseJson = await client.PostJsonAnonymousAsync(
                "v1/auth/login",
                json,
                cancellationToken);

            return ApplyLoginResponse(responseJson, request.Provider.Trim(), request.ExternalId.Trim());
        }

        /// <inheritdoc />
        public async Task<LoginResult> CreateGuestAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            var client = Backend.ClientOrThrow();
            var responseJson = await client.PostJsonAnonymousAsync(
                "v1/auth/guest",
                "{}",
                cancellationToken);

            return ApplyLoginResponse(responseJson, AuthProviders.Guest, null);
        }

        /// <inheritdoc />
        public async Task<LoginResult> LoginByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId.Trim(), out var parsedId))
            {
                throw new BackendException("A valid player user id is required.", "invalid_login_request");
            }

            var client = Backend.ClientOrThrow();
            var json = AuthJson.BuildImpersonateRequest(parsedId.ToString());
            var responseJson = await client.PostJsonAnonymousAsync(
                "v1/auth/impersonate",
                json,
                cancellationToken);

            return ApplyLoginResponse(responseJson, null, null);
        }

        /// <inheritdoc />
        public async Task<LoginResult> LinkAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ExternalId))
            {
                throw new BackendException("Link provider and external identifier are required.", "invalid_login_request");
            }

            var client = Backend.ClientOrThrow();
            var json = AuthJson.BuildLoginRequest(request.Provider.Trim(), request.ExternalId.Trim());
            var responseJson = await client.PostJsonRawAsync(
                "v1/auth/link",
                json,
                cancellationToken);

            return ApplyLoginResponse(responseJson, request.Provider.Trim(), request.ExternalId.Trim());
        }

        /// <inheritdoc />
        public async Task<LoginResult> EnsureSessionAsync(
            LoginRequest platform = null,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            var hasPlatform = platform != null
                && !string.IsNullOrWhiteSpace(platform.Provider)
                && !string.IsNullOrWhiteSpace(platform.ExternalId);

            if (hasPlatform &&
                string.Equals(platform.Provider.Trim(), AuthProviders.Guest, StringComparison.OrdinalIgnoreCase))
            {
                throw new BackendException(
                    "EnsureSessionAsync platform argument must be a real platform identity, not guest.",
                    "invalid_login_request");
            }

            if (IsAuthenticated)
            {
                if (hasPlatform &&
                    string.Equals(Session.Provider, AuthProviders.Guest, StringComparison.OrdinalIgnoreCase))
                {
                    await LinkAsync(platform, cancellationToken);
                }

                return RequireSession();
            }

            var applicationId = Backend.Settings?.ApplicationId ?? string.Empty;

#if UNITY_EDITOR && !BACKEND_SDK_DOTNET
            if (!hasPlatform && await TryEditorLoginAsync(applicationId, cancellationToken))
            {
                return RequireSession();
            }
#endif

            var store = GuestCredentials ?? new PlayerPrefsGuestCredentialStore();

            if (store.TryGet(applicationId, out var storedGuestKey))
            {
                try
                {
                    await LoginAsync(
                        new LoginRequest
                        {
                            Provider = AuthProviders.Guest,
                            ExternalId = storedGuestKey
                        },
                        cancellationToken);
                }
                catch (BackendException ex) when (IsUnknownGuest(ex))
                {
                    store.Clear(applicationId);
                    await CreateGuestAsync(cancellationToken);
                }
            }
            else if (!hasPlatform)
            {
                await CreateGuestAsync(cancellationToken);
            }

            if (hasPlatform)
            {
                if (IsAuthenticated &&
                    string.Equals(Session.Provider, AuthProviders.Guest, StringComparison.OrdinalIgnoreCase))
                {
                    await LinkAsync(platform, cancellationToken);
                }
                else if (!IsAuthenticated)
                {
                    await LoginAsync(platform, cancellationToken);
                }
            }

            return RequireSession();
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
            Backend.Economy.ClearCache();
        }

        internal void ClearSession()
        {
            session = null;
            Backend.Economy.ClearCache();
        }

        LoginResult ApplyLoginResponse(string responseJson, string fallbackProvider, string fallbackExternalId)
        {
            var parsed = AuthJson.ParseLogin(responseJson);
            if (string.IsNullOrWhiteSpace(parsed.AccessToken))
            {
                throw new BackendException("Login response did not include an access token.", "invalid_login_response");
            }

            var provider = !string.IsNullOrWhiteSpace(parsed.Provider)
                ? parsed.Provider
                : fallbackProvider ?? string.Empty;
            var externalId = !string.IsNullOrWhiteSpace(parsed.GuestKey)
                ? parsed.GuestKey
                : fallbackExternalId ?? string.Empty;

            var playerSession = new PlayerSession(
                parsed.UserId,
                parsed.AccessToken,
                parsed.ExpiresAt,
                provider,
                externalId,
                true);

            SetSession(playerSession);

            var applicationId = Backend.Settings?.ApplicationId ?? string.Empty;
            if (string.Equals(provider, AuthProviders.Guest, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(externalId))
            {
                GuestCredentials?.Save(applicationId, externalId);
            }

#if UNITY_EDITOR && !BACKEND_SDK_DOTNET
            if (!string.IsNullOrWhiteSpace(playerSession.PlayerId))
            {
                EditorLogin?.Save(applicationId, playerSession.PlayerId);
            }
#endif

            return new LoginResult(playerSession, parsed.GuestKey ?? GuestKeyFromSession(playerSession));
        }

#if UNITY_EDITOR && !BACKEND_SDK_DOTNET
        async Task<bool> TryEditorLoginAsync(string applicationId, CancellationToken cancellationToken)
        {
            var editorStore = EditorLogin ?? new PlayerPrefsGuestCredentialStore(
                PlayerPrefsGuestCredentialStore.EditorLoginPrefix);
            if (!editorStore.TryGet(applicationId, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            raw = raw.Trim();
            if (Guid.TryParse(raw, out _))
            {
                await LoginByUserIdAsync(raw, cancellationToken);
            }
            else
            {
                await LoginAsync(
                    new LoginRequest
                    {
                        Provider = AuthProviders.Guest,
                        ExternalId = raw
                    },
                    cancellationToken);
            }

            RememberEditorLogin(applicationId);
            return true;
        }

        void RememberEditorLogin(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(Session?.PlayerId))
            {
                return;
            }

            var editorStore = EditorLogin ?? new PlayerPrefsGuestCredentialStore(
                PlayerPrefsGuestCredentialStore.EditorLoginPrefix);
            editorStore.Save(applicationId, Session.PlayerId);
        }
#endif

        LoginResult RequireSession()
        {
            if (!IsAuthenticated)
            {
                throw new BackendException("Could not establish a player session.", "auth_session_failed");
            }

            return new LoginResult(Session, GuestKeyFromSession(Session));
        }

        static string GuestKeyFromSession(PlayerSession playerSession)
        {
            if (playerSession == null)
            {
                return null;
            }

            if (!string.Equals(playerSession.Provider, AuthProviders.Guest, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(playerSession.ExternalId) ? null : playerSession.ExternalId;
        }

        static bool IsUnknownGuest(BackendException exception)
        {
            if (exception == null)
            {
                return false;
            }

            if (exception.StatusCode == 401)
            {
                return true;
            }

            var haystack = (exception.ErrorCode + " " + exception.ServerError + " " + exception.Message)
                .ToLowerInvariant();
            return haystack.IndexOf("guest_unknown", StringComparison.Ordinal) >= 0;
        }

        static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }
    }
}
