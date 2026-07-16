using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BackendSdk.Editor
{
    internal static class BackendSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Backend", SettingsScope.Project)
            {
                label = "Backend",
                activateHandler = (_, rootElement) =>
                {
                    var settings = BackendProjectSettings.Load();

                    rootElement.style.paddingLeft = 12;
                    rootElement.style.paddingRight = 12;
                    rootElement.style.paddingTop = 8;

                    rootElement.Add(new HelpBox(
                        "These settings are saved to Project Settings and mirrored to a runtime JSON resource used by Backend.InitializeAsync().",
                        HelpBoxMessageType.Info));

                    rootElement.Add(CreateTextField("Server URL", settings.ServerUrl, value =>
                    {
                        settings.ServerUrl = value;
                        settings.Save();
                    }));

                    rootElement.Add(CreateTextField("Application ID", settings.ApplicationId, value =>
                    {
                        settings.ApplicationId = value;
                        settings.Save();
                    }));

                    var timeoutField = new IntegerField("Timeout (seconds)")
                    {
                        value = settings.TimeoutSeconds
                    };
                    timeoutField.RegisterValueChangedCallback(evt =>
                    {
                        settings.TimeoutSeconds = evt.newValue;
                        settings.Save();
                    });
                    rootElement.Add(timeoutField);

                    var loggingField = new Toggle("Enable Logging")
                    {
                        value = settings.EnableLogging
                    };
                    loggingField.RegisterValueChangedCallback(evt =>
                    {
                        settings.EnableLogging = evt.newValue;
                        settings.Save();
                    });
                    rootElement.Add(loggingField);

                    rootElement.Add(CreateTextField("API Key (future)", settings.ApiKey, value =>
                    {
                        settings.ApiKey = value;
                        settings.Save();
                    }));
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[]
                {
                    "backend",
                    "sdk",
                    "server",
                    "application",
                    "timeout",
                    "logging",
                    "api key"
                })
            };
        }

        private static TextField CreateTextField(string label, string value, System.Action<string> onChanged)
        {
            var field = new TextField(label)
            {
                value = value ?? string.Empty
            };

            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            return field;
        }
    }
}
