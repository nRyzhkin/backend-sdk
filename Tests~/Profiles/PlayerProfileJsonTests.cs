using System;
using System.Collections.Generic;
using BackendSdk;
using BackendSdk.Internal;
using NUnit.Framework;

namespace BackendSdk.Tests.Profiles
{
    public sealed class PlayerProfileJsonTests
    {
        private static readonly Guid UserId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        private static readonly Guid MissingUserId = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        [Test]
        public void ParseProfile_ReadsCoreFieldsAndNullAvatarId()
        {
            var json =
                "{" +
                "\"userId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"," +
                "\"applicationId\":\"test-game\"," +
                "\"displayName\":\"Player\"," +
                "\"avatarId\":null," +
                "\"publicData\":{}," +
                "\"createdAt\":\"2026-07-22T12:00:00Z\"," +
                "\"updatedAt\":\"2026-07-22T12:05:00Z\"" +
                "}";

            var profile = ProfileJson.ParseProfile(json);

            Assert.AreEqual(UserId, profile.UserId);
            Assert.AreEqual("test-game", profile.ApplicationId);
            Assert.AreEqual("Player", profile.DisplayName);
            Assert.IsNull(profile.AvatarId);
            Assert.AreEqual(new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), profile.CreatedAt);
            Assert.AreEqual(new DateTime(2026, 7, 22, 12, 5, 0, DateTimeKind.Utc), profile.UpdatedAt);
            Assert.AreEqual("{}", profile.PublicDataJson);
        }

        [Test]
        public void ParseProfile_ReadsNestedPublicData()
        {
            var json =
                "{" +
                "\"userId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"," +
                "\"applicationId\":\"test-game\"," +
                "\"displayName\":\"Player\"," +
                "\"avatarId\":\"avatar_01\"," +
                "\"publicData\":{" +
                "\"status\":\"Online\"," +
                "\"rank\":42," +
                "\"ratio\":1.5," +
                "\"verified\":true," +
                "\"league\":{\"name\":\"Gold\"}," +
                "\"badges\":[\"founder\",\"tester\"]" +
                "}," +
                "\"createdAt\":\"2026-07-22T12:00:00Z\"," +
                "\"updatedAt\":\"2026-07-22T12:05:00Z\"" +
                "}";

            var profile = ProfileJson.ParseProfile(json);
            var data = profile.GetPublicData<NestedPublicData>();

            Assert.AreEqual("avatar_01", profile.AvatarId);
            Assert.AreEqual("Online", data.status);
            Assert.AreEqual(42, data.rank);
            Assert.AreEqual(1.5f, data.ratio);
            Assert.AreEqual(true, data.verified);
            Assert.AreEqual("Gold", data.league.name);
            Assert.AreEqual(2, data.badges.Length);
            Assert.AreEqual("founder", data.badges[0]);
            Assert.AreEqual("tester", data.badges[1]);
        }

        [Test]
        public void GetPublicData_DeserializesTypedObject()
        {
            var json =
                "{" +
                "\"userId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"," +
                "\"applicationId\":\"test-game\"," +
                "\"displayName\":\"Player\"," +
                "\"avatarId\":null," +
                "\"publicData\":{\"status\":\"Looking for team\",\"level\":12}," +
                "\"createdAt\":\"2026-07-22T12:00:00Z\"," +
                "\"updatedAt\":\"2026-07-22T12:05:00Z\"" +
                "}";

            var profile = ProfileJson.ParseProfile(json);
            var data = profile.GetPublicData<MyPublicProfileData>();

            Assert.AreEqual("Looking for team", data.status);
            Assert.AreEqual(12, data.level);
        }

        [Test]
        public void ParseBatchResult_PreservesOrderAndCollections()
        {
            var json =
                "{" +
                "\"profiles\":[" +
                "{" +
                "\"userId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"," +
                "\"applicationId\":\"test-game\"," +
                "\"displayName\":\"Player One\"," +
                "\"avatarId\":null," +
                "\"publicData\":{}," +
                "\"createdAt\":\"2026-07-22T12:00:00Z\"," +
                "\"updatedAt\":\"2026-07-22T12:05:00Z\"" +
                "},{" +
                "\"userId\":\"11111111-1111-1111-1111-111111111111\"," +
                "\"applicationId\":\"test-game\"," +
                "\"displayName\":\"Player Two\"," +
                "\"avatarId\":\"avatar_02\"," +
                "\"publicData\":{}," +
                "\"createdAt\":\"2026-07-22T12:00:00Z\"," +
                "\"updatedAt\":\"2026-07-22T12:05:00Z\"" +
                "}" +
                "]," +
                "\"missingUserIds\":[\"6ba7b810-9dad-11d1-80b4-00c04fd430c8\"]" +
                "}";

            var result = ProfileJson.ParseBatchResult(json);

            Assert.IsNotNull(result.Profiles);
            Assert.IsNotNull(result.MissingUserIds);
            Assert.AreEqual(2, result.Profiles.Count);
            Assert.AreEqual("Player One", result.Profiles[0].DisplayName);
            Assert.AreEqual("Player Two", result.Profiles[1].DisplayName);
            Assert.AreEqual(1, result.MissingUserIds.Count);
            Assert.AreEqual(MissingUserId, result.MissingUserIds[0]);
            Assert.IsTrue(result.TryGetProfile(UserId, out var profile));
            Assert.AreEqual("Player One", profile.DisplayName);
            Assert.IsTrue(result.ByUserId.ContainsKey(UserId));
        }

