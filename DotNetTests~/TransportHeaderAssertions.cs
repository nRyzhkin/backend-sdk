using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.DotNetTests
{
    internal static class TransportHeaderAssertions
    {
        internal const string AccessToken = "test-access-token";

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
    }
}
