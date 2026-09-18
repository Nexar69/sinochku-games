using System.Text.Json;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LauncherSettings Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
                return new LauncherSettings();

            var json = File.ReadAllText(AppPaths.SettingsFile);
            return JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions)
                   ?? new LauncherSettings();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load settings; using defaults", ex);
            return new LauncherSettings();
        }
    }

    public void Save(LauncherSettings settings)
    {
        AppPaths.EnsureCreated();
        File.WriteAllText(
            AppPaths.SettingsFile,
            JsonSerializer.Serialize(settings, JsonOptions));
    }
}
