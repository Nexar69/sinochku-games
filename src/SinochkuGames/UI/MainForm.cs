using SinochkuGames.Services;
using SinochkuGames.UI.Pages;

namespace SinochkuGames.UI;

public sealed class MainForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly Button _libraryButton;
    private readonly Button _settingsButton;
    private readonly Button _updateButton;
    private UpdateRelease? _pendingUpdate;

    public MainForm(LauncherContext context)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = Program.Brand;
        BackColor = _theme.Window;
        ForeColor = _theme.Text;
        MinimumSize = new Size(980, 680);
        Size = new Size(1220, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);

        var nav = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(23, 26, 33),
            Padding = new Padding(18, 8, 18, 8)
        };

        var brand = _theme.Label(Program.Brand, 15, FontStyle.Bold);
        brand.Location = new Point(18, 16);
        nav.Controls.Add(brand);

        _libraryButton = NavButton("LIBRARY", 260);
        _libraryButton.Click += (_, _) => ShowLibrary();
        nav.Controls.Add(_libraryButton);

        _settingsButton = NavButton("SETTINGS", 360);
        _settingsButton.Click += (_, _) => ShowSettings();
        nav.Controls.Add(_settingsButton);

        _updateButton = _theme.Button("UPDATE", 150, 34);
        _updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _updateButton.Location = new Point(nav.Width - 178, 11);
        _updateButton.Visible = false;
        _updateButton.Click += async (_, _) => await InstallPendingUpdateAsync();
        nav.Controls.Add(_updateButton);
        nav.Resize += (_, _) => _updateButton.Left = nav.ClientSize.Width - _updateButton.Width - 18;

        Controls.Add(_content);
        Controls.Add(nav);

        Shown += async (_, _) =>
        {
            ShowLibrary();
            await CheckForUpdatesAsync();
        };
    }

    private Button NavButton(string text, int left)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = 10,
            Width = 90,
            Height = 34,
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
        ReplacePage(page);
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
        _libraryButton.ForeColor = ReferenceEquals(active, _libraryButton) ? _theme.Accent : _theme.Text;
        _settingsButton.ForeColor = ReferenceEquals(active, _settingsButton) ? _theme.Accent : _theme.Text;
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
}
