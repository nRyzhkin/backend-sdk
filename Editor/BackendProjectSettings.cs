using System;
using System.IO;
using BackendSdk.Internal;
using UnityEditor;
using UnityEngine;

namespace BackendSdk.Editor
{
    [Serializable]
    internal sealed class BackendProjectSettings
    {
        private const string ProjectSettingsPath = "ProjectSettings/BackendSdkSettings.json";
        private const string RuntimeResourcesDirectory = "Assets/Resources";
        private const string RuntimeSettingsPath = RuntimeResourcesDirectory + "/BackendSdkSettings.json";

        [SerializeField]
        private string serverUrl = string.Empty;

        [SerializeField]
        private string applicationId = string.Empty;

        [SerializeField]
        private int timeoutSeconds = 30;

        [SerializeField]
        private bool enableLogging;

        [SerializeField]
        private string apiKey = string.Empty;

        internal string ServerUrl
        {
            get => serverUrl;
            set => serverUrl = value ?? string.Empty;
        }

        internal string ApplicationId
        {
            get => applicationId;
            set => applicationId = value ?? string.Empty;
        }

        internal int TimeoutSeconds
        {
            get => timeoutSeconds;
            set => timeoutSeconds = Mathf.Max(1, value);
        }

        internal bool EnableLogging
        {
            get => enableLogging;
            set => enableLogging = value;
        }

        internal string ApiKey
        {
            get => apiKey;
            set => apiKey = value ?? string.Empty;
        }

        internal static BackendProjectSettings Load()
        {
            if (!File.Exists(ProjectSettingsPath))
            {
                return new BackendProjectSettings();
            }

            var json = File.ReadAllText(ProjectSettingsPath);
            return JsonUtility.FromJson<BackendProjectSettings>(json) ?? new BackendProjectSettings();
        }

        internal void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProjectSettingsPath) ?? "ProjectSettings");
            File.WriteAllText(ProjectSettingsPath, JsonUtility.ToJson(this, true));
            WriteRuntimeSettings();
        }

        internal BackendOptions ToRuntimeOptions()
        {
            return new BackendOptions
            {
                ServerUrl = ServerUrl,
                ApplicationId = ApplicationId,
                TimeoutSeconds = TimeoutSeconds,
                EnableLogging = EnableLogging,
                ApiKey = ApiKey
            };
        }

        private void WriteRuntimeSettings()
        {
            Directory.CreateDirectory(RuntimeResourcesDirectory);
            File.WriteAllText(RuntimeSettingsPath, JsonUtility.ToJson(ToRuntimeOptions(), true));
            AssetDatabase.ImportAsset(RuntimeSettingsPath);
            AssetDatabase.Refresh();
        }
    }
}
