#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace BackendSdk
{
    /// <summary>
    /// Editor login identity as a project UserSettings file (visible, not PlayerPrefs).
    /// Player builds do not use this store.
    /// </summary>
    public sealed class EditorAccountFileStore : IGuestCredentialStore
    {
        public const string FileName = "BackendSdkEditorAccount.json";

        [Serializable]
        sealed class Payload
        {
            public string userId = string.Empty;
        }

        public static string FilePath
        {
            get
            {
                var data = Application.dataPath;
                if (string.IsNullOrEmpty(data))
                    return FileName;
                var projectRoot = Directory.GetParent(data);
                if (projectRoot == null)
                    return FileName;
                return Path.Combine(projectRoot.FullName, "UserSettings", FileName);
            }
        }

        public bool TryGet(string applicationId, out string guestKey)
        {
            guestKey = null;
            var path = FilePath;
            if (!File.Exists(path))
                return false;

            try
            {
                var json = File.ReadAllText(path);
                var payload = JsonUtility.FromJson<Payload>(json);
                var value = payload != null ? payload.userId : null;
                if (string.IsNullOrWhiteSpace(value))
                    return false;
                guestKey = value.Trim();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Save(string applicationId, string guestKey)
        {
            if (string.IsNullOrWhiteSpace(guestKey))
            {
                Clear(applicationId);
                return;
            }

            var path = FilePath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonUtility.ToJson(new Payload { userId = guestKey.Trim() }, true);
            File.WriteAllText(path, json);
        }

        public void Clear(string applicationId)
        {
            var path = FilePath;
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
#endif
