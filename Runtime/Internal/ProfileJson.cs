using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BackendSdk;

namespace BackendSdk.Internal
{
    internal static class ProfileJson
    {
        internal static PlayerProfile ParseProfile(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw CreateDeserializationException("Response body is empty.");
            }

            var trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                throw CreateDeserializationException("Response is not a JSON object.");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "userId", out var userIdJson))
            {
                throw CreateDeserializationException("Missing userId property.");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "applicationId", out var applicationIdJson))
            {
                throw CreateDeserializationException("Missing applicationId property.");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "displayName", out var displayNameJson))
            {
                throw CreateDeserializationException("Missing displayName property.");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "createdAt", out var createdAtJson))
            {
                throw CreateDeserializationException("Missing createdAt property.");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "updatedAt", out var updatedAtJson))
            {
                throw CreateDeserializationException("Missing updatedAt property.");
            }

            var userId = ParseGuid(userIdJson, "userId");
            var applicationId = RemoteConfigJson.ParseJsonString(applicationIdJson);
            var displayName = RemoteConfigJson.ParseJsonString(displayNameJson);
            var avatarId = ParseNullableString(trimmed, "avatarId");
            var publicDataJson = ParsePublicDataJson(trimmed);
            var createdAt = ParseUtcDateTime(createdAtJson, "createdAt");
            var updatedAt = ParseUtcDateTime(updatedAtJson, "updatedAt");

            return new PlayerProfile(
                userId,
                applicationId,
                displayName,
                avatarId,
                publicDataJson,
                createdAt,
                updatedAt);
        }

        internal static PlayerProfileBatchResult ParseBatchResult(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw CreateDeserializationException("Response body is empty.");
            }

            var trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                throw CreateDeserializationException("Response is not a JSON object.");
            }

            var profiles = new List<PlayerProfile>();
            if (RemoteConfigJson.TryGetObjectProperty(trimmed, "profiles", out var profilesJson))
            {
                foreach (var profileJson in RemoteConfigJson.SplitTopLevelArray(profilesJson))
                {
                    profiles.Add(ParseProfile(profileJson));
                }
            }

            var missingUserIds = new List<Guid>();
            if (RemoteConfigJson.TryGetObjectProperty(trimmed, "missingUserIds", out var missingJson))
            {
                foreach (var userIdJson in RemoteConfigJson.SplitTopLevelArray(missingJson))
                {
                    missingUserIds.Add(ParseGuid(userIdJson, "missingUserIds"));
                }
            }

            return new PlayerProfileBatchResult(profiles, missingUserIds);
        }

        internal static string BuildUpdateRequest(string displayName, string avatarId, object publicData)
        {
            var builder = new StringBuilder();
            builder.Append("{\"displayName\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(displayName));
            builder.Append(",\"avatarId\":");

            if (avatarId == null)
            {
                builder.Append("null");
            }
            else
            {
                builder.Append(AnalyticsParametersJson.QuoteJsonString(avatarId));
            }

            builder.Append(",\"publicData\":");
            builder.Append(AnalyticsParametersJson.SerializeJsonValue(publicData ?? new EmptyPublicData()));
            builder.Append('}');
            return builder.ToString();
        }

        internal static string BuildUpdateRequest(string displayName, string avatarId, string publicDataJson)
        {
            var normalizedPublicData = string.IsNullOrWhiteSpace(publicDataJson)
                ? "{}"
                : publicDataJson.Trim();

            if (!normalizedPublicData.StartsWith("{", StringComparison.Ordinal))
            {
                throw new ArgumentException("Public data must be a JSON object.", nameof(publicDataJson));
            }

            var builder = new StringBuilder();
            builder.Append("{\"displayName\":");
            builder.Append(AnalyticsParametersJson.QuoteJsonString(displayName));
            builder.Append(",\"avatarId\":");

            if (avatarId == null)
            {
                builder.Append("null");
            }
            else
            {
                builder.Append(AnalyticsParametersJson.QuoteJsonString(avatarId));
            }

            builder.Append(",\"publicData\":");
            builder.Append(normalizedPublicData);
            builder.Append('}');
            return builder.ToString();
        }

        internal static string BuildBatchRequest(IReadOnlyList<Guid> userIds)
        {
            var builder = new StringBuilder();
            builder.Append("{\"userIds\":[");

            for (var i = 0; i < userIds.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(AnalyticsParametersJson.QuoteJsonString(userIds[i].ToString()));
            }

            builder.Append("]}");
            return builder.ToString();
        }

        internal static List<Guid> DedupeUserIdsPreserveOrder(IReadOnlyCollection<Guid> userIds)
        {
            var seen = new HashSet<Guid>();
            var deduped = new List<Guid>(userIds.Count);

            foreach (var userId in userIds)
            {
                if (seen.Add(userId))
                {
                    deduped.Add(userId);
                }
            }

            return deduped;
        }

        private static string ParsePublicDataJson(string profileJson)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(profileJson, "publicData", out var publicDataJson)
                || string.Equals(publicDataJson.Trim(), "null", StringComparison.Ordinal))
            {
                return "{}";
            }

            return publicDataJson;
        }

        private static string ParseNullableString(string profileJson, string propertyName)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(profileJson, propertyName, out var valueJson))
            {
                return null;
            }

            if (string.Equals(valueJson.Trim(), "null", StringComparison.Ordinal))
            {
                return null;
            }

            return RemoteConfigJson.ParseJsonString(valueJson);
        }

        private static Guid ParseGuid(string json, string fieldName)
        {
            var value = RemoteConfigJson.ParseJsonString(json);
            if (!Guid.TryParse(value, out var guid))
            {
                throw CreateDeserializationException($"Invalid GUID in '{fieldName}': '{value}'.");
            }

            return guid;
        }

        private static DateTime ParseUtcDateTime(string json, string fieldName)
        {
            var value = RemoteConfigJson.ParseJsonString(json);
            if (!DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                throw CreateDeserializationException($"Invalid DateTime in '{fieldName}': '{value}'.");
            }

            return parsed.ToUniversalTime();
        }

        private static BackendException CreateDeserializationException(string message)
        {
            return new BackendException(
                $"Failed to deserialize player profile. {message}",
                "profile_deserialization_failed");
        }

        [Serializable]
        private sealed class EmptyPublicData
        {
        }
    }
}
