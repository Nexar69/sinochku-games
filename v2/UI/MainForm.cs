using SinochkuGames.Models;
using SinochkuGames.Services;

namespace SinochkuGames.UI;

public sealed class MainForm : Form
{
    private readonly SettingsService _settings;
    private readonly GameRegistry _registry;
    private readonly LauncherService _launcher;
    private readonly UpdateService _updater;

    private readonly Panel _content = new();
    private readonly Label _status = new();
    private readonly Label _title = new();
    private Button? _updateButton;

    public MainForm(
        SettingsService settings,
        GameRegistry registry,
        LauncherService launcher,
        UpdateService updater)
    {
        _settings = settings;
        _registry = registry;
        _launcher = launcher;
        _updater = updater;

        Text = Program.Brand;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1240, 780);
        BackColor = Theme.Window;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10f);
        DoubleBuffered = true;

        BuildShell();
        ShowLibrary();
        _ = CheckForUpdatesAsync();
    }

    private void BuildShell()
    {
        var top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Theme.TopBar
        };
        Controls.Add(top);

        var brand = LabelText(Program.Brand, 16f, FontStyle.Bold, Color.White);
        brand.Location = new Point(18, 16);
        brand.Cursor = Cursors.Hand;
        brand.Click += (_, _) => ShowLibrary();
        top.Controls.Add(brand);

        var library = NavButton("LIBRARY", 240);
        library.Click += (_, _) => ShowLibrary();
        top.Controls.Add(library);

        var settings = NavButton("SETTINGS", 340);
        settings.Click += (_, _) => ShowSettings();
        top.Controls.Add(settings);

        _updateButton = new Button
        {
            Text = "UP TO DATE",
            Width = 120,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width - 160, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Panel2,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Enabled = false
        };
        _updateButton.FlatAppearance.BorderSize = 0;
        top.Controls.Add(_updateButton);
        top.Resize += (_, _) => _updateButton.Location = new Point(top.Width - 138, 12);

        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.BackColor = Theme.Window;
        Controls.Add(_content);
        _content.BringToFront();

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Theme.TopBar
        };
        _status.Text = $"СИНОЧКУ GAMES™  v{Program.Version}";
        _status.ForeColor = Theme.Muted;
        _status.AutoSize = true;
        _status.Location = new Point(14, 9);
        bottom.Controls.Add(_status);
        Controls.Add(bottom);
    }

    private Button NavButton(string text, int x)
    {
        var b = new Button
        {
            Text = text,
            Width = 90,
            Height = 40,
            Location = new Point(x, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Theme.Panel;
        return b;
    }

    private void ShowLibrary()
    {
        _content.Controls.Clear();
        _content.BackColor = Color.FromArgb(35, 44, 58);

        var heading = LabelText(Program.Brand, 21f, FontStyle.Regular, Theme.Text);
        heading.Location = new Point(28, 24);
        _content.Controls.Add(heading);

        var sub = LabelText($"{_registry.Games.Count} GAME", 9f, FontStyle.Regular, Theme.Muted);
        sub.Location = new Point(30, 61);
        _content.Controls.Add(sub);

        var line = new Panel
        {
            Location = new Point(28, 88),
            Height = 1,
            Width = Math.Max(860, _content.ClientSize.Width - 56),
            BackColor = Color.FromArgb(76, 87, 100),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _content.Controls.Add(line);

        int x = 28;
        int y = 118;
        foreach (var game in _registry.Games)
        {
            var card = BuildGameCard(game);
            card.Location = new Point(x, y);
            _content.Controls.Add(card);
            x += card.Width + 24;
        }
    }

    private Control BuildGameCard(GameDefinition game)
    {
        var card = new Panel
        {
            Size = new Size(240, 392),
            BackColor = Color.FromArgb(20, 28, 39),
            Cursor = Cursors.Hand
        };

        var cover = new PictureBox
        {
            Location = new Point(6, 6),
            Size = new Size(228, 342),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(13, 19, 27),
            Image = LoadAsset(game.CoverAsset),
            Cursor = Cursors.Hand
        };

        var name = LabelText(game.DisplayName, 11f, FontStyle.Bold, Color.White);
        name.Location = new Point(10, 355);

        var installed = LabelText(
            _registry.IsInstalled(game) ? "INSTALLED" : "NOT FOUND",
            8f,
            FontStyle.Bold,
            _registry.IsInstalled(game) ? Theme.Green : Theme.Muted);
        installed.Location = new Point(158, 358);

        void open(object? _, EventArgs __) => ShowGame(game);
        card.Click += open;
        cover.Click += open;
        name.Click += open;

        card.Controls.Add(cover);
        card.Controls.Add(name);
        card.Controls.Add(installed);
        return card;
    }

    private void ShowGame(GameDefinition game)
    {
        _content.Controls.Clear();
        _content.BackColor = Theme.Window;

        var hero = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 360,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(17, 28, 40),
            Image = LoadAsset(game.HeroAsset)
        };
        _content.Controls.Add(hero);

        var logo = new PictureBox
        {
            Location = new Point(36, 120),
            Size = new Size(430, 150),
            BackColor = Color.Transparent,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = LoadAsset(game.LogoAsset)
        };
        hero.Controls.Add(logo);

        var actions = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = Color.FromArgb(27, 40, 53)
        };
        _content.Controls.Add(actions);
        actions.BringToFront();

        var play = new Button
        {
            Text = IsProcessRunning("cs2") ? "RUNNING" : "PLAY",
            Location = new Point(34, 18),
            Size = new Size(180, 56),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Green,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        play.FlatAppearance.BorderSize = 0;
        play.Enabled = _registry.IsInstalled(game) && !IsProcessRunning("cs2");
        play.Click += async (_, _) => await LaunchGameAsync(game, play);
        actions.Controls.Add(play);

        var state = LabelText(
            _registry.IsInstalled(game) ? "READY TO PLAY" : "GAME NOT FOUND",
            10f,
            FontStyle.Bold,
            _registry.IsInstalled(game) ? Theme.Text : Color.Orange);
        state.Location = new Point(238, 22);
        actions.Controls.Add(state);

        var baseInfo = LabelText($"Steam AppID {game.SteamAppId}", 9f, FontStyle.Regular, Theme.Muted);
        baseInfo.Location = new Point(238, 50);
        actions.Controls.Add(baseInfo);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            BackColor = Theme.Window
        };
        _content.Controls.Add(body);
        body.BringToFront();

        var info = new Panel
        {
            Location = new Point(28, 24),
            Size = new Size(650, 190),
            BackColor = Theme.Panel
        };
        body.Controls.Add(info);

        var gameTitle = LabelText(game.DisplayName, 23f, FontStyle.Bold, Color.White);
        gameTitle.Location = new Point(22, 20);
        info.Controls.Add(gameTitle);

        var description = LabelText(game.Description, 10f, FontStyle.Regular, Color.FromArgb(166, 180, 193));
        description.Location = new Point(24, 66);
        description.MaximumSize = new Size(600, 0);
        info.Controls.Add(description);

        var files = SmallButton("GAME FILES", 22, 132);
        files.Click += (_, _) => OpenGameFolder(game);
        info.Controls.Add(files);

        var steam = SmallButton("STEAM", 154, 132);
        steam.Click += (_, _) => new SteamService().OpenSteam();
        info.Controls.Add(steam);

        var statusPanel = new Panel
        {
            Location = new Point(700, 24),
            Size = new Size(360, 190),
            BackColor = Theme.Panel
        };
        body.Controls.Add(statusPanel);

        var st = LabelText("SYSTEM STATUS", 11f, FontStyle.Bold, Theme.Text);
        st.Location = new Point(18, 16);
        statusPanel.Controls.Add(st);

        AddStatus(statusPanel, 52, "STEAM", new SteamService().SteamExe is not null);
        AddStatus(statusPanel, 92, game.DisplayName, _registry.IsInstalled(game));
        AddStatus(statusPanel, 132, "STEAMEDIT", !game.RequiresSteamEdit || _launcher.FindSteamEdit() is not null);
    }

    private void AddStatus(Control parent, int y, string name, bool ok)
    {
        var l = LabelText($"[{(ok ? "OK" : "!")}]  {name}", 9f, FontStyle.Bold, ok ? Theme.Blue : Color.Orange);
        l.Location = new Point(18, y);
        l.Size = new Size(320, 30);
        l.BackColor = Theme.Panel2;
        l.Padding = new Padding(10, 7, 0, 0);
        parent.Controls.Add(l);
    }

    private async Task LaunchGameAsync(GameDefinition game, Button button)
    {
        try
        {
            button.Enabled = false;
            button.Text = "LAUNCHING...";
            _status.Text = $"Launching {game.DisplayName}…";
            await _launcher.LaunchAsync(game, this);
            button.Text = "RUNNING";
            _status.Text = $"{game.DisplayName} launched";
        }
        catch (Exception ex)
        {
            button.Enabled = true;
            button.Text = "PLAY";
            _status.Text = "Launch failed";
            MessageBox.Show(this, ex.Message, Program.Brand, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowSettings()
    {
        _content.Controls.Clear();
        _content.BackColor = Color.FromArgb(28, 38, 50);

        var heading = LabelText("SETTINGS", 22f, FontStyle.Bold, Theme.Text);
        heading.Location = new Point(32, 28);
        _content.Controls.Add(heading);

        var version = LabelText($"{Program.Brand}  v{Program.Version}", 10f, FontStyle.Regular, Theme.Muted);
        version.Location = new Point(34, 70);
        _content.Controls.Add(version);

        var general = SettingsSection("GENERAL", 34, 118, 800, 180);
        _content.Controls.Add(general);

        AddCheck(general, "Check automatically for updates", _settings.Current.CheckForUpdates, 22, value =>
        {
            _settings.Current.CheckForUpdates = value;
            _settings.Save();
        });

        AddCheck(general, "Close launcher after starting a game", _settings.Current.CloseLauncherAfterPlay, 62, value =>
        {
            _settings.Current.CloseLauncherAfterPlay = value;
            _settings.Save();
        });

        var steamEdit = LabelText("SteamEdit path", 10f, FontStyle.Regular, Theme.Text);
        steamEdit.Location = new Point(22, 111);
        general.Controls.Add(steamEdit);

        var pathBox = new TextBox
        {
            Location = new Point(142, 106),
            Width = 490,
            Text = _settings.Current.SteamEditPath,
            BackColor = Theme.Panel2,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
        general.Controls.Add(pathBox);

        var browse = SmallButton("BROWSE", 644, 103);
        browse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "SteamEdit.exe|SteamEdit.exe|Executable|*.exe" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            pathBox.Text = dialog.FileName;
            _settings.Current.SteamEditPath = dialog.FileName;
            _settings.Save();
        };
        general.Controls.Add(browse);

        var updates = SettingsSection("UPDATES", 34, 320, 800, 130);
        _content.Controls.Add(updates);

        var channel = LabelText("Channel", 10f, FontStyle.Regular, Theme.Text);
        channel.Location = new Point(22, 32);
        updates.Controls.Add(channel);

        var combo = new ComboBox
        {
            Location = new Point(110, 28),
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.Panel2,
            ForeColor = Theme.Text
        };
        combo.Items.AddRange(["stable", "beta"]);
        combo.SelectedItem = _settings.Current.UpdateChannel;
        combo.SelectedIndexChanged += (_, _) =>
        {
            _settings.Current.UpdateChannel = combo.SelectedItem?.ToString() ?? "stable";
            _settings.Save();
        };
        updates.Controls.Add(combo);

        var checkNow = SmallButton("CHECK NOW", 22, 72);
        checkNow.Click += async (_, _) => await ManualUpdateCheckAsync();
        updates.Controls.Add(checkNow);
    }

    private Panel SettingsSection(string title, int x, int y, int width, int height)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Theme.Panel
        };
        var label = LabelText(title, 11f, FontStyle.Bold, Theme.Text);
        label.Location = new Point(18, 10);
        panel.Controls.Add(label);
        return panel;
    }

    private void AddCheck(Control parent, string text, bool value, int y, Action<bool> changed)
    {
        var box = new CheckBox
        {
            Text = text,
            Checked = value,
            Location = new Point(22, y),
            AutoSize = true,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };
        box.CheckedChanged += (_, _) => changed(box.Checked);
        parent.Controls.Add(box);
    }

    private async Task CheckForUpdatesAsync()
    {
        if (!_settings.Current.CheckForUpdates || _updateButton is null)
            return;

        var update = await _updater.CheckAsync();
        if (update is null || IsDisposed)
            return;

        BeginInvoke(() =>
        {
            _updateButton.Text = $"UPDATE v{update.Version}";
            _updateButton.ForeColor = Color.White;
            _updateButton.BackColor = Theme.Blue;
            _updateButton.Enabled = true;
            _updateButton.Click += async (_, _) => await OpenReleaseAsync(update);
        });
    }

    private async Task ManualUpdateCheckAsync()
    {
        _status.Text = "Checking for updates…";
        var update = await _updater.CheckAsync();
        if (update is null)
        {
            _status.Text = $"You're on the latest version (v{Program.Version})";
            MessageBox.Show(this, "You're up to date.", Program.Brand);
            return;
        }

        await OpenReleaseAsync(update);
    }

    private Task OpenReleaseAsync(UpdateService.UpdateInfo update)
    {
        var url = $"https://github.com/{Program.Repository}/releases/tag/v{update.Version}";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    private void OpenGameFolder(GameDefinition game)
    {
        var path = _registry.InstallPath(game);
        if (path is null) return;
        System.Diagnostics.Process.Start("explorer.exe", $""{path}"");
    }

    private static bool IsProcessRunning(string name)
    {
        try { return System.Diagnostics.Process.GetProcessesByName(name).Length > 0; }
        catch { return false; }
    }

    private static Image? LoadAsset(string relative)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? Image.FromFile(path) : null;
        }
        catch { return null; }
    }

    private static Label LabelText(string text, float size, FontStyle style, Color color) =>
        new()
        {
            Text = text,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            AutoSize = true,
            BackColor = Color.Transparent
        };

    private static Button SmallButton(string text, int x, int y)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(112, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(49, 66, 82),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
