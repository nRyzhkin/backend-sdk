namespace BackendSdk
{
    /// <summary>
    /// Describes how an entitlement behaves in the economy.
    /// </summary>
    public enum EntitlementKind
    {
        /// <summary>
        /// A permanent entitlement owned at most once.
        /// </summary>
        Permanent = 0,

        /// <summary>
        /// A consumable entitlement with a stackable quantity.
        /// </summary>
        Consumable = 1
    }
}
