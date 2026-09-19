using SinochkuGames.Services;

namespace SinochkuGames.Tests;

public sealed class UpdatePolicyTests
{
    [Theory]
    [InlineData("v2.0.0", false, "Stable", true)]
    [InlineData("v2.0.1-beta.1", true, "Stable", false)]
    [InlineData("v2.0.1-beta.1", true, "Beta", true)]
    [InlineData("v3.0.0-alpha.1", true, "Stable", false)]
    [InlineData("v3.0.0-alpha.1", true, "Beta", true)]
    [InlineData("v3.0.0", false, "Stable", true)]
    [InlineData("v3.0.0", false, "Beta", true)]
    [InlineData("v4.0.0", false, "Beta", false)]
    public void IsEligibleRelease_RespectsChannelAndBridge(
        string tag,
        bool prerelease,
        string channel,
        bool expected)
    {
        Assert.Equal(expected, UpdatePolicy.IsEligibleRelease(tag, prerelease, channel));
    }
}
