using System.Threading.Tasks;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.Tests.Transport
{
    public sealed class WriteOperationRequestIdMatrixTests
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
        public async Task StorageSetAsync_SendsRequestId()
        {
            await InitializeAuthenticatedAsync().ConfigureAwait(false);

            await Backend.Storage.SetAsync("save-key", new SaveData { Level = 1 }).ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Put, request.Method);
            TransportTestSupport.AssertRequestId(request);
        }

        [Test]
        public async Task StorageGetAsync_DoesNotSendRequestId()
        {
            await InitializeAuthenticatedAsync().ConfigureAwait(false);

            await Backend.Storage.GetAsync<SaveData>("save-key").ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Get, request.Method);
            TransportTestSupport.AssertNoRequestId(request);
        }

        [Test]
        public async Task StorageDeleteAsync_SendsRequestId()
        {
            await InitializeAuthenticatedAsync().ConfigureAwait(false);

            await Backend.Storage.DeleteAsync("save-key").ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Delete, request.Method);
            TransportTestSupport.AssertRequestId(request);
        }

        [Test]
        public async Task LeaderboardsSubmitAsync_SendsRequestId()
        {
            await InitializeAuthenticatedAsync().ConfigureAwait(false);

            await Backend.Leaderboards.SubmitAsync("highscore", 1000, SortMode.Descending).ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Put, request.Method);
            TransportTestSupport.AssertRequestId(request);
        }

        [Test]
        public async Task LeaderboardsGetTopAsync_DoesNotSendRequestId()
        {
            await InitializeAuthenticatedAsync().ConfigureAwait(false);

            await Backend.Leaderboards.GetTopAsync("highscore").ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Get, request.Method);
            TransportTestSupport.AssertNoRequestId(request);
        }

        [Test]
        public async Task AnalyticsTrackAsync_SendsRequestId()
        {
            await InitializeAuthenticatedAsync().ConfigureAwait(false);

            await Backend.Analytics.TrackAsync("LevelStarted", new { level = 5 }).ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Post, request.Method);
            TransportTestSupport.AssertRequestId(request);
            Assert.IsInstanceOf<JsonRequestBody>(session.Bodies[0]);
        }

        [Test]
        public async Task RemoteConfigGetAsync_DoesNotSendRequestId()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);

            await Backend.RemoteConfig.GetAsync("apiUrl").ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Get, request.Method);
            TransportTestSupport.AssertNoRequestId(request);
        }

        private async Task InitializeAuthenticatedAsync()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);
            TransportTestSupport.Authenticate();
        }

        [System.Serializable]
        private sealed class SaveData
        {
            public int Level;
        }
    }
}
