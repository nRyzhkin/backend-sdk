using System;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Represents a player profile returned by the backend.
    /// </summary>
    public sealed class PlayerProfile
    {
        internal PlayerProfile(
            Guid userId,
            string applicationId,
            string displayName,
            string avatarId,
            string publicDataJson,
            DateTime createdAt,
            DateTime updatedAt)
        {
            UserId = userId;
            ApplicationId = applicationId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AvatarId = avatarId;
            PublicDataJson = publicDataJson ?? "{}";
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Gets the public user identifier.
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// Gets the application identifier this profile belongs to.
        /// </summary>
        public string ApplicationId { get; }

        /// <summary>
        /// Gets the display name shown to other players.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the avatar identifier, or <c>null</c> when no avatar is set.
        /// </summary>
        public string AvatarId { get; }

        /// <summary>
        /// Gets when the profile was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets when the profile was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; }

        internal string PublicDataJson { get; }

        /// <summary>
        /// Deserializes the client-controlled public data payload to the requested type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <returns>The deserialized public data.</returns>
        public T GetPublicData<T>()
        {
            return RemoteConfigJson.DeserializeValue<T>(PublicDataJson, ApplicationId, "publicData");
        }
    }
}
