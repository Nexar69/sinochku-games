using System.Diagnostics;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class GameDetectionService
{
    private readonly SteamLocator _steam;
    private readonly SettingsStore _store;
    private readonly LauncherSettings _settings;

    public GameDetectionService(SteamLocator steam, SettingsStore store, LauncherSettings settings)
    {
        _steam = steam;
        _store = store;
        _settings = settings;
    }

    public GameRuntimeState Detect(GameDefinition game)
    {
        var path = game.SteamAppId is int appId ? _steam.FindInstalledApp(appId) : "";
        var running = !string.IsNullOrWhiteSpace(game.ProcessName)
            && Process.GetProcessesByName(game.ProcessName).Length > 0;

        return new GameRuntimeState
        {
            Game = game,
            Installed = !string.IsNullOrWhiteSpace(path),
            Running = running,
            InstallPath = path
        };
    }

    public string ResolveSteamEdit(bool rescan = false)
    {
        if (!rescan && File.Exists(_settings.SteamEditPath))
            return _settings.SteamEditPath;

        var roots = new[]
        {
            AppContext.BaseDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetEnvironmentVariable("OneDrive") ?? ""
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            var found = FindFile(root, "SteamEdit.exe", 3);
            if (string.IsNullOrWhiteSpace(found)) continue;

            _settings.SteamEditPath = found;
            _store.Save(_settings);
            return found;
        }

        return "";
    }

    public string BuildDiagnostics(GameDefinition game)
    {
        var state = Detect(game);
        var steamEdit = ResolveSteamEdit();
        return string.Join(Environment.NewLine, new[]
        {
            $"{Program.Brand} v{Program.Version}",
            $"Windows: {Environment.OSVersion}",
            $"Steam: {(_steam.IsSteamInstalled ? _steam.SteamRoot : "not detected")}",
            $"{game.Name}: {(state.Installed ? state.InstallPath : "not detected")}",
            $"SteamEdit: {(File.Exists(steamEdit) ? steamEdit : "not detected")}",
            $"Running: {state.Running}",
            $"Update channel: {_settings.UpdateChannel}",
            $"Logs: {AppPaths.LogDirectory}"
        });
    }

    private static string FindFile(string root, string fileName, int depth)
    {
        try
        {
            if (depth < 0 || string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return "";

            var direct = Path.Combine(root, fileName);
            if (File.Exists(direct)) return direct;

            if (depth == 0) return "";

            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var leaf = Path.GetFileName(dir);
                if (leaf.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                    || leaf.Equals(".git", StringComparison.OrdinalIgnoreCase))
                    continue;

                var found = FindFile(dir, fileName, depth - 1);
                if (!string.IsNullOrWhiteSpace(found))
                    return found;
            }
        }
        catch { }

        return "";
    }
}
