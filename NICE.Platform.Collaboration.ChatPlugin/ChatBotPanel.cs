namespace NICE.Platform.Collaboration.ChatPlugin;

using System.Drawing.Drawing2D;

/// <summary>
/// Bot-chat UserControl that is injected directly into the host WinForms application
/// window — it appears as an overlay panel inside the same window (not a separate window).
///
/// Clicking "Talk with internal staff" opens a separate <see cref="ChatLiveForm"/> window
/// in the Windows taskbar.
/// </summary>
public sealed class ChatBotPanel : UserControl
{
    // ── Fixed size — panel does not resize with the host form ──────────────
    private const int PanelW  = 380;
    private const int PanelH  = 520;
    private const int HeaderH = 44;

    // ── NICE brand colours ─────────────────────────────────────────────────
    private static readonly Color NicePrimary       = Color.FromArgb(0,   91,  65);
    private static readonly Color NicePrimaryStrong = Color.FromArgb(0,   55,  39);
    private static readonly Color NiceBorder        = Color.FromArgb(180, 195, 190);
    private static readonly Color NiceText          = Color.FromArgb(26,  32,  44);
    private static readonly Color BotBubbleBg       = Color.FromArgb(237, 242, 248);

    private static readonly Font BubbleFont = new("Segoe UI", 9.5f);

    // ── Controls ───────────────────────────────────────────────────────────
    private readonly Panel   _msgScroll;
    private readonly Button  _escalateBtn;
    private readonly TextBox _msgInput;

    // ── Bot state ──────────────────────────────────────────────────────────
    private int  _nextMsgY = 10;
    private bool _greeted;
    private bool _escalated;

    // ── Live-chat companion (separate window) ──────────────────────────────
    private ChatLiveForm?     _liveForm;
    private ChatPluginConfig? _presetConfig;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler?    CloseRequested;
    public event EventHandler<int>? UnreadCountChanged;

    // ── Constructor ────────────────────────────────────────────────────────
    public ChatBotPanel()
    {
        Size        = new Size(PanelW, PanelH);
        BackColor   = Color.White;
        BorderStyle = BorderStyle.None; // painted manually
        Visible     = false;

        // ── Green header ───────────────────────────────────────────────────
        var header = new Panel
        {
            Location  = Point.Empty,
            Size      = new Size(PanelW, HeaderH),
            BackColor = NicePrimary
        };

        var title = new Label
        {
            Text      = "NICE Internal Chat",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10f, FontStyle.Regular),
            AutoSize  = true,
            Location  = new Point(12, 12)
        };

        var closeBtn = new Button
        {
            Text      = "✕",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(32, 32),
            Location  = new Point(PanelW - 38, 6),
            Font      = new Font("Segoe UI", 9f),
            Cursor    = Cursors.Hand,
            TabStop   = false
        };
        closeBtn.FlatAppearance.BorderSize         = 0;
        closeBtn.FlatAppearance.MouseOverBackColor = NicePrimaryStrong;
        closeBtn.Click += (_, _) => { Hide(); CloseRequested?.Invoke(this, EventArgs.Empty); };

        header.Controls.Add(title);
        header.Controls.Add(closeBtn);

        // ── Scrollable message area ────────────────────────────────────────
        _msgScroll = new Panel
        {
            Location   = new Point(0, HeaderH),
            Size       = new Size(PanelW, PanelH - HeaderH - 100),
            AutoScroll = true,
            BackColor  = Color.White,
            Anchor     = AnchorStyles.Top | AnchorStyles.Left
        };

        // ── Input area ─────────────────────────────────────────────────────
        int inputTop = PanelH - 100;
        var inputArea = new Panel
        {
            Location  = new Point(0, inputTop),
            Size      = new Size(PanelW, 100),
            BackColor = Color.White
        };
        inputArea.Paint += (_, e) =>
            e.Graphics.DrawLine(new Pen(Color.FromArgb(229, 231, 235)), 0, 0, inputArea.Width, 0);

