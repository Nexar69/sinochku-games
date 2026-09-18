using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class GameRegistry
{
    private readonly SteamService _steam;

    public GameRegistry(SteamService steam) => _steam = steam;

    public IReadOnlyList<GameDefinition> Games { get; } =
    [
        new(
            Id: "sinochku2",
            DisplayName: "СИНОЧКУ 2",
            SteamAppId: 730,
            SteamInstallDirHint: "Counter-Strike Global Offensive",
            CoverAsset: "Assets/cover.png",
            HeroAsset: "Assets/hero.png",
            LogoAsset: "Assets/logo.png",
            Description: "The original СИНОЧКУ GAMES™ experience. Custom-branded Counter-Strike 2 for you and your friend.",
            RequiresSteamEdit: true)
    ];

    public bool IsInstalled(GameDefinition game) => _steam.FindInstalledGame(game.SteamAppId) is not null;
    public string? InstallPath(GameDefinition game) => _steam.FindInstalledGame(game.SteamAppId);
}
