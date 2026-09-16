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
            var provider = new SettingsProvider("Project/Backend", SettingsScope.Project)
            {
                label = "Backend",
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
                    "guest",
                    "editor account",
                    "authentication"
                })
            };

            provider.activateHandler = (_, rootElement) =>
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
                    "Editor Account is the only local Editor login: UserSettings/BackendSdkEditorAccount.json. " +
                    "The server remains the source of truth for the player. Empty + Play creates a guest and writes the public player id here. " +
                    "Paste a public player GUID to impersonate (Auth:AllowPublicIdLogin, on in Development). Clear the field for a new guest.",
                    HelpBoxMessageType.Info));

                var editorStore = new EditorAccountFileStore();
                var guestStore = new PlayerPrefsGuestCredentialStore();
                MigrateLegacyEditorPrefs(settings.ApplicationId, editorStore);
                editorStore.TryGet(settings.ApplicationId, out var storedAccount);
                var editorAccountField = CreateTextField(
                    "Editor Account",
                    storedAccount ?? string.Empty,
                    value => SaveEditorAccount(editorStore, guestStore, value));
                rootElement.Add(editorAccountField);

                void RefreshEditorAccountField()
                {
                    editorStore.TryGet(BackendProjectSettings.Load().ApplicationId, out var latest);
                    editorAccountField.SetValueWithoutNotify(latest ?? string.Empty);
                }

                void OnPlayModeStateChanged(PlayModeStateChange change)
                {
                    if (change == PlayModeStateChange.EnteredEditMode ||
                        change == PlayModeStateChange.ExitingPlayMode)
                        RefreshEditorAccountField();
                }

                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                provider.deactivateHandler = () =>
                {
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                };

                var buttons = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, marginTop = 4, marginBottom = 8 }
                };
                buttons.Add(new Button(RefreshEditorAccountField)
                {
                    text = "Reload"
                });
                buttons.Add(new Button(() =>
                {
                    SaveEditorAccount(editorStore, guestStore, string.Empty);
                    editorAccountField.SetValueWithoutNotify(string.Empty);
                })
                {
                    text = "New guest"
                });
                rootElement.Add(buttons);

                var advanced = new Foldout
                {
                    text = "Advanced (legacy development login)",
                    value = false
                };
                advanced.Add(new HelpBox(
                    "Optional fake platform identity for Backend.Auth.LoginAsync() in the Editor. WormsBase Play Mode does not use this.",
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
                advanced.Add(developmentModeField);
                advanced.Add(developmentProviderField);
                advanced.Add(developmentExternalIdField);
                rootElement.Add(advanced);
            };

            return provider;
        }

        static void MigrateLegacyEditorPrefs(string applicationId, EditorAccountFileStore editorStore)
        {
            if (editorStore.TryGet(applicationId, out var existing) && !string.IsNullOrWhiteSpace(existing))
            {
                new PlayerPrefsGuestCredentialStore(PlayerPrefsGuestCredentialStore.EditorLoginPrefix)
                    .Clear(applicationId);
                return;
            }

            var legacy = new PlayerPrefsGuestCredentialStore(PlayerPrefsGuestCredentialStore.EditorLoginPrefix);
            if (!legacy.TryGet(applicationId, out var leftover) || string.IsNullOrWhiteSpace(leftover))
                return;

            editorStore.Save(applicationId, leftover.Trim());
            legacy.Clear(applicationId);
        }

        static void SaveEditorAccount(
            EditorAccountFileStore editorStore,
            PlayerPrefsGuestCredentialStore guestStore,
            string value)
        {
            var applicationId = BackendProjectSettings.Load().ApplicationId;
            guestStore.Clear(applicationId);
            new PlayerPrefsGuestCredentialStore(PlayerPrefsGuestCredentialStore.EditorLoginPrefix)
                .Clear(applicationId);

            if (string.IsNullOrWhiteSpace(value))
            {
                editorStore.Clear(applicationId);
                return;
            }

            editorStore.Save(applicationId, value);
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
