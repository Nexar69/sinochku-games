using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class SocialService : IAsyncDisposable
{
    private readonly LauncherSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly GameRegistry _registry;
    private readonly GameDetectionService _detection;
    private readonly SecureSessionStore _session = new();
    private readonly System.Windows.Forms.Timer _presenceTimer;
    private string? _activeGameSession;

    public SocialApiClient Api { get; }
    public SocialRealtimeClient Realtime { get; } = new();
    public SocialProfile? CurrentUser { get; private set; }
    public bool IsSignedIn => CurrentUser is not null && !string.IsNullOrWhiteSpace(Api.AccessToken);

    public event Action<SocialProfile?>? CurrentUserChanged;
    public event Action<SocialMessage>? MessageReceived;
    public event Action<SocialNotification>? NotificationReceived;
    public event Action<SocialFriendRequest>? FriendRequestReceived;
    public event Action? FriendsChanged;
    public event Action<SocialFriend>? PresenceChanged;
    public event Action<string, bool>? TypingChanged;

    public SocialService(
        LauncherSettings settings,
        SettingsStore settingsStore,
        GameRegistry registry,
        GameDetectionService detection)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _registry = registry;
        _detection = detection;

        Api = new SocialApiClient(settings.SocialServerUrl);
        var token = _session.LoadToken();
        if (!string.IsNullOrWhiteSpace(token))
            Api.SetToken(token);

        Realtime.MessageReceived += m => MessageReceived?.Invoke(m);
        Realtime.NotificationReceived += n => NotificationReceived?.Invoke(n);
        Realtime.FriendRequestReceived += r => FriendRequestReceived?.Invoke(r);
        Realtime.FriendsChanged += () => FriendsChanged?.Invoke();
        Realtime.PresenceChanged += f => PresenceChanged?.Invoke(f);
        Realtime.TypingChanged += (id, typing) => TypingChanged?.Invoke(id, typing);

        _presenceTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _presenceTimer.Tick += async (_, _) => await SyncGamePresenceAsync();
    }

    public async Task<bool> TryRestoreAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Api.AccessToken))
            return false;

        try
        {
            CurrentUser = await Api.MeAsync(ct);
            await ConnectRealtimeAsync(ct);
            _presenceTimer.Start();
            CurrentUserChanged?.Invoke(CurrentUser);
            return true;
        }
        catch
        {
            _session.Clear();
            Api.SetToken(null);
            CurrentUser = null;
            return false;
        }
    }

    public async Task SignInAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        var auth = await Api.LoginAsync(usernameOrEmail, password, ct);
        await ApplyAuthAsync(auth, ct);
    }

    public async Task RegisterAsync(
        string username,
        string email,
        string password,
        string inviteCode,
        string displayName,
        CancellationToken ct = default)
    {
        var auth = await Api.RegisterAsync(username, email, password, inviteCode, displayName, ct);
        await ApplyAuthAsync(auth, ct);
    }

    public async Task SignOutAsync()
    {
        _presenceTimer.Stop();
        if (IsSignedIn)
        {
            try { await Api.UpdatePresenceAsync("Offline"); } catch { }
        }

        await Realtime.DisconnectAsync();
        _session.Clear();
        Api.SetToken(null);
        CurrentUser = null;
        _activeGameSession = null;
        CurrentUserChanged?.Invoke(null);
    }

    public void SetServerUrl(string url)
    {
        _settings.SocialServerUrl = string.IsNullOrWhiteSpace(url)
            ? "http://localhost:5187"
            : url.Trim().TrimEnd('/');
        _settingsStore.Save(_settings);
        Api.SetBaseUrl(_settings.SocialServerUrl);
    }

    public async Task RefreshMeAsync(CancellationToken ct = default)
    {
        if (!IsSignedIn) return;
        CurrentUser = await Api.MeAsync(ct);
        CurrentUserChanged?.Invoke(CurrentUser);
    }

    private async Task ApplyAuthAsync(SocialAuthResponse auth, CancellationToken ct)
    {
        Api.SetToken(auth.AccessToken);
        _session.SaveToken(auth.AccessToken);
        CurrentUser = auth.User;
        await ConnectRealtimeAsync(ct);
        _presenceTimer.Start();
        try { await Api.UpdatePresenceAsync("Online", null, ct); } catch { }
        await RefreshMeAsync(ct);
    }

    private async Task ConnectRealtimeAsync(CancellationToken ct)
    {
        await Realtime.ConnectAsync(Api.BaseUrl, () => Api.AccessToken, ct);
    }

    private async Task SyncGamePresenceAsync()
    {
        if (!IsSignedIn) return;

        var running = _registry.Games.FirstOrDefault(g => _detection.Detect(g).Running);

        try
        {
            if (running is not null && _activeGameSession != running.Id)
            {
                if (_activeGameSession is not null)
                    await Api.EndSessionAsync();

                await Api.StartSessionAsync(running.Id);
                _activeGameSession = running.Id;
                return;
            }

            if (running is null && _activeGameSession is not null)
            {
                await Api.EndSessionAsync();
                _activeGameSession = null;
            }
        }
        catch (Exception ex)
        {
            LogService.Warn($"Could not sync social presence: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _presenceTimer.Stop();
        _presenceTimer.Dispose();

        if (IsSignedIn && _activeGameSession is not null)
        {
            try { await Api.EndSessionAsync(); } catch { }
        }

        await Realtime.DisposeAsync();
        Api.Dispose();
    }
}
