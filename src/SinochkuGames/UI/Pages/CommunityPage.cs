using SinochkuGames.Models;

namespace SinochkuGames.UI.Pages;

public sealed class CommunityPage : UserControl
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly FlowLayoutPanel _feed = new();
    private readonly FlowLayoutPanel _notifications = new();

    public event EventHandler<string>? ProfileRequested;
    public event EventHandler? LoginRequested;

    public CommunityPage(LauncherContext context, ThemePalette theme)
    {
        _context = context;
        _theme = theme;
        BackColor = _theme.Window;

        if (!_context.Social.IsSignedIn)
        {
            var title = _theme.Label("COMMUNITY", 22, FontStyle.Bold);
            title.Location = new Point(30, 26);
            Controls.Add(title);

            var text = _theme.Label("Sign in to see activity from your friends.", 10, FontStyle.Regular, _theme.Muted);
            text.Location = new Point(32, 78);
            Controls.Add(text);

            var signIn = _theme.Button("SIGN IN", 120, 40);
            signIn.Location = new Point(32, 116);
            signIn.Click += (_, _) => LoginRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(signIn);
            return;
        }

        var heading = _theme.Label("COMMUNITY", 22, FontStyle.Bold);
        heading.Location = new Point(30, 22);
        Controls.Add(heading);

        var refresh = _theme.Button("REFRESH", 100, 30);
        refresh.Location = new Point(1030, 24);
        refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refresh.Click += async (_, _) => await RefreshAsync();
        Controls.Add(refresh);

        var activityTitle = _theme.Label("FRIEND ACTIVITY", 10, FontStyle.Bold);
        activityTitle.Location = new Point(30, 78);
        Controls.Add(activityTitle);

        _feed.Location = new Point(30, 108);
        _feed.Size = new Size(720, 560);
        _feed.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        _feed.AutoScroll = true;
        _feed.FlowDirection = FlowDirection.TopDown;
        _feed.WrapContents = false;
        _feed.BackColor = _theme.Surface;
        _feed.Padding = new Padding(8);
        Controls.Add(_feed);

        var notificationTitle = _theme.Label("NOTIFICATIONS", 10, FontStyle.Bold);
        notificationTitle.Location = new Point(778, 78);
        Controls.Add(notificationTitle);

        _notifications.Location = new Point(778, 108);
        _notifications.Size = new Size(352, 560);
        _notifications.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _notifications.AutoScroll = true;
        _notifications.FlowDirection = FlowDirection.TopDown;
        _notifications.WrapContents = false;
        _notifications.BackColor = _theme.Surface;
        _notifications.Padding = new Padding(8);
        Controls.Add(_notifications);

        Load += async (_, _) => await RefreshAsync();
        _context.Social.NotificationReceived += OnNotification;
        Disposed += (_, _) => _context.Social.NotificationReceived -= OnNotification;
    }

    private async Task RefreshAsync()
    {
        try
        {
            var friends = await _context.Social.Api.FriendsAsync();
            var profiles = new List<(string Username, string DisplayName, SocialActivity Activity)>();

            if (_context.Social.CurrentUser is { } me)
            {
                var own = await _context.Social.Api.ActivityAsync(me.Username);
                profiles.AddRange(own.Select(a => (me.Username, me.DisplayName, a)));
            }

            foreach (var friend in friends.Take(20))
            {
                try
                {
                    var items = await _context.Social.Api.ActivityAsync(friend.Username);
                    profiles.AddRange(items.Select(a => (friend.Username, friend.DisplayName, a)));
                }
                catch { }
            }

            _feed.Controls.Clear();
            foreach (var item in profiles.OrderByDescending(x => x.Activity.AtUtc).Take(50))
                _feed.Controls.Add(ActivityCard(item.Username, item.DisplayName, item.Activity));

            var notifications = await _context.Social.Api.NotificationsAsync();
            _notifications.Controls.Clear();
            foreach (var notification in notifications.Take(40))
                _notifications.Controls.Add(NotificationCard(notification));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Program.Brand);
        }
    }

    private Control ActivityCard(string username, string displayName, SocialActivity activity)
    {
        var card = new Panel
        {
            Width = 680,
            Height = 92,
            Margin = new Padding(4),
            BackColor = _theme.SurfaceRaised,
            Cursor = Cursors.Hand
        };

        var name = _theme.Label(displayName, 10, FontStyle.Bold, _theme.Accent);
        name.Location = new Point(14, 12);
        card.Controls.Add(name);

        var text = _theme.Label(activity.Text, 10, FontStyle.Regular, _theme.Text);
        text.Location = new Point(14, 39);
        text.MaximumSize = new Size(630, 0);
        card.Controls.Add(text);

        var at = _theme.Label(activity.AtUtc.LocalDateTime.ToString("g"), 8, FontStyle.Regular, _theme.Muted);
        at.Location = new Point(520, 14);
        card.Controls.Add(at);

        card.Click += (_, _) => ProfileRequested?.Invoke(this, username);
        name.Click += (_, _) => ProfileRequested?.Invoke(this, username);
        return card;
    }

    private Control NotificationCard(SocialNotification notification)
    {
        var card = new Panel
        {
            Width = 316,
            Height = 82,
            Margin = new Padding(4),
            BackColor = notification.IsRead ? _theme.SurfaceRaised : _theme.SurfaceSelected
        };

        var text = _theme.Label(notification.Text, 9, notification.IsRead ? FontStyle.Regular : FontStyle.Bold);
        text.Location = new Point(12, 10);
        text.MaximumSize = new Size(290, 42);
        card.Controls.Add(text);

        var at = _theme.Label(notification.CreatedAtUtc.LocalDateTime.ToString("g"), 8, FontStyle.Regular, _theme.Muted);
        at.Location = new Point(12, 58);
        card.Controls.Add(at);

        if (!notification.IsRead)
        {
            card.Cursor = Cursors.Hand;
            card.Click += async (_, _) =>
            {
                await _context.Social.Api.MarkNotificationReadAsync(notification.Id);
                await RefreshAsync();
            };
        }

        return card;
    }

    private async void OnNotification(SocialNotification _)
    {
        if (IsDisposed) return;
        try
        {
            if (InvokeRequired)
                BeginInvoke(async () => await RefreshAsync());
            else
                await RefreshAsync();
        }
        catch { }
    }
}
