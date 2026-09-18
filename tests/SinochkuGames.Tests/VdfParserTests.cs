using SinochkuGames.Services;

namespace SinochkuGames.Tests;

public sealed class VdfParserTests
{
    [Fact]
    public void ParseLibraryPaths_ReadsAndDeduplicatesLibraries()
    {
        const string vdf = """
"libraryfolders"
{
    "0"
    {
        "path"  "C:\\Program Files (x86)\\Steam"
    }
    "1"
    {
        "path"  "D:\\SteamLibrary"
    }
    "2"
    {
        "path"  "D:\\SteamLibrary"
    }
}
""";

        var paths = VdfParser.ParseLibraryPaths(vdf);

        Assert.Equal(2, paths.Count);
        Assert.Contains(@"C:\Program Files (x86)\Steam", paths);
        Assert.Contains(@"D:\SteamLibrary", paths);
    }

    [Fact]
    public void ParseInstallDir_ReadsManifestDirectory()
    {
        const string manifest = """
"AppState"
{
    "appid" "730"
    "installdir" "Counter-Strike Global Offensive"
}
""";

        Assert.Equal(
            "Counter-Strike Global Offensive",
            VdfParser.ParseInstallDir(manifest));
    }
}
