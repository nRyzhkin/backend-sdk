using System;
using System.Threading.Tasks;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.Tests.Transport
{
    public sealed class ProfilesTransportHeaderTests
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
        public async Task GetBatchAsync_UsesAnonymousPostWithoutRequestIdAndNativeJsonBody()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);

            await Backend.Profiles.GetBatchAsync(new[] { TransportTestSupport.ProfileUserId }).ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Post, request.Method);
            Assert.IsTrue(request.Url.EndsWith("/v1/profiles/test-game/batch", StringComparison.Ordinal));
            Assert.AreEqual("application/json", request.Headers["Content-Type"]);
            TransportTestSupport.AssertAuthorizationAbsent(request);
            TransportTestSupport.AssertNoRequestId(request);
            Assert.AreEqual(1, session.Bodies.Count);
            Assert.IsInstanceOf<ReadOnlyJsonRequestBody>(session.Bodies[0]);
            Assert.IsTrue(request.Payload.Contains("\"userIds\":[\"" + TransportTestSupport.ProfileUserId + "\"]"));
            Assert.IsFalse(request.Payload.Contains("\"userIds\":\"["));
        }

        [Test]
        public async Task UpdateMeAsync_UsesPutWithAuthorizationRequestIdAndNativePublicData()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Profiles.UpdateMeAsync(
                "Player One",
                "avatar_03",
                new PublicProfileData
                {
                    status = "Looking for team",
                    level = 12
                }).ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Put, request.Method);
            TransportTestSupport.AssertAuthorizationPresent(request);
            TransportTestSupport.AssertRequestId(request);
            Assert.IsInstanceOf<JsonRequestBody>(session.Bodies[0]);
            Assert.IsTrue(request.Payload.Contains("\"publicData\":{\"status\":\"Looking for team\",\"level\":12}"));
            Assert.IsFalse(request.Payload.Contains("\"publicData\":\"{"));
        }

        [Test]
        public async Task UpdateMeAsync_RetriesReuseSameRequestIdAcrossAttempts()
        {
            var attemptRequestIds = new System.Collections.Generic.List<string>();
            var options = TransportTestSupport.CreateOptions(retryCount: 1, retryDelayMilliseconds: 0);
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));
            var attempts = 0;

            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                session.Snapshots.Add(TransportRequestBuilder.Build(
                    invocation.Settings,
                    invocation.Verb,
                    invocation.Path,
                    invocation.Body,
                    invocation.AuthorizationHeader,
                    invocation.Context));

                attemptRequestIds.Add(invocation.Context.RequestId);

                attempts++;
                if (attempts == 1)
                {
                    return Task.FromResult(new TransportSendResult
                    {
                        ExceptionToThrow = new BackendException("Transient", "request_failed", true, null, 503)
                    });
                }

                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = TransportTestSupport.BuildProfileResponse()
                });
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Profiles.UpdateMeAsync(
                "Player One",
                "avatar_03",
                new PublicProfileData { status = "Online", level = 1 }).ConfigureAwait(false);

            Assert.AreEqual(2, session.Snapshots.Count);
            var firstRequestId = TransportTestSupport.AssertRequestId(session.Snapshots[0]);
            var secondRequestId = TransportTestSupport.AssertRequestId(session.Snapshots[1]);
            Assert.AreEqual(firstRequestId, secondRequestId);
            Assert.AreEqual(firstRequestId, attemptRequestIds[0]);
            Assert.AreEqual(firstRequestId, attemptRequestIds[1]);
        }

        [Test]
        public async Task UpdateMeAsync_SeparateCallsReceiveDifferentRequestIds()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Profiles.UpdateMeAsync(
                "Player One",
                "avatar_03",
                new PublicProfileData { status = "Online", level = 1 }).ConfigureAwait(false);

            await Backend.Profiles.UpdateMeAsync(
                "Player Two",
                null,
                new PublicProfileData { status = "Away", level = 2 }).ConfigureAwait(false);

            Assert.AreEqual(2, session.Snapshots.Count);
            var firstRequestId = TransportTestSupport.AssertRequestId(session.Snapshots[0]);
            var secondRequestId = TransportTestSupport.AssertRequestId(session.Snapshots[1]);
            Assert.AreNotEqual(firstRequestId, secondRequestId);
        }

        [Test]
        public async Task GetBatchAsync_RetriesRemainWithoutRequestId()
        {
            var options = TransportTestSupport.CreateOptions(retryCount: 1, retryDelayMilliseconds: 0);
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));
            var attempts = 0;

            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                session.Snapshots.Add(TransportRequestBuilder.Build(
                    invocation.Settings,
                    invocation.Verb,
                    invocation.Path,
                    invocation.Body,
                    invocation.AuthorizationHeader,
                    invocation.Context));

                attempts++;
                if (attempts == 1)
                {
                    return Task.FromResult(new TransportSendResult
                    {
                        ExceptionToThrow = new BackendException("Transient", "request_failed", true, null, 503)
                    });
                }

                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = "{\"profiles\":[],\"missingUserIds\":[]}"
                });
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);

            await Backend.Profiles.GetBatchAsync(new[] { TransportTestSupport.ProfileUserId }).ConfigureAwait(false);

            Assert.AreEqual(2, session.Snapshots.Count);
            TransportTestSupport.AssertNoRequestId(session.Snapshots[0]);
            TransportTestSupport.AssertNoRequestId(session.Snapshots[1]);
        }

        [Test]
        public async Task GetMeAsync_UsesAuthenticatedGetWithoutRequestId()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);
            TransportTestSupport.Authenticate();

            await Backend.Profiles.GetMeAsync().ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Get, request.Method);
            Assert.IsTrue(request.Url.EndsWith("/v1/profiles/test-game/me", StringComparison.Ordinal));
            TransportTestSupport.AssertAuthorizationPresent(request);
            TransportTestSupport.AssertNoRequestId(request);
        }

        [Test]
        public async Task GetAsync_UsesAnonymousGetWithoutRequestId()
        {
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session).ConfigureAwait(false);

            await Backend.Profiles.GetAsync(TransportTestSupport.ProfileUserId).ConfigureAwait(false);

            var request = TransportTestSupport.AssertSingleRequest(session);
            Assert.AreEqual(TransportHttpMethods.Get, request.Method);
            Assert.IsTrue(request.Url.Contains("/v1/profiles/test-game/" + TransportTestSupport.ProfileUserId));
            TransportTestSupport.AssertAuthorizationAbsent(request);
            TransportTestSupport.AssertNoRequestId(request);
        }

        [Serializable]
        private sealed class PublicProfileData
        {
            public string status;
            public int level;
        }
    }
}
