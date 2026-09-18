namespace SinochkuGames.Models;

public sealed record GameDefinition(
    string Id,
    string DisplayName,
    uint SteamAppId,
    string? SteamInstallDirHint,
    string CoverAsset,
    string HeroAsset,
    string LogoAsset,
    string Description,
    bool RequiresSteamEdit = false
);
