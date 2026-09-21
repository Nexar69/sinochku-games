using Microsoft.EntityFrameworkCore;
using SinochkuGames.Server.Contracts;
using SinochkuGames.Server.Data;

namespace SinochkuGames.Server.Services;

public sealed class SocialQueryService(SocialDbContext db)
{
    public async Task<bool> AreFriendsAsync(string a, string b, CancellationToken ct = default)
    {
        var (x, y) = Normalize(a, b);
        return await db.Friendships.AnyAsync(f => f.UserAId == x && f.UserBId == y, ct);
    }

    public async Task<List<string>> FriendIdsAsync(string userId, CancellationToken ct = default)
    {
        return await db.Friendships
            .Where(f => f.UserAId == userId || f.UserBId == userId)
            .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
            .ToListAsync(ct);
    }

    public static (string A, string B) Normalize(string a, string b) =>
        string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);

    public static FriendDto ToFriend(AppUser user) => new(
        user.Id,
        user.UserName ?? "",
        string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "" : user.DisplayName,
        user.AvatarUrl,
        user.Presence,
        user.CurrentGameId,
        user.StatusText,
        user.LastSeenAtUtc);
}
