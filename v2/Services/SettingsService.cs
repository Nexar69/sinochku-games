using System.Text.Json;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SinochkuGames");

    public string SettingsPath => Path.Combine(_folder, "settings.json");
    public LauncherSettings Current { get; private set; }

    public SettingsService() => Current = Load();

    public LauncherSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new LauncherSettings();

            return JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(SettingsPath), JsonOptions) ?? new LauncherSettings();
        }
        catch
        {
            return new LauncherSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, JsonOptions));
    }

    public void Reset()
    {
        Current = new LauncherSettings();
        Save();
    }
}
