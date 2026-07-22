using System;
using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.DotNetTests
{
  public sealed class TransportRequestBuilderTests
  {
    private TestTransportSettings settings;

    [SetUp]
    public void SetUp()
    {
      settings = new TestTransportSettings("https://api.example.com", "test-game");
    }

    [Test]
    public void CreateRequestContext_ReadOnlyJsonPost_DoesNotCreateRequestId()
    {
      var context = TransportRequestBuilder.CreateRequestContext(
          HttpVerb.Post,
          new ReadOnlyJsonRequestBody("{\"userIds\":[]}"));

      Assert.IsNull(context.RequestId);
    }

    [Test]
    public void CreateRequestContext_WritePut_CreatesRequestId()
    {
      var context = TransportRequestBuilder.CreateRequestContext(
          HttpVerb.Put,
          new JsonRequestBody("{\"value\":1}"));

      Assert.IsFalse(string.IsNullOrWhiteSpace(context.RequestId));
    }

    [Test]
    public void CreateRequestContext_Get_DoesNotCreateRequestId()
    {
      var context = TransportRequestBuilder.CreateRequestContext(HttpVerb.Get, null);

      Assert.IsNull(context.RequestId);
    }

    [Test]
    public void Build_ReadOnlyJsonPost_DoesNotAttachRequestIdHeader()
    {
      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Post,
          "v1/profiles/test-game/batch",
          new ReadOnlyJsonRequestBody("{\"userIds\":[]}"),
          null,
          new RequestContext(null));

      TransportHeaderAssertions.AssertNoRequestId(snapshot);
      Assert.AreEqual("application/json", snapshot.Headers["Content-Type"]);
      Assert.AreEqual("{\"userIds\":[]}", snapshot.Payload);
    }

    [Test]
    public void Build_WritePut_AttachesRequestIdHeader()
    {
      var requestId = "abc123";
      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Put,
          "v1/profiles/test-game/me",
          new JsonRequestBody("{\"displayName\":\"Player\"}"),
          "Bearer token",
          new RequestContext(requestId));

      Assert.AreEqual(requestId, snapshot.Headers[TransportRequestBuilder.RequestIdHeader]);
      Assert.AreEqual("Bearer token", snapshot.Headers[TransportRequestBuilder.AuthorizationHeader]);
    }

    [Test]
    public void ReadOnlyJsonRequestBody_DoesNotAlterSerializedPayload()
    {
      const string json = "{\"userIds\":[\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"]}";
      var body = new ReadOnlyJsonRequestBody(json);

      var snapshot = TransportRequestBuilder.Build(
          settings,
          HttpVerb.Post,
          "v1/profiles/test-game/batch",
          body,
          null,
          TransportRequestBuilder.CreateRequestContext(HttpVerb.Post, body));

      Assert.AreEqual(json, snapshot.Payload);
    }
  }
}
