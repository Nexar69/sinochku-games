using System.Diagnostics;
using SinochkuGames.Models;
using SinochkuGames.Services;

namespace SinochkuGames.UI.Pages;

public sealed class GamePage : UserControl
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly GameDefinition _game;
    private readonly Label _runtime;
    private Label _playtime = null!;
    private readonly Button _play;
    private readonly System.Windows.Forms.Timer _timer;

    public event EventHandler? BackRequested;
    public event EventHandler? SettingsRequested;

    public GamePage(LauncherContext context, ThemePalette theme, GameDefinition game)
    {
        _context = context;
        _theme = theme;
        _game = game;

        BackColor = _theme.Window;

        var back = _theme.Button("← LIBRARY", 110, 34);
        back.Location = new Point(20, 18);
        back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(back);

        var hero = new HeroPanel(AssetService.LoadBySuffix(game.HeroResourceSuffix), _theme)
        {
            Location = new Point(0, 62),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Height = 330,
            Width = Width
        };
        Resize += (_, _) => hero.Width = Width;
        Controls.Add(hero);

        var logo = new PictureBox
        {
            Image = AssetService.LoadBySuffix(game.LogoResourceSuffix),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Location = new Point(36, 110),
            Size = new Size(420, 150)
        };
        hero.Controls.Add(logo);

        var action = new Panel
        {
            Location = new Point(0, 392),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = Width,
            Height = 88,
            BackColor = _theme.Surface
        };
        Resize += (_, _) => action.Width = Width;
        Controls.Add(action);

        _play = _theme.Button("PLAY", 180, 56);
        _play.BackColor = _theme.Play;
        _play.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        _play.Location = new Point(34, 16);
        _play.Click += (_, _) => Launch();
        action.Controls.Add(_play);

        _runtime = _theme.Label("READY TO PLAY", 10, FontStyle.Bold);
        _runtime.Location = new Point(235, 24);
        action.Controls.Add(_runtime);

        var baseName = _theme.Label($"Based on {game.BaseGameName}", 9, FontStyle.Regular, _theme.Muted);
        baseName.Location = new Point(235, 48);
        action.Controls.Add(baseName);

        var settings = _theme.Button("SETTINGS", 110, 42);
        settings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        settings.Location = new Point(action.Width - 140, 22);
        settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        action.Controls.Add(settings);
        action.Resize += (_, _) => settings.Left = action.ClientSize.Width - settings.Width - 30;

        BuildInfoArea();

        _timer = new System.Windows.Forms.Timer { Interval = 1500 };
        _timer.Tick += (_, _) => RefreshState();
        _timer.Start();
        Disposed += (_, _) => _timer.Dispose();

        RefreshState();
    }

    private void BuildInfoArea()
    {
        var info = new Panel
        {
            Location = new Point(28, 505),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size = new Size(Math.Max(100, Width - 56), Math.Max(140, Height - 525)),
            BackColor = _theme.Surface
        };
        Resize += (_, _) => info.Size = new Size(Math.Max(100, Width - 56), Math.Max(140, Height - 525));
        Controls.Add(info);

        var title = _theme.Label(_game.Name, 24, FontStyle.Bold);
        title.Location = new Point(24, 20);
        info.Controls.Add(title);

        var description = _theme.Label(_game.Description, 10, FontStyle.Regular, _theme.Muted);
        description.Location = new Point(26, 63);
        description.MaximumSize = new Size(620, 0);
        info.Controls.Add(description);

        var lastPlayedText = _context.Settings.LastPlayed.TryGetValue(_game.Id, out var lastPlayed)
            ? $"Last played: {lastPlayed.LocalDateTime:g}"
            : "Last played: never through this launcher";
        var lastPlayedLabel = _theme.Label(lastPlayedText, 9, FontStyle.Regular, _theme.Muted);
        lastPlayedLabel.Location = new Point(26, 104);
        info.Controls.Add(lastPlayedLabel);

        _playtime = _theme.Label("", 9, FontStyle.Regular, _theme.Muted);
        _playtime.Location = new Point(310, 104);
        info.Controls.Add(_playtime);

        var steam = _theme.Button("OPEN STEAM", 120, 38);
        steam.Location = new Point(26, 140);
        steam.Click += (_, _) => _context.Steam.OpenSteam();
        info.Controls.Add(steam);

        var files = _theme.Button("GAME FILES", 120, 38);
        files.Location = new Point(154, 140);
        files.Click += (_, _) =>
        {
            var state = _context.Detection.Detect(_game);
            if (Directory.Exists(state.InstallPath))
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{state.InstallPath}\"") { UseShellExecute = true });
        };
        info.Controls.Add(files);

        var favorite = _theme.Button(
            _context.Settings.Favorites.Contains(_game.Id) ? "★ FAVORITE" : "☆ FAVORITE",
            120,
            38);
        favorite.Location = new Point(282, 140);
        favorite.Click += (_, _) =>
        {
            if (!_context.Settings.Favorites.Add(_game.Id))
                _context.Settings.Favorites.Remove(_game.Id);
            _context.SettingsStore.Save(_context.Settings);
            favorite.Text = _context.Settings.Favorites.Contains(_game.Id) ? "★ FAVORITE" : "☆ FAVORITE";
        };
        info.Controls.Add(favorite);
    }

    private void RefreshState()
    {
        var state = _context.Detection.Detect(_game);
        _runtime.Text = state.Running ? $"{_game.Name} IS RUNNING" : state.Installed ? "READY TO PLAY" : "NOT INSTALLED";
        _runtime.ForeColor = state.Running ? _theme.Play : state.Installed ? _theme.Text : Color.FromArgb(255, 177, 80);
        _play.Text = state.Running ? "RUNNING" : "PLAY";
        _play.Enabled = state.Installed && !state.Running;
        _playtime.Text = $"Tracked playtime: {PlaytimeTracker.Format(_context.Playtime.GetTrackedSeconds(_game.Id))}";
    }

    private void Launch()
    {
        try
        {
            var validation = _context.Launcher.Validate(_game, _context.Detection);
            if (validation is not null)
            {
                if (validation.Contains("SteamEdit", StringComparison.OrdinalIgnoreCase))
                {
                    var answer = MessageBox.Show(
                        validation + "\n\nChoose SteamEdit.exe now?",
                        Program.Brand,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    if (answer == DialogResult.Yes)
                        _context.Launcher.BrowseForSteamEdit(this);
                    return;
                }

                MessageBox.Show(validation, Program.Brand, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _context.Launcher.Launch(_game, _context.Detection);
            RefreshState();
        }
        catch (Exception ex)
        {
            LogService.Error($"Failed to launch {_game.Id}", ex);
            MessageBox.Show(ex.Message, Program.Brand, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed class HeroPanel : Panel
    {
        private readonly Image? _image;
        private readonly ThemePalette _theme;

        public HeroPanel(Image? image, ThemePalette theme)
        {
            _image = image;
            _theme = theme;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_image is not null)
                e.Graphics.DrawImage(_image, ClientRectangle);

            using var bottom = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, Height / 2, Width, Height / 2),
                Color.FromArgb(0, _theme.Window),
                Color.FromArgb(240, _theme.Window),
                90f);
            e.Graphics.FillRectangle(bottom, new Rectangle(0, Height / 2, Width, Height / 2));
        }
    }
}
