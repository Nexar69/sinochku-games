using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SinochkuGames.Services;

public sealed class SteamService
{
    private string? _steamPath;
    private IReadOnlyList<string>? _libraries;

    public string? SteamPath => _steamPath ??= DetectSteam();

    public string? SteamExe =>
        SteamPath is { } path && File.Exists(Path.Combine(path, "steam.exe"))
            ? Path.Combine(path, "steam.exe")
            : null;

    public IReadOnlyList<string> Libraries => _libraries ??= DetectLibraries();

    public string? FindInstalledGame(uint appId)
    {
        foreach (var library in Libraries)
        {
            var manifest = Path.Combine(library, "steamapps", $"appmanifest_{appId}.acf");
            if (!File.Exists(manifest))
                continue;

            try
            {
                var text = File.ReadAllText(manifest);
                var match = Regex.Match(text, ""installdir"\s+"([^"]+)"", RegexOptions.IgnoreCase);
                if (!match.Success)
                    continue;

                var folder = Path.Combine(library, "steamapps", "common", match.Groups[1].Value);
                if (Directory.Exists(folder))
                    return folder;
            }
            catch { }
        }

        return null;
    }

    public void OpenSteam()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("steam://open/main")
            {
                UseShellExecute = true
            });
        }
        catch
        {
            if (SteamExe is { } exe)
                System.Diagnostics.Process.Start(exe);
        }
    }

    private static string? DetectSteam()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var value = key?.GetValue("SteamPath")?.ToString()?.Replace('/', '\\');
            if (!string.IsNullOrWhiteSpace(value) && File.Exists(Path.Combine(value, "steam.exe")))
                return value;
        }
        catch { }

        foreach (var path in new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
        })
        {
            if (File.Exists(Path.Combine(path, "steam.exe")))
                return path;
        }

        return null;
    }

    private IReadOnlyList<string> DetectLibraries()
    {
        var result = new List<string>();
        if (SteamPath is not { } steam)
            return result;

        result.Add(steam);
        var file = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(file))
            return result;

        try
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(text, ""path"\s+"([^"]+)"", RegexOptions.IgnoreCase))
            {
                var path = match.Groups[1].Value.Replace(@"\\", @"\");
                if (Directory.Exists(path) && !result.Contains(path, StringComparer.OrdinalIgnoreCase))
                    result.Add(path);
            }
        }
        catch { }

        return result;
    }
}
