using System;
using UnityEngine;

namespace BackendSdk.Internal
{
    internal static class UnityJsonSerializer
    {
        internal static string Serialize<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return JsonUtility.ToJson(value);
        }

        internal static T Deserialize<T>(string json)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)(json ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonUtility.FromJson<T>(json);
        }
    }
}
