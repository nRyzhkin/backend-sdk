using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents an SDK operation that is part of the public contract but has not been implemented yet.
    /// </summary>
    public sealed class BackendNotImplementedException : BackendException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackendNotImplementedException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public BackendNotImplementedException(string message)
            : base(message, "not_implemented")
        {
        }
    }
}
