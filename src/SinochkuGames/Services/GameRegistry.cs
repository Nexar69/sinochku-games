using System.Reflection;
using System.Text.Json;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class GameRegistry
{
    private readonly IReadOnlyList<GameDefinition> _games;

    public GameRegistry()
    {
        var embedded = LoadEmbeddedGames();
        var external = LoadExternalGames();

        var merged = new Dictionary<string, GameDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var game in embedded)
            merged[game.Id] = game;

        foreach (var game in external)
        {
            if (merged.ContainsKey(game.Id))
            {
                LogService.Warn($"Ignoring external game '{game.Id}' because that ID is built in.");
                continue;
            }

            merged[game.Id] = game;
        }

        _games = merged.Values
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        LogService.Info($"Loaded {_games.Count} game definition(s) ({external.Count} custom)");
    }

    public IReadOnlyList<GameDefinition> Games => _games;

    public GameDefinition? Find(string id) =>
        _games.FirstOrDefault(g => g.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<GameDefinition> LoadEmbeddedGames()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var games = new List<GameDefinition>();
        foreach (var resource in assembly.GetManifestResourceNames()
                     .Where(n => n.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                              && n.Contains(".Games.", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null) continue;

            var game = JsonSerializer.Deserialize<GameDefinition>(stream, options);
            if (game is not null && !string.IsNullOrWhiteSpace(game.Id))
                games.Add(game);
        }

        return games.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private static IReadOnlyList<GameDefinition> LoadExternalGames()
    {
        var result = new List<GameDefinition>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            Directory.CreateDirectory(AppPaths.CustomGamesDirectory);

            foreach (var file in Directory.EnumerateFiles(AppPaths.CustomGamesDirectory, "*.json"))
            {
                try
                {
                    var game = JsonSerializer.Deserialize<GameDefinition>(
                        File.ReadAllText(file),
                        options);

                    if (game is null || !IsSafeDefinition(game))
                    {
                        LogService.Warn($"Ignoring invalid custom game manifest: {file}");
                        continue;
                    }

                    result.Add(game);
                }
                catch (Exception ex)
                {
                    LogService.Warn($"Could not read custom game manifest {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Warn($"Could not scan custom game manifests: {ex.Message}");
        }

        return result;
    }

    private static bool IsSafeDefinition(GameDefinition game)
    {
        if (string.IsNullOrWhiteSpace(game.Id)
            || string.IsNullOrWhiteSpace(game.Name)
            || game.Id.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')))
            return false;

        var allowedHooks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SteamEditAutofix"
        };

        return game.PreLaunchHooks.All(hook => allowedHooks.Contains(hook));
    }
}
