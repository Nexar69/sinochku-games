using SinochkuGames.Models;

namespace SinochkuGames.UI.Pages;

public sealed class FriendsPage : UserControl
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly FlowLayoutPanel _friends = new();
    private readonly FlowLayoutPanel _requests = new();
    private readonly FlowLayoutPanel _searchResults = new();
    private readonly TextBox _search = new();

    public event EventHandler<string>? ProfileRequested;
    public event EventHandler<string>? MessageRequested;
    public event EventHandler? LoginRequested;

    public FriendsPage(LauncherContext context, ThemePalette theme)
    {
        _context = context;
        _theme = theme;
        BackColor = _theme.Window;
        Padding = new Padding(26);

        if (!_context.Social.IsSignedIn)
        {
            BuildSignedOut();
            return;
        }

        var title = _theme.Label("FRIENDS", 22, FontStyle.Bold);
        title.Location = new Point(28, 20);
        Controls.Add(title);

        _search.PlaceholderText = "Find people by username or display name";
        _search.Location = new Point(28, 72);
        _search.Width = 360;
        _search.BackColor = _theme.SurfaceRaised;
        _search.ForeColor = _theme.Text;
        Controls.Add(_search);

        var searchButton = _theme.Button("SEARCH", 100, 30);
        searchButton.Location = new Point(400, 70);
        searchButton.Click += async (_, _) => await SearchAsync();
        Controls.Add(searchButton);

        var refresh = _theme.Button("REFRESH", 100, 30);
        refresh.Location = new Point(510, 70);
        refresh.Click += async (_, _) => await RefreshAsync();
        Controls.Add(refresh);

        BuildSection("FRIEND REQUESTS", _requests, 28, 120, 520, 190);
        BuildSection("YOUR FRIENDS", _friends, 568, 120, 560, 520);
        BuildSection("SEARCH RESULTS", _searchResults, 28, 330, 520, 310);

        Load += async (_, _) => await RefreshAsync();
        _context.Social.FriendsChanged += OnFriendsChanged;
        _context.Social.FriendRequestReceived += OnFriendRequest;
        Disposed += (_, _) =>
        {
            _context.Social.FriendsChanged -= OnFriendsChanged;
            _context.Social.FriendRequestReceived -= OnFriendRequest;
        };
    }

    private void BuildSection(string title, FlowLayoutPanel panel, int x, int y, int width, int height)
    {
        var label = _theme.Label(title, 10, FontStyle.Bold);
        label.Location = new Point(x, y);
        Controls.Add(label);

        panel.Location = new Point(x, y + 30);
        panel.Size = new Size(width, height - 30);
        panel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
        panel.AutoScroll = true;
        panel.FlowDirection = FlowDirection.TopDown;
        panel.WrapContents = false;
        panel.BackColor = _theme.Surface;
        panel.Padding = new Padding(8);
        Controls.Add(panel);
    }

    private async Task RefreshAsync()
    {
        try
        {
            var friends = await _context.Social.Api.FriendsAsync();
            var requests = await _context.Social.Api.FriendRequestsAsync();

            _friends.Controls.Clear();
            foreach (var friend in friends)
                _friends.Controls.Add(FriendRow(friend));

            _requests.Controls.Clear();
            var incoming = requests.Where(r => r.Recipient.Id == _context.Social.CurrentUser?.Id);
            foreach (var request in incoming)
                _requests.Controls.Add(RequestRow(request));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Program.Brand);
        }
    }

    private async Task SearchAsync()
    {
        _searchResults.Controls.Clear();
        if (_search.Text.Trim().Length < 2) return;

        try
        {
            var results = await _context.Social.Api.SearchUsersAsync(_search.Text.Trim());
            foreach (var user in results)
                _searchResults.Controls.Add(SearchRow(user));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Program.Brand);
        }
    }

    private Control FriendRow(SocialFriend friend)
    {
        var row = BaseRow(510, 68);

        var avatar = new PictureBox
        {
            Location = new Point(8, 10),
            Size = new Size(44, 44),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _theme.Surface
        };
        row.Controls.Add(avatar);
        _ = LoadAvatarAsync(avatar, friend.AvatarUrl);

        var dot = _theme.Label("●", 10, FontStyle.Bold, PresenceColor(friend));
        dot.Location = new Point(48, 38);
        row.Controls.Add(dot);

        var name = _theme.Label(friend.DisplayName, 10, FontStyle.Bold);
        name.Location = new Point(64, 10);
        row.Controls.Add(name);

        var stateText = friend.Presence == "Playing" && !string.IsNullOrWhiteSpace(friend.CurrentGameId)
            ? $"Playing {friend.CurrentGameId}"
            : friend.Presence;
        var state = _theme.Label(stateText, 8, FontStyle.Regular, _theme.Muted);
        state.Location = new Point(66, 36);
        row.Controls.Add(state);

        var profile = _theme.Button("PROFILE", 76, 30);
        profile.Location = new Point(332, 18);
        profile.Click += (_, _) => ProfileRequested?.Invoke(this, friend.Username);
        row.Controls.Add(profile);

        var message = _theme.Button("MESSAGE", 82, 30);
        message.Location = new Point(414, 18);
        message.Click += (_, _) => MessageRequested?.Invoke(this, friend.Username);
        row.Controls.Add(message);

        return row;
    }

    private Control RequestRow(SocialFriendRequest request)
    {
        var row = BaseRow(470, 74);

        var avatar = new PictureBox
        {
            Location = new Point(8, 14),
            Size = new Size(42, 42),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _theme.Surface
        };
        row.Controls.Add(avatar);
        _ = LoadAvatarAsync(avatar, request.Sender.AvatarUrl);

        var name = _theme.Label(request.Sender.DisplayName, 10, FontStyle.Bold);
        name.Location = new Point(62, 10);
        row.Controls.Add(name);

        var user = _theme.Label($"@{request.Sender.Username}", 8, FontStyle.Regular, _theme.Muted);
        user.Location = new Point(64, 35);
        row.Controls.Add(user);

        var accept = _theme.Button("ACCEPT", 80, 30);
        accept.Location = new Point(292, 20);
        accept.BackColor = _theme.Play;
        accept.Click += async (_, _) =>
        {
            try
            {
                await _context.Social.Api.AcceptFriendRequestAsync(request.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Program.Brand);
            }
        };
        row.Controls.Add(accept);

        var reject = _theme.Button("DECLINE", 82, 30);
        reject.Location = new Point(378, 20);
        reject.Click += async (_, _) =>
        {
            try
            {
                await _context.Social.Api.RejectFriendRequestAsync(request.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Program.Brand);
            }
        };
        row.Controls.Add(reject);

        return row;
    }

    private Control SearchRow(SocialFriend user)
    {
        var row = BaseRow(470, 66);

        var avatar = new PictureBox
        {
            Location = new Point(8, 11),
            Size = new Size(42, 42),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _theme.Surface
        };
        row.Controls.Add(avatar);
        _ = LoadAvatarAsync(avatar, user.AvatarUrl);

        var name = _theme.Label(user.DisplayName, 10, FontStyle.Bold);
        name.Location = new Point(62, 9);
        row.Controls.Add(name);

        var username = _theme.Label($"@{user.Username}", 8, FontStyle.Regular, _theme.Muted);
        username.Location = new Point(64, 34);
        row.Controls.Add(username);

        var profile = _theme.Button("PROFILE", 80, 30);
        profile.Location = new Point(292, 17);
        profile.Click += (_, _) => ProfileRequested?.Invoke(this, user.Username);
        row.Controls.Add(profile);

        var add = _theme.Button("ADD", 76, 30);
        add.Location = new Point(378, 17);
        add.Click += async (_, _) =>
        {
            try
            {
                await _context.Social.Api.SendFriendRequestAsync(user.Username);
                add.Text = "SENT";
                add.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Program.Brand);
            }
        };
        row.Controls.Add(add);

        return row;
    }

    private async Task LoadAvatarAsync(PictureBox box, string url)
    {
        var image = await _context.Social.Api.DownloadImageAsync(url);
        if (IsDisposed || box.IsDisposed || image is null) return;
        box.Image?.Dispose();
        box.Image = image;
    }

    private Panel BaseRow(int width, int height) => new()
    {
        Width = width,
        Height = height,
        Margin = new Padding(3),
        BackColor = _theme.SurfaceRaised
    };

    private Color PresenceColor(SocialFriend friend) =>
        friend.Presence == "Playing" ? _theme.Play
        : friend.Presence == "Online" ? _theme.Accent
        : friend.Presence == "Away" ? Color.FromArgb(220, 180, 80)
        : _theme.Muted;

    private void BuildSignedOut()
    {
        var title = _theme.Label("FRIENDS", 22, FontStyle.Bold);
        title.Location = new Point(28, 24);
        Controls.Add(title);

        var text = _theme.Label("Sign in to add friends and use chat.", 10, FontStyle.Regular, _theme.Muted);
        text.Location = new Point(30, 76);
        Controls.Add(text);

        var button = _theme.Button("SIGN IN", 120, 40);
        button.Location = new Point(30, 116);
        button.Click += (_, _) => LoginRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(button);
    }

    private async void OnFriendsChanged() => await SafeRefreshAsync();
    private async void OnFriendRequest(SocialFriendRequest _) => await SafeRefreshAsync();

    private async Task SafeRefreshAsync()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((Action)(async () => await RefreshAsync()));
            return;
        }
        await RefreshAsync();
    }
}
