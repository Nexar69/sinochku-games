using SinochkuGames.Models;

namespace SinochkuGames.Services;

public enum ArtworkKind
{
    Cover,
    Hero,
    Logo
}

public sealed class ArtworkService
{
    private readonly SteamLocator _steam;

    public ArtworkService(SteamLocator steam) => _steam = steam;

    public Image? Load(GameDefinition game, ArtworkKind kind)
    {
        var custom = FindCustomSteamArtwork(game, kind);
        if (!string.IsNullOrWhiteSpace(custom))
        {
            try
            {
                using var source = Image.FromFile(custom);
                return new Bitmap(source);
            }
            catch (Exception ex)
            {
                LogService.Warn($"Could not load custom artwork {custom}: {ex.Message}");
            }
        }

        var suffix = kind switch
        {
            ArtworkKind.Cover => game.CoverResourceSuffix,
            ArtworkKind.Hero => game.HeroResourceSuffix,
            ArtworkKind.Logo => game.LogoResourceSuffix,
            _ => game.CoverResourceSuffix
        };

        return AssetService.LoadBySuffix(suffix);
    }

    public string FindCustomSteamArtwork(GameDefinition game, ArtworkKind kind)
    {
        if (game.SteamAppId is not int appId || string.IsNullOrWhiteSpace(_steam.SteamRoot))
            return "";

        var userdata = Path.Combine(_steam.SteamRoot, "userdata");
        if (!Directory.Exists(userdata))
            return "";

        var stem = kind switch
        {
            ArtworkKind.Cover => $"{appId}p",
            ArtworkKind.Hero => $"{appId}_hero",
            ArtworkKind.Logo => $"{appId}_logo",
            _ => $"{appId}p"
        };

        var extensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };

        try
        {
            foreach (var user in Directory.EnumerateDirectories(userdata))
            {
                var grid = Path.Combine(user, "config", "grid");
                if (!Directory.Exists(grid))
                    continue;

                foreach (var extension in extensions)
                {
                    var path = Path.Combine(grid, stem + extension);
                    if (File.Exists(path))
                        return path;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Warn($"Could not inspect Steam custom artwork: {ex.Message}");
        }

        return "";
    }
}
