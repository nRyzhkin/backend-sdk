using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;
using BackendSdk.Transport.Core;

namespace BackendSdk
{
    /// <summary>
    /// Provides the public entry point for Backend SDK services.
    /// </summary>
    public static class Backend
    {
        private static readonly SemaphoreSlim InitializeGate = new SemaphoreSlim(1, 1);

        private static BackendClient client;

        /// <summary>
        /// Gets the authentication service facade.
        /// </summary>
        public static AuthService Auth { get; } = new AuthService();

        /// <summary>
        /// Gets the storage service facade.
        /// </summary>
        public static StorageService Storage { get; } = new StorageService();

        /// <summary>
        /// Gets the leaderboards service facade.
        /// </summary>
        public static LeaderboardsService Leaderboards { get; } = new LeaderboardsService();

        /// <summary>
        /// Gets the analytics service facade.
        /// </summary>
        public static AnalyticsService Analytics { get; } = new AnalyticsService();

        /// <summary>
        /// Gets the remote config service facade.
        /// </summary>
        public static RemoteConfigService RemoteConfig { get; } = new RemoteConfigService();

        /// <summary>
        /// Gets the player profiles service facade.
        /// </summary>
        public static ProfilesService Profiles { get; } = new ProfilesService();

        /// <summary>
        /// Gets the friends service facade.
        /// </summary>
        public static FriendsService Friends { get; } = new FriendsService();

        /// <summary>
        /// Gets the inventory service facade.
        /// </summary>
        public static InventoryService Inventory { get; } = new InventoryService();

        /// <summary>
        /// Gets the economy service facade.
        /// </summary>
        public static EconomyService Economy { get; } = new EconomyService();

        /// <summary>
        /// Gets a value indicating whether the SDK has been initialized.
        /// </summary>
        public static bool IsInitialized => client != null;

        /// <summary>
        /// Gets the active SDK settings after initialization.
        /// </summary>
        public static BackendSettings Settings { get; private set; }

        /// <summary>
        /// Initializes the SDK using runtime settings generated from the project configuration.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token for the initialization operation.</param>
        /// <returns>A task that completes when initialization finishes.</returns>
        public static Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var loadedOptions = RuntimeSettingsLoader.LoadOptions();
            return InitializeAsync(loadedOptions, cancellationToken);
        }

        /// <summary>
        /// Initializes the SDK using the provided configuration values.
        /// </summary>
        /// <param name="options">The configuration values to use.</param>
        /// <param name="cancellationToken">A cancellation token for the initialization operation.</param>
        /// <returns>A task that completes when initialization finishes.</returns>
        public static async Task InitializeAsync(BackendOptions options, CancellationToken cancellationToken = default)
        {
            await InitializeGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (client != null)
                {
                    return;
                }

                var resolvedOptions = options ?? new BackendOptions();
                Settings = BackendSettings.FromOptions(resolvedOptions);
                TransportRequestBuilder.BodySerializer = UnityTransportBodySerializer.Instance;

                if (string.IsNullOrWhiteSpace(Settings.ServerUrl))
                {
                    throw new BackendException(
                        "Backend URL is not configured. Set it in Project Settings > Backend.",
                        "missing_server_url");
                }

                var transport = new UnityWebRequestTransport(Settings);
                client = new BackendClient(Settings, transport);

                await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                InitializeGate.Release();
            }
        }

        internal static BackendClient ClientOrThrow()
        {
            if (client == null)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }

            return client;
        }

        internal static async Task InitializeForTestsAsync(
            BackendOptions options,
            IBackendTransport transport,
            CancellationToken cancellationToken = default)
        {
            await InitializeGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                client = null;
                var resolvedOptions = options ?? new BackendOptions();
                Settings = BackendSettings.FromOptions(resolvedOptions);
                TransportRequestBuilder.BodySerializer = UnityTransportBodySerializer.Instance;
                client = new BackendClient(Settings, transport);
                await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                InitializeGate.Release();
            }
        }

        internal static void ResetForTests()
        {
            client = null;
            Settings = null;
            Economy.ClearCache();
            Auth.ClearSession();
            UnityWebRequestTransport.TestSendOnceHandler = null;
        }
    }
}
