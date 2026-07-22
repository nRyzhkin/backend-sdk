using System;
using System.Collections.Generic;

namespace BackendSdk
{
    /// <summary>
    /// Represents the authenticated player's economy state returned by the backend.
    /// </summary>
    /// <remarks>
    /// The backend is the source of truth. This object may be served from an in-memory SDK cache.
    /// Use <see cref="EconomyService.RefreshAsync"/> after server-authoritative gameplay actions.
    /// </remarks>
    public sealed class PlayerEconomyState
    {
        internal PlayerEconomyState(
            IReadOnlyList<PlayerCurrencyBalance> currencies,
            IReadOnlyList<PlayerEntitlement> entitlements,
            DateTime serverTime)
        {
            Currencies = EconomyModelUtilities.ToReadOnlyCopy(currencies);
            Entitlements = EconomyModelUtilities.ToReadOnlyCopy(entitlements);
            ServerTime = serverTime;

            currenciesByKey = BuildCurrencyLookup(Currencies);
            entitlementsByKey = BuildEntitlementLookup(Entitlements);
        }

        private readonly Dictionary<string, PlayerCurrencyBalance> currenciesByKey;
        private readonly Dictionary<string, PlayerEntitlement> entitlementsByKey;

        /// <summary>
        /// Gets currency balances in backend order.
        /// </summary>
        public IReadOnlyList<PlayerCurrencyBalance> Currencies { get; }

        /// <summary>
        /// Gets entitlement states in backend order.
        /// </summary>
        public IReadOnlyList<PlayerEntitlement> Entitlements { get; }

        /// <summary>
        /// Gets the server timestamp associated with the response (UTC).
        /// </summary>
        public DateTime ServerTime { get; }

        /// <summary>
        /// Gets the balance for a currency key.
        /// </summary>
        /// <param name="key">The currency key.</param>
        /// <returns>The balance, or <c>0</c> when the currency is missing.</returns>
        public long GetCurrencyBalance(string key)
        {
            return TryGetCurrency(key, out var currency) ? currency.Balance : 0L;
        }

        /// <summary>
        /// Attempts to get a currency balance entry by key.
        /// </summary>
        /// <param name="key">The currency key.</param>
        /// <param name="currency">When this method returns, contains the currency state if found.</param>
        /// <returns><c>true</c> when the currency exists; otherwise <c>false</c>.</returns>
        public bool TryGetCurrency(string key, out PlayerCurrencyBalance currency)
        {
            ValidateKey(key);
            return currenciesByKey.TryGetValue(key, out currency);
        }

        /// <summary>
        /// Gets a value indicating whether the player owns an entitlement.
        /// </summary>
        /// <param name="key">The entitlement key.</param>
        /// <returns><c>true</c> when quantity is greater than zero; otherwise <c>false</c>.</returns>
        public bool HasEntitlement(string key)
        {
            return GetEntitlementQuantity(key) > 0L;
        }

        /// <summary>
        /// Gets the owned quantity for an entitlement key.
        /// </summary>
        /// <param name="key">The entitlement key.</param>
        /// <returns>The quantity, or <c>0</c> when the entitlement is missing.</returns>
        public long GetEntitlementQuantity(string key)
        {
            return TryGetEntitlement(key, out var entitlement) ? entitlement.Quantity : 0L;
        }

        /// <summary>
        /// Attempts to get an entitlement state entry by key.
        /// </summary>
        /// <param name="key">The entitlement key.</param>
        /// <param name="entitlement">When this method returns, contains the entitlement state if found.</param>
        /// <returns><c>true</c> when the entitlement exists; otherwise <c>false</c>.</returns>
        public bool TryGetEntitlement(string key, out PlayerEntitlement entitlement)
        {
            ValidateKey(key);
            return entitlementsByKey.TryGetValue(key, out entitlement);
        }

        private static Dictionary<string, PlayerCurrencyBalance> BuildCurrencyLookup(
            IReadOnlyList<PlayerCurrencyBalance> currencies)
        {
            var lookup = new Dictionary<string, PlayerCurrencyBalance>(currencies.Count, StringComparer.Ordinal);
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

        private static Dictionary<string, PlayerEntitlement> BuildEntitlementLookup(
            IReadOnlyList<PlayerEntitlement> entitlements)
        {
            var lookup = new Dictionary<string, PlayerEntitlement>(entitlements.Count, StringComparer.Ordinal);
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
