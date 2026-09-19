using Microsoft.AspNetCore.Identity;

namespace SinochkuGames.Server.Data;

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string BackgroundUrl { get; set; } = "";
    public string AccentHex { get; set; } = "#66C0F4";
    public string Presence { get; set; } = "Offline";
    public string CurrentGameId { get; set; } = "";
    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsFounder { get; set; }
    public string ProfileVisibility { get; set; } = "Friends";
    public bool AllowFriendRequests { get; set; } = true;
    public bool AllowMessagesFromFriends { get; set; } = true;
    public bool ShowGameActivity { get; set; } = true;
}

public sealed class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserAId { get; set; } = "";
    public string UserBId { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FriendRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderId { get; set; } = "";
    public string RecipientId { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAtUtc { get; set; }
}

public sealed class DirectMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderId { get; set; } = "";
    public string RecipientId { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAtUtc { get; set; }
}

public sealed class NotificationEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Text { get; set; } = "";
    public string ActorUserId { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string GameId { get; set; } = "";
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAtUtc { get; set; }
}

public sealed class InviteCode
{
    public string Code { get; set; } = "";
    public string CreatedByUserId { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public int MaxUses { get; set; } = 1;
    public int Uses { get; set; }
}

public sealed class ProfileShowcase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class ProfileComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProfileUserId { get; set; } = "";
    public string AuthorUserId { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RecoveryCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UsedAtUtc { get; set; }
}
