namespace BackendSdk
{
    /// <summary>
    /// Describes an active entitlement definition returned by the player economy API.
    /// </summary>
    public sealed class EntitlementDefinition
    {
        internal EntitlementDefinition(string key, string displayName, EntitlementKind kind)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            IsActive = true;
        }

        /// <summary>
        /// Gets the stable entitlement key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the display name configured for the entitlement.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the entitlement kind.
        /// </summary>
        public EntitlementKind Kind { get; }

        /// <summary>
        /// Gets a value indicating whether the definition is active.
        /// </summary>
        /// <remarks>
        /// The player economy endpoint returns active definitions only.
        /// </remarks>
        public bool IsActive { get; }
    }
}
