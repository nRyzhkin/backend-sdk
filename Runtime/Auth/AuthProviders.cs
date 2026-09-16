namespace BackendSdk
{
    /// <summary>
    /// Well-known authentication provider identifiers understood by my-backend.
    /// </summary>
    public static class AuthProviders
    {
        /// <summary>
        /// Server-issued guest accounts created via <c>POST /v1/auth/guest</c>.
        /// </summary>
        public const string Guest = "guest";
    }
}
