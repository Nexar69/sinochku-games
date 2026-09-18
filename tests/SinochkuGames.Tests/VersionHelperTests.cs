using SinochkuGames.Services;

namespace SinochkuGames.Tests;

public sealed class VersionHelperTests
{
    [Theory]
    [InlineData("2.0.1", "2.0.0", true)]
    [InlineData("2.0.0", "2.0.0-beta.1", true)]
    [InlineData("2.0.0-beta.2", "2.0.0-beta.1", true)]
    [InlineData("2.0.0-beta.1", "2.0.0", false)]
    [InlineData("1.9.9", "2.0.0-beta.1", false)]
    public void IsNewer_ComparesReleaseAndPrereleaseVersions(
        string candidate,
        string current,
        bool expected)
    {
        Assert.Equal(expected, VersionHelper.IsNewer(candidate, current));
    }
}
