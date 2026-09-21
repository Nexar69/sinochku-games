using SinochkuGames.Models;

namespace SinochkuGames.UI;

public sealed class ProfileEditForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly SocialProfile _profile;
    private readonly TextBox _display;
    private readonly TextBox _status;
    private readonly TextBox _bio;
    private readonly TextBox _accent;

    public ProfileEditForm(LauncherContext context, SocialProfile profile)
    {
        _context = context;
        _profile = profile;
        _theme = ThemePalette.From(context.Settings);

        Text = "Edit profile";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 570);
        BackColor = _theme.Window;
        ForeColor = _theme.Text;
        Font = new Font("Segoe UI", 10f);

        var title = _theme.Label("EDIT PROFILE", 20, FontStyle.Bold);
        title.Location = new Point(28, 24);
        Controls.Add(title);

        _display = Field("Display name", 82, profile.DisplayName);
        _status = Field("Status text", 136, profile.StatusText);
        _accent = Field("Accent (#RRGGBB)", 190, profile.AccentHex);

        var bioLabel = _theme.Label("Bio", 9, FontStyle.Bold);
        bioLabel.Location = new Point(28, 246);
        Controls.Add(bioLabel);

        _bio = new TextBox
        {
            Location = new Point(180, 242),
            Size = new Size(500, 110),
            Multiline = true,
            Text = profile.Bio,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        Controls.Add(_bio);

        var avatar = _theme.Button("CHANGE AVATAR", 150, 40);
        avatar.Location = new Point(28, 390);
        avatar.Click += async (_, _) => await UploadAsync("/api/me/avatar", "Avatar");
        Controls.Add(avatar);

        var background = _theme.Button("CHANGE BACKGROUND", 180, 40);
        background.Location = new Point(190, 390);
        background.Click += async (_, _) => await UploadAsync("/api/me/background", "Background");
        Controls.Add(background);

        var showcase = _theme.Button("EDIT SHOWCASES", 160, 40);
        showcase.Location = new Point(382, 390);
        showcase.Click += (_, _) =>
        {
            using var editor = new ShowcaseEditorForm(_context, _profile.Showcases);
            editor.ShowDialog(this);
        };
        Controls.Add(showcase);

        var save = _theme.Button("SAVE", 120, 42);
        save.Location = new Point(560, 492);
        save.BackColor = _theme.Play;
        save.Click += async (_, _) =>
        {
            try
            {
                await _context.Social.Api.UpdateProfileAsync(
                    _display.Text,
                    _bio.Text,
                    _status.Text,
                    _accent.Text);
                await _context.Social.RefreshMeAsync();
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

    private TextBox Field(string labelText, int y, string value)
    {
        var label = _theme.Label(labelText, 9, FontStyle.Bold);
        label.Location = new Point(28, y);
        Controls.Add(label);

        var box = new TextBox
        {
            Location = new Point(180, y - 4),
            Width = 500,
            Text = value,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        Controls.Add(box);
        return box;
    }

    private async Task UploadAsync(string endpoint, string label)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.webp"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _context.Social.Api.UploadImageAsync(endpoint, dialog.FileName);
            await _context.Social.RefreshMeAsync();
            MessageBox.Show($"{label} updated.", Program.Brand);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Program.Brand);
        }
    }
}
