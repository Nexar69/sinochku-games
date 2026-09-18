namespace SinochkuGames.Models;

public sealed class GameDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string BaseGameName { get; set; } = "";
    public int? SteamAppId { get; set; }
    public string ProcessName { get; set; } = "";
    public string[] PreLaunchHooks { get; set; } = Array.Empty<string>();
    public string HeroResourceSuffix { get; set; } = "hero.png";
    public string LogoResourceSuffix { get; set; } = "logo.png";
    public string CoverResourceSuffix { get; set; } = "cover.png";
    public string Description { get; set; } = "";
}

public sealed class GameRuntimeState
{
    public required GameDefinition Game { get; init; }
    public bool Installed { get; init; }
    public bool Running { get; init; }
    public string InstallPath { get; init; } = "";
    public string StatusText => Running ? "Running" : Installed ? "Ready to play" : "Not installed";
}
