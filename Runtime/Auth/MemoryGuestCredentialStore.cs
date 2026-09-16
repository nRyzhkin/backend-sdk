using System;
using System.Collections.Generic;

namespace BackendSdk
{
    /// <summary>
    /// In-memory guest credential store for tests and hosts that persist credentials themselves.
    /// </summary>
    public sealed class MemoryGuestCredentialStore : IGuestCredentialStore
    {
        readonly Dictionary<string, string> _keys = new Dictionary<string, string>(StringComparer.Ordinal);

        public bool TryGet(string applicationId, out string guestKey)
        {
            guestKey = null;
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return false;
            }

            if (!_keys.TryGetValue(applicationId.Trim(), out var value) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            guestKey = value;
            return true;
        }

        public void Save(string applicationId, string guestKey)
        {
            if (string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(guestKey))
            {
                return;
            }

            _keys[applicationId.Trim()] = guestKey.Trim();
        }

        public void Clear(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return;
            }

            _keys.Remove(applicationId.Trim());
        }
    }
}
