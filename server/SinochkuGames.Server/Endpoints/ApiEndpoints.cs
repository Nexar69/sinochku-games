using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SinochkuGames.Server.Contracts;
using SinochkuGames.Server.Data;
using SinochkuGames.Server.Services;

namespace SinochkuGames.Server.Endpoints;

public static class ApiEndpoints
{
    private static readonly Regex UsernamePattern = new(
        "^[A-Za-z0-9_.-]{3,24}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AccentPattern = new(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ValidPresence = new(StringComparer.OrdinalIgnoreCase)
    {
        "Online", "Away", "Busy", "Invisible", "Playing", "Offline"
    };

    public static void MapSinochkuApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/auth/register", RegisterAsync)
            .RequireRateLimiting("auth");

        api.MapPost("/auth/login", LoginAsync)
            .RequireRateLimiting("auth");

        var auth = api.MapGroup("")
            .RequireAuthorization();

        auth.MapGet("/me", MeAsync);
        auth.MapPut("/me/profile", UpdateProfileAsync);
        auth.MapPost("/me/change-password", ChangePasswordAsync);
        auth.MapPost("/me/avatar", UploadAvatarAsync);
        auth.MapPost("/me/background", UploadBackgroundAsync);
        auth.MapGet("/me/privacy", PrivacyAsync);
        auth.MapPut("/me/privacy", UpdatePrivacyAsync);
        auth.MapGet("/me/showcases", MyShowcasesAsync);
        auth.MapPut("/me/showcases", SaveShowcasesAsync);

        auth.MapGet("/users/search", SearchUsersAsync);
        auth.MapGet("/users/{username}", UserProfileAsync);
        auth.MapGet("/users/{username}/activity", UserActivityAsync);
        auth.MapGet("/users/{username}/comments", ProfileCommentsAsync);
        auth.MapPost("/users/{username}/comments", CreateProfileCommentAsync);
        auth.MapDelete("/profile-comments/{id:guid}", DeleteProfileCommentAsync);

        auth.MapGet("/friends", FriendsAsync);
        auth.MapGet("/friends/requests", FriendRequestsAsync);
        auth.MapPost("/friends/requests", SendFriendRequestAsync);
        auth.MapPost("/friends/requests/{id:guid}/accept", AcceptFriendRequestAsync);
        auth.MapPost("/friends/requests/{id:guid}/reject", RejectFriendRequestAsync);
        auth.MapDelete("/friends/{username}", RemoveFriendAsync);

        auth.MapGet("/messages/conversations", ConversationsAsync);
        auth.MapGet("/messages/{username}", ConversationAsync);
        auth.MapPost("/messages/{username}", SendMessageAsync)
            .RequireRateLimiting("messages");
        auth.MapPost("/messages/{username}/read", MarkConversationReadAsync);

        auth.MapGet("/notifications", NotificationsAsync);
        auth.MapPost("/notifications/{id:guid}/read", MarkNotificationReadAsync);
        auth.MapPost("/notifications/read-all", MarkAllNotificationsReadAsync);

        auth.MapPost("/presence", UpdatePresenceAsync);
        auth.MapPost("/sessions/start", StartSessionAsync);
        auth.MapPost("/sessions/end", EndSessionAsync);

        auth.MapPost("/invites", CreateInviteAsync);
        auth.MapGet("/invites", ListInvitesAsync);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<AppUser> users,
        SocialDbContext db,
        JwtTokenService tokens,
        CancellationToken ct)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var inviteText = request.InviteCode.Trim();

        if (!UsernamePattern.IsMatch(username))
            return Results.BadRequest(new { error = "Username must be 3–24 characters using letters, numbers, ., _, or -." });

        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            return Results.BadRequest(new { error = "A valid email address is required." });

        var invite = await db.InviteCodes.SingleOrDefaultAsync(x => x.Code == inviteText, ct);
        if (invite is null
            || invite.Uses >= invite.MaxUses
            || invite.ExpiresAtUtc is { } expires && expires <= DateTimeOffset.UtcNow)
            return Results.BadRequest(new { error = "That invite code is invalid or has expired." });

        var isFounder = !await db.Users.AnyAsync(ct);

