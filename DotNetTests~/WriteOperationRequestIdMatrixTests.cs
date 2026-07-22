using BackendSdk.Transport.Core;
using NUnit.Framework;

namespace BackendSdk.DotNetTests
{
  public sealed class WriteOperationRequestIdMatrixTests
  {
    private TestTransportSettings settings;

    [SetUp]
    public void SetUp()
    {
      settings = new TestTransportSettings("https://api.example.com", "test-game");
    }

    [Test]
    public void StorageSetAsync_SendsRequestId()
    {
      var body = new JsonRequestBody("{\"value\":\"{}\"}");
      var snapshot = BuildWrite(
          HttpVerb.Put,
          "v1/storage/test-game/save-key",
          body,
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Put, snapshot.Method);
      TransportHeaderAssertions.AssertRequestId(snapshot);
    }

    [Test]
    public void StorageGetAsync_DoesNotSendRequestId()
    {
      var snapshot = BuildRead(
          HttpVerb.Get,
          "v1/storage/test-game/save-key",
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Get, snapshot.Method);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
    }

    [Test]
    public void StorageDeleteAsync_SendsRequestId()
    {
      var snapshot = BuildWrite(
          HttpVerb.Delete,
          "v1/storage/test-game/save-key",
          null,
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Delete, snapshot.Method);
      TransportHeaderAssertions.AssertRequestId(snapshot);
    }

    [Test]
    public void LeaderboardsSubmitAsync_SendsRequestId()
    {
      var body = new JsonRequestBody("{\"value\":1000,\"sortMode\":1}");
      var snapshot = BuildWrite(
          HttpVerb.Put,
          "v1/leaderboards/test-game/highscore",
          body,
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Put, snapshot.Method);
      TransportHeaderAssertions.AssertRequestId(snapshot);
    }

    [Test]
    public void LeaderboardsGetTopAsync_DoesNotSendRequestId()
    {
      var snapshot = BuildRead(
          HttpVerb.Get,
          "v1/leaderboards/test-game/highscore?limit=100",
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Get, snapshot.Method);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
    }

    [Test]
    public void AnalyticsTrackAsync_SendsRequestId()
    {
      var body = new JsonRequestBody("{\"eventName\":\"LevelStarted\",\"parameters\":{\"level\":5}}");
      var snapshot = BuildWrite(
          HttpVerb.Post,
          "v1/analytics/test-game/events",
          body,
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Post, snapshot.Method);
      TransportHeaderAssertions.AssertRequestId(snapshot);
    }

    [Test]
    public void RemoteConfigGetAsync_DoesNotSendRequestId()
    {
      var snapshot = BuildRead(
          HttpVerb.Get,
          "v1/remote-config/test-game/apiUrl",
          authorized: false);

      Assert.AreEqual(TransportHttpMethods.Get, snapshot.Method);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
    }

    [Test]
    public void AuthLoginAsync_SendsRequestId()
    {
      var body = new JsonRequestBody("{\"provider\":\"dev\",\"externalId\":\"player-1\"}");
      var snapshot = BuildWrite(
          HttpVerb.Post,
          "v1/auth/login",
          body,
          authorized: false);

      Assert.AreEqual(TransportHttpMethods.Post, snapshot.Method);
      TransportHeaderAssertions.AssertRequestId(snapshot);
      TransportHeaderAssertions.AssertAuthorizationAbsent(snapshot);
    }

    [Test]
    public void ProfilesBatch_DoesNotSendRequestId()
    {
      var body = new ReadOnlyJsonRequestBody("{\"userIds\":[\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"]}");
      var snapshot = BuildWrite(
          HttpVerb.Post,
          "v1/profiles/test-game/batch",
          body,
          authorized: false);

      Assert.AreEqual(TransportHttpMethods.Post, snapshot.Method);
      TransportHeaderAssertions.AssertNoRequestId(snapshot);
    }

    [Test]
    public void ProfilesUpdate_SendsRequestId()
    {
      var body = new JsonRequestBody("{\"displayName\":\"Player One\",\"avatarId\":null,\"publicData\":{}}");
      var snapshot = BuildWrite(
          HttpVerb.Put,
          "v1/profiles/test-game/me",
          body,
          authorized: true);

      Assert.AreEqual(TransportHttpMethods.Put, snapshot.Method);
      TransportHeaderAssertions.AssertRequestId(snapshot);
    }

    private TransportRequestSnapshot BuildWrite(
        HttpVerb verb,
        string path,
        object body,
        bool authorized)
    {
      var context = TransportRequestBuilder.CreateRequestContext(verb, body);
      return TransportRequestBuilder.Build(
          settings,
          verb,
          path,
          body,
          authorized ? "Bearer " + TransportHeaderAssertions.AccessToken : null,
          context);
    }

    private TransportRequestSnapshot BuildRead(HttpVerb verb, string path, bool authorized)
    {
      var context = TransportRequestBuilder.CreateRequestContext(verb, null);
      return TransportRequestBuilder.Build(
          settings,
          verb,
          path,
          null,
          authorized ? "Bearer " + TransportHeaderAssertions.AccessToken : null,
          context);
    }
  }
}
