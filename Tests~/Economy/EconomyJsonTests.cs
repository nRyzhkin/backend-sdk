using System;
using BackendSdk;
using BackendSdk.Internal;
using NUnit.Framework;

namespace BackendSdk.Tests.Economy
{
    [Category("Economy")]
    public sealed class EconomyJsonTests
    {
        private const string SampleResponse =
            "{" +
            "\"currencies\":[" +
            "{" +
            "\"key\":\"gold\"," +
            "\"displayName\":\"Gold\"," +
            "\"balance\":150," +
            "\"maxBalance\":10000," +
            "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
            "}" +
            "]," +
            "\"entitlements\":[" +
            "{" +
            "\"key\":\"remove_ads\"," +
            "\"displayName\":\"Remove Ads\"," +
            "\"kind\":\"permanent\"," +
            "\"quantity\":1," +
            "\"owned\":true," +
            "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
            "},{" +
            "\"key\":\"arena_ticket\"," +
            "\"displayName\":\"Arena Ticket\"," +
            "\"kind\":\"consumable\"," +
            "\"quantity\":3," +
            "\"owned\":true," +
            "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
            "}" +
            "]," +
            "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
            "}";

        [Test]
        public void GetDefinitionsAsync_MapsCurrencyDefinitions()
        {
            var snapshot = EconomyJson.ParseSnapshot(SampleResponse);
            var definitions = snapshot.Definitions;

            Assert.AreEqual(1, definitions.Currencies.Count);
            Assert.IsTrue(definitions.TryGetCurrencyDefinition("gold", out var gold));
            Assert.AreEqual("Gold", gold.DisplayName);
            Assert.AreEqual(10000L, gold.MaxBalance);
            Assert.IsTrue(gold.IsActive);
        }

        [Test]
        public void GetDefinitionsAsync_MapsEntitlementKinds()
        {
            var snapshot = EconomyJson.ParseSnapshot(SampleResponse);
            var definitions = snapshot.Definitions;

            Assert.AreEqual(2, definitions.Entitlements.Count);
            Assert.IsTrue(definitions.TryGetEntitlementDefinition("remove_ads", out var removeAds));
            Assert.AreEqual(EntitlementKind.Permanent, removeAds.Kind);
            Assert.IsTrue(definitions.TryGetEntitlementDefinition("arena_ticket", out var arenaTicket));
            Assert.AreEqual(EntitlementKind.Consumable, arenaTicket.Kind);
        }

