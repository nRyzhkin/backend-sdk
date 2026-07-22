namespace BackendSdk
{
    /// <summary>
    /// Describes an active currency definition returned by the player economy API.
    /// </summary>
    public sealed class CurrencyDefinition
    {
        internal CurrencyDefinition(string key, string displayName, long? maxBalance)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            MaxBalance = maxBalance;
            IsActive = true;
        }

        /// <summary>
        /// Gets the stable currency key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the display name configured for the currency.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the configured maximum balance, if any.
        /// </summary>
        public long? MaxBalance { get; }

        /// <summary>
        /// Gets a value indicating whether the definition is active.
        /// </summary>
        /// <remarks>
        /// The player economy endpoint returns active definitions only.
        /// </remarks>
        public bool IsActive { get; }
    }
}
