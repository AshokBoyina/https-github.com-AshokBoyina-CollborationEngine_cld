namespace NICE.Platform.Collaboration.ChatPlugin;

using System.Drawing.Drawing2D;

/// <summary>
/// Drop-in WinForms control — adds a Floating Action Button (FAB) to any host
/// Windows application.  Clicking it opens/closes <see cref="ChatFloatingForm"/>,
/// a borderless top-level Form that hosts the login card and the WebView2 chat.
///
/// Using a top-level Form (rather than a Panel injected into the host form) gives
/// WebView2 its own well-initialised window and correct SynchronizationContext,
/// which eliminates the "Expecting object to be local" COM threading errors.
/// </summary>
public sealed class ChatLauncherControl : UserControl
{
    // ── NICE brand colours ─────────────────────────────────────────────────
    private static readonly Color NicePrimary = Color.FromArgb(0, 91, 65);   // #005B41
    private static readonly Color NiceError   = Color.FromArgb(220, 53, 69); // #DC3545

    // ── FAB dimensions ─────────────────────────────────────────────────────
    private const int FabSize   = 56;
    private const int BadgeSize = 20;

    // ── Controls ───────────────────────────────────────────────────────────
    private readonly Button _fab;
    private readonly Label  _badge;

    // ── State ──────────────────────────────────────────────────────────────
    private ChatFloatingForm? _floatingForm;
    private ChatPluginConfig? _presetConfig;

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

    // ── FAB paint ──────────────────────────────────────────────────────────

    private void Fab_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = RoundedRect(0, 0, FabSize, FabSize, FabSize / 2);
        g.SetClip(path);
        using var bg = new SolidBrush(NicePrimary);
        g.FillRectangle(bg, 0, 0, FabSize, FabSize);
        g.ResetClip();

        const int bx = 12, by = 11, bw = 32, bh = 26;
        using var bubblePath  = RoundedRect(bx, by, bw, bh, 6);
        using var bubbleBrush = new SolidBrush(Color.White);
        g.FillPath(bubbleBrush, bubblePath);

        var tail = new Point[] {
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

    // ── FAB click ──────────────────────────────────────────────────────────

    private void Fab_Click(object? sender, EventArgs e)
    {
        if (_floatingForm == null || _floatingForm.IsDisposed)
        {
            _floatingForm = new ChatFloatingForm();

            if (_presetConfig is not null)
                _floatingForm.SetConfig(_presetConfig);

            _floatingForm.UnreadCountChanged += (_, count) =>
            {
                UnreadCountChanged?.Invoke(this, count);
                UpdateBadge(count);
            };

            // When the floating form is closed/hidden, clear badge
            _floatingForm.VisibleChanged += (_, _) =>
            {
                if (_floatingForm?.Visible == false)
                    UpdateBadge(0);
            };
        }

        if (_floatingForm.Visible)
        {
            // Window already open — bring it to the front instead of toggling.
            _floatingForm.BringToFront();
            _floatingForm.Activate();
        }
        else
        {
            // Show as an independent taskbar window (no owner form).
            _floatingForm.Show();
            _floatingForm.Activate();
        }
    }

    // ── Badge ──────────────────────────────────────────────────────────────

    private void UpdateBadge(int count)
    {
        if (count <= 0)
        {
            _badge.Visible = false;
            return;
        }
        _badge.Text    = count > 99 ? "99+" : count.ToString();
        _badge.Visible = true;
    }

    // ── Disposal ───────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _floatingForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
