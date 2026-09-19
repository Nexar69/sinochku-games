namespace SinochkuGames.UI;

public sealed class RecoverAccountForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;

    public RecoverAccountForm(LauncherContext context)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = "Recover account";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 400);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = _theme.Window;
        ForeColor = _theme.Text;

        var heading = _theme.Label("RECOVER СИНОЧКУ ACCOUNT", 18, FontStyle.Bold);
        heading.Location = new Point(24, 22);
        Controls.Add(heading);

        var account = Field("Username or email", 88, false);
        var code = Field("Recovery code", 144, false);
        var password = Field("New password", 200, true);
        var confirm = Field("Confirm password", 256, true);

        var recover = _theme.Button("RESET PASSWORD", 160, 42);
        recover.Location = new Point(432, 328);
        recover.Click += async (_, _) =>
        {
            if (password.Text != confirm.Text)
            {
                MessageBox.Show("The passwords do not match.", Program.Brand);
                return;
            }

            try
            {
                await _context.Social.Api.RecoverAccountAsync(account.Text, code.Text, password.Text);
                MessageBox.Show("Password reset. You can sign in now.", Program.Brand);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Program.Brand);
            }
        };
        Controls.Add(recover);
    }

    private TextBox Field(string labelText, int y, bool password)
    {
        var label = _theme.Label(labelText, 9, FontStyle.Bold);
        label.Location = new Point(24, y);
        Controls.Add(label);

        var box = new TextBox
        {
            Location = new Point(190, y - 4),
            Width = 400,
            UseSystemPasswordChar = password,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text
        };
        Controls.Add(box);
        return box;
    }
}
