# Getting Started

Configure the package from `Project Settings > Backend`, then initialize it from game code:

```csharp
using BackendSdk;

public static class GameBootstrap
{
    public static async System.Threading.Tasks.Task InitializeAsync()
    {
        await Backend.InitializeAsync();
    }
}
```

Future modules will hang off `Backend`, for example `Backend.Auth` or `Backend.Leaderboards`.
