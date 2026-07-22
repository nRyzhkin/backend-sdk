using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace BackendSdk.Internal
{
    internal static class RemoteConfigJson
    {
        internal static Dictionary<string, RemoteConfigValue> ParseAll(string json, string applicationId)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, RemoteConfigValue>();
            }

            var trimmed = json.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                return ParseEntryArray(trimmed);
            }

            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                return ParseFlatObject(trimmed);
            }

            throw CreateDeserializationException(applicationId, null, typeof(Dictionary<string, RemoteConfigValue>), "Response is not a JSON object or array.");
        }

        internal static string ExtractValueJson(string json, string applicationId, string key)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw CreateDeserializationException(applicationId, key, typeof(RemoteConfigValue), "Response body is empty.");
            }

            var trimmed = json.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal)
                && TryGetObjectProperty(trimmed, "value", out var wrappedValue))
            {
                return wrappedValue;
            }

            return trimmed;
        }

        internal static T DeserializeValue<T>(string valueJson, string applicationId, string key)
        {
            if (valueJson == null)
            {
                throw CreateDeserializationException(applicationId, key, typeof(T), "Value JSON is null.");
            }

            var trimmed = valueJson.Trim();
            if (trimmed.Length == 0)
            {
                throw CreateDeserializationException(applicationId, key, typeof(T), "Value JSON is empty.");
            }

            try
            {
                var targetType = typeof(T);

                if (targetType == typeof(RemoteConfigValue))
                {
                    return (T)(object)new RemoteConfigValue(trimmed);
                }

                if (targetType == typeof(string))
                {
                    return (T)(object)ParseJsonString(trimmed);
                }

                if (targetType == typeof(bool))
                {
                    return (T)(object)ParseJsonBool(trimmed);
                }

                if (targetType == typeof(int))
                {
                    return (T)(object)ParseJsonInt(trimmed);
                }

                if (targetType == typeof(long))
                {
                    return (T)(object)ParseJsonLong(trimmed);
                }

                if (targetType == typeof(float))
                {
                    return (T)(object)ParseJsonFloat(trimmed);
                }

                if (targetType == typeof(double))
                {
                    return (T)(object)ParseJsonDouble(trimmed);
                }

                if (string.Equals(trimmed, "null", StringComparison.Ordinal))
                {
                    return default;
                }

                if (trimmed.StartsWith("[", StringComparison.Ordinal) && targetType.IsArray)
                {
                    return (T)ParseJsonArray(trimmed, targetType);
                }

                if (!trimmed.StartsWith("{", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Value is not a JSON object.");
                }

                var deserialized = UnityJsonSerializer.Deserialize<T>(trimmed);
                if (deserialized == null && !targetType.IsValueType)
                {
                    throw new InvalidOperationException("JsonUtility returned null.");
                }

                return deserialized;
            }
            catch (Exception exception) when (exception is not BackendException)
            {
                throw CreateDeserializationException(applicationId, key, typeof(T), exception.Message, exception);
            }
        }

        private static Dictionary<string, RemoteConfigValue> ParseEntryArray(string json)
        {
            var result = new Dictionary<string, RemoteConfigValue>(StringComparer.Ordinal);
            foreach (var element in SplitTopLevelArray(json))
            {
                if (!TryGetObjectProperty(element, "key", out var keyJson)
                    || !TryGetObjectProperty(element, "value", out var valueJson))
                {
                    continue;
                }

                var key = ParseJsonString(keyJson);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                result[key] = new RemoteConfigValue(valueJson);
            }

            return result;
        }

        private static Dictionary<string, RemoteConfigValue> ParseFlatObject(string json)
        {
            var result = new Dictionary<string, RemoteConfigValue>(StringComparer.Ordinal);
            foreach (var property in SplitTopLevelObjectProperties(json))
            {
                result[property.Key] = new RemoteConfigValue(property.Value);
            }

            return result;
        }

        internal static string ParseJsonString(string json)
        {
            var trimmed = json.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            {
                return Unquote(trimmed.Substring(1, trimmed.Length - 2));
            }

            return trimmed;
        }

        private static bool ParseJsonBool(string json)
        {
            var trimmed = json.Trim();
            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new FormatException($"Value '{json}' is not a JSON boolean.");
        }

        private static int ParseJsonInt(string json)
        {
            if (int.TryParse(json.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new FormatException($"Value '{json}' is not a JSON integer.");
        }

        private static long ParseJsonLong(string json)
        {
            if (long.TryParse(json.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new FormatException($"Value '{json}' is not a JSON integer.");
        }

        private static float ParseJsonFloat(string json)
        {
            if (float.TryParse(json.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new FormatException($"Value '{json}' is not a JSON number.");
        }

        private static double ParseJsonDouble(string json)
        {
            if (double.TryParse(json.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new FormatException($"Value '{json}' is not a JSON number.");
        }

        private static object ParseJsonArray(string json, Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            if (elementType == null)
            {
                throw new InvalidOperationException("Array type is invalid.");
            }

            var elements = SplitTopLevelArray(json);
            var array = Array.CreateInstance(elementType, elements.Count);
            for (var i = 0; i < elements.Count; i++)
            {
                var converted = DeserializeValueDynamic(elements[i], elementType);
                array.SetValue(converted, i);
            }

            return array;
        }

        private static object DeserializeValueDynamic(string valueJson, Type targetType)
        {
            if (targetType == typeof(string))
            {
                return ParseJsonString(valueJson);
            }

            if (targetType == typeof(bool))
            {
                return ParseJsonBool(valueJson);
            }

            if (targetType == typeof(int))
            {
                return ParseJsonInt(valueJson);
            }

            if (targetType == typeof(long))
            {
                return ParseJsonLong(valueJson);
            }

            if (targetType == typeof(float))
            {
                return ParseJsonFloat(valueJson);
            }

            if (targetType == typeof(double))
            {
                return ParseJsonDouble(valueJson);
            }

            return UnityJsonSerializer.Deserialize(valueJson, targetType);
        }

        internal static bool TryGetObjectProperty(string json, string propertyName, out string valueJson)
        {
            valueJson = null;
            var trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                return false;
            }

            foreach (var property in SplitTopLevelObjectProperties(trimmed))
            {
                if (string.Equals(property.Key, propertyName, StringComparison.Ordinal))
                {
                    valueJson = property.Value;
                    return true;
                }
            }

            return false;
        }

        internal static List<string> SplitTopLevelArray(string json)
        {
            var items = new List<string>();
            var trimmed = json.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '[')
            {
                return items;
            }

            var index = 1;
            while (index < trimmed.Length)
            {
                SkipWhitespace(trimmed, ref index);
                if (index >= trimmed.Length || trimmed[index] == ']')
                {
                    break;
                }

                if (!TryReadJsonValue(trimmed, ref index, out var value))
                {
                    break;
                }

                items.Add(value);
                SkipWhitespace(trimmed, ref index);
                if (index < trimmed.Length && trimmed[index] == ',')
                {
                    index++;
                }
            }

            return items;
        }

        private static List<KeyValuePair<string, string>> SplitTopLevelObjectProperties(string json)
        {
            var properties = new List<KeyValuePair<string, string>>();
            var trimmed = json.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '{')
            {
                return properties;
            }

            var index = 1;
            while (index < trimmed.Length)
            {
                SkipWhitespace(trimmed, ref index);
                if (index >= trimmed.Length || trimmed[index] == '}')
                {
                    break;
                }

                if (!TryReadJsonValue(trimmed, ref index, out var keyJson))
                {
                    break;
                }

                SkipWhitespace(trimmed, ref index);
                if (index >= trimmed.Length || trimmed[index] != ':')
                {
                    break;
                }

                index++;
                if (!TryReadJsonValue(trimmed, ref index, out var valueJson))
                {
                    break;
                }

                properties.Add(new KeyValuePair<string, string>(ParseJsonString(keyJson), valueJson));
                SkipWhitespace(trimmed, ref index);
                if (index < trimmed.Length && trimmed[index] == ',')
                {
                    index++;
                }
            }

            return properties;
        }

        private static bool TryReadJsonValue(string json, ref int index, out string valueJson)
        {
            valueJson = null;
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
            {
                return false;
            }

            var start = index;
            switch (json[index])
            {
                case '"':
                    if (!TryReadJsonString(json, ref index, out valueJson))
                    {
                        return false;
                    }

                    return true;
                case '{':
                    if (!TryReadBalanced(json, ref index, '{', '}', out valueJson))
                    {
                        return false;
                    }

                    return true;
                case '[':
                    if (!TryReadBalanced(json, ref index, '[', ']', out valueJson))
                    {
                        return false;
                    }

                    return true;
                default:
                    while (index < json.Length && ",]}".IndexOf(json[index]) < 0)
                    {
                        index++;
                    }

                    valueJson = json.Substring(start, index - start).Trim();
                    return valueJson.Length > 0;
            }
        }

        private static bool TryReadJsonString(string json, ref int index, out string valueJson)
        {
            valueJson = null;
            if (index >= json.Length || json[index] != '"')
            {
                return false;
            }

            var builder = new StringBuilder();
            index++;
            while (index < json.Length)
            {
                var character = json[index++];
                if (character == '"')
                {
                    valueJson = "\"" + builder + "\"";
                    return true;
                }

                if (character == '\\' && index < json.Length)
                {
                    builder.Append('\\');
                    builder.Append(json[index++]);
                    continue;
                }

                builder.Append(character);
            }

            return false;
        }

        private static bool TryReadBalanced(string json, ref int index, char open, char close, out string valueJson)
        {
            valueJson = null;
            if (index >= json.Length || json[index] != open)
            {
                return false;
            }

            var start = index;
            var depth = 0;
            var inString = false;
            while (index < json.Length)
            {
                var character = json[index++];
                if (character == '"')
                {
                    if (index > 1 && json[index - 2] != '\\')
                    {
                        inString = !inString;
                    }

                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (character == open)
                {
                    depth++;
                    continue;
                }

                if (character == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        valueJson = json.Substring(start, index - start);
                        return true;
                    }
                }
            }

            return false;
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        private static string Unquote(string value)
        {
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (character == '\\' && i + 1 < value.Length)
                {
                    var next = value[++i];
                    switch (next)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        default: builder.Append(next); break;
                    }

                    continue;
                }

                builder.Append(character);
            }

            return builder.ToString();
        }

        private static BackendException CreateDeserializationException(
            string applicationId,
            string key,
            Type targetType,
            string message,
            Exception innerException = null)
        {
            var context = new StringBuilder("Failed to deserialize remote config value.");
            if (!string.IsNullOrWhiteSpace(applicationId))
            {
                context.Append(" ApplicationId=").Append(applicationId);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                context.Append(" Key=").Append(key);
            }

            if (targetType != null)
            {
                context.Append(" TargetType=").Append(targetType.Name);
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                context.Append(' ').Append(message);
            }

            return new BackendException(context.ToString(), "remote_config_deserialization_failed", false, innerException);
        }
    }
}