        var user = new AppUser
        {
            UserName = username,
            Email = email,
            DisplayName = Clean(request.DisplayName, 40, username),
            Presence = "Offline",
            IsFounder = isFounder
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return Results.BadRequest(new { errors = created.Errors.Select(e => e.Description).ToArray() });

        invite.Uses++;
        await db.SaveChangesAsync(ct);

        var profile = await BuildProfileAsync(user, db, ct);
        return Results.Ok(new AuthResponse(tokens.Create(user), profile));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<AppUser> users,
        SocialDbContext db,
        JwtTokenService tokens,
        CancellationToken ct)
    {
        var key = request.UsernameOrEmail.Trim();
        var user = key.Contains('@')
            ? await users.FindByEmailAsync(key)
            : await users.FindByNameAsync(key);

        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Results.Unauthorized();

        user.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await users.UpdateAsync(user);

        return Results.Ok(new AuthResponse(
            tokens.Create(user),
            await BuildProfileAsync(user, db, ct)));
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(await BuildProfileAsync(user, db, ct));
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        UserManager<AppUser> users,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        if (user is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return Results.BadRequest(new { error = "New password must be at least 8 characters." });

        var result = await users.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors.Select(x => x.Description).ToArray() });

        return Results.Ok();
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        if (user is null) return Results.Unauthorized();

        if (!AccentPattern.IsMatch(request.AccentHex))
            return Results.BadRequest(new { error = "Accent must be a hex color such as #66C0F4." });

        user.DisplayName = Clean(request.DisplayName, 40, user.UserName ?? "Player");
        user.Bio = Clean(request.Bio, 500);
        user.StatusText = Clean(request.StatusText, 120);
        user.AccentHex = request.AccentHex;

