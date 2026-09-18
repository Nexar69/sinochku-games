using System.Reflection;
using System.Text.Json;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class GameRegistry
{
    private readonly IReadOnlyList<GameDefinition> _games;

    public GameRegistry()
    {
        _games = LoadEmbeddedGames();
        LogService.Info($"Loaded {_games.Count} game definition(s)");
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
}
