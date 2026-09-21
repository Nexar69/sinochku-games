using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SinochkuGames.Server.Data;
using SinochkuGames.Server.Services;

namespace SinochkuGames.Server.Hubs;

[Authorize]
public sealed class SocialHub(
    SocialDbContext db,
    SocialQueryService social) : Hub
{
    private static readonly ConcurrentDictionary<string, int> Connections = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new HubException("Missing user id.");

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        Connections.AddOrUpdate(userId, 1, (_, count) => count + 1);

        var user = await db.Users.FindAsync(userId);
        if (user is not null && user.Presence == "Offline")
        {
            user.Presence = "Online";
            user.LastSeenAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            await BroadcastPresenceAsync(user);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var count = Connections.AddOrUpdate(userId, 0, (_, current) => Math.Max(0, current - 1));
            if (count == 0)
            {
                Connections.TryRemove(userId, out _);
                var user = await db.Users.FindAsync(userId);
                if (user is not null)
                {
                    user.Presence = "Offline";
                    user.CurrentGameId = "";
                    user.LastSeenAtUtc = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync();
                    await BroadcastPresenceAsync(user);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Typing(string targetUserId, bool isTyping)
    {
        var me = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new HubException("Missing user id.");

        if (!await social.AreFriendsAsync(me, targetUserId))
            throw new HubException("Not friends.");

        await Clients.Group(UserGroup(targetUserId))
            .SendAsync("TypingChanged", me, isTyping);
    }

    private async Task BroadcastPresenceAsync(AppUser user)
    {
        var friendIds = await social.FriendIdsAsync(user.Id);
        var dto = SocialQueryService.ToFriend(user);
        foreach (var friendId in friendIds)
            await Clients.Group(UserGroup(friendId)).SendAsync("PresenceChanged", dto);
    }

    public static string UserGroup(string userId) => $"user:{userId}";
}
