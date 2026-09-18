using SinochkuGames.Models;
using SinochkuGames.Services;

namespace SinochkuGames.UI.Pages;

public sealed class LibraryPage : UserControl
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly FlowLayoutPanel _grid;
    private readonly TextBox _search;
    private readonly ComboBox _sort;

    public event EventHandler<GameDefinition>? GameSelected;

    public LibraryPage(LauncherContext context, ThemePalette theme)
    {
        _context = context;
        _theme = theme;
        BackColor = Color.FromArgb(35, 44, 58);
        Padding = new Padding(24);

        var title = _theme.Label(Program.Brand, 20, FontStyle.Regular);
        title.Location = new Point(28, 20);
        Controls.Add(title);

        var count = _theme.Label($"({_context.Registry.Games.Count})", 12, FontStyle.Regular, _theme.Muted);
        count.Location = new Point(270, 27);
        Controls.Add(count);

        _search = new TextBox
        {
            PlaceholderText = "Search games",
            Width = 230,
            Height = 30,
            Location = new Point(28, 72),
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
        _search.TextChanged += (_, _) => RefreshGames();
        Controls.Add(_search);

        _sort = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150,
            Location = new Point(272, 72),
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        _sort.Items.AddRange(new object[] { "Alphabetical", "Recently played", "Installed first" });
        _sort.SelectedIndex = 0;
        _sort.SelectedIndexChanged += (_, _) => RefreshGames();
        Controls.Add(_sort);

        var status = BuildStatusBanner();
        status.Location = new Point(28, 116);
        Controls.Add(status);

        _grid = new FlowLayoutPanel
        {
            Location = new Point(20, 170),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size = new Size(Width - 40, Height - 190),
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(8),
            WrapContents = true
        };
        Resize += (_, _) => _grid.Size = new Size(Math.Max(100, Width - 40), Math.Max(100, Height - 190));
        Controls.Add(_grid);

        RefreshGames();
    }

    private Control BuildStatusBanner()
    {
        var game = _context.Registry.Games.FirstOrDefault();
        var state = game is null ? null : _context.Detection.Detect(game);
        var steamEdit = _context.Detection.ResolveSteamEdit();

        var panel = new Panel
        {
            Width = 780,
            Height = 42,
            BackColor = _theme.Surface
        };

        var ok = _context.Steam.IsSteamInstalled
                 && state?.Installed == true
                 && File.Exists(steamEdit);

        var text = ok
            ? "All systems ready • Steam ✓  Game ✓  SteamEdit ✓"
            : $"Setup check • Steam {Mark(_context.Steam.IsSteamInstalled)}  Game {Mark(state?.Installed == true)}  SteamEdit {Mark(File.Exists(steamEdit))}";

        var label = _theme.Label(text, 9, FontStyle.Bold, ok ? _theme.Accent : Color.FromArgb(255, 177, 80));
        label.Location = new Point(14, 12);
        panel.Controls.Add(label);

        return panel;
    }

    private static string Mark(bool ok) => ok ? "✓" : "✕";

    private void RefreshGames()
    {
        if (_grid is null) return;

        foreach (Control control in _grid.Controls)
            control.Dispose();
        _grid.Controls.Clear();

        IEnumerable<GameDefinition> games = _context.Registry.Games;

        var query = _search.Text.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            games = games.Where(g =>
                g.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || g.BaseGameName.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        games = _sort.SelectedItem?.ToString() switch
        {
            "Recently played" => games.OrderByDescending(g =>
                _context.Settings.LastPlayed.TryGetValue(g.Id, out var date) ? date : DateTimeOffset.MinValue),
            "Installed first" => games.OrderByDescending(g => _context.Detection.Detect(g).Installed)
                                      .ThenBy(g => g.Name),
            _ => games.OrderBy(g => g.Name)
        };

        foreach (var game in games)
            _grid.Controls.Add(BuildCard(game));
    }

    private Control BuildCard(GameDefinition game)
    {
        var state = _context.Detection.Detect(game);
        var panel = new Panel
        {
            Width = 230,
            Height = 390,
            Margin = new Padding(10),
            BackColor = _theme.Surface,
            Cursor = Cursors.Hand
        };

        var cover = new PictureBox
        {
            Location = new Point(5, 5),
            Size = new Size(220, 330),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = AssetService.LoadBySuffix(game.CoverResourceSuffix),
            BackColor = Color.FromArgb(15, 20, 28),
            Cursor = Cursors.Hand
        };
        panel.Controls.Add(cover);

        var name = _theme.Label(game.Name, 11, FontStyle.Bold);
        name.Location = new Point(10, 344);
        panel.Controls.Add(name);

        var stateLabel = _theme.Label(
            state.StatusText.ToUpperInvariant(),
            8,
            FontStyle.Bold,
            state.Running ? _theme.Play : state.Installed ? _theme.Accent : _theme.Muted);
        stateLabel.Location = new Point(10, 369);
        panel.Controls.Add(stateLabel);

        void Open(object? _, EventArgs __) => GameSelected?.Invoke(this, game);
        panel.Click += Open;
        cover.Click += Open;
        name.Click += Open;

        return panel;
    }
}
