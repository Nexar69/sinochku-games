using SinochkuGames.Models;

namespace SinochkuGames.UI;

public sealed class ShowcaseEditorForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly List<(TextBox Title, TextBox Body)> _slots = new();

    public ShowcaseEditorForm(LauncherContext context, IReadOnlyList<SocialShowcase> showcases)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = "Profile showcases";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 620);
        BackColor = _theme.Window;
        ForeColor = _theme.Text;

        var heading = _theme.Label("PROFILE SHOWCASES", 20, FontStyle.Bold);
        heading.Location = new Point(28, 24);
        Controls.Add(heading);

        for (var i = 0; i < 3; i++)
        {
            var slot = showcases.FirstOrDefault(x => x.SortOrder == i);
            BuildSlot(i, slot?.Title ?? "", slot?.Body ?? "");
        }

        var save = _theme.Button("SAVE SHOWCASES", 170, 42);
        save.Location = new Point(522, 550);
        save.Click += async (_, _) =>
        {
            try
            {
                var items = _slots.Select((slot, index) =>
                    (slot.Title.Text.Trim(), slot.Body.Text.Trim(), index))
                    .Where(x => !string.IsNullOrWhiteSpace(x.Item1) || !string.IsNullOrWhiteSpace(x.Item2))
                    .ToArray();

                await _context.Social.Api.SaveShowcasesAsync(items);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Program.Brand);
            }
        };
        Controls.Add(save);
    }

    private void BuildSlot(int index, string titleText, string bodyText)
    {
        var top = 82 + index * 150;
        var label = _theme.Label($"Showcase {index + 1}", 10, FontStyle.Bold);
        label.Location = new Point(28, top);
        Controls.Add(label);

        var title = new TextBox
        {
            Location = new Point(150, top - 4),
            Width = 540,
            Text = titleText,
            PlaceholderText = "Title",
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        Controls.Add(title);

        var body = new TextBox
        {
            Location = new Point(150, top + 34),
            Size = new Size(540, 84),
            Multiline = true,
            Text = bodyText,
            PlaceholderText = "Showcase text",
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        Controls.Add(body);

        _slots.Add((title, body));
    }
}
