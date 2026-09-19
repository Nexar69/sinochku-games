using SinochkuGames.Models;
using SinochkuGames.Services;

namespace SinochkuGames;

public sealed record LauncherContext(
    SettingsStore SettingsStore,
    LauncherSettings Settings,
    SteamLocator Steam,
    GameRegistry Registry,
    GameDetectionService Detection,
    LaunchService Launcher,
    UpdateService Updater,
    PlaytimeTracker Playtime,
    ArtworkService Artwork,
    SocialService Social);
