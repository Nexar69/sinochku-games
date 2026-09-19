using SinochkuGames.Services;

namespace SinochkuGames.UI;

public sealed class SocialAuthForm : Form
{
    private readonly LauncherContext _context;
    private readonly ThemePalette _theme;
    private readonly TextBox _server = new();
    private readonly Label _status;

    public SocialAuthForm(LauncherContext context)
    {
        _context = context;
        _theme = ThemePalette.From(context.Settings);

        Text = $"Sign in — {Program.Brand}";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 610);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = _theme.Window;
        ForeColor = _theme.Text;
        Font = new Font("Segoe UI", 10f);

        var brand = _theme.Label(Program.Brand, 24, FontStyle.Bold);
        brand.Location = new Point(34, 26);
        Controls.Add(brand);

        var subtitle = _theme.Label(
            "Your profile, friends, presence and chats travel with your account.",
            10,
            FontStyle.Regular,
            _theme.Muted);
        subtitle.Location = new Point(36, 72);
        Controls.Add(subtitle);

        var serverLabel = _theme.Label("Social server", 9, FontStyle.Bold);
        serverLabel.Location = new Point(36, 112);
        Controls.Add(serverLabel);

        _server.Location = new Point(150, 108);
        _server.Size = new Size(430, 28);
        _server.Text = _context.Settings.SocialServerUrl;
        _server.BackColor = _theme.SurfaceRaised;
        _server.ForeColor = _theme.Text;
        Controls.Add(_server);

        var test = _theme.Button("TEST", 90, 30);
        test.Location = new Point(592, 106);
        test.Click += async (_, _) => await TestServerAsync();
        Controls.Add(test);

        var tabs = new TabControl
        {
            Location = new Point(34, 158),
            Size = new Size(692, 350)
        };
        tabs.TabPages.Add(BuildLoginTab());
        tabs.TabPages.Add(BuildRegisterTab());
        Controls.Add(tabs);

        _status = _theme.Label("", 9, FontStyle.Regular, _theme.Muted);
        _status.Location = new Point(36, 522);
        _status.MaximumSize = new Size(500, 45);
        Controls.Add(_status);

        var offline = _theme.Button("CONTINUE OFFLINE", 170, 38);
        offline.Location = new Point(556, 526);
        offline.Click += (_, _) =>
        {
            _context.Settings.SocialEnabled = false;
            _context.SettingsStore.Save(_context.Settings);
            DialogResult = DialogResult.Ignore;
            Close();
        };
        Controls.Add(offline);
    }

    private TabPage BuildLoginTab()
    {
        var page = Page("SIGN IN");
        var user = Field(page, "Username or email", 36);
        var password = Field(page, "Password", 104, password: true);

        var login = _theme.Button("SIGN IN", 150, 44);
        login.Location = new Point(30, 182);
        login.BackColor = _theme.Play;
        login.Click += async (_, _) =>
        {
            SetBusy(login, true, "SIGNING IN...");
            try
            {
                ApplyServer();
                await _context.Social.SignInAsync(user.Text, password.Text);
                _status.Text = $"Signed in as {_context.Social.CurrentUser?.DisplayName}.";
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _status.Text = Friendly(ex);
            }
            finally
            {
                SetBusy(login, false, "SIGN IN");
            }
        };
        page.Controls.Add(login);

        var recover = _theme.Button("RECOVER ACCOUNT", 170, 36);
        recover.Location = new Point(194, 186);
        recover.Click += (_, _) =>
        {
            ApplyServer();
            using var dialog = new RecoverAccountForm(_context);
            dialog.ShowDialog(this);
        };
        page.Controls.Add(recover);

        var note = _theme.Label(
            "This is a separate СИНОЧКУ account. Never enter your Steam password here.",
            9,
            FontStyle.Regular,
            _theme.Muted);
        note.Location = new Point(30, 250);
        page.Controls.Add(note);

        return page;
    }

    private TabPage BuildRegisterTab()
    {
        var page = Page("CREATE ACCOUNT");
        var username = Field(page, "Username", 24);
        var display = Field(page, "Display name", 76);
        var email = Field(page, "Email", 128);
        var password = Field(page, "Password", 180, password: true);
        var invite = Field(page, "Invite code", 232);

        var create = _theme.Button("CREATE ACCOUNT", 180, 42);
        create.Location = new Point(474, 276);
        create.BackColor = _theme.Play;
        create.Click += async (_, _) =>
        {
            SetBusy(create, true, "CREATING...");
            try
            {
                ApplyServer();
                await _context.Social.RegisterAsync(
                    username.Text,
                    email.Text,
                    password.Text,
                    invite.Text,
                    display.Text);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _status.Text = Friendly(ex);
            }
            finally
            {
                SetBusy(create, false, "CREATE ACCOUNT");
            }
        };
        page.Controls.Add(create);

        return page;
    }

    private TextBox Field(Control parent, string labelText, int y, bool password = false)
    {
        var label = _theme.Label(labelText, 9, FontStyle.Bold);
        label.Location = new Point(30, y);
        parent.Controls.Add(label);

        var box = new TextBox
        {
            Location = new Point(190, y - 4),
            Width = 430,
            BackColor = _theme.SurfaceRaised,
            ForeColor = _theme.Text,
            UseSystemPasswordChar = password
        };
        parent.Controls.Add(box);
        return box;
    }

    private TabPage Page(string title) => new(title)
    {
        BackColor = _theme.Surface,
        ForeColor = _theme.Text
    };

    private void ApplyServer()
    {
        _context.Social.SetServerUrl(_server.Text);
        _context.Settings.SocialEnabled = true;
        _context.SettingsStore.Save(_context.Settings);
    }

    private async Task TestServerAsync()
    {
        ApplyServer();
        _status.Text = "Testing server…";
        _status.ForeColor = _theme.Muted;
        var ok = await _context.Social.Api.HealthAsync();
        _status.Text = ok ? "Server is online ✓" : "Could not reach the server.";
        _status.ForeColor = ok ? _theme.Accent : Color.FromArgb(255, 177, 80);
    }

    private static void SetBusy(Button button, bool busy, string text)
    {
        button.Enabled = !busy;
        button.Text = text;
    }

    private static string Friendly(Exception ex)
    {
        var message = ex.Message;
        if (message.Length > 420) message = message[..420] + "…";
        return message;
    }
}
