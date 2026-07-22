using System;
using System.Collections.Generic;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.DotNetTests
{
  public sealed class TransportRetryPolicyTests
  {
    private static readonly Guid ProfileUserId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private TestTransportSettings settings;

    [SetUp]
    public void SetUp()
    {
      settings = new TestTransportSettings("https://api.example.com", "test-game");
    }

    [Test]
    public void UpdateProfile_RetriesReuseSameRequestIdAcrossAttempts()
    {
      var body = new JsonRequestBody("{\"displayName\":\"Player One\"}");
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Put, body);
      var attemptRequestIds = new List<string>();

      for (var attempt = 1; attempt <= 2; attempt++)
      {
        context.Attempt = attempt;
        var snapshot = TransportRequestBuilder.Build(
            settings,
            HttpVerb.Put,
            "v1/profiles/test-game/me",
            body,
            "Bearer " + TransportHeaderAssertions.AccessToken,
            context);

        attemptRequestIds.Add(TransportHeaderAssertions.AssertRequestId(snapshot));
      }

      Assert.AreEqual(attemptRequestIds[0], attemptRequestIds[1]);
      Assert.AreEqual(context.RequestId, attemptRequestIds[0]);
    }

    [Test]
    public void UpdateProfile_SeparateCallsReceiveDifferentRequestIds()
    {
      var body = new JsonRequestBody("{\"displayName\":\"Player One\"}");
      var firstContext = TransportRequestBuilder.CreateRequestContext(HttpVerb.Put, body);
      var secondContext = TransportRequestBuilder.CreateRequestContext(HttpVerb.Put, body);

      var firstSnapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Put,
          "v1/profiles/test-game/me",
          body,
          "Bearer " + TransportHeaderAssertions.AccessToken,
          firstContext);

      var secondSnapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Put,
          "v1/profiles/test-game/me",
          body,
          "Bearer " + TransportHeaderAssertions.AccessToken,
          secondContext);

      var firstRequestId = TransportHeaderAssertions.AssertRequestId(firstSnapshot);
      var secondRequestId = TransportHeaderAssertions.AssertRequestId(secondSnapshot);
      Assert.AreNotEqual(firstRequestId, secondRequestId);
    }

    [Test]
    public void BatchProfile_RetriesRemainWithoutRequestId()
    {
      var body = new ReadOnlyJsonRequestBody("{\"userIds\":[\"" + ProfileUserId + "\"]}");
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Post, body);

      for (var attempt = 1; attempt <= 2; attempt++)
      {
        context.Attempt = attempt;
        var snapshot = TransportRequestBuilder.Build(
            settings,
            HttpVerb.Post,
            "v1/profiles/test-game/batch",
            body,
            null,
            context);

        TransportHeaderAssertions.AssertNoRequestId(snapshot);
        Assert.AreEqual(TransportHttpMethods.Post, snapshot.Method);
      }
    }

    [Test]
    public void ReadOnlyJsonRequestBody_SuppressesOnlyIdempotencyKey_NotRetryAttempts()
    {
      var body = new ReadOnlyJsonRequestBody("{\"userIds\":[]}");
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Post, body);

      Assert.IsNull(context.RequestId);
      context.Attempt = 2;

      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Post,
          "v1/profiles/test-game/batch",
          body,
          null,
          context);

      TransportHeaderAssertions.AssertNoRequestId(snapshot);
      Assert.AreEqual(2, context.Attempt);
    }
  }
}
