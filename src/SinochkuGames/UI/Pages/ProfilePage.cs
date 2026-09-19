using SinochkuGames.Models;
using SinochkuGames.Services;

namespace SinochkuGames.UI.Pages;

public sealed class ProfilePage : UserControl
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly string? _username;
    private readonly Panel _body = new();

    public event EventHandler<string>? MessageRequested;
    public event EventHandler? LoginRequested;

    public ProfilePage(LauncherContext context, ThemePalette theme, string? username = null)
    {
        _context = context;
        _theme = theme;
        _username = username;

        BackColor = _theme.Window;
        _body.Dock = DockStyle.Fill;
        _body.AutoScroll = true;
        Controls.Add(_body);

        Load += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _body.Controls.Clear();

        if (!_context.Social.IsSignedIn)
        {
            ShowSignedOut();
            return;
        }

        try
        {
            var profile = string.IsNullOrWhiteSpace(_username)
                          || _username.Equals(_context.Social.CurrentUser?.Username, StringComparison.OrdinalIgnoreCase)
                ? await _context.Social.Api.MeAsync()
                : await _context.Social.Api.ProfileAsync(_username);

            BuildProfile(profile);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void BuildProfile(SocialProfile profile)
    {
        var isMe = profile.Id == _context.Social.CurrentUser?.Id;

        var hero = new Panel
        {
            Location = new Point(0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = Math.Max(900, Width),
            Height = 250,
            BackColor = _theme.Surface
        };
        _body.Controls.Add(hero);
        Resize += (_, _) => hero.Width = Math.Max(900, Width);

        var background = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _theme.Surface
        };
        hero.Controls.Add(background);
        _ = LoadImageAsync(background, profile.BackgroundUrl);

        var shade = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(120, 10, 16, 22)
        };
        background.Controls.Add(shade);

        var avatar = new PictureBox
        {
            Location = new Point(36, 92),
            Size = new Size(132, 132),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _theme.SurfaceRaised,
            BorderStyle = BorderStyle.FixedSingle
        };
        shade.Controls.Add(avatar);
        _ = LoadImageAsync(avatar, profile.AvatarUrl);

        var name = _theme.Label(profile.DisplayName, 28, FontStyle.Bold);
        name.Location = new Point(190, 108);
        shade.Controls.Add(name);

        var username = _theme.Label($"@{profile.Username}", 10, FontStyle.Regular, _theme.Muted);
        username.Location = new Point(194, 153);
        shade.Controls.Add(username);

        var presenceColor = profile.Presence == "Playing"
            ? _theme.Play
            : profile.Presence == "Online"
                ? _theme.Accent
                : _theme.Muted;

        var presence = _theme.Label(PresenceText(profile), 10, FontStyle.Bold, presenceColor);
        presence.Location = new Point(194, 180);
        shade.Controls.Add(presence);

        if (!string.IsNullOrWhiteSpace(profile.StatusText))
        {
            var status = _theme.Label($"“{profile.StatusText}”", 9, FontStyle.Italic, _theme.Text);
            status.Location = new Point(194, 207);
            shade.Controls.Add(status);
        }

        var controls = new FlowLayoutPanel
        {
            Location = new Point(36, 274),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        _body.Controls.Add(controls);

        if (isMe)
        {
            var edit = _theme.Button("EDIT PROFILE", 130, 38);
            edit.Click += async (_, _) =>
            {
                using var form = new ProfileEditForm(_context, profile);
                if (form.ShowDialog(this) == DialogResult.OK)
                    await RefreshAsync();
            };
            controls.Controls.Add(edit);

            if (profile.IsFounder)
            {
                var invite = _theme.Button("CREATE INVITE", 140, 38);
                invite.Click += async (_, _) => await CreateInviteAsync();
                controls.Controls.Add(invite);
            }
        }
        else
        {
            var message = _theme.Button("MESSAGE", 120, 38);
            message.Click += (_, _) => MessageRequested?.Invoke(this, profile.Username);
            controls.Controls.Add(message);

            var add = _theme.Button("ADD FRIEND", 130, 38);
            add.Click += async (_, _) =>
            {
                try
                {
                    await _context.Social.Api.SendFriendRequestAsync(profile.Username);
                    add.Text = "REQUEST SENT";
                    add.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Program.Brand);
                }
            };
            controls.Controls.Add(add);
        }

        var info = new Panel
        {
            Location = new Point(36, 334),
            Size = new Size(650, 390),
            BackColor = _theme.Surface
        };
        _body.Controls.Add(info);

        AddTitle(info, "ABOUT", 20);
        AddText(info, string.IsNullOrWhiteSpace(profile.Bio) ? "No bio yet." : profile.Bio, 58, 590);

        var playtime = PlaytimeTracker.Format(profile.TotalPlaySeconds);
        AddText(info, $"Total tracked social playtime: {playtime}", 132, 590);
        AddText(info, $"Member since {profile.CreatedAtUtc.LocalDateTime:d}", 158, 590);

        AddTitle(info, "BADGES", 205);
        var badgeText = profile.Badges.Count == 0 ? "No badges yet." : string.Join("   •   ", profile.Badges);
        AddText(info, badgeText, 242, 590);

        AddTitle(info, "ACHIEVEMENTS", 286);
        var unlocked = profile.Achievements.Where(x => x.Unlocked).Select(x => x.Name).ToArray();
        AddText(info, unlocked.Length == 0 ? "No achievements yet." : string.Join("   •   ", unlocked), 323, 590);

        var showcases = new Panel
        {
            Location = new Point(708, 334),
            Size = new Size(420, 390),
            BackColor = _theme.Surface
        };
        _body.Controls.Add(showcases);

        AddTitle(showcases, "SHOWCASES", 20);
        var y = 58;
        if (profile.Showcases.Count == 0)
        {
            AddText(showcases, "No showcases yet.", y, 360);
        }
        else
        {
            foreach (var slot in profile.Showcases.OrderBy(x => x.SortOrder))
            {
                var title = _theme.Label(slot.Title, 11, FontStyle.Bold);
                title.Location = new Point(20, y);
                showcases.Controls.Add(title);

                var body = _theme.Label(slot.Body, 9, FontStyle.Regular, _theme.Muted);
                body.Location = new Point(22, y + 28);
                body.MaximumSize = new Size(360, 55);
                showcases.Controls.Add(body);
                y += 88;
            }
        }

        _ = BuildCommentsAsync(profile);
    }

    private async Task BuildCommentsAsync(SocialProfile profile)
    {
        try
        {
            var comments = await _context.Social.Api.ProfileCommentsAsync(profile.Username);
            if (IsDisposed) return;

            var panel = new Panel
            {
                Location = new Point(36, 748),
                Size = new Size(1092, 340),
                BackColor = _theme.Surface
            };
            _body.Controls.Add(panel);

            AddTitle(panel, "PROFILE COMMENTS", 18);

            var input = new TextBox
            {
                Location = new Point(20, 54),
                Width = 780,
                BackColor = _theme.SurfaceRaised,
                ForeColor = _theme.Text,
                PlaceholderText = "Leave a comment…"
            };
            panel.Controls.Add(input);

            var post = _theme.Button("POST", 90, 30);
            post.Location = new Point(812, 52);
            post.Click += async (_, _) =>
            {
                var body = input.Text.Trim();
                if (body.Length == 0) return;
                try
                {
                    await _context.Social.Api.CreateProfileCommentAsync(profile.Username, body);
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Program.Brand);
                }
            };
            panel.Controls.Add(post);

            var flow = new FlowLayoutPanel
            {
                Location = new Point(20, 96),
                Size = new Size(1048, 220),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = _theme.SurfaceRaised,
                Padding = new Padding(6)
            };
            panel.Controls.Add(flow);

            foreach (var comment in comments)
            {
                var row = new Panel
                {
                    Width = 1000,
                    Height = 66,
                    Margin = new Padding(3),
                    BackColor = _theme.Surface
                };

                var author = _theme.Label(comment.Author.DisplayName, 9, FontStyle.Bold, _theme.Accent);
                author.Location = new Point(10, 8);
                row.Controls.Add(author);

                var time = _theme.Label(comment.CreatedAtUtc.LocalDateTime.ToString("g"), 8, FontStyle.Regular, _theme.Muted);
                time.Location = new Point(820, 8);
                row.Controls.Add(time);

                var text = _theme.Label(comment.Body, 9, FontStyle.Regular, _theme.Text);
                text.Location = new Point(10, 32);
                text.MaximumSize = new Size(900, 30);
                row.Controls.Add(text);

                if (comment.Author.Id == _context.Social.CurrentUser?.Id
                    || profile.Id == _context.Social.CurrentUser?.Id)
                {
                    var delete = _theme.Button("×", 34, 28);
                    delete.Location = new Point(952, 18);
                    delete.Click += async (_, _) =>
                    {
                        try
                        {
                            await _context.Social.Api.DeleteProfileCommentAsync(comment.Id);
                            await RefreshAsync();
                        }
                        catch { }
                    };
                    row.Controls.Add(delete);
                }

                flow.Controls.Add(row);
            }
        }
        catch
        {
            // Privacy settings may intentionally hide comments.
        }
    }

    private void ShowSignedOut()
    {
        var title = _theme.Label("PROFILE", 24, FontStyle.Bold);
        title.Location = new Point(36, 34);
        _body.Controls.Add(title);

        var text = _theme.Label(
            "Sign in to use profiles, friends, chat, presence and activity.",
            11,
            FontStyle.Regular,
            _theme.Muted);
        text.Location = new Point(38, 86);
        _body.Controls.Add(text);

        var login = _theme.Button("SIGN IN", 130, 42);
        login.Location = new Point(38, 130);
        login.Click += (_, _) => LoginRequested?.Invoke(this, EventArgs.Empty);
        _body.Controls.Add(login);
    }

    private async Task CreateInviteAsync()
    {
        try
        {
            var invite = await _context.Social.Api.CreateInviteAsync(1, 24 * 14);
            Clipboard.SetText(invite.Code);
            MessageBox.Show(
                $"Invite code copied to clipboard:\n\n{invite.Code}\n\nExpires in 14 days.",
                Program.Brand);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Program.Brand);
        }
    }

    private async Task LoadImageAsync(PictureBox target, string url)
    {
        var image = await _context.Social.Api.DownloadImageAsync(url);
        if (IsDisposed || target.IsDisposed || image is null) return;
        target.Image?.Dispose();
        target.Image = image;
    }

    private void AddTitle(Control parent, string text, int y)
    {
        var label = _theme.Label(text, 11, FontStyle.Bold);
        label.Location = new Point(20, y);
        parent.Controls.Add(label);
    }

    private void AddText(Control parent, string text, int y, int width)
    {
        var label = _theme.Label(text, 9, FontStyle.Regular, _theme.Muted);
        label.Location = new Point(22, y);
        label.MaximumSize = new Size(width, 0);
        parent.Controls.Add(label);
    }

    private void ShowError(string text)
    {
        var label = _theme.Label(text, 10, FontStyle.Regular, Color.FromArgb(255, 177, 80));
        label.Location = new Point(36, 36);
        label.MaximumSize = new Size(780, 0);
        _body.Controls.Add(label);
    }

    private static string PresenceText(SocialProfile profile) =>
        profile.Presence == "Playing" && !string.IsNullOrWhiteSpace(profile.CurrentGameId)
            ? $"Playing {profile.CurrentGameId}"
            : profile.Presence;
}
