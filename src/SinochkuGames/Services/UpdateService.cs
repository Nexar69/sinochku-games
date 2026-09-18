using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class UpdateRelease
{
    public string Version { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string HtmlUrl { get; init; } = "";
    public string Name { get; init; } = "";
    public bool Prerelease { get; init; }
}

public sealed class UpdateService
{
    private readonly LauncherSettings _settings;
    private readonly HttpClient _http = new();

    public UpdateService(LauncherSettings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SinochkuGames", Program.Version));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<UpdateRelease?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.CheckUpdatesAutomatically)
            return null;

        try
        {
            var json = await _http.GetStringAsync(
                $"https://api.github.com/repos/{Program.Repository}/releases",
                cancellationToken);

            using var document = JsonDocument.Parse(json);
            foreach (var release in document.RootElement.EnumerateArray())
            {
                var prerelease = release.GetProperty("prerelease").GetBoolean();
                if (_settings.UpdateChannel.Equals("Stable", StringComparison.OrdinalIgnoreCase) && prerelease)
                    continue;

                var tag = release.GetProperty("tag_name").GetString() ?? "";
                if (!tag.StartsWith("v2.", StringComparison.OrdinalIgnoreCase))
                    continue;

                var version = tag.TrimStart('v', 'V');
                if (!VersionHelper.IsNewer(version, Program.Version))
                    continue;

                foreach (var asset in release.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (!name.Equals("SinochkuGames.exe", StringComparison.OrdinalIgnoreCase))
                        continue;

                    return new UpdateRelease
                    {
                        Version = version,
                        DownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "",
                        HtmlUrl = release.GetProperty("html_url").GetString() ?? "",
                        Name = release.GetProperty("name").GetString() ?? tag,
                        Prerelease = prerelease
                    };
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Warn($"Update check failed: {ex.Message}");
        }

        return null;
    }

    public async Task InstallAsync(UpdateRelease release, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();

        var downloaded = Path.Combine(AppPaths.UpdateDirectory, "SinochkuGames.new.exe");
        var backup = Path.Combine(AppPaths.UpdateDirectory, "SinochkuGames.previous.exe");
        var script = Path.Combine(AppPaths.UpdateDirectory, "apply-update.ps1");
        var current = Environment.ProcessPath
                      ?? throw new InvalidOperationException("Current executable path is unavailable.");

        var bytes = await _http.GetByteArrayAsync(release.DownloadUrl, cancellationToken);
        await File.WriteAllBytesAsync(downloaded, bytes, cancellationToken);

        var processId = Environment.ProcessId;
        var ps = string.Join(Environment.NewLine, new[]
        {
            "$ErrorActionPreference = 'Stop'",
            $"while (Get-Process -Id {processId} -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 250 }}",
            $"$current = '{EscapePs(current)}'",
            $"$new = '{EscapePs(downloaded)}'",
            $"$backup = '{EscapePs(backup)}'",
            "try {",
            "    if (Test-Path $backup) { Remove-Item $backup -Force }",
            "    Copy-Item $current $backup -Force",
            "    Copy-Item $new $current -Force",
            "    Start-Process $current",
            "} catch {",
            "    if (Test-Path $backup) { Copy-Item $backup $current -Force }",
            "    Start-Process $current",
            "}"
        }) + Environment.NewLine;

        await File.WriteAllTextAsync(script, ps, cancellationToken);

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Application.Exit();
    }

    private static string EscapePs(string value) => value.Replace("'", "''");
}
