using UnityEngine;

namespace BackendSdk.Internal
{
    internal static class RuntimeSettingsLoader
    {
        internal const string ResourceName = "BackendSdkSettings";

        internal static BackendOptions LoadOptions()
        {
            var asset = Resources.Load<TextAsset>(ResourceName);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return new BackendOptions();
            }

            return UnityJsonSerializer.Deserialize<BackendOptions>(asset.text) ?? new BackendOptions();
        }
    }
}
