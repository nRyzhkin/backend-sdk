using System;
using System.Collections.Generic;
using System.Globalization;
using BackendSdk;

namespace BackendSdk.Internal
{
    internal static class JobsJson
    {
        internal static JobOfferBatch ParseBatch(string json)
        {
            var trimmed = RequireObject(json);
            var jobs = new List<JobOffer>();
            if (RemoteConfigJson.TryGetObjectProperty(trimmed, "jobs", out var jobsJson))
            {
                foreach (var element in RemoteConfigJson.SplitTopLevelArray(jobsJson))
                {
                    jobs.Add(ParseOffer(element));
                }
            }

            var serverTime = ParseUtcDateTime(RequireProperty(trimmed, "serverTimeUtc"), "serverTimeUtc");
            return new JobOfferBatch(jobs, serverTime);
        }

        internal static JobOffer ParseOffer(string json)
        {
            var trimmed = RequireObject(json);
            return new JobOffer(
                ParseRequiredString(trimmed, "jobId"),
                ParseRequiredString(trimmed, "displayName"),
                ParseRequiredInt(trimmed, "level"),
                ParseRequiredLong(trimmed, "xp"),
                ParseRequiredLong(trimmed, "xpToNext"),
                ParseRequiredInt(trimmed, "freeTickets"),
                ParseRequiredInt(trimmed, "bonusTickets"),
                ParseRequiredInt(trimmed, "freeTicketMax"),
                ParseOptionalUtcDateTime(trimmed, "freeTicketsFullAtUtc"),
                ParseRequiredLong(trimmed, "rewardPreview"),
                ParseRequiredBool(trimmed, "hasActiveRun"),
                ParseRequiredBool(trimmed, "canStart"),
                ParseRequiredBool(trimmed, "canComplete"),
                ParseUtcDateTime(RequireProperty(trimmed, "serverTimeUtc"), "serverTimeUtc"));
        }

        internal static JobStartResult ParseStart(string json)
        {
            var trimmed = RequireObject(json);
            return new JobStartResult(ParseOffer(RequireProperty(trimmed, "job")));
        }

        internal static JobCompleteResult ParseComplete(string json)
        {
            var trimmed = RequireObject(json);
            return new JobCompleteResult(
                ParseOffer(RequireProperty(trimmed, "job")),
                ParseRequiredLong(trimmed, "coinsGranted"),
                ParseRequiredLong(trimmed, "xpGranted"),
                ParseRequiredInt(trimmed, "levelsGained"),
                ParseRequiredLong(trimmed, "levelUpCoinsGranted"),
                ParseRequiredInt(trimmed, "levelUpBonusTicketsGranted"),
                ParseRequiredString(trimmed, "currencyKey"));
        }

        private static string RequireObject(string json)
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

            return trimmed;
        }

        private static string RequireProperty(string json, string propertyName)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, propertyName, out var valueJson))
            {
                throw CreateDeserializationException($"Missing {propertyName} property.");
            }

            return valueJson;
        }

        private static string ParseRequiredString(string json, string propertyName)
        {
            return RemoteConfigJson.ParseJsonString(RequireProperty(json, propertyName));
        }

        private static long ParseRequiredLong(string json, string propertyName)
        {
            var valueJson = RequireProperty(json, propertyName);
            if (string.Equals(valueJson.Trim(), "null", StringComparison.Ordinal))
            {
                throw CreateDeserializationException($"Property '{propertyName}' cannot be null.");
            }

            return RemoteConfigJson.DeserializeValue<long>(valueJson, null, propertyName);
        }

        private static int ParseRequiredInt(string json, string propertyName)
        {
            var value = ParseRequiredLong(json, propertyName);
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw CreateDeserializationException($"Property '{propertyName}' is out of int range.");
            }

            return (int)value;
        }

        private static bool ParseRequiredBool(string json, string propertyName)
        {
            var valueJson = RequireProperty(json, propertyName);
            return RemoteConfigJson.DeserializeValue<bool>(valueJson, null, propertyName);
        }

        private static DateTime? ParseOptionalUtcDateTime(string json, string propertyName)
        {
            if (!RemoteConfigJson.TryGetObjectProperty(json, propertyName, out var valueJson)
                || string.Equals(valueJson.Trim(), "null", StringComparison.Ordinal))
            {
                return null;
            }

            return ParseUtcDateTime(valueJson, propertyName);
        }

        private static DateTime ParseUtcDateTime(string json, string propertyName)
        {
            var value = RemoteConfigJson.ParseJsonString(json);
            if (!DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                throw CreateDeserializationException($"Invalid DateTime in '{propertyName}': '{value}'.");
            }

            return parsed.ToUniversalTime();
        }

        private static BackendException CreateDeserializationException(string message)
        {
            return new BackendException(
                $"Failed to deserialize jobs response. {message}",
                "jobs_deserialization_failed");
        }
    }
}
