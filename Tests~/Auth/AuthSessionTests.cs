using System;
using System.Threading.Tasks;
using BackendSdk;
using BackendSdk.Internal;
using BackendSdk.Tests.Transport;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.Tests.Auth
{
    public sealed class AuthSessionTests
    {
        MemoryGuestCredentialStore store;

        [SetUp]
        public void SetUp()
        {
            store = new MemoryGuestCredentialStore();
        }

        [TearDown]
        public void TearDown()
        {
            Backend.ResetForTests();
        }

        [Test]
        public async Task CreateGuest_PersistsServerIssuedKey()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "server-guest-key");

            var result = await Backend.Auth.CreateGuestAsync();

            Assert.AreEqual("server-guest-key", result.GuestKey);
            Assert.AreEqual(AuthProviders.Guest, result.Session.Provider);
            Assert.IsTrue(store.TryGet(TransportTestSupport.ApplicationId, out var saved));
            Assert.AreEqual("server-guest-key", saved);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/guest", StringComparison.Ordinal));
            TransportTestSupport.AssertAuthorizationAbsent(session.Snapshots[0]);
            TransportTestSupport.AssertRequestId(session.Snapshots[0]);
        }

        [Test]
        public async Task EnsureSession_WithoutPlatform_CreatesGuestOnceThenRestores()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "issued-1");

            await Backend.Auth.EnsureSessionAsync();
            Assert.AreEqual(1, session.Snapshots.Count);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/guest", StringComparison.Ordinal));

            session.Snapshots.Clear();
            session.Bodies.Clear();
            await Backend.Auth.LogoutAsync();
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "issued-1");
            await Backend.Auth.EnsureSessionAsync();

            Assert.AreEqual(1, session.Snapshots.Count);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/login", StringComparison.Ordinal));
            var body = (JsonRequestBody)session.Bodies[0];
            StringAssert.Contains("issued-1", body.Json);
        }

        [Test]
        public async Task EnsureSession_WithPlatformAndStoredGuest_Links()
        {
            store.Save(TransportTestSupport.ApplicationId, "issued-1");
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "issued-1");

            await Backend.Auth.EnsureSessionAsync(new LoginRequest
            {
                Provider = "steam",
                ExternalId = "steam-1"
            });

            Assert.AreEqual(2, session.Snapshots.Count);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/login", StringComparison.Ordinal));
            Assert.IsTrue(session.Snapshots[1].Url.EndsWith("/v1/auth/link", StringComparison.Ordinal));
            TransportTestSupport.AssertAuthorizationPresent(session.Snapshots[1]);
        }

        [Test]
        public async Task EnsureSession_AlreadyAuthenticatedGuest_WithPlatform_OnlyLinks()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "issued-1");

            await Backend.Auth.CreateGuestAsync();
            session.Snapshots.Clear();
            session.Bodies.Clear();

            await Backend.Auth.EnsureSessionAsync(new LoginRequest
            {
                Provider = "steam",
                ExternalId = "steam-1"
            });

            Assert.AreEqual(1, session.Snapshots.Count);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/link", StringComparison.Ordinal));
        }

        [Test]
        public async Task EnsureSession_AlreadyAuthenticated_WithoutPlatform_DoesNotRelogin()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "issued-1");

            await Backend.Auth.EnsureSessionAsync();
            session.Snapshots.Clear();
            session.Bodies.Clear();

            await Backend.Auth.EnsureSessionAsync();

            Assert.AreEqual(0, session.Snapshots.Count);
        }

        [Test]
        public async Task EnsureSession_UnknownStoredGuest_CreatesReplacement()
        {
            store.Save(TransportTestSupport.ApplicationId, "stale-key");
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(
                session,
                guestKey: "fresh-key",
                unknownGuestOnLogin: true);

            await Backend.Auth.EnsureSessionAsync();

            Assert.AreEqual(2, session.Snapshots.Count);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/login", StringComparison.Ordinal));
            Assert.IsTrue(session.Snapshots[1].Url.EndsWith("/v1/auth/guest", StringComparison.Ordinal));
            Assert.IsTrue(store.TryGet(TransportTestSupport.ApplicationId, out var saved));
            Assert.AreEqual("fresh-key", saved);
        }

        [Test]
        public async Task EnsureSession_PlatformOnly_LogsInProvider()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, provider: "yandexgames");

            await Backend.Auth.EnsureSessionAsync(new LoginRequest
            {
                Provider = "yandexgames",
                ExternalId = "yg-1"
            });

            Assert.AreEqual(1, session.Snapshots.Count);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/login", StringComparison.Ordinal));
            var body = (JsonRequestBody)session.Bodies[0];
            StringAssert.Contains("yg-1", body.Json);
        }

        [Test]
        public async Task GuestLogin_IsAnonymous()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            TransportTestSupport.Authenticate();
            Backend.Auth.GuestCredentials = store;
            UnityWebRequestTransportOverride.SetLoginHandler(session, guestKey: "issued-1");

            await Backend.Auth.LoginAsync(new LoginRequest
            {
                Provider = AuthProviders.Guest,
                ExternalId = "issued-1"
            });

            TransportTestSupport.AssertAuthorizationAbsent(session.Snapshots[0]);
        }

        [Test]
        public async Task LoginByUserIdAsync_PostsImpersonateAnonymously()
        {
            var session = new RecordingTransportSession();
            await TransportTestSupport.InitializeWithRecordingTransportAsync(
                TransportTestSupport.CreateOptions(),
                session);
            UnityWebRequestTransportOverride.SetLoginHandler(session, provider: "guest");

            var result = await Backend.Auth.LoginByUserIdAsync("3fa85f64-5717-4562-b3fc-2c963f66afa6");

            Assert.AreEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6", result.Session.PlayerId);
            Assert.IsTrue(session.Snapshots[0].Url.EndsWith("/v1/auth/impersonate", StringComparison.Ordinal));
            TransportTestSupport.AssertAuthorizationAbsent(session.Snapshots[0]);
            var body = (JsonRequestBody)session.Bodies[0];
            StringAssert.Contains("3fa85f64-5717-4562-b3fc-2c963f66afa6", body.Json);
        }
    }

    internal static class UnityWebRequestTransportOverride
    {
        internal static void SetLoginHandler(
            RecordingTransportSession session,
            string guestKey = null,
            string provider = null,
            bool unknownGuestOnLogin = false)
        {
            UnityWebRequestTransport.TestSendOnceHandler = invocation =>
            {
                session.Bodies.Add(invocation.Body);
                session.Snapshots.Add(TransportRequestBuilder.Build(
                    invocation.Settings,
                    invocation.Verb,
                    invocation.Path,
                    invocation.Body,
                    invocation.AuthorizationHeader,
                    invocation.Context));

                if (unknownGuestOnLogin && invocation.Path.EndsWith("/login"))
                {
                    throw new BackendException(
                        "Unknown guest credential.",
                        "guest_unknown",
                        statusCode: 401);
                }

                var resolvedProvider = provider;
                if (string.IsNullOrEmpty(resolvedProvider))
                {
                    if (invocation.Path.EndsWith("/guest") ||
                        guestKey != null && invocation.Path.EndsWith("/login"))
                        resolvedProvider = "guest";
                    else
                        resolvedProvider = "steam";
                }

                var keyJson = guestKey != null ? "\"guestKey\":\"" + guestKey + "\"," : string.Empty;
                var json = "{" +
                           "\"userId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"," +
                           "\"accessToken\":\"test-access-token\"," +
                           keyJson +
                           "\"provider\":\"" + resolvedProvider + "\"," +
                           "\"expiresAt\":\"2030-01-01T00:00:00Z\"" +
                           "}";

                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = json
                });
            };
        }
    }
}
