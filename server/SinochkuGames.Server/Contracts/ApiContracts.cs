namespace SinochkuGames.Server.Contracts;

public sealed record RegisterRequest(string Username, string Email, string Password, string InviteCode, string? DisplayName);
public sealed record LoginRequest(string UsernameOrEmail, string Password);
public sealed record AuthResponse(string AccessToken, UserProfileDto User);

public sealed record UserProfileDto(
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
    IReadOnlyList<ShowcaseDto> Showcases,
    IReadOnlyList<string> Badges);

public sealed record UpdateProfileRequest(
    string DisplayName,
    string Bio,
    string StatusText,
    string AccentHex);

public sealed record PrivacySettingsDto(
    string ProfileVisibility,
    bool AllowFriendRequests,
    bool AllowMessagesFromFriends,
    bool ShowGameActivity);

public sealed record UpdatePrivacySettingsRequest(
    string ProfileVisibility,
    bool AllowFriendRequests,
    bool AllowMessagesFromFriends,
    bool ShowGameActivity);

public sealed record FriendDto(
    string Id,
    string Username,
    string DisplayName,
    string AvatarUrl,
    string Presence,
    string CurrentGameId,
    string StatusText,
    DateTimeOffset LastSeenAtUtc);

public sealed record SendFriendRequest(string Username);
public sealed record FriendRequestDto(Guid Id, FriendDto Sender, FriendDto Recipient, string Status, DateTimeOffset CreatedAtUtc);

public sealed record SendMessageRequest(string Body);
public sealed record MessageDto(
    Guid Id,
    string SenderId,
    string RecipientId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record ConversationDto(
    FriendDto Friend,
    MessageDto? LastMessage,
    int UnreadCount);

public sealed record PresenceRequest(string Presence, string? CurrentGameId);
public sealed record GameSessionRequest(string GameId);

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Text,
    string ActorUserId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateInviteRequest(int MaxUses = 1, int? ExpiresInHours = null);
public sealed record InviteDto(string Code, int MaxUses, int Uses, DateTimeOffset CreatedAtUtc, DateTimeOffset? ExpiresAtUtc);

public sealed record ShowcaseDto(Guid Id, string Title, string Body, int SortOrder);
public sealed record AchievementDto(string Id, string Name, string Description, bool Unlocked);
public sealed record ProfileCommentDto(Guid Id, FriendDto Author, string Body, DateTimeOffset CreatedAtUtc);
public sealed record CreateProfileCommentRequest(string Body);
public sealed record SaveShowcasesRequest(IReadOnlyList<SaveShowcaseItem> Items);
public sealed record SaveShowcaseItem(string Title, string Body, int SortOrder);

public sealed record ActivityItemDto(string Type, string Text, DateTimeOffset AtUtc, string? GameId = null);
