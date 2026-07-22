using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the authenticated player's entitlement state.
    /// </summary>
    public sealed class PlayerEntitlement
    {
        internal PlayerEntitlement(
            string key,
            string displayName,
            EntitlementKind kind,
            long quantity,
            DateTime updatedAt)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            Quantity = quantity;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Gets the entitlement key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the display name from the active definition.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the entitlement kind.
        /// </summary>
        public EntitlementKind Kind { get; }

        /// <summary>
        /// Gets the owned quantity.
        /// </summary>
        public long Quantity { get; }

        /// <summary>
        /// Gets when the entitlement was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; }

        /// <summary>
        /// Gets a value indicating whether the player currently owns the entitlement.
        /// </summary>
        public bool IsOwned => Quantity > 0;
    }
}
