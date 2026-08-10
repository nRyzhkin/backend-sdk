using BackendSdk.Internal;
using NUnit.Framework;

namespace BackendSdk.Tests.Jobs
{
    [Category("Jobs")]
    public sealed class JobsJsonTests
    {
        private const string SampleOffer = @"
            {
              ""jobId"": ""cleaner"",
              ""displayName"": ""Cleaner"",
              ""level"": 2,
              ""xp"": 10,
              ""xpToNext"": 60,
              ""freeTickets"": 4,
              ""bonusTickets"": 1,
              ""freeTicketMax"": 5,
              ""freeTicketsFullAtUtc"": ""2026-08-10T12:00:00Z"",
              ""rewardPreview"": 89000,
              ""hasActiveRun"": false,
              ""canStart"": true,
              ""canComplete"": false,
              ""serverTimeUtc"": ""2026-08-10T11:00:00Z""
            }";

        [Test]
        public void ParseOffer_MapsFields()
        {
            var offer = JobsJson.ParseOffer(SampleOffer);

            Assert.AreEqual("cleaner", offer.JobId);
            Assert.AreEqual("Cleaner", offer.DisplayName);
            Assert.AreEqual(2, offer.Level);
            Assert.AreEqual(10L, offer.Xp);
            Assert.AreEqual(60L, offer.XpToNext);
            Assert.AreEqual(4, offer.FreeTickets);
            Assert.AreEqual(1, offer.BonusTickets);
            Assert.AreEqual(5, offer.FreeTicketMax);
            Assert.IsNotNull(offer.FreeTicketsFullAtUtc);
            Assert.AreEqual(89000L, offer.RewardPreview);
            Assert.IsFalse(offer.HasActiveRun);
            Assert.IsTrue(offer.CanStart);
            Assert.IsFalse(offer.CanComplete);
        }

        [Test]
        public void ParseOffer_AllowsNullFullAt()
        {
            var json = SampleOffer.Replace(
                @"""freeTicketsFullAtUtc"": ""2026-08-10T12:00:00Z""",
                @"""freeTicketsFullAtUtc"": null");

            var offer = JobsJson.ParseOffer(json);
            Assert.IsNull(offer.FreeTicketsFullAtUtc);
        }

        [Test]
        public void ParseBatch_ParsesJobsArray()
        {
            var json =
                "{\"jobs\":[" + SampleOffer + "],\"serverTimeUtc\":\"2026-08-10T11:00:00Z\"}";

            var batch = JobsJson.ParseBatch(json);
            Assert.AreEqual(1, batch.Jobs.Count);
            Assert.AreEqual("cleaner", batch.Jobs[0].JobId);
        }

        [Test]
        public void ParseStartAndComplete_MapNestedJob()
        {
            var startJson = "{\"job\":" + SampleOffer + "}";
            var start = JobsJson.ParseStart(startJson);
            Assert.AreEqual("cleaner", start.Job.JobId);

            var completeJson =
                "{\"job\":" + SampleOffer +
                ",\"coinsGranted\":89000,\"xpGranted\":20,\"levelsGained\":0," +
                "\"levelUpCoinsGranted\":0,\"levelUpBonusTicketsGranted\":0,\"currencyKey\":\"coins\"}";
            var complete = JobsJson.ParseComplete(completeJson);
            Assert.AreEqual(89000L, complete.CoinsGranted);
            Assert.AreEqual(20L, complete.XpGranted);
            Assert.AreEqual("coins", complete.CurrencyKey);
        }
    }
}