        [Test]
        public void GetBatchAsync_Validation_RejectsNullUserIds()
        {
            var service = new ProfilesService();

            Assert.Throws<ArgumentNullException>(() =>
                service.GetBatchAsync(null).GetAwaiter().GetResult());
        }

        [Test]
        public void GetBatchAsync_Validation_RejectsEmptyUserIds()
        {
            var service = new ProfilesService();

            var exception = Assert.Throws<ArgumentException>(() =>
                service.GetBatchAsync(Array.Empty<Guid>()).GetAwaiter().GetResult());

            Assert.AreEqual("userIds", exception.ParamName);
        }

        [Test]
        public void GetBatchAsync_Validation_RejectsMoreThanMaxBatchSize()
        {
            var service = new ProfilesService();
            var userIds = new Guid[ProfilesService.MaxBatchSize + 1];
            for (var i = 0; i < userIds.Length; i++)
            {
                userIds[i] = Guid.NewGuid();
            }

            var exception = Assert.Throws<ArgumentException>(() =>
                service.GetBatchAsync(userIds).GetAwaiter().GetResult());

            Assert.AreEqual("userIds", exception.ParamName);
        }

        [Test]
        public void GetBatchAsync_Validation_RejectsGuidEmpty()
        {
            var service = new ProfilesService();

            var exception = Assert.Throws<ArgumentException>(() =>
                service.GetBatchAsync(new[] { Guid.Empty }).GetAwaiter().GetResult());

            Assert.AreEqual("userIds", exception.ParamName);
        }

        [Test]
        public void DedupeUserIds_PreservesFirstOccurrenceOrder()
        {
            var userId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var deduped = ProfileJson.DedupeUserIdsPreserveOrder(new[]
            {
                userId,
                userId,
                new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            });

            Assert.AreEqual(2, deduped.Count);
            Assert.AreEqual(userId, deduped[0]);
            Assert.AreEqual(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), deduped[1]);
        }

        [Test]
        public void BuildBatchRequest_SerializesGuidArray()
        {
            var json = ProfileJson.BuildBatchRequest(new[]
            {
                new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            });

            Assert.AreEqual(
                "{\"userIds\":[\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\"]}",
                json);
        }

        [Test]
        public void BuildUpdateRequest_SerializesPublicDataAsJsonObject()
        {
            var json = ProfileJson.BuildUpdateRequest(
                "Player One",
                "avatar_03",
                new MyPublicProfileData
                {
                    status = "Looking for team",
                    level = 12,
                    badges = new[] { "founder", "tester" }
                });

            Assert.IsTrue(json.Contains("\"publicData\":{\"status\":\"Looking for team\",\"level\":12,\"badges\":[\"founder\",\"tester\"]}"));
            Assert.IsFalse(json.Contains("\"publicData\":\"{"));
            Assert.IsTrue(json.Contains("\"avatarId\":\"avatar_03\""));
        }

        [Test]
        public void BuildUpdateRequest_AllowsNullAvatarId()
        {
            var json = ProfileJson.BuildUpdateRequest(
                "Player One",
                null,
                new MyPublicProfileData { status = "Online", level = 1 });

            Assert.IsTrue(json.Contains("\"avatarId\":null"));
        }

        [Test]
        public void BuildUpdateRequest_FromRawJsonObject_DoesNotEscapePublicData()
        {
            var json = ProfileJson.BuildUpdateRequest(
                "Player One",
                "avatar_03",
                "{\"status\":\"Online\",\"level\":5}");

            Assert.IsTrue(json.Contains("\"publicData\":{\"status\":\"Online\",\"level\":5}"));
            Assert.IsFalse(json.Contains("\"publicData\":\"{"));
        }

        [Serializable]
        private sealed class MyPublicProfileData
        {
            public string status;
            public int level;
            public string[] badges;
        }

        [Serializable]
        private sealed class NestedPublicData
        {
            public string status;
            public int rank;
            public float ratio;
            public bool verified;
            public LeagueData league;
            public string[] badges;
        }

        [Serializable]
        private sealed class LeagueData
        {
            public string name;
        }
    }
}
