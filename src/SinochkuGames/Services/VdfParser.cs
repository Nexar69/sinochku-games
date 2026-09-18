using System.Text.RegularExpressions;

namespace SinochkuGames.Services;

public static partial class VdfParser
{
    [GeneratedRegex(""path"\s+"([^"]+)"", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryPathRegex();

    [GeneratedRegex(""installdir"\s+"([^"]+)"", RegexOptions.IgnoreCase)]
    private static partial Regex InstallDirRegex();

    public static IReadOnlyList<string> ParseLibraryPaths(string text)
    {
        return LibraryPathRegex()
            .Matches(text)
            .Select(m => m.Groups[1].Value.Replace(@"\\", @""))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? ParseInstallDir(string text)
    {
        var match = InstallDirRegex().Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }
}
