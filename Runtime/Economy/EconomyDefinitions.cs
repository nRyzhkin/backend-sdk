using System;
using System.Collections.Generic;

namespace BackendSdk
{
    /// <summary>
    /// Contains active economy definitions for the configured application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The player economy endpoint returns the full active catalog for the application merged with
    /// the authenticated player's balances and entitlement quantities.
    /// </para>
    /// <para>
    /// Inactive definitions are excluded by the backend. Description and admin metadata are not
    /// included in the player response.
    /// </para>
    /// </remarks>
    public sealed class EconomyDefinitions
    {
        internal EconomyDefinitions(
            IReadOnlyList<CurrencyDefinition> currencies,
            IReadOnlyList<EntitlementDefinition> entitlements)
        {
            Currencies = EconomyModelUtilities.ToReadOnlyCopy(currencies);
            Entitlements = EconomyModelUtilities.ToReadOnlyCopy(entitlements);

            currenciesByKey = BuildCurrencyLookup(Currencies);
            entitlementsByKey = BuildEntitlementLookup(Entitlements);
        }

        private readonly Dictionary<string, CurrencyDefinition> currenciesByKey;
        private readonly Dictionary<string, EntitlementDefinition> entitlementsByKey;

        /// <summary>
        /// Gets active currency definitions in backend order.
        /// </summary>
        public IReadOnlyList<CurrencyDefinition> Currencies { get; }

        /// <summary>
        /// Gets active entitlement definitions in backend order.
        /// </summary>
        public IReadOnlyList<EntitlementDefinition> Entitlements { get; }

        /// <summary>
        /// Attempts to get a currency definition by key.
        /// </summary>
        public bool TryGetCurrencyDefinition(string key, out CurrencyDefinition definition)
        {
            ValidateKey(key);
            return currenciesByKey.TryGetValue(key, out definition);
        }

        /// <summary>
        /// Attempts to get an entitlement definition by key.
        /// </summary>
        public bool TryGetEntitlementDefinition(string key, out EntitlementDefinition definition)
        {
            ValidateKey(key);
            return entitlementsByKey.TryGetValue(key, out definition);
        }

        private static Dictionary<string, CurrencyDefinition> BuildCurrencyLookup(
            IReadOnlyList<CurrencyDefinition> currencies)
        {
            var lookup = new Dictionary<string, CurrencyDefinition>(currencies.Count, StringComparer.Ordinal);
            for (var i = 0; i < currencies.Count; i++)
            {
                var currency = currencies[i];
                if (!lookup.ContainsKey(currency.Key))
                {
                    lookup[currency.Key] = currency;
                }
            }

            return lookup;
        }

        private static Dictionary<string, EntitlementDefinition> BuildEntitlementLookup(
            IReadOnlyList<EntitlementDefinition> entitlements)
        {
            var lookup = new Dictionary<string, EntitlementDefinition>(entitlements.Count, StringComparer.Ordinal);
            for (var i = 0; i < entitlements.Count; i++)
            {
                var entitlement = entitlements[i];
                if (!lookup.ContainsKey(entitlement.Key))
                {
                    lookup[entitlement.Key] = entitlement;
                }
            }

            return lookup;
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Resource key is required.", nameof(key));
            }
        }
    }
}
