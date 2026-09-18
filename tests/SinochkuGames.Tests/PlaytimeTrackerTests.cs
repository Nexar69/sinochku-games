using SinochkuGames.Services;

namespace SinochkuGames.Tests;

public sealed class PlaytimeTrackerTests
{
    [Theory]
    [InlineData(0, "0s")]
    [InlineData(42, "42s")]
    [InlineData(60, "1m")]
    [InlineData(3599, "59m")]
    [InlineData(3600, "1h 0m")]
    [InlineData(7260, "2h 1m")]
    public void Format_ReturnsHumanReadableDuration(long seconds, string expected)
    {
        Assert.Equal(expected, PlaytimeTracker.Format(seconds));
    }
}
