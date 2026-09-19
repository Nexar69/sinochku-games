namespace SinochkuGames.Models;

public sealed class LauncherSettings
{
    public bool FirstRunCompleted { get; set; } = false;
    public string Theme { get; set; } = "SteamDark";
    public string AccentHex { get; set; } = "#66C0F4";
    public string UpdateChannel { get; set; } = "Stable";
    public bool CheckUpdatesAutomatically { get; set; } = true;
    public string SteamEditPath { get; set; } = "";
    public string SocialServerUrl { get; set; } = "http://localhost:5187";
    public bool SocialEnabled { get; set; } = true;
    public bool OpenFriendsChatOnStart { get; set; } = false;
    public bool ShowSocialToasts { get; set; } = true;
    public HashSet<string> Favorites { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DateTimeOffset> LastPlayed { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, long> TrackedPlaySeconds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
