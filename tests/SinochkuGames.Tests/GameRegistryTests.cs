using SinochkuGames.Services;

namespace SinochkuGames.Tests;

public sealed class GameRegistryTests
{
    [Fact]
    public void EmbeddedRegistry_LoadsSinochku2()
    {
        var registry = new GameRegistry();
        var game = registry.Find("sinochku2");

        Assert.NotNull(game);
        Assert.Equal(730, game!.SteamAppId);
        Assert.Equal("cs2", game.ProcessName);
        Assert.Contains("SteamEditAutofix", game.PreLaunchHooks);
    }
}