        await db.SaveChangesAsync(ct);
        return Results.Ok(await BuildProfileAsync(user, db, ct));
    }

    private static async Task<IResult> UploadAvatarAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        MediaStorageService media,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        if (user is null) return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data expected." });

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("image");
        if (file is null) return Results.BadRequest(new { error = "Upload field must be named image." });

        try
        {
            user.AvatarUrl = await media.SaveAsync(file, "avatars", 5 * 1024 * 1024, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.AvatarUrl });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UploadBackgroundAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        MediaStorageService media,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        if (user is null) return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data expected." });

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("image");
        if (file is null) return Results.BadRequest(new { error = "Upload field must be named image." });

        try
        {
            user.BackgroundUrl = await media.SaveAsync(file, "backgrounds", 10 * 1024 * 1024, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.BackgroundUrl });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> PrivacyAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        if (user is null) return Results.Unauthorized();

        return Results.Ok(new PrivacySettingsDto(
            user.ProfileVisibility,
            user.AllowFriendRequests,
            user.AllowMessagesFromFriends,
            user.ShowGameActivity));
    }

    private static async Task<IResult> UpdatePrivacyAsync(
        UpdatePrivacySettingsRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var user = await CurrentUserAsync(principal, db, ct);
        if (user is null) return Results.Unauthorized();

        var visibility = request.ProfileVisibility.Trim();
        if (visibility is not ("Public" or "Friends" or "Private"))
            return Results.BadRequest(new { error = "Profile visibility must be Public, Friends, or Private." });

        user.ProfileVisibility = visibility;
        user.AllowFriendRequests = request.AllowFriendRequests;
        user.AllowMessagesFromFriends = request.AllowMessagesFromFriends;
        user.ShowGameActivity = request.ShowGameActivity;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new PrivacySettingsDto(
            user.ProfileVisibility,
            user.AllowFriendRequests,
            user.AllowMessagesFromFriends,
            user.ShowGameActivity));
    }

    private static async Task<IResult> SearchUsersAsync(
        string? q,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        q = (q ?? "").Trim();
        if (q.Length < 2) return Results.Ok(Array.Empty<FriendDto>());

        var matches = await db.Users
            .AsNoTracking()
            .Where(u => u.Id != me
                        && (EF.Functions.Like(u.UserName!, $"%{q}%")
                            || EF.Functions.Like(u.DisplayName, $"%{q}%")))
            .OrderBy(u => u.UserName)
            .Take(20)
            .ToListAsync(ct);

        return Results.Ok(matches.Select(SocialQueryService.ToFriend));
    }

    private static async Task<IResult> UserProfileAsync(
        string username,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var user = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);

        if (user is null) return Results.NotFound();

        if (user.Id != me)
        {
            var friends = await social.AreFriendsAsync(me, user.Id, ct);
            if (user.ProfileVisibility == "Private"
                || user.ProfileVisibility == "Friends" && !friends)
                return Results.Forbid();
        }

        return Results.Ok(await BuildProfileAsync(user, db, ct));
    }

    private static async Task<IResult> UserActivityAsync(
        string username,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var user = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (user is null) return Results.NotFound();

        if (user.Id != me
            && (!user.ShowGameActivity || !await social.AreFriendsAsync(me, user.Id, ct)))
            return Results.Forbid();

        var sessions = await db.GameSessions
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(30)
            .ToListAsync(ct);

        var items = sessions.Select(x =>
        {
            var seconds = (long)((x.EndedAtUtc ?? DateTimeOffset.UtcNow) - x.StartedAtUtc).TotalSeconds;
            return new ActivityItemDto(
                "game",
                $"Played {x.GameId} for {FormatDuration(seconds)}",
                x.StartedAtUtc,
                x.GameId);
        }).ToArray();

        return Results.Ok(items);
    }

    private static async Task<IResult> ProfileCommentsAsync(
        string username,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var profile = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (profile is null) return Results.NotFound();

        if (profile.Id != me)
        {
            var friends = await social.AreFriendsAsync(me, profile.Id, ct);
            if (profile.ProfileVisibility == "Private"
                || profile.ProfileVisibility == "Friends" && !friends)
                return Results.Forbid();
        }

        var comments = await db.ProfileComments.AsNoTracking()
            .Where(x => x.ProfileUserId == profile.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        var authorIds = comments.Select(x => x.AuthorUserId).Distinct().ToArray();
        var authors = await db.Users.AsNoTracking()
            .Where(x => authorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        return Results.Ok(comments
            .Where(x => authors.ContainsKey(x.AuthorUserId))
            .Select(x => new ProfileCommentDto(
                x.Id,
                SocialQueryService.ToFriend(authors[x.AuthorUserId]),
                x.Body,
                x.CreatedAtUtc)));
    }

    private static async Task<IResult> CreateProfileCommentAsync(
        string username,
        CreateProfileCommentRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var profile = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (profile is null) return Results.NotFound();

        if (profile.Id != me && !await social.AreFriendsAsync(me, profile.Id, ct))
            return Results.Forbid();

        var body = Clean(request.Body, 500);
        if (string.IsNullOrWhiteSpace(body))
            return Results.BadRequest(new { error = "Comment cannot be empty." });

        var author = await db.Users.FindAsync(new object[] { me }, ct);
        if (author is null) return Results.Unauthorized();

        var comment = new ProfileComment
        {
            ProfileUserId = profile.Id,
            AuthorUserId = me,
            Body = body
        };
        db.ProfileComments.Add(comment);

        if (profile.Id != me)
        {
            var notification = new NotificationEntity
            {
                UserId = profile.Id,
                Type = "profile_comment",
                ActorUserId = me,
                Text = $"{author.DisplayName} commented on your profile."
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);
            await realtime.NotificationAsync(profile.Id, ToNotificationDto(notification));
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new ProfileCommentDto(
            comment.Id,
            SocialQueryService.ToFriend(author),
            comment.Body,
            comment.CreatedAtUtc));
    }

    private static async Task<IResult> DeleteProfileCommentAsync(
        Guid id,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var comment = await db.ProfileComments.FindAsync(new object[] { id }, ct);
        if (comment is null) return Results.NotFound();

        if (comment.AuthorUserId != me && comment.ProfileUserId != me)
            return Results.Forbid();

        db.ProfileComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> FriendsAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var ids = await social.FriendIdsAsync(me, ct);
        var users = await db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .OrderByDescending(u => u.Presence == "Playing")
            .ThenByDescending(u => u.Presence == "Online")
            .ThenBy(u => u.DisplayName)
            .ToListAsync(ct);

        return Results.Ok(users.Select(SocialQueryService.ToFriend));
    }

    private static async Task<IResult> FriendRequestsAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var requests = await db.FriendRequests
            .AsNoTracking()
            .Where(x => x.Status == "Pending" && (x.SenderId == me || x.RecipientId == me))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        var ids = requests.SelectMany(x => new[] { x.SenderId, x.RecipientId }).Distinct().ToArray();
        var users = await db.Users
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        return Results.Ok(requests.Select(x => ToRequestDto(x, users)));
    }

    private static async Task<IResult> SendFriendRequestAsync(
        SendFriendRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var meId = UserId(principal);
        if (meId is null) return Results.Unauthorized();

        var target = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == request.Username.Trim().ToUpper(),
            ct);

        if (target is null) return Results.NotFound(new { error = "User not found." });
        if (target.Id == meId) return Results.BadRequest(new { error = "You cannot add yourself." });
        if (!target.AllowFriendRequests)
            return Results.BadRequest(new { error = "This user is not accepting friend requests." });
        if (await social.AreFriendsAsync(meId, target.Id, ct))
            return Results.BadRequest(new { error = "You are already friends." });

        var pending = await db.FriendRequests.AnyAsync(x =>
            x.Status == "Pending"
            && ((x.SenderId == meId && x.RecipientId == target.Id)
                || (x.SenderId == target.Id && x.RecipientId == meId)), ct);

        if (pending)
            return Results.BadRequest(new { error = "A friend request already exists." });

        var me = await db.Users.FindAsync(new object[] { meId }, ct);
        if (me is null) return Results.Unauthorized();

        var entity = new FriendRequest
        {
            SenderId = meId,
            RecipientId = target.Id
        };
        db.FriendRequests.Add(entity);

        var notification = new NotificationEntity
        {
            UserId = target.Id,
            Type = "friend_request",
            ActorUserId = meId,
            Text = $"{me.DisplayName} sent you a friend request."
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        var dto = new FriendRequestDto(
            entity.Id,
            SocialQueryService.ToFriend(me),
            SocialQueryService.ToFriend(target),
            entity.Status,
            entity.CreatedAtUtc);

        await realtime.FriendRequestAsync(target.Id, dto);
        await realtime.NotificationAsync(target.Id, ToNotificationDto(notification));

        return Results.Ok(dto);
    }

    private static async Task<IResult> AcceptFriendRequestAsync(
        Guid id,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var request = await db.FriendRequests.FindAsync(new object[] { id }, ct);
        if (request is null || request.RecipientId != me || request.Status != "Pending")
            return Results.NotFound();

        request.Status = "Accepted";
        request.RespondedAtUtc = DateTimeOffset.UtcNow;

        var (a, b) = SocialQueryService.Normalize(request.SenderId, request.RecipientId);
        if (!await db.Friendships.AnyAsync(x => x.UserAId == a && x.UserBId == b, ct))
            db.Friendships.Add(new Friendship { UserAId = a, UserBId = b });

        var meUser = await db.Users.FindAsync(new object[] { me }, ct);
        if (meUser is not null)
        {
            var notification = new NotificationEntity
            {
                UserId = request.SenderId,
                Type = "friend_accepted",
                ActorUserId = me,
                Text = $"{meUser.DisplayName} accepted your friend request."
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);
            await realtime.NotificationAsync(request.SenderId, ToNotificationDto(notification));
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        await realtime.FriendsChangedAsync(request.SenderId);
        await realtime.FriendsChangedAsync(request.RecipientId);
        return Results.Ok();
    }

    private static async Task<IResult> RejectFriendRequestAsync(
        Guid id,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var request = await db.FriendRequests.FindAsync(new object[] { id }, ct);
        if (request is null || request.RecipientId != me || request.Status != "Pending")
            return Results.NotFound();

        request.Status = "Rejected";
        request.RespondedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> RemoveFriendAsync(
        string username,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var target = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (target is null) return Results.NotFound();

        var (a, b) = SocialQueryService.Normalize(me, target.Id);
        var friendship = await db.Friendships.SingleOrDefaultAsync(
            x => x.UserAId == a && x.UserBId == b,
            ct);
        if (friendship is null) return Results.NotFound();

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync(ct);
        await realtime.FriendsChangedAsync(me);
        await realtime.FriendsChangedAsync(target.Id);
        return Results.NoContent();
    }

    private static async Task<IResult> ConversationsAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var friendIds = await social.FriendIdsAsync(me, ct);
        var friends = await db.Users.AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var result = new List<ConversationDto>();
        foreach (var friendId in friendIds)
        {
            if (!friends.TryGetValue(friendId, out var friend)) continue;

            var last = await db.DirectMessages.AsNoTracking()
                .Where(m => (m.SenderId == me && m.RecipientId == friendId)
                            || (m.SenderId == friendId && m.RecipientId == me))
                .OrderByDescending(m => m.SentAtUtc)
                .FirstOrDefaultAsync(ct);

            var unread = await db.DirectMessages.AsNoTracking()
                .CountAsync(m => m.SenderId == friendId
                                 && m.RecipientId == me
                                 && m.ReadAtUtc == null, ct);

            result.Add(new ConversationDto(
                SocialQueryService.ToFriend(friend),
                last is null ? null : ToMessageDto(last),
                unread));
        }

        return Results.Ok(result
            .OrderByDescending(x => x.LastMessage?.SentAtUtc ?? DateTimeOffset.MinValue));
    }

    private static async Task<IResult> ConversationAsync(
        string username,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var friend = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (friend is null) return Results.NotFound();
        if (!await social.AreFriendsAsync(me, friend.Id, ct))
            return Results.Forbid();

        var messages = await db.DirectMessages.AsNoTracking()
            .Where(m => (m.SenderId == me && m.RecipientId == friend.Id)
                        || (m.SenderId == friend.Id && m.RecipientId == me))
            .OrderByDescending(m => m.SentAtUtc)
            .Take(100)
            .ToListAsync(ct);

        messages.Reverse();
        return Results.Ok(messages.Select(ToMessageDto));
    }

    private static async Task<IResult> SendMessageAsync(
        string username,
        SendMessageRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var body = request.Body.Trim();
        if (body.Length is < 1 or > 4000)
            return Results.BadRequest(new { error = "Messages must be 1–4000 characters." });

        var friend = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (friend is null) return Results.NotFound();
        if (!await social.AreFriendsAsync(me, friend.Id, ct))
            return Results.Forbid();
        if (!friend.AllowMessagesFromFriends)
            return Results.BadRequest(new { error = "This friend is not accepting messages." });

        var entity = new DirectMessage
        {
            SenderId = me,
            RecipientId = friend.Id,
            Body = body
        };
        db.DirectMessages.Add(entity);
        await db.SaveChangesAsync(ct);

        var dto = ToMessageDto(entity);
        await realtime.MessageAsync(friend.Id, dto);
        return Results.Ok(dto);
    }

    private static async Task<IResult> MarkConversationReadAsync(
        string username,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var friend = await db.Users.SingleOrDefaultAsync(
            u => u.NormalizedUserName == username.ToUpper(),
            ct);
        if (friend is null) return Results.NotFound();

        var unread = await db.DirectMessages
            .Where(m => m.SenderId == friend.Id && m.RecipientId == me && m.ReadAtUtc == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var message in unread)
            message.ReadAtUtc = now;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { read = unread.Count });
    }

    private static async Task<IResult> NotificationsAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var notifications = await db.Notifications.AsNoTracking()
            .Where(x => x.UserId == me)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        return Results.Ok(notifications.Select(ToNotificationDto));
    }

    private static async Task<IResult> MarkNotificationReadAsync(
        Guid id,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var entity = await db.Notifications.FindAsync(new object[] { id }, ct);
        if (entity is null || entity.UserId != me) return Results.NotFound();

        entity.IsRead = true;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> MarkAllNotificationsReadAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var unread = await db.Notifications
            .Where(x => x.UserId == me && !x.IsRead)
            .ToListAsync(ct);

        foreach (var entity in unread)
            entity.IsRead = true;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { read = unread.Count });
    }

    private static async Task<IResult> UpdatePresenceAsync(
        PresenceRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        if (!ValidPresence.Contains(request.Presence))
            return Results.BadRequest(new { error = "Invalid presence." });

        var user = await db.Users.FindAsync(new object[] { me }, ct);
        if (user is null) return Results.Unauthorized();

        user.Presence = request.Presence;
        user.CurrentGameId = Clean(request.CurrentGameId, 80);
        user.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var publicDto = SocialQueryService.ToFriend(user);
        if (request.Presence.Equals("Invisible", StringComparison.OrdinalIgnoreCase))
            publicDto = publicDto with { Presence = "Offline", CurrentGameId = "" };

        foreach (var friendId in await social.FriendIdsAsync(me, ct))
            await realtime.PresenceAsync(friendId, publicDto);

        return Results.Ok(SocialQueryService.ToFriend(user));
    }

    private static async Task<IResult> StartSessionAsync(
        GameSessionRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var gameId = Clean(request.GameId, 80);
        if (string.IsNullOrWhiteSpace(gameId))
            return Results.BadRequest(new { error = "Game id is required." });

        var open = await db.GameSessions
            .Where(x => x.UserId == me && x.EndedAtUtc == null)
            .ToListAsync(ct);
        foreach (var session in open)
            session.EndedAtUtc = DateTimeOffset.UtcNow;

        db.GameSessions.Add(new GameSession { UserId = me, GameId = gameId });

        var user = await db.Users.FindAsync(new object[] { me }, ct);
        if (user is null) return Results.Unauthorized();

        user.Presence = "Playing";
        user.CurrentGameId = gameId;
        await db.SaveChangesAsync(ct);

        var dto = SocialQueryService.ToFriend(user);
        var friendIds = await social.FriendIdsAsync(me, ct);
        foreach (var friendId in friendIds)
        {
            await realtime.PresenceAsync(friendId, dto);

            var notification = new NotificationEntity
            {
                UserId = friendId,
                Type = "game_started",
                ActorUserId = me,
                Text = $"{user.DisplayName} is now playing {gameId}."
            };
            db.Notifications.Add(notification);
        }

        await db.SaveChangesAsync(ct);

        foreach (var notification in await db.Notifications.AsNoTracking()
                     .Where(x => friendIds.Contains(x.UserId)
                                 && x.ActorUserId == me
                                 && x.Type == "game_started")
                     .OrderByDescending(x => x.CreatedAtUtc)
                     .Take(friendIds.Count)
                     .ToListAsync(ct))
        {
            await realtime.NotificationAsync(notification.UserId, ToNotificationDto(notification));
        }

        return Results.Ok();
    }

    private static async Task<IResult> EndSessionAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        SocialQueryService social,
        RealtimeNotifier realtime,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var sessions = await db.GameSessions
            .Where(x => x.UserId == me && x.EndedAtUtc == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.EndedAtUtc = DateTimeOffset.UtcNow;

        var user = await db.Users.FindAsync(new object[] { me }, ct);
        if (user is null) return Results.Unauthorized();

        user.Presence = "Online";
        user.CurrentGameId = "";
        user.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = SocialQueryService.ToFriend(user);
        foreach (var friendId in await social.FriendIdsAsync(me, ct))
            await realtime.PresenceAsync(friendId, dto);

        return Results.Ok();
    }

    private static async Task<IResult> CreateInviteAsync(
        CreateInviteRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = await CurrentUserAsync(principal, db, ct);
        if (me is null) return Results.Unauthorized();
        if (!me.IsFounder) return Results.Forbid();

        var maxUses = Math.Clamp(request.MaxUses, 1, 50);
        var code = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..12].ToLowerInvariant();

        var invite = new InviteCode
        {
            Code = code,
            CreatedByUserId = me.Id,
            MaxUses = maxUses,
            ExpiresAtUtc = request.ExpiresInHours is > 0
                ? DateTimeOffset.UtcNow.AddHours(Math.Clamp(request.ExpiresInHours.Value, 1, 24 * 90))
                : null
        };

        db.InviteCodes.Add(invite);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new InviteDto(
            invite.Code,
            invite.MaxUses,
            invite.Uses,
            invite.CreatedAtUtc,
            invite.ExpiresAtUtc));
    }

    private static async Task<IResult> ListInvitesAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = await CurrentUserAsync(principal, db, ct);
        if (me is null) return Results.Unauthorized();
        if (!me.IsFounder) return Results.Forbid();

        var invites = await db.InviteCodes.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        return Results.Ok(invites.Select(x => new InviteDto(
            x.Code, x.MaxUses, x.Uses, x.CreatedAtUtc, x.ExpiresAtUtc)));
    }

    private static async Task<IResult> MyShowcasesAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        var items = await db.ProfileShowcases.AsNoTracking()
            .Where(x => x.UserId == me)
            .OrderBy(x => x.SortOrder)
            .Take(3)
            .Select(x => new ShowcaseDto(x.Id, x.Title, x.Body, x.SortOrder))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> SaveShowcasesAsync(
        SaveShowcasesRequest request,
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var me = UserId(principal);
        if (me is null) return Results.Unauthorized();

        if (request.Items.Count > 3)
            return Results.BadRequest(new { error = "Up to 3 showcase slots are supported." });

        var existing = await db.ProfileShowcases.Where(x => x.UserId == me).ToListAsync(ct);
        db.ProfileShowcases.RemoveRange(existing);

        foreach (var item in request.Items.OrderBy(x => x.SortOrder).Take(3))
        {
            db.ProfileShowcases.Add(new ProfileShowcase
            {
                UserId = me,
                Title = Clean(item.Title, 50),
                Body = Clean(item.Body, 300),
                SortOrder = Math.Clamp(item.SortOrder, 0, 2)
            });
        }

        await db.SaveChangesAsync(ct);
        return await MyShowcasesAsync(principal, db, ct);
    }

    private static async Task<AppUser?> CurrentUserAsync(
        ClaimsPrincipal principal,
        SocialDbContext db,
        CancellationToken ct)
    {
        var id = UserId(principal);
        return id is null ? null : await db.Users.FindAsync(new object[] { id }, ct);
    }

    private static string? UserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static string Clean(string? value, int max, string fallback = "")
    {
        var text = (value ?? "").Trim();
        if (text.Length > max) text = text[..max];
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static async Task<UserProfileDto> BuildProfileAsync(
        AppUser user,
        SocialDbContext db,
        CancellationToken ct)
    {
        var sessions = await db.GameSessions.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .ToListAsync(ct);

        long totalSeconds = 0;
        foreach (var session in sessions)
        {
            var end = session.EndedAtUtc ?? DateTimeOffset.UtcNow;
            totalSeconds += Math.Max(0, (long)(end - session.StartedAtUtc).TotalSeconds);
        }

        var showcases = await db.ProfileShowcases.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderBy(x => x.SortOrder)
            .Take(3)
            .Select(x => new ShowcaseDto(x.Id, x.Title, x.Body, x.SortOrder))
            .ToListAsync(ct);

        var badges = new List<string> { "СИНОЧКУ Citizen" };
        if (user.IsFounder) badges.Insert(0, "СИНОЧКУ GAMES Founder");

        var hasFriend = await db.Friendships.AsNoTracking()
            .AnyAsync(x => x.UserAId == user.Id || x.UserBId == user.Id, ct);
        var hasMessage = await db.DirectMessages.AsNoTracking()
            .AnyAsync(x => x.SenderId == user.Id, ct);
        var hasGame = sessions.Count > 0;

        var achievements = new[]
        {
            new AchievementDto("welcome", "Welcome Aboard", "Created a СИНОЧКУ account.", true),
            new AchievementDto("first_friend", "Not Alone", "Made your first friend.", hasFriend),
            new AchievementDto("first_message", "Say Something", "Sent your first message.", hasMessage),
            new AchievementDto("first_game", "Press PLAY", "Started a game through the social launcher.", hasGame),
            new AchievementDto("founder", "Founder", "Created the first СИНОЧКУ GAMES account.", user.IsFounder)
        };

        return new UserProfileDto(
            user.Id,
            user.UserName ?? "",
            string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "" : user.DisplayName,
            user.Bio,
            user.StatusText,
            user.AvatarUrl,
            user.BackgroundUrl,
            user.AccentHex,
            user.Presence,
            user.CurrentGameId,
            user.LastSeenAtUtc,
            user.CreatedAtUtc,
            user.IsFounder,
            totalSeconds,
            showcases,
            badges,
            achievements);
    }

    private static FriendRequestDto ToRequestDto(
        FriendRequest request,
        IReadOnlyDictionary<string, AppUser> users) => new(
            request.Id,
            SocialQueryService.ToFriend(users[request.SenderId]),
            SocialQueryService.ToFriend(users[request.RecipientId]),
            request.Status,
            request.CreatedAtUtc);

    private static MessageDto ToMessageDto(DirectMessage message) => new(
        message.Id,
        message.SenderId,
        message.RecipientId,
        message.Body,
        message.SentAtUtc,
        message.ReadAtUtc);

    private static NotificationDto ToNotificationDto(NotificationEntity entity) => new(
        entity.Id,
        entity.Type,
        entity.Text,
        entity.ActorUserId,
        entity.IsRead,
        entity.CreatedAtUtc);

    private static string FormatDuration(long seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes}m"
            : span.TotalMinutes >= 1
                ? $"{span.Minutes}m"
                : $"{span.Seconds}s";
    }
}
