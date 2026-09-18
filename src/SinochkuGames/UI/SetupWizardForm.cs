using SinochkuGames.Services;

namespace SinochkuGames.UI;

public sealed class SetupWizardForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly Label _steamState;
    private readonly Label _gameState;
    private readonly Label _steamEditState;
    private readonly Button _continue;

    public SetupWizardForm(LauncherContext context)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = $"{Program.Brand} Setup";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 500);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = _theme.Window;
        ForeColor = _theme.Text;
        Font = new Font("Segoe UI", 10f);

        var title = _theme.Label($"WELCOME TO {Program.Brand}", 20, FontStyle.Bold);
        title.Location = new Point(34, 28);
        Controls.Add(title);

        var subtitle = _theme.Label(
            "Quick setup check. Nothing here needs administrator rights.",
            10,
            FontStyle.Regular,
            _theme.Muted);
        subtitle.Location = new Point(36, 70);
        Controls.Add(subtitle);

        var panel = new Panel
        {
            Location = new Point(34, 116),
            Size = new Size(652, 230),
            BackColor = _theme.Surface
        };
        Controls.Add(panel);

        _steamState = Row(panel, 28, "Steam");
        _gameState = Row(panel, 88, "СИНОЧКУ 2 / Counter-Strike 2");
        _steamEditState = Row(panel, 148, "SteamEdit");

        var browse = _theme.Button("CHOOSE STEAMEDIT", 170, 38);
        browse.Location = new Point(430, 164);
        browse.Click += (_, _) =>
        {
            _context.Launcher.BrowseForSteamEdit(this);
            RefreshState();
        };
        panel.Controls.Add(browse);

        var rescan = _theme.Button("RESCAN", 110, 40);
        rescan.Location = new Point(34, 386);
        rescan.Click += (_, _) =>
        {
            _context.Detection.ResolveSteamEdit(true);
            RefreshState();
        };
        Controls.Add(rescan);

        _continue = _theme.Button("CONTINUE", 140, 44);
        _continue.Location = new Point(546, 382);
        _continue.BackColor = _theme.Play;
        _continue.Click += (_, _) =>
        {
            _context.Settings.FirstRunCompleted = true;
            _context.SettingsStore.Save(_context.Settings);
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(_continue);

        RefreshState();
    }

    private Label Row(Control parent, int y, string name)
    {
        var label = _theme.Label(name, 11, FontStyle.Bold);
        label.Location = new Point(24, y);
        parent.Controls.Add(label);

        var state = _theme.Label("checking…", 10, FontStyle.Bold, _theme.Muted);
        state.Location = new Point(300, y + 2);
        parent.Controls.Add(state);
        return state;
    }

    private void RefreshState()
    {
        var game = _context.Registry.Games.First();
        var gameState = _context.Detection.Detect(game);
        var steamEdit = _context.Detection.ResolveSteamEdit();

        Set(_steamState, _context.Steam.IsSteamInstalled, _context.Steam.IsSteamInstalled ? "Detected" : "Not found");
        Set(_gameState, gameState.Installed, gameState.Installed ? "Installed" : "Not found");
        Set(_steamEditState, File.Exists(steamEdit), File.Exists(steamEdit) ? "Ready" : "Choose SteamEdit.exe");

        _continue.Text = _context.Steam.IsSteamInstalled && gameState.Installed && File.Exists(steamEdit)
            ? "LET'S GO"
            : "CONTINUE";
    }

    private void Set(Label label, bool ok, string text)
    {
        label.Text = ok ? $"✓  {text}" : $"✕  {text}";
        label.ForeColor = ok ? _theme.Accent : Color.FromArgb(255, 177, 80);
    }
}
