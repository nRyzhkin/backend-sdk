using System;
using System.Globalization;

namespace BackendSdk.Internal
{
    internal static class AuthJson
    {
        internal static ParsedLoginResponse ParseLogin(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new BackendException("Login response body is empty.", "invalid_login_response");
            }

            var trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                throw new BackendException("Login response is not a JSON object.", "invalid_login_response");
            }

            if (!RemoteConfigJson.TryGetObjectProperty(trimmed, "userId", out var userIdJson) ||
                !RemoteConfigJson.TryGetObjectProperty(trimmed, "accessToken", out var accessTokenJson) ||
                !RemoteConfigJson.TryGetObjectProperty(trimmed, "expiresAt", out var expiresAtJson))
            {
                throw new BackendException(
                    "Login response did not include userId, accessToken, and expiresAt.",
                    "invalid_login_response");
            }

            var userId = RemoteConfigJson.ParseJsonString(userIdJson);
            var accessToken = RemoteConfigJson.ParseJsonString(accessTokenJson);
            var expiresAtRaw = RemoteConfigJson.ParseJsonString(expiresAtJson);
            var provider = ReadOptionalString(trimmed, "provider");
            var guestKey = ReadOptionalString(trimmed, "guestKey");

            if (!DateTime.TryParse(
                    expiresAtRaw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var expiresAt))
            {
                expiresAt = DateTime.UtcNow;
            }
            else if (expiresAt.Kind == DateTimeKind.Unspecified)
            {
                expiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
            }
            else
            {
                expiresAt = expiresAt.ToUniversalTime();
            }

            return new ParsedLoginResponse(userId, accessToken, expiresAt, provider, guestKey);
        }

        internal static string BuildLoginRequest(string provider, string externalId)
        {
            return "{\"provider\":" + Quote(provider ?? string.Empty) +
                   ",\"externalId\":" + Quote(externalId ?? string.Empty) + "}";
        }

        internal static string BuildImpersonateRequest(string userId)
        {
            return "{\"userId\":" + Quote(userId ?? string.Empty) + "}";
        }

        static string Quote(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"") + "\"";
        }

        static string ReadOptionalString(string json, string property)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, property, out var raw) ||
                string.Equals(raw, "null", StringComparison.Ordinal))
            {
                return null;
            }

            return RemoteConfigJson.ParseJsonString(raw);
        }

        internal readonly struct ParsedLoginResponse
        {
            internal ParsedLoginResponse(
                string userId,
                string accessToken,
                DateTime expiresAt,
                string provider,
                string guestKey)
            {
                UserId = userId ?? string.Empty;
                AccessToken = accessToken ?? string.Empty;
                ExpiresAt = expiresAt;
                Provider = provider ?? string.Empty;
                GuestKey = guestKey;
            }

            internal string UserId { get; }
            internal string AccessToken { get; }
            internal DateTime ExpiresAt { get; }
            internal string Provider { get; }
            internal string GuestKey { get; }
        }
    }
}
