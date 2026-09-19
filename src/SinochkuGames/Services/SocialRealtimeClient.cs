using Microsoft.AspNetCore.SignalR.Client;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class SocialRealtimeClient : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<SocialMessage>? MessageReceived;
    public event Action<SocialNotification>? NotificationReceived;
    public event Action<SocialFriendRequest>? FriendRequestReceived;
    public event Action? FriendsChanged;
    public event Action<SocialFriend>? PresenceChanged;
    public event Action<string, bool>? TypingChanged;

    public async Task ConnectAsync(string baseUrl, Func<string?> token, CancellationToken ct = default)
    {
        await DisconnectAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl.TrimEnd('/')}/hub/social", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token());
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<SocialMessage>("MessageReceived", message => MessageReceived?.Invoke(message));
        _connection.On<SocialNotification>("NotificationReceived", n => NotificationReceived?.Invoke(n));
        _connection.On<SocialFriendRequest>("FriendRequestReceived", r => FriendRequestReceived?.Invoke(r));
        _connection.On("FriendsChanged", () => FriendsChanged?.Invoke());
        _connection.On<SocialFriend>("PresenceChanged", f => PresenceChanged?.Invoke(f));
        _connection.On<string, bool>("TypingChanged", (id, typing) => TypingChanged?.Invoke(id, typing));

        await _connection.StartAsync(ct);
    }

    public Task SetTypingAsync(string targetUserId, bool isTyping, CancellationToken ct = default) =>
        _connection?.State == HubConnectionState.Connected
            ? _connection.InvokeAsync("Typing", targetUserId, isTyping, ct)
            : Task.CompletedTask;

    public async Task DisconnectAsync()
    {
        if (_connection is null) return;
        try { await _connection.StopAsync(); } catch { }
        await _connection.DisposeAsync();
        _connection = null;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
