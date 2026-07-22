using System;
using System.Collections.Generic;
using System.Globalization;
using BackendSdk.Internal;

namespace BackendSdk.Internal
{
    internal static class EconomyJson
    {
        internal sealed class EconomySnapshot
        {
            internal EconomySnapshot(PlayerEconomyState state, EconomyDefinitions definitions)
            {
                State = state;
                Definitions = definitions;
            }

            internal PlayerEconomyState State { get; }

            internal EconomyDefinitions Definitions { get; }
        }

        internal static EconomySnapshot ParseSnapshot(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw CreateDeserializationException("Response body is empty.");
            }

            var trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                throw CreateDeserializationException("Response is not a JSON object.");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "serverTime", out var serverTimeJson))
            {
                throw CreateDeserializationException("Missing serverTime property.");
            }

            var serverTime = ParseUtcDateTime(serverTimeJson, "serverTime");
            var currencies = ParseCurrencies(trimmed);
            var entitlements = ParseEntitlements(trimmed);
            var definitions = BuildDefinitions(currencies, entitlements);
            var state = new PlayerEconomyState(currencies, entitlements, serverTime);
            return new EconomySnapshot(state, definitions);
        }

        private static IReadOnlyList<PlayerCurrencyBalance> ParseCurrencies(string json)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, "currencies", out var currenciesJson))
            {
                return Array.Empty<PlayerCurrencyBalance>();
            }

            var items = new List<PlayerCurrencyBalance>();
            foreach (var element in RemoteConfigJson.SplitTopLevelArray(currenciesJson))
            {
                items.Add(ParseCurrency(element));
            }

            return items;
        }

        private static IReadOnlyList<PlayerEntitlement> ParseEntitlements(string json)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, "entitlements", out var entitlementsJson))
            {
                return Array.Empty<PlayerEntitlement>();
            }

            var items = new List<PlayerEntitlement>();
            foreach (var element in RemoteConfigJson.SplitTopLevelArray(entitlementsJson))
            {
                items.Add(ParseEntitlement(element));
            }

            return items;
        }

        private static PlayerCurrencyBalance ParseCurrency(string json)
        {
            var key = ParseRequiredString(json, "key");
            var displayName = ParseRequiredString(json, "displayName");
            var balance = ParseRequiredLong(json, "balance");
            var maxBalance = ParseOptionalLong(json, "maxBalance");
            var updatedAt = ParseUtcDateTime(ParseRequiredProperty(json, "updatedAt"), "updatedAt");
            return new PlayerCurrencyBalance(key, displayName, balance, maxBalance, updatedAt);
        }

        private static PlayerEntitlement ParseEntitlement(string json)
        {
            var key = ParseRequiredString(json, "key");
            var displayName = ParseRequiredString(json, "displayName");
            var kind = ParseEntitlementKind(ParseRequiredProperty(json, "kind"));
            var quantity = ParseRequiredLong(json, "quantity");
            var updatedAt = ParseUtcDateTime(ParseRequiredProperty(json, "updatedAt"), "updatedAt");
            return new PlayerEntitlement(key, displayName, kind, quantity, updatedAt);
        }

        private static EconomyDefinitions BuildDefinitions(
            IReadOnlyList<PlayerCurrencyBalance> currencies,
            IReadOnlyList<PlayerEntitlement> entitlements)
        {
            var currencyDefinitions = new List<CurrencyDefinition>(currencies.Count);
            for (var i = 0; i < currencies.Count; i++)
            {
                var currency = currencies[i];
                currencyDefinitions.Add(new CurrencyDefinition(currency.Key, currency.DisplayName, currency.MaxBalance));
            }

            var entitlementDefinitions = new List<EntitlementDefinition>(entitlements.Count);
            for (var i = 0; i < entitlements.Count; i++)
            {
                var entitlement = entitlements[i];
                entitlementDefinitions.Add(new EntitlementDefinition(entitlement.Key, entitlement.DisplayName, entitlement.Kind));
            }

            return new EconomyDefinitions(currencyDefinitions, entitlementDefinitions);
        }

        private static EntitlementKind ParseEntitlementKind(string kindJson)
        {
            var value = RemoteConfigJson.ParseJsonString(kindJson);
            if (string.Equals(value, "permanent", StringComparison.OrdinalIgnoreCase))
            {
                return EntitlementKind.Permanent;
            }

            if (string.Equals(value, "consumable", StringComparison.OrdinalIgnoreCase))
            {
                return EntitlementKind.Consumable;
            }

            throw CreateDeserializationException($"Unsupported entitlement kind '{value}'.");
        }

        private static string ParseRequiredString(string json, string propertyName)
        {
            return RemoteConfigJson.ParseJsonString(ParseRequiredProperty(json, propertyName));
        }

        private static long ParseRequiredLong(string json, string propertyName)
        {
            var valueJson = ParseRequiredProperty(json, propertyName);
            if (string.Equals(valueJson.Trim(), "null", StringComparison.Ordinal))
            {
                throw CreateDeserializationException($"Property '{propertyName}' cannot be null.");
            }

            return RemoteConfigJson.DeserializeValue<long>(valueJson, null, propertyName);
        }

        private static long? ParseOptionalLong(string json, string propertyName)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, propertyName, out var valueJson)
                || string.Equals(valueJson.Trim(), "null", StringComparison.Ordinal))
            {
                return null;
            }

            return RemoteConfigJson.DeserializeValue<long>(valueJson, null, propertyName);
        }

        private static string ParseRequiredProperty(string json, string propertyName)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, propertyName, out var valueJson))
            {
                throw CreateDeserializationException($"Missing {propertyName} property.");
            }

            return valueJson;
        }

        private static DateTime ParseUtcDateTime(string json, string propertyName)
        {
            var value = RemoteConfigJson.ParseJsonString(json);
            if (!DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                throw CreateDeserializationException($"Invalid DateTime in '{propertyName}': '{value}'.");
            }

            return parsed.ToUniversalTime();
        }

        private static BackendException CreateDeserializationException(string message)
        {
            return new BackendException(
                $"Failed to deserialize player economy response. {message}",
                "economy_deserialization_failed");
        }
    }
}
