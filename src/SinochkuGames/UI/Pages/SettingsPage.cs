using System.Diagnostics;
using SinochkuGames.Services;

namespace SinochkuGames.UI.Pages;

public sealed class SettingsPage : UserControl
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly ComboBox _themeChoice = new();
    private readonly TextBox _accent = new();
    private readonly ComboBox _channel = new();
    private readonly CheckBox _autoUpdate = new();
    private readonly TextBox _steamEdit = new();

    public event EventHandler? LibraryRequested;
    public event EventHandler? SocialLoginRequested;

    public SettingsPage(LauncherContext context, ThemePalette theme)
    {
        _context = context;
        _theme = theme;
        BackColor = _theme.Window;
        Padding = new Padding(28);

        var title = _theme.Label("SETTINGS", 22, FontStyle.Bold);
        title.Location = new Point(30, 20);
        Controls.Add(title);

        var subtitle = _theme.Label($"{Program.Brand}  •  v{Program.Version}", 10, FontStyle.Regular, _theme.Muted);
        subtitle.Location = new Point(32, 60);
        Controls.Add(subtitle);

        var tabs = new TabControl
        {
            Location = new Point(30, 100),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size = new Size(Math.Max(600, Width - 60), Math.Max(420, Height - 150))
        };
        Resize += (_, _) => tabs.Size = new Size(Math.Max(600, Width - 60), Math.Max(420, Height - 150));
        Controls.Add(tabs);

        tabs.TabPages.Add(BuildAccount());
        tabs.TabPages.Add(BuildFriendsPrivacy());
        tabs.TabPages.Add(BuildNotifications());
        tabs.TabPages.Add(BuildAppearance());
        tabs.TabPages.Add(BuildUpdates());
        tabs.TabPages.Add(BuildSteam());
        tabs.TabPages.Add(BuildGames());
        tabs.TabPages.Add(BuildAbout());
    }


    private TabPage BuildAccount()
    {
        var page = Page("Account");

        var heading = _theme.Label("СИНОЧКУ ACCOUNT", 13, FontStyle.Bold);
        heading.Location = new Point(28, 28);
        page.Controls.Add(heading);

        var signedIn = _theme.Label(
            _context.Social.IsSignedIn
                ? $"Signed in as {_context.Social.CurrentUser?.DisplayName} (@{_context.Social.CurrentUser?.Username})"
                : "Not signed in",
            10,
            FontStyle.Regular,
            _context.Social.IsSignedIn ? _theme.Accent : _theme.Muted);
        signedIn.Location = new Point(28, 66);
        page.Controls.Add(signedIn);

        AddLabel(page, "Social server", 28, 120);
        var server = new TextBox
        {
            Location = new Point(180, 116),
            Width = 470,
            Text = _context.Settings.SocialServerUrl,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        page.Controls.Add(server);

        var test = _theme.Button("TEST", 90, 32);
        test.Location = new Point(662, 114);
        test.Click += async (_, _) =>
        {
            _context.Social.SetServerUrl(server.Text);
            test.Text = "TESTING...";
            test.Enabled = false;
            var ok = await _context.Social.Api.HealthAsync();
            test.Text = ok ? "ONLINE ✓" : "OFFLINE";
            test.ForeColor = ok ? _theme.Accent : Color.FromArgb(255, 177, 80);
            test.Enabled = true;
        };
        page.Controls.Add(test);

        var enabled = new CheckBox
        {
            Text = "Enable profiles, friends and chat",
            Checked = _context.Settings.SocialEnabled,
            Location = new Point(28, 174),
            AutoSize = true,
            ForeColor = _theme.Text,
            BackColor = Color.Transparent
        };
        page.Controls.Add(enabled);

        var save = _theme.Button("SAVE ACCOUNT SETTINGS", 200, 40);
        save.Location = new Point(28, 220);
        save.Click += (_, _) =>
        {
            _context.Settings.SocialEnabled = enabled.Checked;
            _context.Social.SetServerUrl(server.Text);
            _context.SettingsStore.Save(_context.Settings);
            MessageBox.Show("Account settings saved.", Program.Brand);
        };
        page.Controls.Add(save);

        if (_context.Social.IsSignedIn)
        {
            var profile = _theme.Button("REFRESH PROFILE", 150, 40);
            profile.Location = new Point(240, 220);
            profile.Click += async (_, _) => await _context.Social.RefreshMeAsync();
            page.Controls.Add(profile);

            var signOut = _theme.Button("SIGN OUT", 120, 40);
            signOut.Location = new Point(402, 220);
            signOut.Click += async (_, _) =>
            {
                await _context.Social.SignOutAsync();
                signedIn.Text = "Not signed in";
                signedIn.ForeColor = _theme.Muted;
                MessageBox.Show("Signed out.", Program.Brand);
            };
            page.Controls.Add(signOut);

            if (_context.Social.CurrentUser?.IsFounder == true)
            {
                var invite = _theme.Button("CREATE INVITE", 150, 40);
                invite.Location = new Point(534, 220);
                invite.Click += async (_, _) =>
                {
                    try
                    {
                        var code = await _context.Social.Api.CreateInviteAsync(1, 24 * 14);
                        Clipboard.SetText(code.Code);
                        MessageBox.Show($"Invite copied:\n\n{code.Code}", Program.Brand);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Program.Brand);
                    }
                };
                page.Controls.Add(invite);
            }
        }
        else
        {
            var signIn = _theme.Button("SIGN IN / CREATE ACCOUNT", 220, 40);
            signIn.Location = new Point(240, 220);
            signIn.Click += (_, _) => SocialLoginRequested?.Invoke(this, EventArgs.Empty);
            page.Controls.Add(signIn);
        }

        var note = _theme.Label(
            "СИНОЧКУ accounts are separate from Steam. Never use your Steam password here.",
            9,
            FontStyle.Regular,
            _theme.Muted);
        note.Location = new Point(28, 292);
        page.Controls.Add(note);

        return page;
    }

    private TabPage BuildFriendsPrivacy()
    {
        var page = Page("Friends & Privacy");

        var heading = _theme.Label("FRIENDS & PRIVACY", 13, FontStyle.Bold);
        heading.Location = new Point(28, 28);
        page.Controls.Add(heading);

        if (!_context.Social.IsSignedIn)
        {
            var note = _theme.Label("Sign in to manage account privacy.", 10, FontStyle.Regular, _theme.Muted);
            note.Location = new Point(28, 72);
            page.Controls.Add(note);
            return page;
        }

        AddLabel(page, "Profile visibility", 28, 82);
        var visibility = new ComboBox
        {
            Location = new Point(190, 78),
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        visibility.Items.AddRange(new object[] { "Public", "Friends", "Private" });
        page.Controls.Add(visibility);

        var friendRequests = PrivacyCheck(page, "Allow friend requests", 28, 134);
        var messages = PrivacyCheck(page, "Allow messages from friends", 28, 174);
        var activity = PrivacyCheck(page, "Show game activity to friends", 28, 214);

        var save = _theme.Button("SAVE PRIVACY", 150, 40);
        save.Location = new Point(28, 274);
        save.Click += async (_, _) =>
        {
            try
            {
                await _context.Social.Api.UpdatePrivacyAsync(
                    visibility.SelectedItem?.ToString() ?? "Friends",
                    friendRequests.Checked,
                    messages.Checked,
                    activity.Checked);
                MessageBox.Show("Privacy settings saved.", Program.Brand);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Program.Brand);
            }
        };
        page.Controls.Add(save);

        page.Enter += async (_, _) =>
        {
            try
            {
                var privacy = await _context.Social.Api.PrivacyAsync();
                visibility.SelectedItem = privacy.ProfileVisibility;
                friendRequests.Checked = privacy.AllowFriendRequests;
                messages.Checked = privacy.AllowMessagesFromFriends;
                activity.Checked = privacy.ShowGameActivity;
            }
            catch { }
        };

        return page;
    }

    private TabPage BuildNotifications()
    {
        var page = Page("Notifications");

        var heading = _theme.Label("NOTIFICATIONS", 13, FontStyle.Bold);
        heading.Location = new Point(28, 28);
        page.Controls.Add(heading);

        var toasts = new CheckBox
        {
            Text = "Show desktop-style social toasts",
            Checked = _context.Settings.ShowSocialToasts,
            Location = new Point(28, 82),
            AutoSize = true,
            ForeColor = _theme.Text,
            BackColor = Color.Transparent
        };
        page.Controls.Add(toasts);

        var chatBehavior = new CheckBox
        {
            Text = "Open Friends & Chat automatically on launcher start",
            Checked = _context.Settings.OpenFriendsChatOnStart,
            Location = new Point(28, 126),
            AutoSize = true,
            ForeColor = _theme.Text,
            BackColor = Color.Transparent
        };
        page.Controls.Add(chatBehavior);

        var save = _theme.Button("SAVE NOTIFICATIONS", 180, 40);
        save.Location = new Point(28, 182);
        save.Click += (_, _) =>
        {
            _context.Settings.ShowSocialToasts = toasts.Checked;
            _context.Settings.OpenFriendsChatOnStart = chatBehavior.Checked;
            _context.SettingsStore.Save(_context.Settings);
            MessageBox.Show("Notification settings saved.", Program.Brand);
        };
        page.Controls.Add(save);

        return page;
    }

    private CheckBox PrivacyCheck(Control parent, string text, int x, int y)
    {
        var check = new CheckBox
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = _theme.Text,
            BackColor = Color.Transparent
        };
        parent.Controls.Add(check);
        return check;
    }

    private TabPage BuildAppearance()
    {
        var page = Page("Appearance");
        AddLabel(page, "Theme", 28, 32);
        _themeChoice.Items.AddRange(new object[] { "SteamDark", "Orange" });
        _themeChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeChoice.SelectedItem = _context.Settings.Theme;
        _themeChoice.Location = new Point(180, 28);
        page.Controls.Add(_themeChoice);

        AddLabel(page, "Accent color", 28, 82);
        _accent.Text = _context.Settings.AccentHex;
        _accent.Location = new Point(180, 78);
        _accent.Width = 140;
        page.Controls.Add(_accent);

        var note = _theme.Label("Theme changes apply after restarting the launcher.", 9, FontStyle.Regular, _theme.Muted);
        note.Location = new Point(28, 132);
        page.Controls.Add(note);

        var save = _theme.Button("SAVE APPEARANCE", 170, 40);
        save.Location = new Point(28, 180);
        save.Click += (_, _) =>
        {
            _context.Settings.Theme = _themeChoice.SelectedItem?.ToString() ?? "SteamDark";
            _context.Settings.AccentHex = _accent.Text.Trim();
            _context.SettingsStore.Save(_context.Settings);
            MessageBox.Show("Saved. Restart the launcher to apply the theme.", Program.Brand);
        };
        page.Controls.Add(save);
        return page;
    }

    private TabPage BuildUpdates()
    {
        var page = Page("Updates");
        AddLabel(page, "Update channel", 28, 32);
        _channel.Items.AddRange(new object[] { "Stable", "Beta" });
        _channel.DropDownStyle = ComboBoxStyle.DropDownList;
        _channel.SelectedItem = _context.Settings.UpdateChannel;
        _channel.Location = new Point(180, 28);
        page.Controls.Add(_channel);

        _autoUpdate.Text = "Check for updates automatically";
        _autoUpdate.Checked = _context.Settings.CheckUpdatesAutomatically;
        _autoUpdate.Location = new Point(28, 82);
        _autoUpdate.AutoSize = true;
        page.Controls.Add(_autoUpdate);

        var explanation = _theme.Label(
            "Stable receives final v2 releases. Beta also receives prereleases such as v2.1.0-beta.1.",
            9,
            FontStyle.Regular,
            _theme.Muted);
        explanation.Location = new Point(28, 122);
        page.Controls.Add(explanation);

        var save = _theme.Button("SAVE UPDATE SETTINGS", 190, 40);
        save.Location = new Point(28, 170);
        save.Click += (_, _) =>
        {
            _context.Settings.UpdateChannel = _channel.SelectedItem?.ToString() ?? "Stable";
            _context.Settings.CheckUpdatesAutomatically = _autoUpdate.Checked;
            _context.SettingsStore.Save(_context.Settings);
            MessageBox.Show("Update settings saved.", Program.Brand);
        };
        page.Controls.Add(save);

        var check = _theme.Button("CHECK NOW", 120, 40);
        check.Location = new Point(230, 170);
        check.Click += async (_, _) =>
        {
            check.Enabled = false;
            check.Text = "CHECKING...";
            try
            {
                var release = await _context.Updater.CheckAsync(force: true);
                if (release is null)
                {
                    MessageBox.Show($"You're up to date on v{Program.Version}.", Program.Brand);
                    return;
                }

                var notes = string.IsNullOrWhiteSpace(release.Notes)
                    ? "No release notes."
                    : release.Notes.Length > 900 ? release.Notes[..900] + "…" : release.Notes;

                MessageBox.Show(
                    $"{release.Name}\n\n{notes}",
                    $"Update {release.Version} available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            finally
            {
                check.Enabled = true;
                check.Text = "CHECK NOW";
            }
        };
        page.Controls.Add(check);

        var rollback = _theme.Button("ROLL BACK", 120, 40);
        rollback.Location = new Point(362, 170);
        rollback.Enabled = _context.Updater.CanRollback;
        rollback.Click += async (_, _) =>
        {
            var answer = MessageBox.Show(
                "Restore the previous launcher version?\n\nThe launcher will restart automatically.",
                Program.Brand,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            rollback.Enabled = false;
            rollback.Text = "ROLLING BACK...";
            try
            {
                await _context.Updater.RollbackAsync();
            }
            catch (Exception ex)
            {
                rollback.Enabled = true;
                rollback.Text = "ROLL BACK";
                MessageBox.Show(ex.Message, "Rollback failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        page.Controls.Add(rollback);
        return page;
    }

    private TabPage BuildSteam()
    {
        var page = Page("Steam");
        AddLabel(page, "Steam", 28, 32);

        var steamPath = _theme.Label(
            _context.Steam.IsSteamInstalled ? _context.Steam.SteamRoot : "Not detected",
            9,
            FontStyle.Regular,
            _context.Steam.IsSteamInstalled ? _theme.Accent : Color.FromArgb(255, 177, 80));
        steamPath.Location = new Point(180, 34);
        page.Controls.Add(steamPath);

        AddLabel(page, "SteamEdit", 28, 86);
        _steamEdit.Text = _context.Detection.ResolveSteamEdit();
        _steamEdit.Location = new Point(180, 82);
        _steamEdit.Width = 480;
        page.Controls.Add(_steamEdit);

        var browse = _theme.Button("BROWSE", 100, 32);
        browse.Location = new Point(670, 80);
        browse.Click += (_, _) =>
        {
            _context.Launcher.BrowseForSteamEdit(this);
            _steamEdit.Text = _context.Settings.SteamEditPath;
        };
        page.Controls.Add(browse);

        var rescan = _theme.Button("RESCAN STEAMEDIT", 150, 36);
        rescan.Location = new Point(28, 138);
        rescan.Click += (_, _) => _steamEdit.Text = _context.Detection.ResolveSteamEdit(true);
        page.Controls.Add(rescan);

        var setup = _theme.Button("RUN SETUP CHECK", 150, 36);
        setup.Location = new Point(190, 138);
        setup.Click += (_, _) =>
        {
            using var wizard = new SinochkuGames.UI.SetupWizardForm(_context);
            wizard.ShowDialog(this);
            _steamEdit.Text = _context.Detection.ResolveSteamEdit();
        };
        page.Controls.Add(setup);

        return page;
    }

    private TabPage BuildGames()
    {
        var page = Page("Games");
        var title = _theme.Label("Registered games", 13, FontStyle.Bold);
        title.Location = new Point(28, 28);
        page.Controls.Add(title);

        var custom = _theme.Button("OPEN CUSTOM GAMES FOLDER", 210, 36);
        custom.Location = new Point(560, 22);
        custom.Click += (_, _) => Process.Start(new ProcessStartInfo(
            "explorer.exe",
            $"\"{AppPaths.CustomGamesDirectory}\"") { UseShellExecute = true });
        page.Controls.Add(custom);

        var y = 72;
        foreach (var game in _context.Registry.Games)
        {
            var state = _context.Detection.Detect(game);
            var card = new Panel
            {
                Location = new Point(28, y),
                Size = new Size(740, 64),
                BackColor = _theme.SurfaceRaised
            };
            page.Controls.Add(card);

            var name = _theme.Label(game.Name, 11, FontStyle.Bold);
            name.Location = new Point(14, 12);
            card.Controls.Add(name);

            var tracked = PlaytimeTracker.Format(_context.Playtime.GetTrackedSeconds(game.Id));
            var status = _theme.Label(
                $"{game.BaseGameName} • {(state.Installed ? "installed" : "not installed")} • tracked {tracked}",
                9,
                FontStyle.Regular,
                state.Installed ? _theme.Accent : _theme.Muted);
            status.Location = new Point(14, 36);
            card.Controls.Add(status);

            y += 76;
        }

        return page;
    }

    private TabPage BuildAbout()
    {
        var page = Page("About");
        var brand = _theme.Label(Program.Brand, 20, FontStyle.Bold);
        brand.Location = new Point(28, 28);
        page.Controls.Add(brand);

        var version = _theme.Label($"Version {Program.Version}", 10, FontStyle.Regular, _theme.Muted);
        version.Location = new Point(30, 66);
        page.Controls.Add(version);

        var note = _theme.Label(
            "The ™ symbol is an informal brand mark. It does not mean the name is a registered trademark.\n" +
            "This launcher is not affiliated with Valve or Steam.",
            9,
            FontStyle.Regular,
            _theme.Muted);
        note.Location = new Point(30, 104);
        note.MaximumSize = new Size(720, 0);
        page.Controls.Add(note);

        var github = _theme.Button("OPEN GITHUB", 130, 38);
        github.Location = new Point(30, 170);
        github.Click += (_, _) => Process.Start(new ProcessStartInfo(
            $"https://github.com/{Program.Repository}") { UseShellExecute = true });
        page.Controls.Add(github);

        var logs = _theme.Button("OPEN LOGS", 120, 38);
        logs.Location = new Point(170, 170);
        logs.Click += (_, _) => Process.Start(new ProcessStartInfo(
            "explorer.exe", $"\"{AppPaths.LogDirectory}\"") { UseShellExecute = true });
        page.Controls.Add(logs);

        var diagnostics = _theme.Button("COPY DIAGNOSTICS", 160, 38);
        diagnostics.Location = new Point(300, 170);
        diagnostics.Click += (_, _) =>
        {
            var game = _context.Registry.Games.FirstOrDefault();
            if (game is null) return;
            Clipboard.SetText(_context.Detection.BuildDiagnostics(game));
            MessageBox.Show("Diagnostics copied to clipboard.", Program.Brand);
        };
        page.Controls.Add(diagnostics);

        var library = _theme.Button("BACK TO LIBRARY", 150, 38);
        library.Location = new Point(470, 170);
        library.Click += (_, _) => LibraryRequested?.Invoke(this, EventArgs.Empty);
        page.Controls.Add(library);

        return page;
    }

    private TabPage Page(string title)
    {
        return new TabPage(title)
        {
            BackColor = _theme.Surface,
            ForeColor = _theme.Text
        };
    }

    private void AddLabel(Control parent, string text, int x, int y)
    {
        var label = _theme.Label(text, 10, FontStyle.Bold);
        label.Location = new Point(x, y);
        parent.Controls.Add(label);
    }
}
