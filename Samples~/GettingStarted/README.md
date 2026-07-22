# Getting Started

1. Open `Project Settings > Backend`.
2. Set Backend URL and Application ID.
3. Optionally enable Development Mode and set Development Provider / External ID.
4. Initialize and use services:

```csharp
using System;
using BackendSdk;
using UnityEngine;

public static class GameBootstrap
{
    public static async System.Threading.Tasks.Task StartAsync()
    {
        await Backend.InitializeAsync();

        var apiUrl = await Backend.RemoteConfig.GetAsync<string>("apiUrl");
        var maintenance = await Backend.RemoteConfig.GetAsync<bool>("maintenance");

        await Backend.Auth.LoginAsync();

        await TestProfilesAsync();

        await Backend.Storage.SetAsync("Save", new MySave { Level = 1 });
        var save = await Backend.Storage.GetAsync<MySave>("Save");

        await Backend.Leaderboards.SubmitAsync("highscore", 1000, SortMode.Descending);
        var top = await Backend.Leaderboards.GetTopAsync("highscore");

        await Backend.Analytics.TrackAsync(
            "LevelStarted",
            new
            {
                level = 5,
                difficulty = "Hard"
            });

        await Backend.Analytics.TrackAsync("TutorialCompleted");
    }

    private static async System.Threading.Tasks.Task TestProfilesAsync()
    {
        var me = await Backend.Profiles.GetMeAsync();

        Debug.Log($"Profile before update: {me.UserId}, {me.DisplayName}");

        var updated = await Backend.Profiles.UpdateMeAsync(
            "Player One",
            "avatar_03",
            new MyPublicProfileData
            {
                status = "Looking for team",
                level = 12,
                badges = new[] { "founder", "tester" }
            });

        var data = updated.GetPublicData<MyPublicProfileData>();

        Debug.Log(
            $"Updated profile: {updated.DisplayName}, " +
            $"status={data.status}, level={data.level}");

        var publicProfile = await Backend.Profiles.GetAsync(updated.UserId);

        var batch = await Backend.Profiles.GetBatchAsync(
            new[]
            {
                updated.UserId,
                Guid.NewGuid()
            });

        Debug.Log(
            $"Batch profiles={batch.Profiles.Count}, " +
            $"missing={batch.MissingUserIds.Count}, " +
            $"public={publicProfile.DisplayName}");
    }
}

[System.Serializable]
public class MySave
{
    public int Level;
}

[System.Serializable]
public sealed class MyPublicProfileData
{
    public string status;
    public int level;
    public string[] badges;
}
```
