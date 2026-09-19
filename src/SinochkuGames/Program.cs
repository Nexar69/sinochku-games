using SinochkuGames.Services;
using SinochkuGames.UI;

namespace SinochkuGames;

internal static class Program
{
    public const string Brand = "СИНОЧКУ GAMES™";
    public const string Version = "3.0.0-alpha.1";
    public const string Repository = "Nexar69/sinochku-games";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstance = new Mutex(true, @"Local\SinochkuGames", out var firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show(
                $"{Brand} is already running.",
                Brand,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

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
            using var playtime = new PlaytimeTracker(registry, detection, settingsStore, settings);
            var artwork = new ArtworkService(steam);
            await using var social = new SocialService(settings, settingsStore, registry, detection);

            var context = new LauncherContext(
                settingsStore,
                settings,
                steam,
                registry,
                detection,
                launcher,
                updater,
                playtime,
                artwork,
                social);

            if (!settings.FirstRunCompleted)
            {
                using var setup = new SetupWizardForm(context);
                setup.ShowDialog();
            }

            if (settings.SocialEnabled)
            {
                var restored = await social.TryRestoreAsync();
                if (!restored)
                {
                    using var auth = new SocialAuthForm(context);
                    auth.ShowDialog();
                }
            }

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
