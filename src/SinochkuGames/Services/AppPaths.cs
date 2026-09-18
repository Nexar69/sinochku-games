namespace SinochkuGames.Services;

public static class AppPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SinochkuGames");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string LogDirectory => Path.Combine(Root, "logs");
    public static string UpdateDirectory => Path.Combine(Root, "updates");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(UpdateDirectory);
    }
}
