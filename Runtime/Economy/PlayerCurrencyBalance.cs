using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the authenticated player's balance for a currency.
    /// </summary>
    public sealed class PlayerCurrencyBalance
    {
        internal PlayerCurrencyBalance(
            string key,
            string displayName,
            long balance,
            long? maxBalance,
            DateTime updatedAt)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Balance = balance;
            MaxBalance = maxBalance;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Gets the currency key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the display name from the active definition.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the current balance.
        /// </summary>
        public long Balance { get; }

        /// <summary>
        /// Gets the configured maximum balance, if any.
        /// </summary>
        public long? MaxBalance { get; }

        /// <summary>
        /// Gets when the balance was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; }
    }
}
