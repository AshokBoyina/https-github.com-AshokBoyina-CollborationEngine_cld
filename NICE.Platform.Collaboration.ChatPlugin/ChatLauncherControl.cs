namespace NICE.Platform.Collaboration.ChatPlugin;

using System.Drawing.Drawing2D;

/// <summary>
/// Drop-in WinForms control — adds a Floating Action Button (FAB) to any host
/// Windows application.
///
/// Clicking the FAB injects a <see cref="ChatBotPanel"/> directly into the host
/// window (same application window — no separate window opens for the bot).
/// The panel appears as an overlay anchored above the FAB, inside the host form.
///
/// Clicking "Talk with internal staff" inside the bot panel opens a separate
/// <see cref="ChatLiveForm"/> window in the Windows taskbar.
/// </summary>
public sealed class ChatLauncherControl : UserControl
{
    // ── NICE brand colours ─────────────────────────────────────────────────
    private static readonly Color NicePrimary = Color.FromArgb(0, 91, 65);
    private static readonly Color NiceError   = Color.FromArgb(220, 53, 69);

    // ── FAB dimensions ─────────────────────────────────────────────────────
    private const int FabSize   = 56;
    private const int BadgeSize = 20;

    // ── Controls ───────────────────────────────────────────────────────────
    private readonly Button _fab;
    private readonly Label  _badge;

    // ── State ──────────────────────────────────────────────────────────────
    private ChatBotPanel?     _botPanel;
    private ChatPluginConfig? _presetConfig;
    private Form?             _hostForm; // cached to unwire Resize on dispose

    // ── Public events ──────────────────────────────────────────────────────
    public event EventHandler<int>? UnreadCountChanged;

    // ── Constructor ────────────────────────────────────────────────────────
    public ChatLauncherControl()
    {
        Size      = new Size(FabSize, FabSize);
        BackColor = Color.Transparent;
        Cursor    = Cursors.Hand;

        _fab = new Button
        {
            Size      = new Size(FabSize, FabSize),
            Location  = Point.Empty,
            BackColor = NicePrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            TabStop   = false
        };
        _fab.FlatAppearance.BorderSize = 0;
        _fab.Paint += Fab_Paint;
        _fab.Click += Fab_Click;

        _badge = new Label
        {
            Size      = new Size(BadgeSize, BadgeSize),
            Location  = new Point(FabSize - BadgeSize + 2, -2),
            BackColor = NiceError,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible   = false
        };

        Controls.Add(_fab);
        Controls.Add(_badge);
        _badge.BringToFront();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void SetConfig(ChatPluginConfig config) => _presetConfig = config;

    // ── FAB click — toggle bot panel inside host form ─────────────────────

    private void Fab_Click(object? sender, EventArgs e)
    {
        var hostForm = FindForm();
        if (hostForm == null) return;

        // Lazily create the bot panel and inject it into the host form's Controls
        if (_botPanel == null || _botPanel.IsDisposed)
        {
            _botPanel = new ChatBotPanel();

            if (_presetConfig != null)
                _botPanel.SetConfig(_presetConfig);

            _botPanel.UnreadCountChanged += (_, count) =>
            {
                UnreadCountChanged?.Invoke(this, count);
                UpdateBadge(count);
            };

            _botPanel.CloseRequested += (_, _) => UpdateBadge(0);

            // Inject into host form so it overlays all other content
            hostForm.Controls.Add(_botPanel);
            _botPanel.BringToFront();

            // Reposition when host form resizes
            _hostForm          = hostForm;
            hostForm.Resize   += RepositionBotPanel;
            hostForm.SizeChanged += RepositionBotPanel;
        }

        if (_botPanel.Visible)
        {
            _botPanel.Hide();
            UpdateBadge(0);
        }
        else
        {
            RepositionBotPanel(null, EventArgs.Empty);
            _botPanel.BringToFront();
            _botPanel.Show();
        }
    }

    // ── Position bot panel above the FAB, inside the host form ────────────

    private void RepositionBotPanel(object? sender, EventArgs e)
    {
        if (_botPanel == null || _botPanel.IsDisposed) return;

        var hostForm = FindForm();
        if (hostForm == null) return;

        // Convert the FAB's screen position to host-form client coordinates
        var fabScreen   = PointToScreen(Point.Empty);
        var fabInClient = hostForm.PointToClient(fabScreen);

        const int Gap = 10;

        // Preferred: align right edge of panel with right edge of FAB,
        //            bottom edge of panel 'Gap' px above top of FAB
        int x = fabInClient.X + FabSize - _botPanel.Width;
        int y = fabInClient.Y - _botPanel.Height - Gap;

        // Clamp within host form's client area
        x = Math.Max(4, Math.Min(x, hostForm.ClientSize.Width  - _botPanel.Width  - 4));
        y = Math.Max(4, Math.Min(y, hostForm.ClientSize.Height - _botPanel.Height - 4));

        _botPanel.Location = new Point(x, y);
    }

    // ── FAB paint (chat-bubble icon) ───────────────────────────────────────

    private void Fab_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var path  = RoundedRect(0, 0, FabSize, FabSize, FabSize / 2);
        g.SetClip(path);
        using var bg = new SolidBrush(NicePrimary);
        g.FillRectangle(bg, 0, 0, FabSize, FabSize);
        g.ResetClip();

        const int bx = 12, by = 11, bw = 32, bh = 26;
        using var bubblePath  = RoundedRect(bx, by, bw, bh, 6);
        using var bubbleBrush = new SolidBrush(Color.White);
        g.FillPath(bubbleBrush, bubblePath);

        var tail = new Point[]
        {
            new(bx + 3, by + bh),
            new(bx + 3, by + bh + 7),
            new(bx + 12, by + bh)
        };
        g.FillPolygon(bubbleBrush, tail);

        using var dotBrush = new SolidBrush(NicePrimary);
        int cy = by + bh / 2;
        g.FillEllipse(dotBrush, bx + 7,  cy - 2, 5, 5);
        g.FillEllipse(dotBrush, bx + 14, cy - 2, 5, 5);
        g.FillEllipse(dotBrush, bx + 21, cy - 2, 5, 5);
    }

    // ── Badge ──────────────────────────────────────────────────────────────

    private void UpdateBadge(int count)
    {
        if (count <= 0) { _badge.Visible = false; return; }
        _badge.Text    = count > 99 ? "99+" : count.ToString();
        _badge.Visible = true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
    {
        var p = new GraphicsPath();
        p.AddArc(x,             y,             r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y,             r * 2, r * 2, 270, 90);
        p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2,   0, 90);
        p.AddArc(x,             y + h - r * 2, r * 2, r * 2,  90, 90);
        p.CloseFigure();
        return p;
    }

    // ── Disposal ───────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_hostForm != null)
            {
                _hostForm.Resize      -= RepositionBotPanel;
                _hostForm.SizeChanged -= RepositionBotPanel;
            }
            _botPanel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
