namespace SinochkuGames.UI;

public sealed class ChangePasswordForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;

    public ChangePasswordForm(LauncherContext context)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = "Change password";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = _theme.Window;
        ForeColor = _theme.Text;

        var heading = _theme.Label("CHANGE PASSWORD", 18, FontStyle.Bold);
        heading.Location = new Point(24, 22);
        Controls.Add(heading);

        var current = PasswordField("Current password", 86);
        var next = PasswordField("New password", 140);
        var confirm = PasswordField("Confirm new password", 194);

        var save = _theme.Button("CHANGE PASSWORD", 170, 42);
        save.Location = new Point(362, 254);
        save.Click += async (_, _) =>
        {
            if (next.Text != confirm.Text)
            {
                MessageBox.Show("The new passwords do not match.", Program.Brand);
                return;
            }

            try
            {
                await _context.Social.Api.ChangePasswordAsync(current.Text, next.Text);
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

    private TextBox PasswordField(string labelText, int y)
    {
        var label = _theme.Label(labelText, 9, FontStyle.Bold);
        label.Location = new Point(24, y);
        Controls.Add(label);

        var box = new TextBox
        {
            Location = new Point(190, y - 4),
            Width = 340,
            UseSystemPasswordChar = true,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        Controls.Add(box);
        return box;
    }
}
