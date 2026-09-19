namespace SinochkuGames.Models;

public sealed record SocialShowcase(Guid Id, string Title, string Body, int SortOrder);

public sealed record SocialProfile(
    string Id,
    string Username,
    string DisplayName,
    string Bio,
    string StatusText,
    string AvatarUrl,
    string BackgroundUrl,
    string AccentHex,
    string Presence,
    string CurrentGameId,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsFounder,
    long TotalPlaySeconds,
    IReadOnlyList<SocialShowcase> Showcases,
    IReadOnlyList<string> Badges);

public sealed record SocialFriend(
    string Id,
    string Username,
    string DisplayName,
    string AvatarUrl,
    string Presence,
    string CurrentGameId,
    string StatusText,
    DateTimeOffset LastSeenAtUtc);

public sealed record SocialFriendRequest(
    Guid Id,
    SocialFriend Sender,
    SocialFriend Recipient,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record SocialMessage(
    Guid Id,
    string SenderId,
    string RecipientId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record SocialConversation(
    SocialFriend Friend,
    SocialMessage? LastMessage,
    int UnreadCount);

public sealed record SocialNotification(
    Guid Id,
    string Type,
    string Text,
    string ActorUserId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record SocialActivity(
    string Type,
    string Text,
    DateTimeOffset AtUtc,
    string? GameId);

public sealed record SocialPrivacy(
    string ProfileVisibility,
    bool AllowFriendRequests,
    bool AllowMessagesFromFriends,
    bool ShowGameActivity);

public sealed record SocialAuthResponse(string AccessToken, SocialProfile User);
public sealed record SocialInvite(string Code, int MaxUses, int Uses, DateTimeOffset CreatedAtUtc, DateTimeOffset? ExpiresAtUtc);
