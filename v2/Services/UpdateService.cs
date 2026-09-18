using System.Net.Http.Headers;
using System.Text.Json;

namespace SinochkuGames.Services;

public sealed class UpdateService
{
    private readonly string _repo;
    private readonly Version _current;
    private readonly HttpClient _http = new();

    public UpdateService(string repo, string currentVersion)
    {
        _repo = repo;
        _current = Version.Parse(currentVersion);
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SinochkuGames", currentVersion));
    }

    public sealed record UpdateInfo(Version Version, string DownloadUrl, string Notes);

    public async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync(
                $"https://api.github.com/repos/{_repo}/releases/latest", ct);
            response.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = json.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v');
            if (!Version.TryParse(tag, out var latest) || latest <= _current)
                return null;

            var asset = root.GetProperty("assets").EnumerateArray()
                .FirstOrDefault(a => string.Equals(
                    a.GetProperty("name").GetString(),
                    "SinochkuGames.exe",
                    StringComparison.OrdinalIgnoreCase));

            if (asset.ValueKind == JsonValueKind.Undefined)
                return null;

            return new UpdateInfo(
                latest,
                asset.GetProperty("browser_download_url").GetString()!,
                root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "");
        }
        catch
        {
            return null;
        }
    }
}
