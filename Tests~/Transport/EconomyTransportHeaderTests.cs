using System;
using System.Threading.Tasks;
using BackendSdk;
using BackendSdk.Internal;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.Tests.Transport
{
    [Category("Economy")]
    public sealed class EconomyTransportHeaderTests
    {
        private RecordingTransportSession session;

        [SetUp]
        public void SetUp()
        {
            session = new RecordingTransportSession();
        }

        [TearDown]
        public void TearDown()
        {
            Backend.ResetForTests();
            session = null;
        }

        [Test]
        public async Task GetStateAsync_UsesAuthenticatedGetWithoutRequestId()
        {
            await InitializeEconomyTransportAsync().ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Economy.GetStateAsync().ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Get, request.Method);
            Assert.IsTrue(request.Url.EndsWith("/v1/economy/test-game/me", StringComparison.Ordinal));
            TransportTestSupport.AssertAuthorizationPresent(request);
            TransportTestSupport.AssertNoRequestId(request);
        }

        private async Task InitializeEconomyTransportAsync()
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

                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = BuildEconomyResponse()
                });
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
        }

        private static string BuildEconomyResponse()
        {
            return "{" +
                   "\"currencies\":[]," +
                   "\"entitlements\":[]," +
                   "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                   "}";
        }
    }
}
