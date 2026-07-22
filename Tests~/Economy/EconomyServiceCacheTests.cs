using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk;
using BackendSdk.Internal;
using BackendSdk.Tests.Transport;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.Tests.Economy
{
    [Category("Economy")]
    public sealed class EconomyServiceCacheTests
    {
        private RecordingTransportSession session;
        private int requestCount;

        [SetUp]
        public void SetUp()
        {
            session = new RecordingTransportSession();
            requestCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Backend.ResetForTests();
            session = null;
        }

        [Test]
        public async Task GetStateAsync_UsesCache()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            await Backend.Economy.GetStateAsync().ConfigureAwait(false);

            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task GetStateAsync_ForceRefreshCallsBackendAgain()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            await Backend.Economy.GetStateAsync(forceRefresh: true).ConfigureAwait(false);

            Assert.AreEqual(2, requestCount);
        }

        [Test]
        public async Task RefreshAsync_ReplacesCachedState()
        {
            await InitializeEconomyTransportAsync(balance: 100L).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            var initial = await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(100L, initial.GetCurrencyBalance("gold"));

            requestCount = 0;
            await InitializeEconomyTransportAsync(balance: 250L).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            var refreshed = await Backend.Economy.RefreshAsync().ConfigureAwait(false);
            Assert.AreEqual(250L, refreshed.GetCurrencyBalance("gold"));
            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task ClearCache_ForcesReload()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Backend.Economy.ClearCache();
            await Backend.Economy.GetStateAsync().ConfigureAwait(false);

            Assert.AreEqual(2, requestCount);
        }

        [Test]
        public async Task GetDefinitionsAsync_UsesSharedCacheWithState()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetDefinitionsAsync().ConfigureAwait(false);
            await Backend.Economy.GetStateAsync().ConfigureAwait(false);

            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task FailedRequest_IsNotCached()
        {
            await InitializeEconomyTransportAsync(failRequests: true).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            Assert.ThrowsAsync<BackendException>(() => Backend.Economy.GetStateAsync());
            Assert.ThrowsAsync<BackendException>(() => Backend.Economy.GetStateAsync());

            Assert.AreEqual(2, requestCount);
        }

        [Test]
        public void Unauthorized_UsesExistingSdkError()
        {
            Assert.ThrowsAsync<BackendException>(async () =>
            {
                await InitializeEconomyTransportAsync().ConfigureAwait(false);
                await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            });

            Assert.AreEqual(0, requestCount);
        }

        [Test]
        public async Task Cancellation_PropagatesCorrectly()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                Assert.ThrowsAsync<OperationCanceledException>(() =>
                    Backend.Economy.GetStateAsync(cancellationToken: cts.Token));
            }

            Assert.AreEqual(0, requestCount);
        }

        [Test]
        public async Task LogoutOrSessionChange_ClearsEconomyCache()
        {
            await InitializeEconomyTransportAsync(balance: 100L).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(1, requestCount);

            await Backend.Auth.LogoutAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(2, requestCount);
        }

        [Test]
        public async Task ConcurrentGetStateAsync_UsesSingleFlight()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            var first = Backend.Economy.GetStateAsync();
            var second = Backend.Economy.GetStateAsync();
            await Task.WhenAll(first, second).ConfigureAwait(false);

            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task ConcurrentInitialLoads_UseSingleRequest()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            var stateTask = Backend.Economy.GetStateAsync();
            var definitionsTask = Backend.Economy.GetDefinitionsAsync();
            await Task.WhenAll(stateTask, definitionsTask).ConfigureAwait(false);

            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task FailedInitialLoad_AllowsRetry()
        {
            await InitializeEconomyTransportAsync(failRequests: true).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            Assert.ThrowsAsync<BackendException>(() => Backend.Economy.GetStateAsync());

            requestCount = 0;
            await InitializeEconomyTransportAsync(balance: 175L).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            var state = await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(175L, state.GetCurrencyBalance("gold"));
            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task CancelledCaller_DoesNotCancelSharedLoad()
        {
            var releaseGate = CreateReleaseGate();
            var options = TransportTestSupport.CreateOptions();
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));

            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                requestCount++;
                return AwaitReleaseAndRespond(releaseGate, BuildEconomyResponse(150L));
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            var sharedLoad = Backend.Economy.GetStateAsync();
            using (var cts = new CancellationTokenSource())
            {
                var cancelledWait = Backend.Economy.GetStateAsync(cancellationToken: cts.Token);
                await Task.Delay(50).ConfigureAwait(false);
                cts.Cancel();

                try
                {
                    await cancelledWait.ConfigureAwait(false);
                    Assert.Fail("Expected OperationCanceledException.");
                }
                catch (OperationCanceledException)
                {
                }
            }

            releaseGate.TrySetResult(true);
            var state = await sharedLoad.ConfigureAwait(false);
            Assert.AreEqual(150L, state.GetCurrencyBalance("gold"));
            Assert.AreEqual(1, requestCount);

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task InFlightRequest_FromOldSession_DoesNotPopulateNewSessionCache()
        {
            var releaseGate = CreateReleaseGate();
            var playerAId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var playerBId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var options = TransportTestSupport.CreateOptions();
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));

            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                requestCount++;
                var tokenAtRequest = invocation.AuthorizationHeader;
                return AwaitReleaseAndRespond(
                    releaseGate,
                    BuildEconomyResponse(
                        string.Equals(tokenAtRequest, "Bearer token-a", StringComparison.Ordinal) ? 100L : 500L));
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
            Backend.Auth.SetSession(new PlayerSession(
                playerAId.ToString(),
                "token-a",
                DateTime.UtcNow.AddHours(1),
                "dev",
                "player-a",
                true));

            var playerARefresh = Backend.Economy.GetStateAsync(forceRefresh: true);
            await Task.Delay(50).ConfigureAwait(false);

            Backend.Auth.SetSession(new PlayerSession(
                playerBId.ToString(),
                "token-b",
                DateTime.UtcNow.AddHours(1),
                "dev",
                "player-b",
                true));

            releaseGate.TrySetResult(true);
            var playerAResult = await playerARefresh.ConfigureAwait(false);
            Assert.AreEqual(100L, playerAResult.GetCurrencyBalance("gold"));

            var playerBState = await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(500L, playerBState.GetCurrencyBalance("gold"));
            Assert.AreEqual(2, requestCount);
        }

        [Test]
        public async Task SharedLoadCompletion_DoesNotCacheAfterSessionChange()
        {
            var releaseGate = CreateReleaseGate();
            var playerAId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var playerBId = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var options = TransportTestSupport.CreateOptions();
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));

            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                requestCount++;
                return AwaitReleaseAndRespond(releaseGate, BuildEconomyResponse(100L));
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
            Backend.Auth.SetSession(new PlayerSession(
                playerAId.ToString(),
                "token-a",
                DateTime.UtcNow.AddHours(1),
                "dev",
                "player-a",
                true));

            var inFlight = Backend.Economy.GetStateAsync(forceRefresh: true);
            await Task.Delay(50).ConfigureAwait(false);
            Backend.Auth.SetSession(new PlayerSession(
                playerBId.ToString(),
                "token-b",
                DateTime.UtcNow.AddHours(1),
                "dev",
                "player-b",
                true));

            releaseGate.TrySetResult(true);
            await inFlight.ConfigureAwait(false);

            requestCount = 0;
            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                requestCount++;
                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = BuildEconomyResponse(900L)
                });
            };

            var playerBState = await Backend.Economy.GetStateAsync().ConfigureAwait(false);
            Assert.AreEqual(900L, playerBState.GetCurrencyBalance("gold"));
            Assert.AreEqual(1, requestCount);
        }

        private async Task InitializeEconomyTransportAsync(
            long balance = 150L,
            bool failRequests = false)
        {
            var options = TransportTestSupport.CreateOptions();
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));

            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                session.Snapshots.Add(TransportRequestBuilder.Build(
                    invocation.Settings,
                    invocation.Verb,
                    invocation.Path,
                    invocation.Body,
                    invocation.AuthorizationHeader,
                    invocation.Context));

                requestCount++;

                if (failRequests)
                {
                    return Task.FromResult(new TransportSendResult
                    {
                        ExceptionToThrow = new BackendException("Server error", "request_failed", false, null, 500)
                    });
                }

                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = BuildEconomyResponse(balance)
                });
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
        }

        private static TaskCompletionSource<bool> CreateReleaseGate()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private static async Task<TransportSendResult> AwaitReleaseAndRespond(
            TaskCompletionSource<bool> releaseGate,
            string responseText)
        {
            await releaseGate.Task.ConfigureAwait(false);
            return new TransportSendResult
            {
                ResponseText = responseText
            };
        }

        private static string BuildEconomyResponse(long goldBalance)
        {
            return "{" +
                   "\"currencies\":[" +
                   "{" +
                   "\"key\":\"gold\"," +
                   "\"displayName\":\"Gold\"," +
                   "\"balance\":" + goldBalance + "," +
                   "\"maxBalance\":10000," +
                   "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                   "}" +
                   "]," +
                   "\"entitlements\":[" +
                   "{" +
                   "\"key\":\"remove_ads\"," +
                   "\"displayName\":\"Remove Ads\"," +
                   "\"kind\":\"permanent\"," +
                   "\"quantity\":1," +
                   "\"owned\":true," +
                   "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                   "},{" +
                   "\"key\":\"arena_ticket\"," +
                   "\"displayName\":\"Arena Ticket\"," +
                   "\"kind\":\"consumable\"," +
                   "\"quantity\":3," +
                   "\"owned\":true," +
                   "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                   "}" +
                   "]," +
                   "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                   "}";
        }
    }
}
