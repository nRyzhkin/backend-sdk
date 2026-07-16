using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents the outcome of a backend service operation.
    /// </summary>
    /// <typeparam name="T">The result payload type.</typeparam>
    public sealed class RequestResult<T>
    {
        private RequestResult(bool succeeded, T data, BackendException error)
        {
            Succeeded = succeeded;
            Data = data;
            Error = error;
        }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the result data when the operation succeeds.
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// Gets the SDK-level error when the operation fails.
        /// </summary>
        public BackendException Error { get; }

        /// <summary>
        /// Returns the successful result data or throws if the operation failed.
        /// </summary>
        /// <returns>The successful result data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the operation failed.</exception>
        public T EnsureSuccess()
        {
            if (Succeeded)
            {
                return Data;
            }

            throw Error ?? new InvalidOperationException("The request did not succeed.");
        }

        internal static RequestResult<T> Success(T data)
        {
            return new RequestResult<T>(true, data, null);
        }

        internal static RequestResult<T> Failure(BackendException error)
        {
            return new RequestResult<T>(false, default, error);
        }
    }
}
