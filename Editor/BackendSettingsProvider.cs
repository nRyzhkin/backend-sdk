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

                    rootElement.Add(CreateTextField("Backend URL", settings.ServerUrl, value =>
                    {
                        settings.ServerUrl = value;
                        settings.Save();
                    }));

                    rootElement.Add(CreateTextField("Application ID", settings.ApplicationId, value =>
                    {
                        settings.ApplicationId = value;
                        settings.Save();
                    }));

                    var timeoutField = new IntegerField("Request Timeout (seconds)")
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

                    var retryCountField = new IntegerField("Retry Count")
                    {
                        value = settings.RetryCount
                    };
                    retryCountField.RegisterValueChangedCallback(evt =>
                    {
                        settings.RetryCount = evt.newValue;
                        settings.Save();
                    });
                    rootElement.Add(retryCountField);

                    var retryDelayField = new IntegerField("Retry Delay (ms)")
                    {
                        value = settings.RetryDelayMilliseconds
                    };
                    retryDelayField.RegisterValueChangedCallback(evt =>
                    {
                        settings.RetryDelayMilliseconds = evt.newValue;
                        settings.Save();
                    });
                    rootElement.Add(retryDelayField);

                    rootElement.Add(new HelpBox(
                        "Development mode allows Backend.Auth.LoginAsync() to authenticate using the credentials below while running in the Unity Editor.",
                        HelpBoxMessageType.Info));

                    var developmentProviderField = CreateTextField(
                        "Development Provider",
                        settings.DevelopmentProvider,
                        value =>
                        {
                            settings.DevelopmentProvider = value;
                            settings.Save();
                        });
                    developmentProviderField.SetEnabled(settings.DevelopmentMode);

                    var developmentExternalIdField = CreateTextField(
                        "Development External ID",
                        settings.DevelopmentExternalId,
                        value =>
                        {
                            settings.DevelopmentExternalId = value;
                            settings.Save();
                        });
                    developmentExternalIdField.SetEnabled(settings.DevelopmentMode);

                    var developmentModeField = new Toggle("Development Mode")
                    {
                        value = settings.DevelopmentMode
                    };
                    developmentModeField.RegisterValueChangedCallback(evt =>
                    {
                        settings.DevelopmentMode = evt.newValue;
                        developmentProviderField.SetEnabled(evt.newValue);
                        developmentExternalIdField.SetEnabled(evt.newValue);
                        settings.Save();
                    });
                    rootElement.Add(developmentModeField);
                    rootElement.Add(developmentProviderField);
                    rootElement.Add(developmentExternalIdField);
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[]
                {
                    "backend",
                    "sdk",
                    "server",
                    "application",
                    "timeout",
                    "logging",
                    "retry",
                    "development",
                    "provider",
                    "external id",
                    "authentication"
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
