using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class LauncherService
{
    private readonly SteamService _steam;
    private readonly SettingsService _settings;

    public LauncherService(SteamService steam, SettingsService settings)
    {
        _steam = steam;
        _settings = settings;
    }

    public async Task LaunchAsync(GameDefinition game, IWin32Window owner)
    {
        if (_steam.FindInstalledGame(game.SteamAppId) is null)
            throw new InvalidOperationException($"{game.DisplayName} is not installed.");

        if (game.RequiresSteamEdit)
        {
            var steamEdit = FindSteamEdit();
            if (steamEdit is null)
                throw new FileNotFoundException("SteamEdit.exe could not be found. Choose it in Settings.");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = steamEdit,
                Arguments = $"-autofix -forcestart -- steam://rungameid/{game.SteamAppId}",
                WorkingDirectory = Path.GetDirectoryName(steamEdit)!,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        else
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                $"steam://rungameid/{game.SteamAppId}") { UseShellExecute = true });
        }

        await Task.Delay(300);
        if (_settings.Current.CloseLauncherAfterPlay)
            Application.Exit();
    }

    public string? FindSteamEdit()
    {
        var saved = _settings.Current.SteamEditPath;
        if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
            return saved;

        foreach (var root in SearchRoots())
        {
            try
            {
                var hit = Directory.EnumerateFiles(root, "SteamEdit.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (hit is not null)
                    return hit;
            }
            catch { }
        }

        return null;
    }

    private static IEnumerable<string> SearchRoots()
    {
        yield return AppContext.BaseDirectory;
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
