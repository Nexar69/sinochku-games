using SinochkuGames.Models;
using SinochkuGames.Services;
using SinochkuGames.UI.Pages;

namespace SinochkuGames.UI;

public sealed class MainForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly Button _libraryButton;
    private readonly Button _communityButton;
    private readonly Button _profileButton;
    private readonly Button _friendsButton;
    private readonly Button _settingsButton;
    private readonly Button _chatButton;
    private readonly Button _notificationButton;
    private readonly Button _updateButton;
    private UpdateRelease? _pendingUpdate;
    private FriendsChatForm? _chatForm;

    public MainForm(LauncherContext context)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = Program.Brand;
        BackColor = _theme.Window;
        ForeColor = _theme.Text;
        MinimumSize = new Size(1120, 720);
        Size = new Size(1320, 840);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);

        var nav = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.FromArgb(23, 26, 33),
            Padding = new Padding(18, 8, 18, 8)
        };

        var brand = _theme.Label(Program.Brand, 15, FontStyle.Bold);
        brand.Location = new Point(18, 17);
        brand.Cursor = Cursors.Hand;
        brand.Click += (_, _) => ShowLibrary();
        nav.Controls.Add(brand);

        _libraryButton = NavButton("LIBRARY", 230);
        _libraryButton.Click += (_, _) => ShowLibrary();
        nav.Controls.Add(_libraryButton);

        _communityButton = NavButton("COMMUNITY", 320, 104);
        _communityButton.Click += (_, _) => ShowCommunity();
        nav.Controls.Add(_communityButton);

        _profileButton = NavButton("PROFILE", 430);
        _profileButton.Click += (_, _) => ShowProfile();
        nav.Controls.Add(_profileButton);

        _friendsButton = NavButton("FRIENDS", 520);
        _friendsButton.Click += (_, _) => ShowFriends();
        nav.Controls.Add(_friendsButton);

        _settingsButton = NavButton("SETTINGS", 610);
        _settingsButton.Click += (_, _) => ShowSettings();
        nav.Controls.Add(_settingsButton);

        _notificationButton = _theme.Button("🔔", 48, 34);
        _notificationButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _notificationButton.Location = new Point(nav.Width - 414, 11);
        _notificationButton.Click += (_, _) => ShowCommunity();
        nav.Controls.Add(_notificationButton);

        _chatButton = _theme.Button("FRIENDS & CHAT", 150, 34);
        _chatButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _chatButton.Location = new Point(nav.Width - 356, 11);
        _chatButton.Click += async (_, _) => await OpenChatAsync();
        nav.Controls.Add(_chatButton);

        _updateButton = _theme.Button("UPDATE", 150, 34);
        _updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _updateButton.Location = new Point(nav.Width - 178, 11);
        _updateButton.Visible = false;
        _updateButton.Click += async (_, _) => await InstallPendingUpdateAsync();
        nav.Controls.Add(_updateButton);

        nav.Resize += (_, _) =>
        {
            _notificationButton.Left = nav.ClientSize.Width - 414;
            _chatButton.Left = nav.ClientSize.Width - 356;
            _updateButton.Left = nav.ClientSize.Width - 178;
        };

        Controls.Add(_content);
        Controls.Add(nav);

        _context.Social.NotificationReceived += OnNotificationReceived;
        _context.Social.MessageReceived += OnMessageReceived;
        _context.Social.FriendRequestReceived += OnFriendRequestReceived;
        _context.Social.FriendsChanged += OnFriendsChanged;
        _context.Social.CurrentUserChanged += OnCurrentUserChanged;

        Shown += async (_, _) =>
        {
            ShowLibrary();
            await CheckForUpdatesAsync();
            await RefreshSocialBadgesAsync();

            if (_context.Settings.SocialEnabled
                && _context.Social.IsSignedIn
                && _context.Settings.OpenFriendsChatOnStart)
            {
                await OpenChatAsync();
            }
        };

        FormClosed += (_, _) =>
        {
            _context.Social.NotificationReceived -= OnNotificationReceived;
            _context.Social.MessageReceived -= OnMessageReceived;
            _context.Social.FriendRequestReceived -= OnFriendRequestReceived;
            _context.Social.FriendsChanged -= OnFriendsChanged;
            _context.Social.CurrentUserChanged -= OnCurrentUserChanged;

            if (_chatForm is { IsDisposed: false })
                _chatForm.Close();
        };
    }

    private Button NavButton(string text, int left, int width = 90)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = 10,
            Width = width,
            Height = 36,
            BackColor = Color.Transparent,
            ForeColor = _theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = _theme.SurfaceRaised;
        return button;
    }

    private void ShowLibrary()
    {
        SetActive(_libraryButton);
        var page = new LibraryPage(_context, _theme)
        {
            Dock = DockStyle.Fill
        };
        page.GameSelected += (_, game) => ShowGame(game.Id);
        ReplacePage(page);
    }

    private void ShowCommunity()
    {
        SetActive(_communityButton);
        var page = new CommunityPage(_context, _theme)
        {
            Dock = DockStyle.Fill
        };
        page.ProfileRequested += (_, username) => ShowProfile(username);
        page.LoginRequested += (_, _) => LoginSocial();
        ReplacePage(page);
    }

    private void ShowProfile(string? username = null)
    {
        SetActive(_profileButton);
        var page = new ProfilePage(_context, _theme, username)
        {
            Dock = DockStyle.Fill
        };
        page.MessageRequested += async (_, name) => await OpenChatAsync(name);
        page.LoginRequested += (_, _) => LoginSocial();
        ReplacePage(page);
    }

    private void ShowFriends()
    {
        SetActive(_friendsButton);
        var page = new FriendsPage(_context, _theme)
        {
            Dock = DockStyle.Fill
        };
        page.ProfileRequested += (_, username) => ShowProfile(username);
        page.MessageRequested += async (_, username) => await OpenChatAsync(username);
        page.LoginRequested += (_, _) => LoginSocial();
        ReplacePage(page);
    }

    private void ShowGame(string gameId)
    {
        var game = _context.Registry.Find(gameId);
        if (game is null) return;

        var page = new GamePage(_context, _theme, game)
        {
            Dock = DockStyle.Fill
        };
        page.BackRequested += (_, _) => ShowLibrary();
        page.SettingsRequested += (_, _) => ShowSettings();
        ReplacePage(page);
    }

    private void ShowSettings()
    {
        SetActive(_settingsButton);
        var page = new SettingsPage(_context, _theme)
        {
            Dock = DockStyle.Fill
        };
        page.LibraryRequested += (_, _) => ShowLibrary();
        page.SocialLoginRequested += (_, _) => LoginSocial();
        ReplacePage(page);
    }

    private void LoginSocial()
    {
        using var auth = new SocialAuthForm(_context);
        if (auth.ShowDialog(this) == DialogResult.OK)
        {
            _context.Settings.SocialEnabled = true;
            _context.SettingsStore.Save(_context.Settings);
            ShowProfile();
            _ = RefreshSocialBadgesAsync();
        }
    }

    private async Task OpenChatAsync(string? username = null)
    {
        if (!_context.Social.IsSignedIn)
        {
            LoginSocial();
            if (!_context.Social.IsSignedIn) return;
        }

        if (_chatForm is null || _chatForm.IsDisposed)
        {
            _chatForm = new FriendsChatForm(_context, username);
            _chatForm.Show(this);
            return;
        }

        _chatForm.Show();
        _chatForm.Activate();
        if (!string.IsNullOrWhiteSpace(username))
            await _chatForm.OpenByUsernameAsync(username);
    }

    private void ReplacePage(Control page)
    {
        while (_content.Controls.Count > 0)
        {
            var child = _content.Controls[0];
            _content.Controls.RemoveAt(0);
            child.Dispose();
        }

        _content.Controls.Add(page);
    }

    private void SetActive(Button active)
    {
        var buttons = new[]
        {
            _libraryButton, _communityButton, _profileButton, _friendsButton, _settingsButton
        };

        foreach (var button in buttons)
            button.ForeColor = ReferenceEquals(active, button) ? _theme.Accent : _theme.Text;
    }

    private async Task RefreshSocialBadgesAsync()
    {
        if (!_context.Social.IsSignedIn)
        {
            _chatButton.Text = "FRIENDS & CHAT";
            _notificationButton.Text = "🔔";
            return;
        }

        try
        {
            var conversations = await _context.Social.Api.ConversationsAsync();
            var notifications = await _context.Social.Api.NotificationsAsync();
            var unreadMessages = conversations.Sum(x => x.UnreadCount);
            var unreadNotifications = notifications.Count(x => !x.IsRead);

            _chatButton.Text = unreadMessages > 0
                ? $"FRIENDS & CHAT ({unreadMessages})"
                : "FRIENDS & CHAT";

            _notificationButton.Text = unreadNotifications > 0
                ? $"🔔 {unreadNotifications}"
                : "🔔";
        }
        catch
        {
            _chatButton.Text = "FRIENDS & CHAT";
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (!_context.Settings.CheckUpdatesAutomatically) return;

        _pendingUpdate = await _context.Updater.CheckAsync();
        if (_pendingUpdate is null) return;

        _updateButton.Text = $"UPDATE {_pendingUpdate.Version}";
        _updateButton.Visible = true;
        _updateButton.BringToFront();
    }

    private async Task InstallPendingUpdateAsync()
    {
        if (_pendingUpdate is null) return;

        var notes = string.IsNullOrWhiteSpace(_pendingUpdate.Notes)
            ? ""
            : "\n\nWhat's new:\n" + (_pendingUpdate.Notes.Length > 700
                ? _pendingUpdate.Notes[..700] + "…"
                : _pendingUpdate.Notes);

        var answer = MessageBox.Show(
            $"Install {Program.Brand} {_pendingUpdate.Version}?\n\nThe launcher will restart automatically.{notes}",
            Program.Brand,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (answer != DialogResult.Yes) return;

        _updateButton.Enabled = false;
        _updateButton.Text = "UPDATING...";

        try
        {
            await _context.Updater.InstallAsync(_pendingUpdate);
        }
        catch (Exception ex)
        {
            LogService.Error("Update installation failed", ex);
            _updateButton.Enabled = true;
            _updateButton.Text = $"UPDATE {_pendingUpdate.Version}";
            MessageBox.Show(ex.Message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnNotificationReceived(SocialNotification notification)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((Action)(() => OnNotificationReceived(notification)));
            return;
        }

        if (_context.Settings.ShowSocialToasts)
            SocialToastForm.ShowNotification(notification, _theme);

        _ = RefreshSocialBadgesAsync();
    }

    private async void OnMessageReceived(SocialMessage message)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((Action)(() => OnMessageReceived(message)));
            return;
        }

        if (_context.Settings.ShowSocialToasts
            && (_chatForm is null || _chatForm.IsDisposed || !_chatForm.Focused))
        {
            var sender = "New message";
            try
            {
                var friends = await _context.Social.Api.FriendsAsync();
                sender = friends.FirstOrDefault(x => x.Id == message.SenderId)?.DisplayName ?? sender;
            }
            catch { }

            SocialToastForm.ShowMessage(message, sender, _theme);
        }

        await RefreshSocialBadgesAsync();
    }

    private async void OnFriendRequestReceived(SocialFriendRequest _)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((Action)(() => OnFriendRequestReceived(_)));
            return;
        }
        await RefreshSocialBadgesAsync();
    }

    private async void OnFriendsChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((Action)OnFriendsChanged);
            return;
        }
        await RefreshSocialBadgesAsync();
    }

    private void OnCurrentUserChanged(SocialProfile? profile)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((Action)(() => OnCurrentUserChanged(profile)));
            return;
        }

        _profileButton.Text = profile is null
            ? "PROFILE"
            : profile.DisplayName.Length > 12
                ? profile.DisplayName[..12]
                : profile.DisplayName;
    }
}