        [Test]
        public void GetStateAsync_MapsCurrencyBalances()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.AreEqual(1, state.Currencies.Count);
            Assert.AreEqual("gold", state.Currencies[0].Key);
            Assert.AreEqual("Gold", state.Currencies[0].DisplayName);
            Assert.AreEqual(150L, state.Currencies[0].Balance);
            Assert.AreEqual(10000L, state.Currencies[0].MaxBalance);
            Assert.AreEqual(new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), state.Currencies[0].UpdatedAt);
        }

        [Test]
        public void GetStateAsync_MapsPermanentEntitlement()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.IsTrue(state.TryGetEntitlement("remove_ads", out var entitlement));
            Assert.AreEqual(EntitlementKind.Permanent, entitlement.Kind);
            Assert.AreEqual(1L, entitlement.Quantity);
            Assert.IsTrue(entitlement.IsOwned);
        }

        [Test]
        public void GetStateAsync_MapsConsumableEntitlement()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.IsTrue(state.TryGetEntitlement("arena_ticket", out var entitlement));
            Assert.AreEqual(EntitlementKind.Consumable, entitlement.Kind);
            Assert.AreEqual(3L, entitlement.Quantity);
            Assert.IsTrue(entitlement.IsOwned);
        }

        [Test]
        public void GetCurrencyBalance_ReturnsBalance()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.AreEqual(150L, state.GetCurrencyBalance("gold"));
        }

        [Test]
        public void GetCurrencyBalance_ReturnsZeroForMissingKey()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.AreEqual(0L, state.GetCurrencyBalance("missing"));
            Assert.IsFalse(state.TryGetCurrency("missing", out _));
        }

        [Test]
        public void HasEntitlement_ReturnsTrueForPositiveQuantity()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.IsTrue(state.HasEntitlement("remove_ads"));
            Assert.IsTrue(state.HasEntitlement("arena_ticket"));
        }

        [Test]
        public void HasEntitlement_ReturnsFalseForZeroOrMissing()
        {
            var zeroQuantityResponse =
                "{" +
                "\"currencies\":[]," +
                "\"entitlements\":[" +
                "{" +
                "\"key\":\"unused\"," +
                "\"displayName\":\"Unused\"," +
                "\"kind\":\"permanent\"," +
                "\"quantity\":0," +
                "\"owned\":false," +
                "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                "}" +
                "]," +
                "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                "}";

            var state = EconomyJson.ParseSnapshot(zeroQuantityResponse).State;

            Assert.IsFalse(state.HasEntitlement("unused"));
            Assert.IsFalse(state.HasEntitlement("missing"));
        }

        [Test]
        public void GetEntitlementQuantity_ReturnsQuantity()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.AreEqual(3L, state.GetEntitlementQuantity("arena_ticket"));
        }

        [Test]
        public void Helpers_RejectNullOrEmptyKeys()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.Throws<ArgumentException>(() => state.GetCurrencyBalance(null));
            Assert.Throws<ArgumentException>(() => state.TryGetCurrency(" ", out _));
            Assert.Throws<ArgumentException>(() => state.HasEntitlement(string.Empty));
            Assert.Throws<ArgumentException>(() => state.GetEntitlementQuantity(null));
            Assert.Throws<ArgumentException>(() => state.TryGetEntitlement(" ", out _));
        }

        [Test]
        public void ParseSnapshot_RejectsUnknownEntitlementKind()
        {
            var json =
                "{" +
                "\"currencies\":[]," +
                "\"entitlements\":[" +
                "{" +
                "\"key\":\"vip\"," +
                "\"displayName\":\"VIP\"," +
                "\"kind\":\"subscription\"," +
                "\"quantity\":1," +
                "\"owned\":true," +
                "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                "}]," +
                "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                "}";

            var exception = Assert.Throws<BackendException>(() => EconomyJson.ParseSnapshot(json));
            Assert.AreEqual("economy_deserialization_failed", exception.ErrorCode);
        }

        [Test]
        public void MapsLongMaxValue()
        {
            var json =
                "{" +
                "\"currencies\":[" +
                "{" +
                "\"key\":\"gold\"," +
                "\"displayName\":\"Gold\"," +
                "\"balance\":" + long.MaxValue + "," +
                "\"maxBalance\":" + long.MaxValue + "," +
                "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                "}]," +
                "\"entitlements\":[]," +
                "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                "}";

            var state = EconomyJson.ParseSnapshot(json).State;

            Assert.AreEqual(long.MaxValue, state.GetCurrencyBalance("gold"));
            Assert.AreEqual(long.MaxValue, state.Currencies[0].MaxBalance);
        }

        [Test]
        public void MapsIntegerAboveDoublePrecisionBoundary()
        {
            const long largeValue = 9007199254740993L;
            var json =
                "{" +
                "\"currencies\":[" +
                "{" +
                "\"key\":\"gold\"," +
                "\"displayName\":\"Gold\"," +
                "\"balance\":" + largeValue + "," +
                "\"maxBalance\":null," +
                "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                "}]," +
                "\"entitlements\":[]," +
                "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                "}";

            var state = EconomyJson.ParseSnapshot(json).State;

            Assert.AreEqual(largeValue, state.GetCurrencyBalance("gold"));
            Assert.IsNull(state.Currencies[0].MaxBalance);
        }

        [Test]
        public void MapsNullMaxBalance()
        {
            var json =
                "{" +
                "\"currencies\":[" +
                "{" +
                "\"key\":\"gold\"," +
                "\"displayName\":\"Gold\"," +
                "\"balance\":0," +
                "\"maxBalance\":null," +
                "\"updatedAt\":\"2026-07-22T12:00:00Z\"" +
                "}]," +
                "\"entitlements\":[]," +
                "\"serverTime\":\"2026-07-22T12:00:00Z\"" +
                "}";

            var state = EconomyJson.ParseSnapshot(json).State;

            Assert.IsNull(state.Currencies[0].MaxBalance);
        }

        [Test]
        public void Helpers_AreCaseSensitive()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.AreEqual(0L, state.GetCurrencyBalance("Gold"));
            Assert.IsFalse(state.HasEntitlement("Remove_Ads"));
        }

        [Test]
        public void ReturnedState_CannotMutateSdkCache()
        {
            var state = EconomyJson.ParseSnapshot(SampleResponse).State;

            Assert.Throws<NotSupportedException>(() =>
            {
                if (state.Currencies is System.Collections.IList currencies)
                {
                    currencies.Clear();
                }
            });
        }
    }
}
