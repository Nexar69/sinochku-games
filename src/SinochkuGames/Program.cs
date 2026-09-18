using SinochkuGames.Services;
using SinochkuGames.UI;

namespace SinochkuGames;

internal static class Program
{
    public const string Brand = "СИНОЧКУ GAMES™";
    public const string Version = "2.0.0-beta.1";
    public const string Repository = "Nexar69/sinochku-games";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppPaths.EnsureCreated();
        LogService.Initialize();
        LogService.Info($"Starting {Brand} v{Version}");

        try
        {
            var settingsStore = new SettingsStore();
            var settings = settingsStore.Load();
            var steam = new SteamLocator();
            var registry = new GameRegistry();
            var detection = new GameDetectionService(steam, settingsStore, settings);
            var launcher = new LaunchService(steam, settingsStore, settings);
            var updater = new UpdateService(settings);

            var context = new LauncherContext(
                settingsStore,
                settings,
                steam,
                registry,
                detection,
                launcher,
                updater);

            Application.Run(new MainForm(context));
        }
        catch (Exception ex)
        {
            LogService.Error("Fatal startup error", ex);
            MessageBox.Show(
                $"СИНОЧКУ GAMES™ could not start.\n\n{ex.Message}\n\nLogs: {AppPaths.LogDirectory}",
                Brand,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
