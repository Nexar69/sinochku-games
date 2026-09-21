using SinochkuGames.Models;

namespace SinochkuGames.UI;

public sealed class SocialToastForm : Form
{
    private readonly System.Windows.Forms.Timer _timer;

    public SocialToastForm(string title, string body, ThemePalette theme)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(360, 100);
        BackColor = theme.SurfaceRaised;
        ForeColor = theme.Text;
        Padding = new Padding(14);

        var titleLabel = theme.Label(title, 10, FontStyle.Bold, theme.Accent);
        titleLabel.Location = new Point(14, 12);
        Controls.Add(titleLabel);

        var bodyLabel = theme.Label(body, 9, FontStyle.Regular, theme.Text);
        bodyLabel.Location = new Point(14, 40);
        bodyLabel.MaximumSize = new Size(330, 48);
        Controls.Add(bodyLabel);

        Shown += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18);
        };

        _timer = new System.Windows.Forms.Timer { Interval = 4500 };
        _timer.Tick += (_, _) => Close();
        _timer.Start();

        FormClosed += (_, _) => _timer.Dispose();
    }

    public static void ShowNotification(SocialNotification notification, ThemePalette theme)
    {
        var toast = new SocialToastForm("СИНОЧКУ GAMES™", notification.Text, theme);
        toast.Show();
    }

    public static void ShowMessage(SocialMessage message, string sender, ThemePalette theme)
    {
        var toast = new SocialToastForm(sender, message.Body, theme);
        toast.Show();
    }
}
