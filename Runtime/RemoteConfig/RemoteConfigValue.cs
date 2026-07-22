using System;

namespace BackendSdk
{
    /// <summary>
    /// Represents an arbitrary remote config JSON value.
    /// </summary>
    /// <remarks>
    /// This type is the SDK equivalent of a schema-free JSON value. Use <see cref="As{T}"/> to convert to a typed value.
    /// </remarks>
    public sealed class RemoteConfigValue
    {
        internal RemoteConfigValue(string rawJson)
        {
            RawJson = rawJson ?? "null";
        }

        /// <summary>
        /// Gets the raw JSON fragment for this value.
        /// </summary>
        public string RawJson { get; }

        /// <summary>
        /// Converts the JSON value to the requested type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <returns>The converted value.</returns>
        public T As<T>()
        {
            return Internal.RemoteConfigJson.DeserializeValue<T>(RawJson, null, null);
        }
    }
}
