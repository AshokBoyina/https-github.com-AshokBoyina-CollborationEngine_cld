namespace NICE.Platform.Collaboration.WinApp;

using Microsoft.Extensions.Configuration;
using NICE.Platform.Collaboration.ChatPlugin;

/// <summary>
/// Demo host form — simulates how a real enterprise Windows application
/// (e.g. a CRM or case-management system) integrates <see cref="ChatLauncherControl"/>.
///
/// The FAB (Floating Action Button) sits in the bottom-right corner.
/// Clicking it opens an overlay with a login form; after authentication the
/// user is taken directly into the internal-chat screen — no separate window.
/// </summary>
public sealed class MainForm : Form
{
    private readonly IConfiguration       _config;
    private readonly ChatLauncherControl  _launcher;
    private readonly NotifyIcon           _trayIcon;

    // ── NICE brand colours (matching ChatLauncherControl) ─────────────────
    private static readonly Color BrandPrimary = Color.FromArgb(0, 91, 65);   // #005B41
    private static readonly Color BgSoft       = Color.FromArgb(245, 247, 250); // #f5f7fa
    private static readonly Color BorderSoft   = Color.FromArgb(229, 231, 235); // #e5e7eb
    private static readonly Color TextMuted    = Color.FromArgb(107, 124, 128); // #6b7280

