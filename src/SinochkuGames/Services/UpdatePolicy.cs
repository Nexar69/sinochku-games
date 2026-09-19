namespace SinochkuGames.Services;

public static class UpdatePolicy
{
    public static bool IsEligibleRelease(string tag, bool prerelease, string channel)
    {
        var isV2 = tag.StartsWith("v2.", StringComparison.OrdinalIgnoreCase);
        var isV3 = tag.StartsWith("v3.", StringComparison.OrdinalIgnoreCase);
        var beta = channel.Equals("Beta", StringComparison.OrdinalIgnoreCase);

        if (isV2)
            return beta || !prerelease;

        if (isV3)
            return beta || !prerelease;

        return false;
    }
}
