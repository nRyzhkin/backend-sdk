using UnityEngine;

namespace BackendSdk
{
    /// <summary>
    /// Default guest credential persistence using Unity PlayerPrefs, scoped by application id.
    /// </summary>
    public sealed class PlayerPrefsGuestCredentialStore : IGuestCredentialStore
    {
        public const string KeyPrefix = "BackendSdk.GuestKey.";

        /// <summary>
        /// PlayerPrefs prefix for Editor login (public user id or guest key).
        /// </summary>
        public const string EditorLoginPrefix = "BackendSdk.EditorLogin.";

        readonly string _prefix;

        public PlayerPrefsGuestCredentialStore()
            : this(KeyPrefix)
        {
        }

        public PlayerPrefsGuestCredentialStore(string keyPrefix)
        {
            _prefix = string.IsNullOrWhiteSpace(keyPrefix) ? KeyPrefix : keyPrefix;
        }

        public bool TryGet(string applicationId, out string guestKey)
        {
            guestKey = null;
            var key = PrefKey(applicationId);
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
            {
                return false;
            }

            var value = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            guestKey = value.Trim();
            return true;
        }

        public void Save(string applicationId, string guestKey)
        {
            var key = PrefKey(applicationId);
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(guestKey))
            {
                return;
            }

            PlayerPrefs.SetString(key, guestKey.Trim());
            PlayerPrefs.Save();
        }

        public void Clear(string applicationId)
        {
            var key = PrefKey(applicationId);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        string PrefKey(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            return _prefix + applicationId.Trim();
        }
    }
}
