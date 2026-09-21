using Microsoft.AspNetCore.SignalR;
using SinochkuGames.Server.Contracts;
using SinochkuGames.Server.Hubs;

namespace SinochkuGames.Server.Services;

public sealed class RealtimeNotifier(IHubContext<SocialHub> hub)
{
    public Task MessageAsync(string userId, MessageDto message) =>
        hub.Clients.Group(SocialHub.UserGroup(userId)).SendAsync("MessageReceived", message);

    public Task NotificationAsync(string userId, NotificationDto notification) =>
        hub.Clients.Group(SocialHub.UserGroup(userId)).SendAsync("NotificationReceived", notification);

    public Task FriendRequestAsync(string userId, FriendRequestDto request) =>
        hub.Clients.Group(SocialHub.UserGroup(userId)).SendAsync("FriendRequestReceived", request);

    public Task FriendsChangedAsync(string userId) =>
        hub.Clients.Group(SocialHub.UserGroup(userId)).SendAsync("FriendsChanged");

    public Task PresenceAsync(string userId, FriendDto friend) =>
        hub.Clients.Group(SocialHub.UserGroup(userId)).SendAsync("PresenceChanged", friend);
}