        // "Talk with internal staff" button
        _escalateBtn = new Button
        {
            Location  = new Point(10, 10),
            Size      = new Size(PanelW - 20, 38),
            Text      = "💬  Talk with internal staff",
            BackColor = NicePrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Cursor    = Cursors.Hand,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _escalateBtn.FlatAppearance.BorderSize         = 0;
        _escalateBtn.FlatAppearance.MouseOverBackColor = NicePrimaryStrong;
        _escalateBtn.Click += (_, _) => _ = EscalateAsync();

        // Text input
        _msgInput = new TextBox
        {
            Location        = new Point(10, 58),
            Size            = new Size(PanelW - 84, 30),
            Font            = new Font("Segoe UI", 9f),
            PlaceholderText = "Type a message…",
            BorderStyle     = BorderStyle.FixedSingle,
            Anchor          = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _msgInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                _ = HandleSendAsync();
            }
        };

        // Send button
        var sendBtn = new Button
        {
            Location  = new Point(PanelW - 70, 58),
            Size      = new Size(60, 30),
            Text      = "Send",
            BackColor = NicePrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f),
            Cursor    = Cursors.Hand,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right
        };
        sendBtn.FlatAppearance.BorderSize         = 0;
        sendBtn.FlatAppearance.MouseOverBackColor = NicePrimaryStrong;
        sendBtn.Click += (_, _) => _ = HandleSendAsync();

        inputArea.Controls.AddRange([_escalateBtn, _msgInput, sendBtn]);

        Controls.Add(header);
        Controls.Add(_msgScroll);
        Controls.Add(inputArea);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void SetConfig(ChatPluginConfig config) => _presetConfig = config;

    // ── Visibility — post greeting on first show ───────────────────────────

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible && !_greeted)
        {
            _greeted = true;
            AddBubble("👋 Hi! I'm the NICE Internal Assistant. How can I help you today?", fromUser: false);
            AddBubble("Type your question below, or click \"Talk with internal staff\" to connect with the team.", fromUser: false);

            // Pre-warm the live-chat window while the user reads the greeting
            EnsureLiveForm();
            _liveForm!.CreateControl();
            _ = _liveForm.PreWarmAsync();
        }
    }

    // ── Border paint ───────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // 1 px border + soft drop shadow suggestion via darker border
        using var pen = new Pen(NiceBorder, 1f);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    // ── Live-form helpers ──────────────────────────────────────────────────

    private void EnsureLiveForm()
    {
        if (_liveForm is { IsDisposed: false }) return;

        _liveForm = new ChatLiveForm();
        if (_presetConfig != null) _liveForm.SetConfig(_presetConfig);

        _liveForm.UnreadCountChanged += (_, n) => UnreadCountChanged?.Invoke(this, n);
        _liveForm.LiveWindowHidden   += (_, _) =>
        {
            // Re-enable the escalate button so the user can reconnect
            if (_escalated)
            {
                _escalated             = false;
                _escalateBtn.Enabled   = true;
                _escalateBtn.Text      = "💬  Talk with internal staff";
                _escalateBtn.BackColor = NicePrimary;
            }
        };
    }

    // ── Bot chat ───────────────────────────────────────────────────────────

    private async Task HandleSendAsync()
    {
        var text = _msgInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        _msgInput.Clear();
        AddBubble(text, fromUser: true);

        await Task.Delay(500);
        AddBubble(GetBotReply(text), fromUser: false);
    }

    private static readonly (string[] Keywords, string Reply)[] BotScript =
    [
        (["hi", "hello", "hey"],
            "Hello! 😊 How can I assist you today?"),
        (["case", "ticket", "ref", "reference"],
            "I can help with case queries. What's the case ID, or can you describe the problem?"),
        (["login", "password", "access", "locked", "sign in"],
            "For access issues, try resetting your password first. Our internal staff can also unlock your account — click the button below."),
        (["report", "download", "export", "csv", "pdf"],
            "For report or download issues, describe what's happening. Or connect with the team directly."),
        (["error", "bug", "broken", "fail", "crash", "not working", "problem"],
            "Sorry to hear that! Describe the error and I'll try to help, or our internal staff can look into it."),
        (["transfer", "escalate", "human", "person", "staff", "team", "connect", "talk"],
            "Sure! Click \"Talk with internal staff\" below and I'll open the live chat. 👇"),
        (["thanks", "thank you", "thx", "cheers"],
            "You're welcome! Anything else I can help with?"),
        (["bye", "goodbye", "done"],
            "Goodbye! Come back anytime. 👋"),
        (["slow", "performance", "lag"],
            "Performance issues can have many causes. Which page or feature is slow?"),
        (["survey", "form"],
            "For survey or form issues, which one and what's going wrong?"),
    ];

    private static string GetBotReply(string input)
    {
        var lower = input.ToLowerInvariant();
        foreach (var (kws, reply) in BotScript)
            if (kws.Any(k => lower.Contains(k)))
                return reply;

        return "I understand! For faster help, click \"Talk with internal staff\" below. 👇";
    }

    // ── Escalation ─────────────────────────────────────────────────────────

    private async Task EscalateAsync()
    {
        if (_escalated) return;
        _escalated           = true;
        _escalateBtn.Enabled = false;
        _escalateBtn.Text    = "Connecting…";

        try
        {
            EnsureLiveForm();

            var role    = _presetConfig?.UserRole   ?? "Internal";
            var apiBase = _presetConfig?.ApiBaseUrl ?? "http://localhost:65168";
            var mintUrl = $"{apiBase}/api/v1/demo/mint-token" +
                          $"?name=Alice&role={Uri.EscapeDataString(role)}";

            var resp = await Http.PostAsync(mintUrl, null);
            resp.EnsureSuccessStatusCode();
            var token = (await resp.Content.ReadAsStringAsync()).Trim().Trim('"');

            var config = new ChatPluginConfig
            {
                ChatUiBaseUrl   = _presetConfig?.ChatUiBaseUrl   ?? "http://localhost:5200",
                ApiBaseUrl      = apiBase,
                ApiKey          = _presetConfig?.ApiKey          ?? string.Empty,
                ApplicationName = _presetConfig?.ApplicationName ?? "SurveyPortal",
                AccessToken     = token,
                UserRole        = role
            };

            AddBubble("Opening live chat in a new window… 🟢", fromUser: false);
            await _liveForm!.NavigateAndShowAsync(config);

            _escalateBtn.Text      = "✅  Connected to internal staff";
            _escalateBtn.BackColor = Color.FromArgb(22, 163, 74);
        }
        catch (Exception ex)
        {
            _escalated             = false;
            _escalateBtn.Enabled   = true;
            _escalateBtn.Text      = "💬  Talk with internal staff";
            _escalateBtn.BackColor = NicePrimary;
            AddBubble($"⚠️ Couldn't connect: {ex.Message}. Please try again.", fromUser: false);
        }
    }

    // ── Bubble rendering ───────────────────────────────────────────────────

    private void AddBubble(string text, bool fromUser)
    {
        const int HPad = 11, VPad = 7, MaxBW = 320, Gap = 8;

        var sz = TextRenderer.MeasureText(text, BubbleFont,
            new Size(MaxBW - HPad * 2, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.Left);

        int bw = Math.Min(sz.Width + HPad * 2 + 4, MaxBW);
        int bh = sz.Height + VPad * 2 + 2;

        int scrollW = _msgScroll.ClientSize.Width > 0
            ? _msgScroll.ClientSize.Width
            : PanelW - SystemInformation.VerticalScrollBarWidth - 2;

        int bx = fromUser ? Math.Max(Gap, scrollW - bw - Gap) : Gap;

        var capturedText = text;
        var capturedUser = fromUser;

        var bubble = new Panel
        {
            Location  = new Point(bx, _nextMsgY),
            Size      = new Size(bw, bh),
            BackColor = fromUser ? NicePrimary : BotBubbleBg
        };
        bubble.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundedRect(0, 0, bubble.Width - 1, bubble.Height - 1, 10);
            using var brush = new SolidBrush(bubble.BackColor);
            g.FillPath(brush, path);
            TextRenderer.DrawText(g, capturedText, BubbleFont,
                new Rectangle(HPad, VPad, bubble.Width - HPad * 2, bubble.Height - VPad * 2),
                capturedUser ? Color.White : NiceText,
                TextFormatFlags.WordBreak | TextFormatFlags.Left);
        };

        _nextMsgY += bh + 7;
        _msgScroll.Controls.Add(bubble);
        _msgScroll.AutoScrollMinSize = new Size(1, _nextMsgY + 10);

        if (IsHandleCreated)
            _msgScroll.AutoScrollPosition = new Point(0, _nextMsgY);
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
            _liveForm?.Dispose();
            BubbleFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
