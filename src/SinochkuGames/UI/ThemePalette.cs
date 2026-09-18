using SinochkuGames.Models;

namespace SinochkuGames.UI;

public sealed class ThemePalette
{
    public Color Window { get; init; }
    public Color Surface { get; init; }
    public Color SurfaceRaised { get; init; }
    public Color SurfaceSelected { get; init; }
    public Color Text { get; init; }
    public Color Muted { get; init; }
    public Color Accent { get; init; }
    public Color Play { get; init; }

    public static ThemePalette From(LauncherSettings settings)
    {
        var orange = settings.Theme.Equals("Orange", StringComparison.OrdinalIgnoreCase);
        return new ThemePalette
        {
            Window = orange ? Color.FromArgb(25, 18, 13) : Color.FromArgb(24, 35, 46),
            Surface = orange ? Color.FromArgb(38, 27, 18) : Color.FromArgb(27, 40, 53),
            SurfaceRaised = orange ? Color.FromArgb(52, 35, 20) : Color.FromArgb(35, 50, 64),
            SurfaceSelected = orange ? Color.FromArgb(89, 49, 15) : Color.FromArgb(46, 69, 91),
            Text = Color.FromArgb(235, 238, 241),
            Muted = Color.FromArgb(145, 160, 174),
            Accent = ParseHex(settings.AccentHex, orange ? Color.FromArgb(255, 128, 0) : Color.FromArgb(102, 192, 244)),
            Play = Color.FromArgb(117, 176, 34)
        };
    }

    private static Color ParseHex(string? value, Color fallback)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    public Button Button(string text, int width = 120, int height = 38)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            BackColor = SurfaceRaised,
            ForeColor = Text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = SurfaceSelected;
        return button;
    }

    public Label Label(string text, float size = 10, FontStyle style = FontStyle.Regular, Color? color = null)
        => new()
        {
            Text = text,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color ?? Text,
            BackColor = Color.Transparent,
            AutoSize = true
        };
}
