using System;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.DotNetTests
{
  public sealed class ProfilesTransportHeaderTests
  {
    private static readonly Guid ProfileUserId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private TestTransportSettings settings;

    [SetUp]
    public void SetUp()
    {
      settings = new TestTransportSettings("https://api.example.com", "test-game");
    }

    [Test]
    public void GetBatchAsync_UsesAnonymousPostWithoutRequestIdAndNativeJsonBody()
    {
      var body = new ReadOnlyJsonRequestBody("{\"userIds\":[\"" + ProfileUserId + "\"]}");
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Post, body);
      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Post,
          "v1/profiles/test-game/batch",
          body,
          null,
          context);

      Assert.AreEqual(TransportHttpMethods.Post, snapshot.Method);
      Assert.IsTrue(snapshot.Url.EndsWith("/v1/profiles/test-game/batch", StringComparison.Ordinal));
      Assert.AreEqual("application/json", snapshot.Headers["Content-Type"]);
      TransportHeaderAssertions.AssertAuthorizationAbsent(snapshot);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
      Assert.IsTrue(snapshot.Payload.Contains("\"userIds\":[\"" + ProfileUserId + "\"]"));
      Assert.IsFalse(snapshot.Payload.Contains("\"userIds\":\"["));
    }

    [Test]
    public void UpdateMeAsync_UsesPutWithAuthorizationRequestIdAndNativePublicData()
    {
      var body = new JsonRequestBody(
          "{\"displayName\":\"Player One\",\"avatarId\":\"avatar_03\",\"publicData\":{\"status\":\"Looking for team\",\"level\":12}}");
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Put, body);
      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Put,
          "v1/profiles/test-game/me",
          body,
          "Bearer " + TransportHeaderAssertions.AccessToken,
          context);

      Assert.AreEqual(TransportHttpMethods.Put, snapshot.Method);
      TransportHeaderAssertions.AssertAuthorizationPresent(snapshot);
      TransportHeaderAssertions.AssertRequestId(snapshot);
      Assert.IsTrue(snapshot.Payload.Contains("\"publicData\":{\"status\":\"Looking for team\",\"level\":12}"));
      Assert.IsFalse(snapshot.Payload.Contains("\"publicData\":\"{"));
    }

    [Test]
    public void GetMeAsync_UsesAuthenticatedGetWithoutRequestId()
    {
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Get, null);
      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Get,
          "v1/profiles/test-game/me",
          null,
          "Bearer " + TransportHeaderAssertions.AccessToken,
          context);

      Assert.AreEqual(TransportHttpMethods.Get, snapshot.Method);
      Assert.IsTrue(snapshot.Url.EndsWith("/v1/profiles/test-game/me", StringComparison.Ordinal));
      TransportHeaderAssertions.AssertAuthorizationPresent(snapshot);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
    }

    [Test]
    public void GetAsync_UsesAnonymousGetWithoutRequestId()
    {
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Get, null);
      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Get,
          "v1/profiles/test-game/" + ProfileUserId,
          null,
          null,
          context);

      Assert.AreEqual(TransportHttpMethods.Get, snapshot.Method);
      Assert.IsTrue(snapshot.Url.Contains("/v1/profiles/test-game/" + ProfileUserId));
      TransportHeaderAssertions.AssertAuthorizationAbsent(snapshot);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
    }
  }
}
