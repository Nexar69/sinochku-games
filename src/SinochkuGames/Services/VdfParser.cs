using System.Text.RegularExpressions;

namespace SinochkuGames.Services;

public static class VdfParser
{
    private static readonly Regex LibraryPathRegex = new(
        @"""path""\s+""([^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InstallDirRegex = new(
        @"""installdir""\s+""([^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<string> ParseLibraryPaths(string text)
    {
        return LibraryPathRegex
            .Matches(text)
            .Select(m => m.Groups[1].Value.Replace(@"\\", @"\"))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? ParseInstallDir(string text)
    {
        var match = InstallDirRegex.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }
}