    public MainForm(IConfiguration config)
    {
        _config   = config;
        _launcher = new ChatLauncherControl();

        // ── Form setup ─────────────────────────────────────────────────────
        Text          = "Acme CRM  —  Agent Console";
        Size          = new Size(1100, 720);    // wide enough for the 680px overlay
        MinimumSize   = new Size(800, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor     = BgSoft;

        // ── Tray icon ──────────────────────────────────────────────────────
        _trayIcon = new NotifyIcon
        {
            Text    = "Acme CRM",
            Visible = true,
            Icon    = SystemIcons.Application
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open",  null, (_, _) => ShowWindow());
        trayMenu.Items.Add("Exit",  null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = trayMenu;

        FormClosing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(2000, "Still running",
                "Acme CRM is minimised to the tray.", ToolTipIcon.Info);
        };

        // ── Top bar ────────────────────────────────────────────────────────
        var topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = BrandPrimary,
            Padding   = new Padding(16, 0, 16, 0)
        };

        var appTitle = new Label
        {
            Text      = "Acme CRM",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize  = true,
            Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
        };
        appTitle.Location = new Point(16, 14);

        var navItems = new[] { "Dashboard", "Cases", "Customers", "Reports" };
        int navX = 130;
        foreach (var item in navItems)
        {
            var nav = new Label
            {
                Text      = item,
                ForeColor = Color.FromArgb(200, 255, 255, 255),
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = true,
                Cursor    = Cursors.Hand
            };
            nav.Location = new Point(navX, 17);
            nav.Click   += (_, _) => { /* demo — no real navigation */ };
            topBar.Controls.Add(nav);
            navX += nav.PreferredWidth + 24;
        }

        var userLabel = new Label
        {
            Text      = "Alice Smith  ▾",
            ForeColor = Color.FromArgb(220, 255, 255, 255),
            Font      = new Font("Segoe UI", 8.5f),
            AutoSize  = true,
            Anchor    = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            Cursor    = Cursors.Hand
        };
        userLabel.Location = new Point(topBar.Width - 120, 17);
        topBar.Resize += (_, _) =>
            userLabel.Location = new Point(topBar.Width - 120, 17);

        topBar.Controls.Add(appTitle);
        topBar.Controls.Add(userLabel);

        // ── Sidebar ────────────────────────────────────────────────────────
        var sidebar = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 200,
            BackColor = Color.White
        };

        // Sidebar border
        sidebar.Paint += (_, e) =>
        {
            e.Graphics.DrawLine(new Pen(BorderSoft), sidebar.Width - 1, 0,
                                sidebar.Width - 1, sidebar.Height);
        };

        var sideItems = new[]
        {
            ("📋", "My Cases",        true),
            ("🎯", "Queue",           false),
            ("📊", "Performance",     false),
            ("🔔", "Notifications",   false),
            ("⚙️", "Settings",        false)
        };

        int sy = 16;
        foreach (var (icon, label, active) in sideItems)
        {
            var row = new Panel
            {
                Location  = new Point(0, sy),
                Size      = new Size(200, 40),
                BackColor = active ? Color.FromArgb(242, 247, 245) : Color.White,
                Cursor    = Cursors.Hand
            };
            if (active)
            {
                row.Paint += (_, e) =>
                    e.Graphics.DrawLine(new Pen(BrandPrimary, 3), 0, 0, 0, row.Height);
            }

            var iconLbl = new Label
            {
                Text      = icon,
                Location  = new Point(16, 10),
                AutoSize  = true,
                Font      = new Font("Segoe UI Emoji", 11f)
            };
            var textLbl = new Label
            {
                Text      = label,
                Location  = new Point(44, 12),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9f, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? BrandPrimary : Color.FromArgb(55, 65, 81)
            };
            row.Controls.Add(iconLbl);
            row.Controls.Add(textLbl);
            sidebar.Controls.Add(row);
            sy += 44;
        }

        // ── Main content area ──────────────────────────────────────────────
        var content = new Panel
        {
            Dock    = DockStyle.Fill,
            Padding = new Padding(24)
        };

        var pageTitle = new Label
        {
            Text      = "My Cases",
            Font      = new Font("Segoe UI", 14f, FontStyle.Regular),
            ForeColor = Color.FromArgb(26, 32, 44),
            Location  = new Point(24, 24),
            AutoSize  = true
        };

        var subTitle = new Label
        {
            Text      = "3 active cases assigned to you",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            Location  = new Point(24, 54),
            AutoSize  = true
        };

        // Fake case-list table header
        var tableHeader = new Panel
        {
            Location  = new Point(24, 84),
            Size      = new Size(content.Width - 48, 36),
            BackColor = Color.FromArgb(249, 250, 251),
            Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };
        tableHeader.Paint += (_, e) =>
        {
            e.Graphics.DrawRectangle(new Pen(BorderSoft), 0, 0,
                tableHeader.Width - 1, tableHeader.Height - 1);
        };
        foreach (var (col, x) in new[] { ("Case ID", 12), ("Customer", 100), ("Subject", 280), ("Status", 520), ("Updated", 620) })
        {
            tableHeader.Controls.Add(new Label
            {
                Text      = col,
                Location  = new Point(x, 10),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMuted
            });
        }

        // Fake case rows
        var cases = new[]
        {
            ("#10541", "Troy M.",       "Login issue with survey portal",  "● Open",   "2 min ago"),
            ("#10538", "Alice Smith",   "Report download failing on Edge",  "● Open",   "18 min ago"),
            ("#10529", "Bob Carter",    "Form validation not working",     "● Pending", "1 hr ago")
        };

        var statusColours = new Dictionary<string, Color>
        {
            ["● Open"]    = Color.FromArgb(5, 150, 105),
            ["● Pending"] = Color.FromArgb(217, 119, 6)
        };

        int ry = 120;
        foreach (var (id, customer, subject, status, updated) in cases)
        {
            var row = new Panel
            {
                Location  = new Point(24, ry),
                Size      = new Size(content.Width - 48, 42),
                BackColor = Color.White,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
                Cursor    = Cursors.Hand
            };
            row.Paint += (_, e) =>
                e.Graphics.DrawRectangle(new Pen(BorderSoft), 0, 0,
                    row.Width - 1, row.Height - 1);

            void AddCell(string text, int x, Color? colour = null)
            {
                row.Controls.Add(new Label
                {
                    Text      = text,
                    Location  = new Point(x, 13),
                    AutoSize  = true,
                    Font      = new Font("Segoe UI", 8.5f),
                    ForeColor = colour ?? Color.FromArgb(55, 65, 81)
                });
            }

            AddCell(id,       12);
            AddCell(customer, 100);
            AddCell(subject,  280);
            AddCell(status,   520, statusColours.GetValueOrDefault(status));
            AddCell(updated,  620, TextMuted);

            content.Controls.Add(row);
            ry += 44;
        }

        content.Controls.Add(pageTitle);
        content.Controls.Add(subTitle);
        content.Controls.Add(tableHeader);

        // ── FAB launcher — anchored bottom-right ───────────────────────────
        _launcher.Anchor   = AnchorStyles.Bottom | AnchorStyles.Right;
        _launcher.Location = new Point(
            content.Width  - _launcher.Width  - 24,
            content.Height - _launcher.Height - 24);

        // Pre-supply server config from appsettings so the user only needs
        // to paste their access token in the overlay (or leave it blank
        // for auto-connect via the demo mint-token endpoint).
        _launcher.SetConfig(new ChatPluginConfig
        {
            ChatUiBaseUrl   = _config["ChatPlugin:ChatUiBaseUrl"]   ?? "http://localhost:5200",
            ApiBaseUrl      = _config["ChatPlugin:ApiBaseUrl"]      ?? "http://localhost:65168",
            ApiKey          = _config["ChatPlugin:ApiKey"]          ?? string.Empty,
            ApplicationName = _config["ChatPlugin:ApplicationName"] ?? "SurveyPortal",
            UserRole        = _config["ChatPlugin:DefaultRole"]     ?? "Internal"
        });

        // Wire unread count to tray balloon
        _launcher.UnreadCountChanged += (_, count) =>
        {
            if (count > 0)
                _trayIcon.ShowBalloonTip(2000, "New messages",
                    $"You have {count} unread internal message{(count == 1 ? "" : "s")}.",
                    ToolTipIcon.Info);
        };

        content.Controls.Add(_launcher);

        // ── Compose ────────────────────────────────────────────────────────
        Controls.Add(content);
        Controls.Add(sidebar);
        Controls.Add(topBar);
    }

    // ── Tray helpers ───────────────────────────────────────────────────────

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _launcher.Dispose();
        }
        base.Dispose(disposing);
    }
}
