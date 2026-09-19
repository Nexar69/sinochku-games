using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SinochkuGames.Models;

namespace SinochkuGames.Services;

public sealed class SocialApiClient : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private string _baseUrl;

    public SocialApiClient(string baseUrl)
    {
        _baseUrl = Normalize(baseUrl);
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public string BaseUrl => _baseUrl;
    public string? AccessToken { get; private set; }

    public void SetBaseUrl(string url) => _baseUrl = Normalize(url);

    public void SetToken(string? token)
    {
        AccessToken = token;
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"{_baseUrl}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SocialAuthResponse> LoginAsync(
        string usernameOrEmail,
        string password,
        CancellationToken ct = default)
    {
        return await PostAsync<SocialAuthResponse>("/api/auth/login",
            new { usernameOrEmail, password }, ct);
    }

    public async Task<SocialAuthResponse> RegisterAsync(
        string username,
        string email,
        string password,
        string inviteCode,
        string displayName,
        CancellationToken ct = default)
    {
        return await PostAsync<SocialAuthResponse>("/api/auth/register",
            new { username, email, password, inviteCode, displayName }, ct);
    }

    public Task<SocialProfile> MeAsync(CancellationToken ct = default) =>
        GetAsync<SocialProfile>("/api/me", ct);

    public Task ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken ct = default) =>
        PostNoContentAsync("/api/me/change-password",
            new { currentPassword, newPassword }, ct);

    public async Task<SocialProfile> UpdateProfileAsync(
        string displayName,
        string bio,
        string statusText,
        string accentHex,
        CancellationToken ct = default)
    {
        return await PutAsync<SocialProfile>("/api/me/profile",
            new { displayName, bio, statusText, accentHex }, ct);
    }

    public Task<SocialProfile> ProfileAsync(string username, CancellationToken ct = default) =>
        GetAsync<SocialProfile>($"/api/users/{Uri.EscapeDataString(username)}", ct);

    public Task<SocialPrivacy> PrivacyAsync(CancellationToken ct = default) =>
        GetAsync<SocialPrivacy>("/api/me/privacy", ct);

    public Task<SocialPrivacy> UpdatePrivacyAsync(
        string profileVisibility,
        bool allowFriendRequests,
        bool allowMessagesFromFriends,
        bool showGameActivity,
        CancellationToken ct = default) =>
        PutAsync<SocialPrivacy>("/api/me/privacy",
            new { profileVisibility, allowFriendRequests, allowMessagesFromFriends, showGameActivity }, ct);

    public Task<IReadOnlyList<SocialActivity>> ActivityAsync(string username, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialActivity>>(
            $"/api/users/{Uri.EscapeDataString(username)}/activity", ct);

    public Task<IReadOnlyList<SocialProfileComment>> ProfileCommentsAsync(
        string username,
        CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialProfileComment>>(
            $"/api/users/{Uri.EscapeDataString(username)}/comments", ct);

    public Task<SocialProfileComment> CreateProfileCommentAsync(
        string username,
        string body,
        CancellationToken ct = default) =>
        PostAsync<SocialProfileComment>(
            $"/api/users/{Uri.EscapeDataString(username)}/comments",
            new { body }, ct);

    public Task DeleteProfileCommentAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync($"/api/profile-comments/{id}", ct);

    public Task<IReadOnlyList<SocialFriend>> SearchUsersAsync(string query, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialFriend>>(
            $"/api/users/search?q={Uri.EscapeDataString(query)}", ct);

    public Task<IReadOnlyList<SocialFriend>> FriendsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialFriend>>("/api/friends", ct);

    public Task<IReadOnlyList<SocialFriendRequest>> FriendRequestsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialFriendRequest>>("/api/friends/requests", ct);

    public Task<SocialFriendRequest> SendFriendRequestAsync(string username, CancellationToken ct = default) =>
        PostAsync<SocialFriendRequest>("/api/friends/requests", new { username }, ct);

    public Task AcceptFriendRequestAsync(Guid id, CancellationToken ct = default) =>
        PostNoContentAsync($"/api/friends/requests/{id}/accept", new { }, ct);

    public Task RejectFriendRequestAsync(Guid id, CancellationToken ct = default) =>
        PostNoContentAsync($"/api/friends/requests/{id}/reject", new { }, ct);

    public Task RemoveFriendAsync(string username, CancellationToken ct = default) =>
        DeleteAsync($"/api/friends/{Uri.EscapeDataString(username)}", ct);

    public Task<IReadOnlyList<SocialConversation>> ConversationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialConversation>>("/api/messages/conversations", ct);

    public Task<IReadOnlyList<SocialMessage>> MessagesAsync(string username, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialMessage>>(
            $"/api/messages/{Uri.EscapeDataString(username)}", ct);

    public Task<SocialMessage> SendMessageAsync(string username, string body, CancellationToken ct = default) =>
        PostAsync<SocialMessage>(
            $"/api/messages/{Uri.EscapeDataString(username)}",
            new { body }, ct);

    public Task MarkConversationReadAsync(string username, CancellationToken ct = default) =>
        PostNoContentAsync(
            $"/api/messages/{Uri.EscapeDataString(username)}/read",
            new { }, ct);

    public Task<IReadOnlyList<SocialNotification>> NotificationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialNotification>>("/api/notifications", ct);

    public Task MarkNotificationReadAsync(Guid id, CancellationToken ct = default) =>
        PostNoContentAsync($"/api/notifications/{id}/read", new { }, ct);

    public Task MarkAllNotificationsReadAsync(CancellationToken ct = default) =>
        PostNoContentAsync("/api/notifications/read-all", new { }, ct);

    public Task UpdatePresenceAsync(string presence, string? currentGameId = null, CancellationToken ct = default) =>
        PostNoContentAsync("/api/presence", new { presence, currentGameId }, ct);

    public Task StartSessionAsync(string gameId, CancellationToken ct = default) =>
        PostNoContentAsync("/api/sessions/start", new { gameId }, ct);

    public Task EndSessionAsync(CancellationToken ct = default) =>
        PostNoContentAsync("/api/sessions/end", new { }, ct);

    public Task<SocialInvite> CreateInviteAsync(int maxUses, int? expiresInHours, CancellationToken ct = default) =>
        PostAsync<SocialInvite>("/api/invites", new { maxUses, expiresInHours }, ct);

    public Task<IReadOnlyList<SocialInvite>> InvitesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialInvite>>("/api/invites", ct);

    public Task<IReadOnlyList<SocialShowcase>> ShowcasesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialShowcase>>("/api/me/showcases", ct);

    public Task<IReadOnlyList<SocialShowcase>> SaveShowcasesAsync(
        IReadOnlyList<(string Title, string Body, int SortOrder)> items,
        CancellationToken ct = default) =>
        PutAsync<IReadOnlyList<SocialShowcase>>("/api/me/showcases",
            new { items = items.Select(x => new { title = x.Title, body = x.Body, sortOrder = x.SortOrder }) }, ct);

    public async Task<string> UploadImageAsync(string endpoint, string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var form = new MultipartFormDataContent();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(ContentType(filePath));
        form.Add(file, "image", Path.GetFileName(filePath));

        using var response = await _http.PostAsync($"{_baseUrl}{endpoint}", form, ct);
        await EnsureSuccessAsync(response, ct);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var property = endpoint.Contains("avatar", StringComparison.OrdinalIgnoreCase)
            ? "avatarUrl"
            : "backgroundUrl";
        return json.RootElement.GetProperty(property).GetString() ?? "";
    }

    public Uri MediaUri(string? relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            return new Uri($"{_baseUrl}/");

        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absolute))
            return absolute;

        return new Uri($"{_baseUrl}/{relativeOrAbsolute.TrimStart('/')}");
    }

    public async Task<Image?> DownloadImageAsync(string? relativeOrAbsolute, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            return null;

        try
        {
            var bytes = await _http.GetByteArrayAsync(MediaUri(relativeOrAbsolute), ct);
            using var stream = new MemoryStream(bytes);
            using var temp = Image.FromStream(stream);
            return new Bitmap(temp);
        }
        catch
        {
            return null;
        }
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"{_baseUrl}{path}", ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(_json, ct))
               ?? throw new InvalidOperationException("Server returned an empty response.");
    }

    private async Task<T> PostAsync<T>(string path, object value, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync($"{_baseUrl}{path}", value, _json, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(_json, ct))
               ?? throw new InvalidOperationException("Server returned an empty response.");
    }

    private async Task<T> PutAsync<T>(string path, object value, CancellationToken ct)
    {
        using var response = await _http.PutAsJsonAsync($"{_baseUrl}{path}", value, _json, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(_json, ct))
               ?? throw new InvalidOperationException("Server returned an empty response.");
    }

    private async Task PostNoContentAsync(string path, object value, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync($"{_baseUrl}{path}", value, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task DeleteAsync(string path, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync($"{_baseUrl}{path}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Your СИНОЧКУ session expired. Please sign in again.");

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(body)
                ? $"Server returned {(int)response.StatusCode} {response.ReasonPhrase}."
                : body);
    }

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "http://localhost:5187"
            : value.Trim().TrimEnd('/');

    private static string ContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

    public void Dispose() => _http.Dispose();
}
