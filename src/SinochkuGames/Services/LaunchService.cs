using System.Diagnostics;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class LaunchService
{
    private readonly SteamLocator _steam;
    private readonly SettingsStore _store;
    private readonly LauncherSettings _settings;

    public LaunchService(SteamLocator steam, SettingsStore store, LauncherSettings settings)
    {
        _steam = steam;
        _store = store;
        _settings = settings;
    }

    public string? Validate(GameDefinition game, GameDetectionService detection)
    {
        if (!_steam.IsSteamInstalled)
            return "Steam could not be detected.";

        if (game.SteamAppId is int appId && string.IsNullOrWhiteSpace(_steam.FindInstalledApp(appId)))
            return $"{game.BaseGameName} is not installed in any detected Steam library.";

        if (game.PreLaunchHooks.Contains("SteamEditAutofix", StringComparer.OrdinalIgnoreCase)
            && !File.Exists(detection.ResolveSteamEdit()))
            return "SteamEdit could not be detected. Choose SteamEdit.exe in Settings first.";

        return null;
    }

    public void Launch(GameDefinition game, GameDetectionService detection)
    {
        var error = Validate(game, detection);
        if (error is not null)
            throw new InvalidOperationException(error);

        LogService.Info($"Launching game {game.Id}");

        if (game.PreLaunchHooks.Contains("SteamEditAutofix", StringComparer.OrdinalIgnoreCase))
        {
            var steamEdit = detection.ResolveSteamEdit();
            var target = game.SteamAppId is int appId
                ? $"steam://rungameid/{appId}"
                : "";

            Process.Start(new ProcessStartInfo
            {
                FileName = steamEdit,
                Arguments = $"-autofix -forcestart -- {target}",
                WorkingDirectory = Path.GetDirectoryName(steamEdit)!,
                UseShellExecute = true
            });
        }
        else if (game.SteamAppId is int appId)
        {
            Process.Start(new ProcessStartInfo($"steam://rungameid/{appId}")
            {
                UseShellExecute = true
            });
        }

        _settings.LastPlayed[game.Id] = DateTimeOffset.Now;
        _store.Save(_settings);
    }

    public void BrowseForSteamEdit(IWin32Window owner)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Locate SteamEdit.exe",
            Filter = "SteamEdit|SteamEdit.exe|Executable files|*.exe"
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return;

        _settings.SteamEditPath = dialog.FileName;
        _store.Save(_settings);
    }
}
