using SinochkuGames.Server.Services;

namespace SinochkuGames.Server.Tests;

public sealed class SocialQueryServiceTests
{
    [Fact]
    public void Normalize_IsStableRegardlessOfInputOrder()
    {
        var first = SocialQueryService.Normalize("user-b", "user-a");
        var second = SocialQueryService.Normalize("user-a", "user-b");

        Assert.Equal(("user-a", "user-b"), first);
        Assert.Equal(first, second);
    }
}
