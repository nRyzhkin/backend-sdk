namespace BackendSdk
{
    /// <summary>
    /// Persists the server-issued guest credential so a device can restore the same player.
    /// </summary>
    public interface IGuestCredentialStore
    {
        /// <summary>
        /// Reads the stored guest key for an application, if any.
        /// </summary>
        bool TryGet(string applicationId, out string guestKey);

        /// <summary>
        /// Stores the guest key for an application.
        /// </summary>
        void Save(string applicationId, string guestKey);

        /// <summary>
        /// Removes the stored guest key for an application.
        /// </summary>
        void Clear(string applicationId);
    }
}
