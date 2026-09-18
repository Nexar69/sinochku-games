namespace SinochkuGames.Services;

public static class VersionHelper
{
    public static bool IsNewer(string candidate, string current)
    {
        var left = Parse(candidate);
        var right = Parse(current);

        var numeric = left.Numeric.CompareTo(right.Numeric);
        if (numeric != 0) return numeric > 0;

        if (left.PreRelease is null && right.PreRelease is not null) return true;
        if (left.PreRelease is not null && right.PreRelease is null) return false;

        return string.Compare(left.PreRelease, right.PreRelease, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private static (Version Numeric, string? PreRelease) Parse(string input)
    {
        var clean = input.Trim().TrimStart('v', 'V');
        var parts = clean.Split('-', 2);
        var numeric = Version.TryParse(parts[0], out var parsed)
            ? parsed
            : new Version(0, 0, 0);
        return (numeric, parts.Length > 1 ? parts[1] : null);
    }
}
