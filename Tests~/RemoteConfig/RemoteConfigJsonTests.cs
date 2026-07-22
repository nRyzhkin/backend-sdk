using System.Collections.Generic;
using BackendSdk;
using BackendSdk.Internal;
using NUnit.Framework;

namespace BackendSdk.Tests.RemoteConfig
{
    public sealed class RemoteConfigJsonTests
    {
        [Test]
        public void ParseAll_SupportsBackendEntryArray()
        {
            var json = "[{\"key\":\"apiUrl\",\"value\":\"https://api.example.com\"},{\"key\":\"maintenance\",\"value\":false}]";

            var result = RemoteConfigJson.ParseAll(json, "game-1");

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("https://api.example.com", result["apiUrl"].As<string>());
            Assert.AreEqual(false, result["maintenance"].As<bool>());
        }

        [Test]
        public void ParseAll_SupportsFlatObject()
        {
            var json = "{\"apiUrl\":\"https://api.example.com\",\"maxPlayers\":100}";

            var result = RemoteConfigJson.ParseAll(json, "game-1");

            Assert.AreEqual("https://api.example.com", result["apiUrl"].As<string>());
            Assert.AreEqual(100, result["maxPlayers"].As<int>());
        }

        [Test]
        public void ExtractValueJson_UnwrapsBackendEntry()
        {
            var json = "{\"key\":\"cdnUrl\",\"value\":\"https://cdn.example.com\"}";

            var valueJson = RemoteConfigJson.ExtractValueJson(json, "game-1", "cdnUrl");

            Assert.AreEqual("https://cdn.example.com", RemoteConfigJson.DeserializeValue<string>(valueJson, "game-1", "cdnUrl"));
        }

        [Test]
        public void DeserializeValue_String_DoesNotDoubleEncode()
        {
            var valueJson = "\"https://cdn.example.com\"";

            var value = RemoteConfigJson.DeserializeValue<string>(valueJson, "game-1", "cdnUrl");

            Assert.AreEqual("https://cdn.example.com", value);
        }

        [Test]
        public void DeserializeValue_NumberAndBool_Work()
        {
            Assert.AreEqual(100, RemoteConfigJson.DeserializeValue<int>("100", "game-1", "maxPlayers"));
            Assert.AreEqual(true, RemoteConfigJson.DeserializeValue<bool>("true", "game-1", "maintenance"));
        }

        [Test]
        public void DeserializeValue_Object_WorksWithSerializableDto()
        {
            var valueJson = "{\"android\":\"https://android\",\"ios\":\"https://ios\"}";

            var value = RemoteConfigJson.DeserializeValue<AssetUrls>(valueJson, "game-1", "assetUrls");

            Assert.AreEqual("https://android", value.android);
            Assert.AreEqual("https://ios", value.ios);
        }

        [System.Serializable]
        private sealed class AssetUrls
        {
            public string android;
            public string ios;
        }
    }
}
