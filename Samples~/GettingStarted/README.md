# Getting Started

1. Open `Project Settings > Backend`.
2. Set Backend URL and Application ID.
3. Optionally enable Development Mode and set Development Provider / External ID.
4. Initialize and use services:

```csharp
using BackendSdk;

public static class GameBootstrap
{
    public static async System.Threading.Tasks.Task StartAsync()
    {
        await Backend.InitializeAsync();

        var apiUrl = await Backend.RemoteConfig.GetAsync<string>("apiUrl");
        var maintenance = await Backend.RemoteConfig.GetAsync<bool>("maintenance");

        await Backend.Auth.LoginAsync();

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
}

[System.Serializable]
public class MySave
{
    public int Level;
}
```
