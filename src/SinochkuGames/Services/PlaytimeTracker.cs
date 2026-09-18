using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class PlaytimeTracker : IDisposable
{
    private readonly GameRegistry _registry;
    private readonly GameDetectionService _detection;
    private readonly SettingsStore _store;
    private readonly LauncherSettings _settings;
    private readonly System.Windows.Forms.Timer _timer;
    private int _secondsSinceSave;

    public PlaytimeTracker(
        GameRegistry registry,
        GameDetectionService detection,
        SettingsStore store,
        LauncherSettings settings)
    {
        _registry = registry;
        _detection = detection;
        _store = store;
        _settings = settings;

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public long GetTrackedSeconds(string gameId) =>
        _settings.TrackedPlaySeconds.TryGetValue(gameId, out var seconds) ? seconds : 0;

    public static string Format(long seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m";
        return $"{span.Seconds}s";
    }

    private void Tick()
    {
        var changed = false;

        foreach (var game in _registry.Games)
        {
            if (!_detection.Detect(game).Running)
                continue;

            _settings.TrackedPlaySeconds[game.Id] = GetTrackedSeconds(game.Id) + 1;
            changed = true;
        }

        if (!changed)
            return;

        _secondsSinceSave++;
        if (_secondsSinceSave < 30)
            return;

        _secondsSinceSave = 0;
        _store.Save(_settings);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        if (_secondsSinceSave > 0)
            _store.Save(_settings);
    }
}
