using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk;
using BackendSdk.Internal;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.Tests.Transport
{
    internal sealed class RecordingTransportSession
    {
        internal IList<TransportRequestSnapshot> Snapshots { get; } = new List<TransportRequestSnapshot>();

        internal IList<object> Bodies { get; } = new List<object>();
    }

    internal static class TransportTestSupport
    {
        internal const string ApplicationId = "test-game";
        internal const string AccessToken = "test-access-token";
        internal static readonly Guid ProfileUserId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        internal static BackendOptions CreateOptions(int retryCount = 0, int retryDelayMilliseconds = 0)
        {
            return new BackendOptions
            {
                ServerUrl = "https://api.example.com",
                ApplicationId = ApplicationId,
                RetryCount = retryCount,
                RetryDelayMilliseconds = retryDelayMilliseconds
            };
        }

        internal static async Task InitializeWithRecordingTransportAsync(
            BackendOptions options,
            RecordingTransportSession session)
        {
            var transport = new UnityWebRequestTransport(BackendSettings.FromOptions(options));
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

                return Task.FromResult(new TransportSendResult
                {
                    ResponseText = ResolveResponse(invocation)
                });
            };

            await Backend.InitializeForTestsAsync(options, transport).ConfigureAwait(false);
        }

        internal static void Authenticate()
        {
            Backend.Auth.SetSession(new PlayerSession(
                ProfileUserId.ToString(),
                AccessToken,
                DateTime.UtcNow.AddHours(1),
                "dev",
                "external",
                true));
        }

        internal static TransportRequestSnapshot AssertSingleRequest(RecordingTransportSession session)
        {
            Assert.AreEqual(1, session.Snapshots.Count, "Expected exactly one transport request.");
            return session.Snapshots[0];
        }

        internal static void AssertNoRequestId(TransportRequestSnapshot snapshot)
        {
            Assert.IsFalse(
                snapshot.HasHeader(TransportRequestBuilder.RequestIdHeader),
                "X-Request-Id must not be present.");
        }

        internal static string AssertRequestId(TransportRequestSnapshot snapshot)
        {
            Assert.IsTrue(
                snapshot.TryGetHeader(TransportRequestBuilder.RequestIdHeader, out var requestId),
                "X-Request-Id must be present.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(requestId), "X-Request-Id must not be empty.");
            return requestId;
        }

        internal static void AssertAuthorizationPresent(TransportRequestSnapshot snapshot)
        {
            Assert.IsTrue(
                snapshot.TryGetHeader(TransportRequestBuilder.AuthorizationHeader, out var authorization),
                "Authorization must be present.");
            Assert.AreEqual("Bearer " + AccessToken, authorization);
        }

        internal static void AssertAuthorizationAbsent(TransportRequestSnapshot snapshot)
        {
            Assert.IsFalse(
                snapshot.HasHeader(TransportRequestBuilder.AuthorizationHeader),
                "Authorization must not be present.");
        }

        internal static string ResolveResponse(TransportSendInvocation invocation)
        {
            if (invocation.Verb == HttpVerb.Post
                && invocation.Path.EndsWith("/batch", StringComparison.Ordinal))
            {
                return "{\"profiles\":[],\"missingUserIds\":[]}";
            }

            if (invocation.Verb == HttpVerb.Delete)
            {
                return string.Empty;
            }

            if (invocation.Verb == HttpVerb.Post
                && invocation.Path == "v1/analytics/" + ApplicationId + "/events")
            {
                return string.Empty;
            }

            if (invocation.Verb == HttpVerb.Put
                && invocation.Path.Contains("/leaderboards/"))
            {
                return "{\"value\":1000,\"rank\":1}";
            }

            if (invocation.Verb == HttpVerb.Get
                && invocation.Path.StartsWith("v1/storage/", StringComparison.Ordinal))
            {
                return "{\"value\":\"{\\\"Level\\\":1}\"}";
            }

            if (invocation.Verb == HttpVerb.Get
                && invocation.Path.StartsWith("v1/leaderboards/", StringComparison.Ordinal))
            {
                return "{\"entries\":[]}";
            }

            if (invocation.Verb == HttpVerb.Get
                && invocation.Path.StartsWith("v1/remote-config/", StringComparison.Ordinal))
            {
                return "\"https://api.example.com\"";
            }

            return BuildProfileResponse();
        }

        internal static string BuildProfileResponse()
        {
            return "{" +
                   "\"userId\":\"" + ProfileUserId + "\"," +
                   "\"applicationId\":\"" + ApplicationId + "\"," +
                   "\"displayName\":\"Player One\"," +
                   "\"avatarId\":null," +
                   "\"publicData\":{\"status\":\"Online\",\"level\":5}," +
                   "\"createdAt\":\"2026-07-22T12:00:00Z\"," +
                   "\"updatedAt\":\"2026-07-22T12:05:00Z\"" +
                   "}";
        }
    }
}
