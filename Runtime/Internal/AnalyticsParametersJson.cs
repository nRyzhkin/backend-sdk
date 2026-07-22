using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace BackendSdk.Internal
{
    internal static class AnalyticsParametersJson
    {
        internal static string SerializeJsonValue(object value)
        {
            return SerializeValue(value);
        }

        internal static string QuoteJsonString(string value)
        {
            return Quote(value);
        }

        internal static string BuildRequestJson(string eventName, object parameters)
        {
            var builder = new StringBuilder();
            builder.Append("{\"eventName\":");
            builder.Append(Quote(eventName));

            if (parameters != null)
            {
                builder.Append(",\"parameters\":");
                builder.Append(SerializeValue(parameters));
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static string SerializeValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            switch (value)
            {
                case string text:
                    return Quote(text);
                case bool boolean:
                    return boolean ? "true" : "false";
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case float or double or decimal:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case IDictionary dictionary:
                    return SerializeDictionary(dictionary);
                case IEnumerable enumerable when value is not string:
                    return SerializeEnumerable(enumerable);
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (IsJsonUtilityCompatible(type))
            {
                return UnityJsonSerializer.Serialize(value);
            }

            return SerializeObjectMembers(value);
        }

        private static string SerializeDictionary(IDictionary dictionary)
        {
            var builder = new StringBuilder();
            builder.Append('{');

            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                builder.Append(Quote(Convert.ToString(entry.Key, CultureInfo.InvariantCulture)));
                builder.Append(':');
                builder.Append(SerializeValue(entry.Value));
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static string SerializeEnumerable(IEnumerable enumerable)
        {
            var builder = new StringBuilder();
            builder.Append('[');

            var first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                builder.Append(SerializeValue(item));
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string SerializeObjectMembers(object value)
        {
            var builder = new StringBuilder();
            builder.Append('{');

            var first = true;
            var type = value.GetType();

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                builder.Append(Quote(field.Name));
                builder.Append(':');
                builder.Append(SerializeValue(field.GetValue(value)));
            }

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                builder.Append(Quote(property.Name));
                builder.Append(':');
                builder.Append(SerializeValue(property.GetValue(value)));
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static bool IsJsonUtilityCompatible(Type type)
        {
            return type.IsDefined(typeof(SerializableAttribute), false) && !type.IsGenericType;
        }

        private static string Quote(string value)
        {
            if (value == null)
            {
                return "null";
            }

            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');

            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
