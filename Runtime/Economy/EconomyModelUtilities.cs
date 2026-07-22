using System;
using System.Collections.Generic;

namespace BackendSdk
{
    internal static class EconomyModelUtilities
    {
        internal static IReadOnlyList<T> ToReadOnlyCopy<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return Array.Empty<T>();
            }

            var copy = new T[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                copy[i] = items[i];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
