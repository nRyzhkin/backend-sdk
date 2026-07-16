using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents an SDK-level failure that can be surfaced to game code without exposing transport details.
    /// </summary>
    public sealed class BackendException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackendException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public BackendException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The underlying exception.</param>
        public BackendException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errorCode">A stable SDK error code.</param>
        /// <param name="isTransient">Whether the failure may succeed if retried later.</param>
        /// <param name="innerException">The underlying exception.</param>
        public BackendException(string message, string errorCode, bool isTransient = false, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode ?? string.Empty;
            IsTransient = isTransient;
        }

        /// <summary>
        /// Gets a stable SDK-level error code.
        /// </summary>
        public string ErrorCode { get; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the failure may succeed if retried later.
        /// </summary>
        public bool IsTransient { get; }
    }
}
