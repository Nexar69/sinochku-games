namespace SinochkuGames.Models;

public sealed class LauncherSettings
{
    public string SteamEditPath { get; set; } = "";
    public bool CheckForUpdates { get; set; } = true;
    public bool CloseLauncherAfterPlay { get; set; } = false;
    public string UpdateChannel { get; set; } = "stable";
    public string Accent { get; set; } = "steam";
}
