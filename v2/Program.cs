using SinochkuGames.Services;
using SinochkuGames.UI;

namespace SinochkuGames;

internal static class Program
{
    public const string Brand = "СИНОЧКУ GAMES™";
    public const string Version = "2.0.0";
    public const string Repository = "Nexar69/sinochku-games";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settingsService = new SettingsService();
        var steam = new SteamService();
        var registry = new GameRegistry(steam);
        var launcher = new LauncherService(steam, settingsService);
        var updater = new UpdateService(Repository, Version);

        Application.Run(new MainForm(settingsService, registry, launcher, updater));
    }
}
