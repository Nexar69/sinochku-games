using SinochkuGames.Models;

namespace SinochkuGames.UI;

public sealed class FriendsChatForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly FlowLayoutPanel _people = new();
    private readonly FlowLayoutPanel _messages = new();
    private readonly TextBox _input = new();
    private readonly Label _chatTitle;
    private readonly Label _typing;
    private readonly System.Windows.Forms.Timer _typingTimer;
    private SocialFriend? _activeFriend;

    public FriendsChatForm(LauncherContext context, string? openUsername = null)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = $"Friends & Chat — {Program.Brand}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(980, 680);
        MinimumSize = new Size(820, 560);
        BackColor = _theme.Window;
        ForeColor = _theme.Text;
        Font = new Font("Segoe UI", 10f);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 300,
            BackColor = _theme.Window
        };
        Controls.Add(split);

        var leftHeader = _theme.Label("FRIENDS & CHAT", 15, FontStyle.Bold);
        leftHeader.Location = new Point(18, 18);
        split.Panel1.Controls.Add(leftHeader);

        var presence = new ComboBox
        {
            Location = new Point(16, 52),
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        presence.Items.AddRange(new object[] { "Online", "Away", "Busy", "Invisible" });
        presence.SelectedItem = _context.Social.CurrentUser?.Presence is "Away" or "Busy" or "Invisible"
            ? _context.Social.CurrentUser.Presence
            : "Online";
        presence.SelectedIndexChanged += async (_, _) =>
        {
            try
            {
                await _context.Social.Api.UpdatePresenceAsync(
                    presence.SelectedItem?.ToString() ?? "Online");
                await _context.Social.RefreshMeAsync();
            }
            catch { }
        };
        split.Panel1.Controls.Add(presence);

        _people.Location = new Point(12, 92);
        _people.Size = new Size(274, 516);
        _people.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _people.AutoScroll = true;
        _people.FlowDirection = FlowDirection.TopDown;
        _people.WrapContents = false;
        _people.BackColor = _theme.Surface;
        _people.Padding = new Padding(6);
        split.Panel1.Controls.Add(_people);

        _chatTitle = _theme.Label("Select a friend", 16, FontStyle.Bold);
        _chatTitle.Location = new Point(22, 18);
        split.Panel2.Controls.Add(_chatTitle);

        _typing = _theme.Label("", 8, FontStyle.Italic, _theme.Muted);
        _typing.Location = new Point(24, 48);
        split.Panel2.Controls.Add(_typing);

        _messages.Location = new Point(18, 76);
        _messages.Size = new Size(620, 470);
        _messages.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _messages.AutoScroll = true;
        _messages.FlowDirection = FlowDirection.TopDown;
        _messages.WrapContents = false;
        _messages.BackColor = _theme.Surface;
        _messages.Padding = new Padding(8);
        split.Panel2.Controls.Add(_messages);

        _input.Location = new Point(18, 562);
        _input.Size = new Size(500, 32);
        _input.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _input.Enabled = false;
        _input.BackColor = _theme.SurfaceRaised;
        _input.ForeColor = _theme.Text;
        _input.PlaceholderText = "Message a friend…";
        _input.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendAsync();
            }
        };
        _input.TextChanged += async (_, _) =>
        {
            if (_activeFriend is null) return;
            try
            {
                await _context.Social.Realtime.SetTypingAsync(_activeFriend.Id, _input.Text.Length > 0);
            }
            catch { }
            _typingTimer.Stop();
            _typingTimer.Start();
        };
        split.Panel2.Controls.Add(_input);

        var send = _theme.Button("SEND", 90, 34);
        send.Location = new Point(530, 560);
        send.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        send.Click += async (_, _) => await SendAsync();
        split.Panel2.Controls.Add(send);

        _typingTimer = new System.Windows.Forms.Timer { Interval = 1200 };
        _typingTimer.Tick += async (_, _) =>
        {
            _typingTimer.Stop();
            if (_activeFriend is null) return;
            try { await _context.Social.Realtime.SetTypingAsync(_activeFriend.Id, false); } catch { }
        };

        Load += async (_, _) =>
        {
            await LoadPeopleAsync();
            if (!string.IsNullOrWhiteSpace(openUsername))
                await OpenByUsernameAsync(openUsername);
        };

        _context.Social.MessageReceived += OnMessage;
        _context.Social.PresenceChanged += OnPresence;
        _context.Social.FriendsChanged += OnFriendsChanged;
        _context.Social.TypingChanged += OnTyping;

        FormClosed += (_, _) =>
        {
            _typingTimer.Dispose();
            _context.Social.MessageReceived -= OnMessage;
            _context.Social.PresenceChanged -= OnPresence;
            _context.Social.FriendsChanged -= OnFriendsChanged;
            _context.Social.TypingChanged -= OnTyping;
        };
    }

    public async Task OpenByUsernameAsync(string username)
    {
        var friends = await _context.Social.Api.FriendsAsync();
        var friend = friends.FirstOrDefault(f =>
            f.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (friend is not null)
            await OpenConversationAsync(friend);
    }

    private async Task LoadPeopleAsync()
    {
        _people.Controls.Clear();
        if (!_context.Social.IsSignedIn)
        {
            var label = _theme.Label("Sign in to use chat.", 9, FontStyle.Regular, _theme.Muted);
            _people.Controls.Add(label);
            return;
        }

        var conversations = await _context.Social.Api.ConversationsAsync();
        var friends = await _context.Social.Api.FriendsAsync();

        foreach (var friend in friends
                     .OrderByDescending(f => f.Presence == "Playing")
                     .ThenByDescending(f => f.Presence == "Online")
                     .ThenBy(f => f.DisplayName))
        {
            var conversation = conversations.FirstOrDefault(c => c.Friend.Id == friend.Id);
            _people.Controls.Add(PersonRow(friend, conversation?.UnreadCount ?? 0));
        }
    }

    private Control PersonRow(SocialFriend friend, int unread)
    {
        var row = new Panel
        {
            Width = 246,
            Height = 62,
            Margin = new Padding(3),
            BackColor = _activeFriend?.Id == friend.Id ? _theme.SurfaceSelected : _theme.SurfaceRaised,
            Cursor = Cursors.Hand
        };

        var dotColor = friend.Presence == "Playing" ? _theme.Play
            : friend.Presence == "Online" ? _theme.Accent
            : friend.Presence == "Away" ? Color.Goldenrod
            : _theme.Muted;

        var dot = _theme.Label("●", 10, FontStyle.Bold, dotColor);
        dot.Location = new Point(10, 20);
        row.Controls.Add(dot);

        var name = _theme.Label(friend.DisplayName, 9, unread > 0 ? FontStyle.Bold : FontStyle.Regular);
        name.Location = new Point(34, 10);
        row.Controls.Add(name);

        var statusText = friend.Presence == "Playing" && !string.IsNullOrWhiteSpace(friend.CurrentGameId)
            ? $"Playing {friend.CurrentGameId}"
            : friend.Presence;
        var status = _theme.Label(statusText, 8, FontStyle.Regular, _theme.Muted);
        status.Location = new Point(36, 34);
        row.Controls.Add(status);

        if (unread > 0)
        {
            var badge = _theme.Label(unread.ToString(), 8, FontStyle.Bold, Color.White);
            badge.BackColor = _theme.Accent;
            badge.Location = new Point(212, 20);
            badge.Padding = new Padding(5, 2, 5, 2);
            row.Controls.Add(badge);
        }

        void open(object? _, EventArgs __) => _ = OpenConversationAsync(friend);
        row.Click += open;
        name.Click += open;
        status.Click += open;
        return row;
    }

    private async Task OpenConversationAsync(SocialFriend friend)
    {
        _activeFriend = friend;
        _chatTitle.Text = friend.DisplayName;
        _typing.Text = friend.Presence == "Playing" && !string.IsNullOrWhiteSpace(friend.CurrentGameId)
            ? $"Playing {friend.CurrentGameId}"
            : friend.Presence;
        _input.Enabled = true;

        var messages = await _context.Social.Api.MessagesAsync(friend.Username);
        _messages.Controls.Clear();
        foreach (var message in messages)
            AppendMessage(message);

        await _context.Social.Api.MarkConversationReadAsync(friend.Username);
        await LoadPeopleAsync();
        ScrollBottom();
    }

    private async Task SendAsync()
    {
        if (_activeFriend is null) return;
        var body = _input.Text.Trim();
        if (body.Length == 0) return;

        _input.Clear();
        try
        {
            var message = await _context.Social.Api.SendMessageAsync(_activeFriend.Username, body);
            AppendMessage(message);
            ScrollBottom();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Program.Brand);
        }
    }

    private void AppendMessage(SocialMessage message)
    {
        var mine = message.SenderId == _context.Social.CurrentUser?.Id;
        var panel = new Panel
        {
            Width = Math.Max(500, _messages.ClientSize.Width - 36),
            Height = 66,
            Margin = new Padding(4),
            BackColor = mine ? _theme.SurfaceSelected : _theme.SurfaceRaised
        };

        var who = _theme.Label(mine ? "You" : _activeFriend?.DisplayName ?? "Friend", 8, FontStyle.Bold,
            mine ? _theme.Accent : _theme.Text);
        who.Location = new Point(12, 8);
        panel.Controls.Add(who);

        var text = _theme.Label(message.Body, 9, FontStyle.Regular, _theme.Text);
        text.Location = new Point(12, 28);
        text.MaximumSize = new Size(panel.Width - 110, 34);
        panel.Controls.Add(text);

        var time = _theme.Label(message.SentAtUtc.LocalDateTime.ToString("t"), 8, FontStyle.Regular, _theme.Muted);
        time.Location = new Point(panel.Width - 72, 8);
        panel.Controls.Add(time);

        _messages.Controls.Add(panel);
    }

    private void ScrollBottom()
    {
        if (_messages.Controls.Count > 0)
            _messages.ScrollControlIntoView(_messages.Controls[^1]);
    }

    private async void OnMessage(SocialMessage message)
    {
        if (IsDisposed) return;
        try
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => OnMessage(message));
                return;
            }

            if (_activeFriend is not null
                && (message.SenderId == _activeFriend.Id || message.RecipientId == _activeFriend.Id))
            {
                AppendMessage(message);
                ScrollBottom();
                await _context.Social.Api.MarkConversationReadAsync(_activeFriend.Username);
            }

            await LoadPeopleAsync();
        }
        catch { }
    }

    private async void OnPresence(SocialFriend friend)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnPresence(friend));
            return;
        }

        if (_activeFriend?.Id == friend.Id)
        {
            _activeFriend = friend;
            _typing.Text = friend.Presence == "Playing" && !string.IsNullOrWhiteSpace(friend.CurrentGameId)
                ? $"Playing {friend.CurrentGameId}"
                : friend.Presence;
        }

        try { await LoadPeopleAsync(); } catch { }
    }

    private async void OnFriendsChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(OnFriendsChanged);
            return;
        }
        try { await LoadPeopleAsync(); } catch { }
    }

    private void OnTyping(string userId, bool typing)
    {
        if (IsDisposed || _activeFriend?.Id != userId) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnTyping(userId, typing));
            return;
        }

        _typing.Text = typing
            ? $"{_activeFriend.DisplayName} is typing…"
            : _activeFriend.Presence == "Playing" && !string.IsNullOrWhiteSpace(_activeFriend.CurrentGameId)
                ? $"Playing {_activeFriend.CurrentGameId}"
                : _activeFriend.Presence;
    }
}
