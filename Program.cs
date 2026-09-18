using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("\u0421\u0418\u041D\u041E\u0427\u041A\u0423 GAMES\u2122")]
[assembly: AssemblyDescription("Portable custom Steam-style game launcher")]
[assembly: AssemblyCompany("Sinochku")]
[assembly: AssemblyProduct("\u0421\u0418\u041D\u041E\u0427\u041A\u0423 GAMES\u2122")]
[assembly: AssemblyVersion("1.1.0.0")]

namespace Sinochku2Launcher
{
    static class Program
    {
        public const string Brand = "\u0421\u0418\u041D\u041E\u0427\u041A\u0423 GAMES\u2122";
        public const string GameName = "\u0421\u0418\u041D\u041E\u0427\u041A\u0423 2";
        public const string Version = "1.1.0";
        public const string Repo = "Nexar69/sinochku-games";
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppSettings settings = AppSettings.Load();
            DetectionResult detected = Detector.Scan(settings);
            Application.Run(new MainForm(settings, detected));
        }
    }

    sealed class AppSettings
    {
        public string SteamEditPath = "";
        public static string Folder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sinochku2Launcher"); }
        }
        public static string FilePath { get { return Path.Combine(Folder, "settings.json"); } }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                Match m = Regex.Match(json, "\"steamEditPath\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase);
                if (m.Success) s.SteamEditPath = JsonUnescape(m.Groups[1].Value);
            }
            catch { }
            return s;
        }

        public void Save()
        {
            Directory.CreateDirectory(Folder);
            string json = "{\r\n  \"steamEditPath\": \"" + JsonEscape(SteamEditPath ?? "") + "\"\r\n}\r\n";
            File.WriteAllText(FilePath, json, new UTF8Encoding(false));
        }

        static string JsonEscape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        static string JsonUnescape(string s)
        {
            return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }

    sealed class DetectionResult
    {
        public string SteamFolder = "";
        public string SteamExe = "";
        public string Cs2Folder = "";
        public string SteamEditExe = "";
        public bool SteamFound { get { return File.Exists(SteamExe); } }
        public bool Cs2Found { get { return Directory.Exists(Cs2Folder); } }
        public bool SteamEditFound { get { return File.Exists(SteamEditExe); } }
    }

    static class Detector
    {
        public static DetectionResult Scan(AppSettings settings)
        {
            DetectionResult r = new DetectionResult();
            r.SteamFolder = FindSteam();
            if (!String.IsNullOrEmpty(r.SteamFolder))
                r.SteamExe = Path.Combine(r.SteamFolder, "steam.exe");
            r.Cs2Folder = FindCs2(r.SteamFolder);
            r.SteamEditExe = FindSteamEdit(settings == null ? "" : settings.SteamEditPath);
            return r;
        }

        static string FindSteam()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("SteamPath");
                        if (v != null)
                        {
                            string p = v.ToString().Replace('/', '\\');
                            if (File.Exists(Path.Combine(p, "steam.exe"))) return p;
                        }
                    }
                }
            }
            catch { }

            string[] guesses = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
            };
            foreach (string p in guesses)
                if (File.Exists(Path.Combine(p, "steam.exe"))) return p;
            return "";
        }

        static string FindCs2(string steamFolder)
        {
            if (String.IsNullOrEmpty(steamFolder)) return "";
            List<string> libraries = new List<string>();
            libraries.Add(steamFolder);
            string vdf = Path.Combine(steamFolder, "steamapps", "libraryfolders.vdf");
            try
            {
                if (File.Exists(vdf))
                {
                    string text = File.ReadAllText(vdf);
                    MatchCollection matches = Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    foreach (Match m in matches)
                    {
                        string p = m.Groups[1].Value.Replace("\\\\", "\\");
                        if (!libraries.Contains(p)) libraries.Add(p);
                    }
                }
            }
            catch { }

            foreach (string lib in libraries)
            {
                string manifest = Path.Combine(lib, "steamapps", "appmanifest_730.acf");
                try
                {
                    if (!File.Exists(manifest)) continue;
                    string text = File.ReadAllText(manifest);
                    Match m = Regex.Match(text, "\"installdir\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (!m.Success) continue;
                    string folder = Path.Combine(lib, "steamapps", "common", m.Groups[1].Value);
                    if (Directory.Exists(folder)) return folder;
                }
                catch { }
            }
            return "";
        }

        static string FindSteamEdit(string saved)
        {
            if (!String.IsNullOrEmpty(saved) && File.Exists(saved)) return saved;
            List<string> roots = new List<string>();
            roots.Add(AppDomain.CurrentDomain.BaseDirectory);
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            string oneDrive = Environment.GetEnvironmentVariable("OneDrive");
            if (!String.IsNullOrEmpty(oneDrive)) roots.Add(oneDrive);

            foreach (string root in roots)
            {
                string found = FindFile(root, "SteamEdit.exe", 3);
                if (!String.IsNullOrEmpty(found)) return found;
            }
            return "";
        }

        static string FindFile(string root, string file, int depth)
        {
            try
            {
                if (String.IsNullOrEmpty(root) || !Directory.Exists(root)) return "";
                string direct = Path.Combine(root, file);
                if (File.Exists(direct)) return direct;
                if (depth <= 0) return "";
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(dir);
                    if (name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(".git", StringComparison.OrdinalIgnoreCase)) continue;
                    string found = FindFile(dir, file, depth - 1);
                    if (!String.IsNullOrEmpty(found)) return found;
                }
            }
            catch { }
            return "";
        }
    }

    sealed class UpdateInfo
    {
        public string Version = "";
        public string DownloadUrl = "";
        public bool Available = false;
    }

    static class UpdateService
    {
        public static UpdateInfo Check()
        {
            UpdateInfo info = new UpdateInfo();
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Sinochku-Games-Launcher");
                    wc.Headers.Add("Accept", "application/vnd.github+json");
                    string api = "https://api.github.com/repos/" + Program.Repo + "/releases/latest";
                    string json = wc.DownloadString(api);

                    Match tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"v?([^\"]+)\"");
                    Match asset = Regex.Match(json,
                        "\"browser_download_url\"\\s*:\\s*\"([^\"]*SinochkuGames\\.exe)\"",
                        RegexOptions.IgnoreCase);

                    if (!tag.Success || !asset.Success) return info;
                    info.Version = tag.Groups[1].Value;
                    info.DownloadUrl = asset.Groups[1].Value.Replace("\\/", "/");

                    Version current;
                    Version latest;
                    if (Version.TryParse(Program.Version, out current) &&
                        Version.TryParse(info.Version, out latest))
                        info.Available = latest > current;
                }
            }
            catch { }
            return info;
        }

        public static void Install(UpdateInfo info)
        {
            if (info == null || String.IsNullOrEmpty(info.DownloadUrl)) return;

            string tempRoot = Path.Combine(Path.GetTempPath(), "SinochkuGamesUpdate");
            Directory.CreateDirectory(tempRoot);
            string downloaded = Path.Combine(tempRoot, "SinochkuGames.exe");
            string script = Path.Combine(tempRoot, "apply-update.ps1");
            string current = Assembly.GetExecutingAssembly().Location;

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "Sinochku-Games-Launcher");
                wc.DownloadFile(info.DownloadUrl, downloaded);
            }

            int currentPid = Process.GetCurrentProcess().Id;
            string ps =
                "while (Get-Process -Id " + currentPid + " -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 250 }\r\n" +
                "Copy-Item -LiteralPath '" + PsEscape(downloaded) +
                "' -Destination '" + PsEscape(current) + "' -Force\r\n" +
                "Start-Process -FilePath '" + PsEscape(current) + "'\r\n" +
                "Remove-Item -LiteralPath '" + PsEscape(downloaded) +
                "' -Force -ErrorAction SilentlyContinue\r\n" +
                "Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue\r\n";

            File.WriteAllText(script, ps, new UTF8Encoding(true));
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process.Start(psi);
            Application.Exit();
        }

        static string PsEscape(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
    }

    sealed class MainForm : Form
    {
        AppSettings settings;
        DetectionResult detected;
        Label steamStatus, cs2Status, editStatus, runtimeStatus;
        Button playButton;
        Button selectedGameButton;
        System.Windows.Forms.Timer timer;
        Panel titleBar;
        Panel gameSidebar;
        PictureBox heroBox;
        PictureBox logoBox;
        Image heroImage;
        Image logoImage;
        Image coverImage;
        Panel libraryPage;
        Panel settingsPage;
        Button updateButton;

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public MainForm(AppSettings settings, DetectionResult detected)
        {
            this.settings = settings;
            this.detected = detected;
            Text = Program.Brand;
            ClientSize = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(20, 31, 43);
            MinimumSize = MaximumSize = Size;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 10f);
            LoadSteamArtwork();
            BuildUi();
            RefreshDetectionUi();
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1500;
            timer.Tick += delegate { RefreshRuntime(); };
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (heroImage != null) heroImage.Dispose();
                if (logoImage != null) logoImage.Dispose();
                if (coverImage != null) coverImage.Dispose();
            }
            base.Dispose(disposing);
        }

        void LoadSteamArtwork()
        {
            string hero = FindGridArt("_hero");
            string logo = FindGridArt("_logo");
            string cover = FindGridArt("p");
            heroImage = LoadImageUnlocked(hero);
            logoImage = LoadImageUnlocked(logo);
            coverImage = LoadImageUnlocked(cover);
            if (heroImage == null) heroImage = LoadEmbeddedImage("Sinochku2.hero.png");
            if (logoImage == null) logoImage = LoadEmbeddedImage("Sinochku2.logo.png");
            if (coverImage == null) coverImage = LoadEmbeddedImage("Sinochku2.cover.png");
        }

        Image LoadEmbeddedImage(string name)
        {
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    if (s == null) return null;
                    using (Image temp = Image.FromStream(s)) return new Bitmap(temp);
                }
            }
            catch { return null; }
        }

        string FindGridArt(string suffix)
        {
            if (String.IsNullOrEmpty(detected.SteamFolder)) return "";
            try
            {
                string userdata = Path.Combine(detected.SteamFolder, "userdata");
                if (!Directory.Exists(userdata)) return "";
                string[] exts = { ".png", ".jpg", ".jpeg", ".webp" };
                foreach (string user in Directory.GetDirectories(userdata))
                {
                    string grid = Path.Combine(user, "config", "grid");
                    if (!Directory.Exists(grid)) continue;
                    foreach (string ext in exts)
                    {
                        string p = Path.Combine(grid, "730" + suffix + ext);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { }
            return "";
        }

        Image LoadImageUnlocked(string path)
        {
            if (String.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                using (Image temp = Image.FromFile(path))
                    return new Bitmap(temp);
            }
            catch { return null; }
        }

        void BuildUi()
        {
            BuildTitleBar();
            BuildGameSidebar();
            BuildGamePage();
            BuildLibraryPage();
            BuildSettingsPage();
            ShowLibraryPage();
            BeginUpdateCheck();
        }

        void BuildTitleBar()
        {
            titleBar = new Panel();
            titleBar.Location = new Point(0, 0);
            titleBar.Size = new Size(1180, 48);
            titleBar.BackColor = Color.FromArgb(23, 26, 33);
            titleBar.MouseDown += DragWindow;
            Controls.Add(titleBar);

            Label steamWord = MakeLabel("STEAM", 14, FontStyle.Bold, Color.White);
            steamWord.Location = new Point(18, 14);
            steamWord.AutoSize = true;
            steamWord.MouseDown += DragWindow;
            titleBar.Controls.Add(steamWord);

            string[] nav = { "STORE", "LIBRARY", "COMMUNITY", "SETTINGS" };
            int x = 105;
            for (int i = 0; i < nav.Length; i++)
            {
                int index = i;
                Label l = MakeLabel(nav[i], 9, i == 1 ? FontStyle.Bold : FontStyle.Regular,
                    i == 1 ? Color.FromArgb(30, 144, 255) : Color.FromArgb(205, 205, 205));
                l.Location = new Point(x, 17);
                l.AutoSize = true;
                l.Cursor = (i == 1 || i == 3) ? Cursors.Hand : Cursors.Default;
                if (index == 1) l.Click += delegate { ShowLibraryPage(); };
                if (index == 3) l.Click += delegate { ShowSettingsPage(); };
                titleBar.Controls.Add(l);
                x += l.PreferredWidth + 22;
            }

            Label appName = MakeLabel(Program.Brand, 9, FontStyle.Regular, Color.FromArgb(128, 128, 128));
            appName.Location = new Point(790, 17);
            appName.AutoSize = true;
            appName.Cursor = Cursors.Hand;
            appName.Click += delegate { ShowLibraryPage(); };
            titleBar.Controls.Add(appName);

            updateButton = MakeButton("UPDATE", new Size(132, 30), Color.FromArgb(47, 88, 116), 8);
            updateButton.Location = new Point(950, 9);
            updateButton.Visible = false;
            updateButton.Click += UpdateClicked;
            titleBar.Controls.Add(updateButton);

            Button min = MakeButton("-", new Size(42, 48), Color.Transparent, 13);
            min.Location = new Point(1096, 0);
            min.Click += delegate { WindowState = FormWindowState.Minimized; };
            titleBar.Controls.Add(min);

            Button close = MakeButton("X", new Size(42, 48), Color.Transparent, 11);
            close.Location = new Point(1138, 0);
            close.Click += delegate { Close(); };
            titleBar.Controls.Add(close);
        }

        void BuildGameSidebar()
        {
            gameSidebar = new Panel();
            gameSidebar.Location = new Point(0, 48);
            gameSidebar.Size = new Size(220, 672);
            gameSidebar.BackColor = Color.FromArgb(24, 35, 46);
            Controls.Add(gameSidebar);

            Label library = MakeLabel("GAMES", 10, FontStyle.Bold, Color.FromArgb(142, 152, 162));
            library.Location = new Point(18, 18);
            library.AutoSize = true;
            gameSidebar.Controls.Add(library);

            Label count = MakeLabel("1 GAME", 8, FontStyle.Regular, Color.FromArgb(86, 102, 116));
            count.Location = new Point(155, 20);
            count.AutoSize = true;
            gameSidebar.Controls.Add(count);

            selectedGameButton = MakeButton(Program.GameName, new Size(196, 58), Color.FromArgb(46, 69, 91), 11);
            selectedGameButton.Location = new Point(12, 48);
            selectedGameButton.TextAlign = ContentAlignment.MiddleLeft;
            selectedGameButton.Padding = new Padding(15, 0, 0, 0);
            selectedGameButton.FlatAppearance.BorderSize = 0;
            selectedGameButton.Click += delegate { ShowGamePage(); };
            gameSidebar.Controls.Add(selectedGameButton);

            Panel selected = new Panel();
            selected.Location = new Point(0, 48);
            selected.Size = new Size(4, 58);
            selected.BackColor = Color.FromArgb(102, 192, 244);
            gameSidebar.Controls.Add(selected);
            selected.BringToFront();

            Label baseGame = MakeLabel("based on Counter-Strike 2", 8, FontStyle.Regular, Color.FromArgb(96, 112, 125));
            baseGame.Location = new Point(24, 112);
            baseGame.AutoSize = true;
            gameSidebar.Controls.Add(baseGame);

            Label hint = MakeLabel("More games can be added later.", 8, FontStyle.Regular, Color.FromArgb(75, 90, 103));
            hint.Location = new Point(18, 620);
            hint.AutoSize = true;
            gameSidebar.Controls.Add(hint);
        }

        void BuildGamePage()
        {
            heroBox = new PictureBox();
            heroBox.Location = new Point(220, 48);
            heroBox.Size = new Size(960, 320);
            heroBox.BackColor = Color.FromArgb(17, 28, 40);
            heroBox.SizeMode = PictureBoxSizeMode.Zoom;
            heroBox.Image = heroImage;
            heroBox.Paint += PaintHeroOverlay;
            Controls.Add(heroBox);

            logoBox = new PictureBox();
            logoBox.BackColor = Color.Transparent;
            logoBox.Location = new Point(28, 122);
            logoBox.Size = new Size(390, 135);
            logoBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoBox.Image = logoImage;
            heroBox.Controls.Add(logoBox);

            if (logoImage == null)
            {
                Label fallbackLogo = MakeLabel(Program.GameName, 42, FontStyle.Bold | FontStyle.Italic, Color.White);
                fallbackLogo.Location = new Point(34, 168);
                fallbackLogo.AutoSize = true;
                heroBox.Controls.Add(fallbackLogo);
            }

            Panel actionBar = new Panel();
            actionBar.Location = new Point(220, 368);
            actionBar.Size = new Size(960, 92);
            actionBar.BackColor = Color.FromArgb(27, 40, 53);
            Controls.Add(actionBar);

            playButton = MakeButton("PLAY", new Size(178, 56), Color.FromArgb(117, 176, 34), 20);
            playButton.Location = new Point(34, 18);
            playButton.Click += PlayClicked;
            actionBar.Controls.Add(playButton);

            runtimeStatus = MakeLabel("READY TO PLAY", 10, FontStyle.Bold, Color.FromArgb(210, 210, 210));
            runtimeStatus.Location = new Point(234, 22);
            runtimeStatus.Size = new Size(300, 24);
            actionBar.Controls.Add(runtimeStatus);

            Label cloud = MakeLabel("STEAM CLOUD   SYNCED", 9, FontStyle.Regular, Color.FromArgb(118, 143, 161));
            cloud.Location = new Point(234, 48);
            cloud.AutoSize = true;
            actionBar.Controls.Add(cloud);

            Button settingsBtn = MakeButton("SETTINGS", new Size(96, 44), Color.FromArgb(42, 55, 68), 9);
            settingsBtn.Location = new Point(830, 24);
            settingsBtn.Click += delegate { ShowSettingsPage(); };
            actionBar.Controls.Add(settingsBtn);

            Panel content = new Panel();
            content.Location = new Point(220, 460);
            content.Size = new Size(960, 260);
            content.BackColor = Color.FromArgb(20, 31, 43);
            Controls.Add(content);

            Panel activity = new Panel();
            activity.Location = new Point(28, 20);
            activity.Size = new Size(560, 205);
            activity.BackColor = Color.FromArgb(28, 42, 55);
            content.Controls.Add(activity);

            Label activityTitle = MakeLabel("ACTIVITY", 11, FontStyle.Bold, Color.FromArgb(200, 210, 220));
            activityTitle.Location = new Point(18, 14);
            activityTitle.AutoSize = true;
            activity.Controls.Add(activityTitle);

            Label activityName = MakeLabel(Program.GameName, 22, FontStyle.Bold, Color.White);
            activityName.Location = new Point(18, 48);
            activityName.AutoSize = true;
            activity.Controls.Add(activityName);

            Label activityText = MakeLabel("Custom launcher edition for you and your friend.", 10, FontStyle.Regular, Color.FromArgb(166, 180, 193));
            activityText.Location = new Point(20, 86);
            activityText.AutoSize = true;
            activity.Controls.Add(activityText);

            Button steam = MakeButton("STEAM", new Size(112, 36), Color.FromArgb(49, 66, 82), 9);
            steam.Location = new Point(18, 142);
            steam.Click += delegate { OpenSteam(); };
            activity.Controls.Add(steam);

            Button files = MakeButton("GAME FILES", new Size(126, 36), Color.FromArgb(49, 66, 82), 9);
            files.Location = new Point(138, 142);
            files.Click += delegate { OpenGameFiles(); };
            activity.Controls.Add(files);

            Button rescan = MakeButton("RESCAN", new Size(112, 36), Color.FromArgb(49, 66, 82), 9);
            rescan.Location = new Point(272, 142);
            rescan.Click += delegate { Rescan(); };
            activity.Controls.Add(rescan);

            Panel detectCard = new Panel();
            detectCard.Location = new Point(606, 20);
            detectCard.Size = new Size(326, 205);
            detectCard.BackColor = Color.FromArgb(28, 42, 55);
            content.Controls.Add(detectCard);

            Label detectTitle = MakeLabel("SYSTEM STATUS", 11, FontStyle.Bold, Color.FromArgb(200, 210, 220));
            detectTitle.Location = new Point(18, 14);
            detectTitle.AutoSize = true;
            detectCard.Controls.Add(detectTitle);

            steamStatus = MakeStatusLabel(new Point(16, 52), 294);
            cs2Status = MakeStatusLabel(new Point(16, 94), 294);
            editStatus = MakeStatusLabel(new Point(16, 136), 294);
            detectCard.Controls.Add(steamStatus);
            detectCard.Controls.Add(cs2Status);
            detectCard.Controls.Add(editStatus);
        }

        void BuildLibraryPage()
        {
            libraryPage = new Panel();
            libraryPage.Location = new Point(0, 48);
            libraryPage.Size = new Size(1180, 672);
            libraryPage.BackColor = Color.FromArgb(35, 44, 58);
            Controls.Add(libraryPage);

            Label title = MakeLabel(Program.Brand, 20, FontStyle.Regular, Color.FromArgb(222, 226, 230));
            title.Location = new Point(26, 22);
            title.AutoSize = true;
            libraryPage.Controls.Add(title);

            Label count = MakeLabel("(1)", 12, FontStyle.Regular, Color.FromArgb(110, 124, 138));
            count.Location = new Point(250, 28);
            count.AutoSize = true;
            libraryPage.Controls.Add(count);

            Panel line = new Panel();
            line.Location = new Point(26, 58);
            line.Size = new Size(900, 1);
            line.BackColor = Color.FromArgb(77, 88, 102);
            libraryPage.Controls.Add(line);

            Label sort = MakeLabel("SORT BY", 9, FontStyle.Regular, Color.FromArgb(145, 154, 164));
            sort.Location = new Point(26, 82);
            sort.AutoSize = true;
            libraryPage.Controls.Add(sort);

            Button alphabetical = MakeButton("Alphabetical  v", new Size(108, 28), Color.FromArgb(71, 82, 97), 8);
            alphabetical.Location = new Point(90, 75);
            alphabetical.Enabled = false;
            libraryPage.Controls.Add(alphabetical);

            Panel card = new Panel();
            card.Location = new Point(26, 126);
            card.Size = new Size(230, 374);
            card.BackColor = Color.FromArgb(20, 28, 39);
            card.Cursor = Cursors.Hand;
            card.Click += delegate { ShowGamePage(); };
            libraryPage.Controls.Add(card);

            PictureBox cover = new PictureBox();
            cover.Location = new Point(5, 5);
            cover.Size = new Size(220, 330);
            cover.SizeMode = PictureBoxSizeMode.Zoom;
            cover.BackColor = Color.FromArgb(14, 20, 28);
            cover.Image = coverImage;
            cover.Cursor = Cursors.Hand;
            cover.Click += delegate { ShowGamePage(); };
            card.Controls.Add(cover);

            Label gameName = MakeLabel(Program.GameName, 11, FontStyle.Bold, Color.White);
            gameName.Location = new Point(8, 342);
            gameName.AutoSize = true;
            gameName.Cursor = Cursors.Hand;
            gameName.Click += delegate { ShowGamePage(); };
            card.Controls.Add(gameName);

            Label installed = MakeLabel("INSTALLED", 8, FontStyle.Bold,
                detected.Cs2Found ? Color.FromArgb(117, 176, 34) : Color.FromArgb(160, 160, 160));
            installed.Location = new Point(154, 346);
            installed.AutoSize = true;
            card.Controls.Add(installed);

            Label note = MakeLabel("More games can be added later.", 9, FontStyle.Regular, Color.FromArgb(104, 119, 133));
            note.Location = new Point(26, 530);
            note.AutoSize = true;
            libraryPage.Controls.Add(note);
        }

        void BuildSettingsPage()
        {
            settingsPage = new Panel();
            settingsPage.Location = new Point(0, 48);
            settingsPage.Size = new Size(1180, 672);
            settingsPage.BackColor = Color.FromArgb(28, 38, 50);
            settingsPage.Visible = false;
            Controls.Add(settingsPage);

            Label title = MakeLabel("SETTINGS", 21, FontStyle.Bold, Color.FromArgb(222, 226, 230));
            title.Location = new Point(34, 30);
            title.AutoSize = true;
            settingsPage.Controls.Add(title);

            Label subtitle = MakeLabel(Program.Brand + "  v" + Program.Version, 10, FontStyle.Regular, Color.FromArgb(112, 128, 142));
            subtitle.Location = new Point(36, 70);
            subtitle.AutoSize = true;
            settingsPage.Controls.Add(subtitle);

            Panel box = new Panel();
            box.Location = new Point(34, 116);
            box.Size = new Size(892, 170);
            box.BackColor = Color.FromArgb(35, 50, 64);
            settingsPage.Controls.Add(box);

            Label emptyTitle = MakeLabel("Nothing here yet.", 16, FontStyle.Bold, Color.White);
            emptyTitle.Location = new Point(26, 30);
            emptyTitle.AutoSize = true;
            box.Controls.Add(emptyTitle);

            Label emptyText = MakeLabel("This page is ready for future launcher settings.", 10,
                FontStyle.Regular, Color.FromArgb(158, 174, 188));
            emptyText.Location = new Point(28, 70);
            emptyText.AutoSize = true;
            box.Controls.Add(emptyText);

            Label updater = MakeLabel("Updates are checked automatically through GitHub Releases.", 9,
                FontStyle.Regular, Color.FromArgb(102, 192, 244));
            updater.Location = new Point(28, 112);
            updater.AutoSize = true;
            box.Controls.Add(updater);
        }

        void ShowLibraryPage()
        {
            if (settingsPage != null) settingsPage.Visible = false;
            if (gameSidebar != null) gameSidebar.Visible = false;
            if (libraryPage != null)
            {
                libraryPage.Visible = true;
                libraryPage.BringToFront();
            }
        }

        void ShowGamePage()
        {
            if (libraryPage != null) libraryPage.Visible = false;
            if (settingsPage != null) settingsPage.Visible = false;
            if (gameSidebar != null) gameSidebar.Visible = true;
            heroBox.BringToFront();
            if (gameSidebar != null) gameSidebar.BringToFront();
        }

        void ShowSettingsPage()
        {
            if (libraryPage != null) libraryPage.Visible = false;
            if (gameSidebar != null) gameSidebar.Visible = false;
            if (settingsPage != null)
            {
                settingsPage.Visible = true;
                settingsPage.BringToFront();
            }
        }

        void BeginUpdateCheck()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateInfo info = UpdateService.Check();
                if (IsDisposed) return;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (info.Available)
                        {
                            updateButton.Text = "UPDATE v" + info.Version;
                            updateButton.Tag = info;
                            updateButton.Visible = true;
                        }
                    });
                }
                catch { }
            });
        }

        void UpdateClicked(object sender, EventArgs e)
        {
            UpdateInfo info = updateButton.Tag as UpdateInfo;
            if (info == null || !info.Available) return;

            DialogResult result = MessageBox.Show(
                "Update " + Program.Brand + " to v" + info.Version + "?\n\n" +
                "The launcher will restart automatically.",
                Program.Brand,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes) return;
            updateButton.Enabled = false;
            updateButton.Text = "UPDATING...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    UpdateService.Install(info);
                }
                catch (Exception ex)
                {
                    if (IsDisposed) return;
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            updateButton.Enabled = true;
                            updateButton.Text = "UPDATE v" + info.Version;
                            MessageBox.Show("Update failed:\n" + ex.Message, Program.Brand,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    catch { }
                }
            });
        }

        void SelectGame(string gameId)
        {
            if (gameId != "sinochku2") return;
            selectedGameButton.BackColor = Color.FromArgb(46, 69, 91);
        }

        void PaintHeroOverlay(object sender, PaintEventArgs e)
        {
            Rectangle r = heroBox.ClientRectangle;
            using (LinearGradientBrush bottom = new LinearGradientBrush(
                new Rectangle(0, 70, r.Width, 250),
                Color.FromArgb(0, 10, 16, 22),
                Color.FromArgb(235, 14, 23, 32),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(bottom, new Rectangle(0, 70, r.Width, 250));
            }

            using (LinearGradientBrush left = new LinearGradientBrush(
                new Rectangle(0, 0, 520, r.Height),
                Color.FromArgb(175, 8, 13, 19),
                Color.FromArgb(0, 8, 13, 19),
                LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(left, new Rectangle(0, 0, 520, r.Height));
            }
        }

        Label MakeStatusLabel(Point location, int width)
        {
            Label l = MakeLabel("", 9, FontStyle.Bold, Color.White);
            l.Location = location;
            l.Size = new Size(width, 32);
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.Padding = new Padding(10, 0, 4, 0);
            l.BackColor = Color.FromArgb(35, 50, 64);
            return l;
        }

        Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI", size, style);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            return l;
        }

        Button MakeButton(string text, Size size, Color back, float fontSize)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = size;
            b.BackColor = back;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = back == Color.Transparent ? Color.FromArgb(45, 50, 55) : ControlPaint.Light(back, 0.08f);
            b.FlatAppearance.MouseDownBackColor = back == Color.Transparent ? Color.FromArgb(58, 62, 68) : ControlPaint.Dark(back, 0.08f);
            b.Font = new Font("Segoe UI", fontSize, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            return b;
        }

        void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, 0);
        }

        void RefreshDetectionUi()
        {
            SetStatus(steamStatus, "STEAM", detected.SteamFound, detected.SteamFound ? "detected" : "not found");
            SetStatus(cs2Status, "CS2", detected.Cs2Found, detected.Cs2Found ? "AppID 730 detected" : "not installed");
            SetStatus(editStatus, "STEAMEDIT", detected.SteamEditFound, detected.SteamEditFound ? "ready" : "select in Settings");
            RefreshRuntime();
        }

        void SetStatus(Label l, string name, bool ok, string detail)
        {
            l.Text = (ok ? "[OK]  " : "[!]  ") + name + "   " + detail;
            l.ForeColor = ok ? Color.FromArgb(102, 192, 244) : Color.FromArgb(255, 177, 80);
        }

        void RefreshRuntime()
        {
            bool steamRunning = IsRunning("steam");
            bool cs2Running = IsRunning("cs2");
            if (cs2Running)
            {
                runtimeStatus.Text = Program.GameName + " IS RUNNING";
                runtimeStatus.ForeColor = Color.FromArgb(117, 176, 34);
                playButton.Text = "RUNNING";
            }
            else
            {
                runtimeStatus.Text = steamRunning ? "READY TO PLAY" : "STEAM WILL START";
                runtimeStatus.ForeColor = Color.FromArgb(210, 210, 210);
                playButton.Text = "PLAY";
            }
        }

        bool IsRunning(string name)
        {
            try { return Process.GetProcessesByName(name).Length > 0; }
            catch { return false; }
        }

        void PlayClicked(object sender, EventArgs e)
        {
            if (!detected.SteamFound || !detected.Cs2Found)
            {
                MessageBox.Show("Steam or CS2 could not be detected. Try RESCAN or check your Steam installation.", Program.Brand, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!detected.SteamEditFound)
            {
                DialogResult answer = MessageBox.Show("SteamEdit was not found automatically.\n\nChoose it now? The launcher will remember it for this Windows account.", Program.Brand, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer == DialogResult.Yes && BrowseSteamEdit()) Launch();
                return;
            }
            Launch();
        }

        void Launch()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = detected.SteamEditExe;
                psi.Arguments = "-autofix -forcestart -- steam://rungameid/730";
                psi.WorkingDirectory = Path.GetDirectoryName(detected.SteamEditExe);
                psi.UseShellExecute = true;
                Process.Start(psi);
                runtimeStatus.Text = "APPLYING " + Program.GameName + "...";
                runtimeStatus.ForeColor = Color.FromArgb(255, 177, 80);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start SteamEdit:\n" + ex.Message, Program.Brand, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        bool BrowseSteamEdit()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "Locate SteamEdit.exe";
                d.Filter = "SteamEdit|SteamEdit.exe|Executable files|*.exe";
                if (d.ShowDialog(this) != DialogResult.OK) return false;
                settings.SteamEditPath = d.FileName;
                settings.Save();
                detected = Detector.Scan(settings);
                RefreshDetectionUi();
                return detected.SteamEditFound;
            }
        }

        void OpenSteam()
        {
            try { Process.Start("steam://open/main"); }
            catch { if (detected.SteamFound) Process.Start(detected.SteamExe); }
        }

        void OpenGameFiles()
        {
            if (!detected.Cs2Found)
            {
                MessageBox.Show("CS2 folder is not currently detected.", Program.Brand);
                return;
            }
            Process.Start("explorer.exe", "\"" + detected.Cs2Folder + "\"");
        }

        void Rescan()
        {
            detected = Detector.Scan(settings);
            RefreshDetectionUi();
            runtimeStatus.Text = "DETECTION REFRESHED";
        }

        void ShowSettings()
        {
            using (SettingsForm f = new SettingsForm(settings, detected))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    settings = f.Settings;
                    settings.Save();
                    detected = Detector.Scan(settings);
                    RefreshDetectionUi();
                }
            }
        }
    }

    sealed class SettingsForm : Form
    {
        public AppSettings Settings;
        DetectionResult detected;
        TextBox editPath;

        public SettingsForm(AppSettings settings, DetectionResult detected)
        {
            Settings = settings;
            this.detected = detected;
            Text = Program.Brand + " Settings";
            ClientSize = new Size(650, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(22,22,22);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            Build();
        }

        void Build()
        {
            Label title = new Label();
            title.Text = "AUTO-DETECTION & SETTINGS";
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(255,135,20);
            title.AutoSize = true;
            title.Location = new Point(25,22);
            Controls.Add(title);

            AddInfo("Steam", detected.SteamFound ? detected.SteamFolder : "Not detected", 76);
            AddInfo("CS2", detected.Cs2Found ? detected.Cs2Folder : "Not detected", 126);

            Label e = new Label();
            e.Text = "SteamEdit";
            e.Location = new Point(25,183);
            e.Size = new Size(90,25);
            Controls.Add(e);

            editPath = new TextBox();
            editPath.Location = new Point(120,180);
            editPath.Size = new Size(390,27);
            editPath.Text = detected.SteamEditFound ? detected.SteamEditExe : Settings.SteamEditPath;
            Controls.Add(editPath);

            Button browse = Make("BROWSE", new Point(520,178), new Size(100,32));
            browse.Click += delegate {
                using (OpenFileDialog d = new OpenFileDialog())
                {
                    d.Filter = "SteamEdit|SteamEdit.exe|Executable files|*.exe";
                    if (d.ShowDialog(this) == DialogResult.OK) editPath.Text = d.FileName;
                }
            };
            Controls.Add(browse);

            Label note = new Label();
            note.Text = "If SteamEdit is moved later, the launcher automatically rescans common folders.";
            note.ForeColor = Color.FromArgb(145,145,145);
            note.Location = new Point(120,214);
            note.AutoSize = true;
            Controls.Add(note);

            Button save = Make("SAVE", new Point(410,285), new Size(100,42));
            save.BackColor = Color.FromArgb(220,100,0);
            save.Click += delegate {
                if (!String.IsNullOrWhiteSpace(editPath.Text) && !File.Exists(editPath.Text))
                {
                    MessageBox.Show("That SteamEdit path does not exist.", Program.Brand);
                    return;
                }
                Settings.SteamEditPath = editPath.Text.Trim();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(save);

            Button cancel = Make("CANCEL", new Point(520,285), new Size(100,42));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);
        }

        void AddInfo(string key, string value, int y)
        {
            Label k = new Label();
            k.Text = key;
            k.Location = new Point(25,y);
            k.Size = new Size(90,25);
            Controls.Add(k);
            Label v = new Label();
            v.Text = value;
            v.ForeColor = Color.FromArgb(175,175,175);
            v.Location = new Point(120,y);
            v.Size = new Size(495,38);
            Controls.Add(v);
        }

        Button Make(string text, Point p, Size s)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = p;
            b.Size = s;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(48,48,48);
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            return b;
        }
    }
}