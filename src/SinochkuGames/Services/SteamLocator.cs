using Microsoft.Win32;

namespace SinochkuGames.Services;

public sealed class SteamLocator
{
    public string SteamRoot { get; private set; } = "";
    public string SteamExe => string.IsNullOrWhiteSpace(SteamRoot)
        ? ""
        : Path.Combine(SteamRoot, "steam.exe");

    public SteamLocator()
    {
        SteamRoot = FindSteamRoot();
    }

    public bool IsSteamInstalled => File.Exists(SteamExe);

    public IReadOnlyList<string> GetLibraries()
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(SteamRoot))
            return result;

        result.Add(SteamRoot);

        try
        {
            var libraryFile = Path.Combine(SteamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFile))
            {
                foreach (var library in VdfParser.ParseLibraryPaths(File.ReadAllText(libraryFile)))
                {
                    if (!result.Contains(library, StringComparer.OrdinalIgnoreCase))
                        result.Add(library);
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to read Steam libraryfolders.vdf", ex);
        }

        return result;
    }

    public string FindInstalledApp(int appId)
    {
        foreach (var library in GetLibraries())
        {
            try
            {
                var manifest = Path.Combine(library, "steamapps", $"appmanifest_{appId}.acf");
                if (!File.Exists(manifest)) continue;

                var installDir = VdfParser.ParseInstallDir(File.ReadAllText(manifest));
                if (string.IsNullOrWhiteSpace(installDir)) continue;

                var path = Path.Combine(library, "steamapps", "common", installDir);
                if (Directory.Exists(path))
                    return path;
            }
            catch (Exception ex)
            {
                LogService.Warn($"Could not inspect app {appId} in {library}: {ex.Message}");
            }
        }

        return "";
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
            if (File.Exists(SteamExe))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(SteamExe)
                {
                    UseShellExecute = true
                });
        }
    }

    private static string FindSteamRoot()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath")?.ToString()?.Replace('/', '\\');
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, "steam.exe")))
                return path;
        }
        catch { }

        var guesses = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
        };

        return guesses.FirstOrDefault(p => File.Exists(Path.Combine(p, "steam.exe"))) ?? "";
    }
}
