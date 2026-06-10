namespace NICE.Platform.Collaboration.ChatPlugin;

using System.Drawing.Drawing2D;

/// <summary>
/// Compact bot-chat window — opens when the FAB is clicked.
/// Shows a WinForms-native bubble chat driven by NICE Internal Assistant.
///
/// When the user clicks "Talk with internal staff", a separate <see cref="ChatLiveForm"/>
/// window opens in the taskbar with the full Blazor WASM collaboration chat.
/// This window stays open so the user can see the bot conversation history.
///
/// No WebView2 is used here — the live-chat window owns that.
/// </summary>
public sealed class ChatFloatingForm : Form
{
    // ── Dimensions (compact chat-widget size) ──────────────────────────────
    private const int W = 420;
    private const int H = 560;

    // ── NICE brand colours ─────────────────────────────────────────────────
    private static readonly Color NicePrimary       = Color.FromArgb(0,   91,  65);
    private static readonly Color NicePrimaryStrong = Color.FromArgb(0,   55,  39);
    private static readonly Color NiceBorder        = Color.FromArgb(209, 213, 219);
    private static readonly Color NiceText          = Color.FromArgb(26,  32,  44);
    private static readonly Color BotBubbleBg       = Color.FromArgb(237, 242, 248);

    private static readonly Font BubbleFont = new("Segoe UI", 9.5f);

    // ── Controls ───────────────────────────────────────────────────────────
    private readonly Panel   _msgScroll;
    private readonly Button  _escalateBtn;
    private readonly TextBox _msgInput;

    // ── State ──────────────────────────────────────────────────────────────
    private int  _nextMsgY  = 12;
    private bool _greeted;
    private bool _escalated; // prevent double-escalation

    // ── Live-chat companion window ─────────────────────────────────────────
    private ChatLiveForm?     _liveForm;
    private ChatPluginConfig? _presetConfig;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public event EventHandler<int>? UnreadCountChanged;

    // ── Constructor ────────────────────────────────────────────────────────
    public ChatFloatingForm()
    {
        Text            = "NICE – Internal Chat";
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar   = true;
        Size            = new Size(W, H);
        MinimumSize     = new Size(340, 400);
        BackColor       = Color.White;
        StartPosition   = FormStartPosition.Manual;
        Load           += PositionOnFirstLoad;

        // ── Scrollable message area ────────────────────────────────────────
        _msgScroll = new Panel
        {
            Dock       = DockStyle.Fill,
            AutoScroll = true,
            BackColor  = Color.White
        };

        // ── Input area (escalate button + text input + send) ───────────────
        var inputTable = BuildInputArea(out _escalateBtn, out _msgInput);

        Controls.Add(_msgScroll);
        Controls.Add(inputTable); // DockStyle.Bottom auto-placed below Fill
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void SetConfig(ChatPluginConfig config) => _presetConfig = config;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
        base.OnFormClosing(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // Post greeting once the layout is complete (so _msgScroll.ClientSize is valid)
        if (!_greeted)
        {
            _greeted = true;
            AddBubble("👋 Hi! I'm the NICE Internal Assistant. How can I help you today?", fromUser: false);
            AddBubble("Type your question below, or click \"Talk with internal staff\" to connect with the team directly.", fromUser: false);
        }

        // Create the live-chat window and pre-warm WebView2 in the background.
        // It stays hidden until the user escalates.
        EnsureLiveForm();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _liveForm?.Dispose();
            BubbleFont.Dispose();
        }
        base.Dispose(disposing);
    }

    // ── Live-form management ───────────────────────────────────────────────

    private void EnsureLiveForm()
    {
        if (_liveForm is { IsDisposed: false }) return;

        _liveForm = new ChatLiveForm();

        if (_presetConfig is not null)
            _liveForm.SetConfig(_presetConfig);

        // Forward unread-count changes to the host application
        _liveForm.UnreadCountChanged += (_, n) => UnreadCountChanged?.Invoke(this, n);

        // Re-enable the escalate button if the user closes the live window
        _liveForm.LiveWindowHidden += (_, _) =>
        {
            if (_escalated)
            {
                _escalated            = false;
                _escalateBtn.Enabled  = true;
                _escalateBtn.Text     = "💬  Talk with internal staff";
                _escalateBtn.BackColor = NicePrimary;
            }
        };

        // Pre-warm WebView2 while the user chats with the bot.
        // Handle is forced so EnsureCoreWebView2Async can run without Show().
        _liveForm.CreateControl();
        _ = _liveForm.PreWarmAsync();
    }

    // ── Bot chat ───────────────────────────────────────────────────────────

    private async Task HandleSendAsync()
    {
        var text = _msgInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        _msgInput.Clear();
        AddBubble(text, fromUser: true);

        await Task.Delay(500); // typing delay
        AddBubble(GetBotReply(text), fromUser: false);
    }

    private static readonly (string[] Keywords, string Reply)[] BotScript =
    [
        (["hi", "hello", "hey"],
            "Hello! 😊 How can I assist you today?"),
        (["case", "ticket", "ref", "reference"],
            "I can help with case queries. What's the case ID, or can you describe the problem?"),
        (["login", "password", "access", "locked", "sign in"],
            "For access issues, try resetting your password first. Our internal staff can unlock your account — click the button below."),
        (["report", "download", "export", "csv", "pdf"],
            "For report or download issues, please describe what's happening. Or I can connect you with the team."),
        (["error", "bug", "broken", "fail", "crash", "not working", "problem"],
            "Sorry to hear that! Can you describe the error? I'll try to help, or connect you with internal staff."),
        (["transfer", "escalate", "human", "person", "staff", "team", "connect", "talk"],
            "Sure! Click \"Talk with internal staff\" below and I'll open the live chat right away. 👇"),
        (["thanks", "thank you", "thx", "cheers"],
            "You're welcome! Is there anything else I can help with?"),
        (["bye", "goodbye", "done"],
            "Goodbye! Feel free to come back anytime. 👋"),
        (["slow", "performance", "lag"],
            "Performance issues can have many causes. Which page or feature feels slow?"),
        (["survey", "form", "questionnaire"],
            "For survey or form issues, let me know which one and what's going wrong."),
    ];

    private static string GetBotReply(string input)
    {
        var lower = input.ToLowerInvariant();
        foreach (var (kws, reply) in BotScript)
            if (kws.Any(k => lower.Contains(k)))
                return reply;

        return "I understand! If you need faster help, our internal staff are available — " +
               "click \"Talk with internal staff\" below. 👇";
    }

    // ── Escalation ─────────────────────────────────────────────────────────

    private async Task EscalateToInternalStaff()
    {
        if (_escalated) return;
        _escalated           = true;
        _escalateBtn.Enabled = false;
        _escalateBtn.Text    = "Connecting…";

        try
        {
            EnsureLiveForm(); // safety — in case form was disposed

            var role    = _presetConfig?.UserRole   ?? "Internal";
            var apiBase = _presetConfig?.ApiBaseUrl ?? "http://localhost:65168";

            // Mint demo token
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

            AddBubble("Opening live chat with internal staff in a new window… 🟢", fromUser: false);

            await _liveForm!.NavigateAndShowAsync(config);

            // Update button to show connected state
            _escalateBtn.Text      = "✅  Connected to internal staff";
            _escalateBtn.BackColor = Color.FromArgb(22, 163, 74); // green-600
        }
        catch (Exception ex)
        {
            _escalated            = false;
            _escalateBtn.Enabled  = true;
            _escalateBtn.Text     = "💬  Talk with internal staff";
            _escalateBtn.BackColor = NicePrimary;
            AddBubble($"⚠️ Couldn't connect: {ex.Message}. Please try again.", fromUser: false);
        }
    }

    // ── Input area builder ─────────────────────────────────────────────────

    private TableLayoutPanel BuildInputArea(out Button escalateBtn, out TextBox msgInput)
    {
        var table = new TableLayoutPanel
        {
            Dock        = DockStyle.Bottom,
            Height      = 104,
            ColumnCount = 1,
            RowCount    = 2,
            BackColor   = Color.White,
            Padding     = new Padding(10, 10, 10, 10)
        };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // escalate btn
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // input row

        table.Paint += (_, e) =>
            e.Graphics.DrawLine(new Pen(NiceBorder), 0, 0, table.Width, 0);

        // "Talk with internal staff" button
        escalateBtn = new Button
        {
            Dock      = DockStyle.Fill,
            Text      = "💬  Talk with internal staff",
            BackColor = NicePrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 0, 6)
        };
        escalateBtn.FlatAppearance.BorderSize         = 0;
        escalateBtn.FlatAppearance.MouseOverBackColor = NicePrimaryStrong;
        escalateBtn.Click += (_, _) => _ = EscalateToInternalStaff();
        table.Controls.Add(escalateBtn, 0, 0);

        // Input row: [TextBox | Send]
        var inputRow = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = Color.White,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty
        };
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));

        msgInput = new TextBox
        {
            Dock            = DockStyle.Fill,
            Font            = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Type a message…",
            BorderStyle     = BorderStyle.FixedSingle,
            Margin          = new Padding(0, 0, 4, 0)
        };
        msgInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                _ = HandleSendAsync();
            }
        };

        var sendBtn = new Button
        {
            Dock      = DockStyle.Fill,
            Text      = "Send",
            BackColor = NicePrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f),
            Cursor    = Cursors.Hand,
            Margin    = Padding.Empty
        };
        sendBtn.FlatAppearance.BorderSize         = 0;
        sendBtn.FlatAppearance.MouseOverBackColor = NicePrimaryStrong;
        sendBtn.Click += (_, _) => _ = HandleSendAsync();

        inputRow.Controls.Add(msgInput, 0, 0);
        inputRow.Controls.Add(sendBtn, 1, 0);
        table.Controls.Add(inputRow, 0, 1);

        return table;
    }

    // ── Bubble rendering ───────────────────────────────────────────────────

    private void AddBubble(string text, bool fromUser)
    {
        const int HPad = 12, VPad = 8, MaxBW = 340, Gap = 10;

        var sz = TextRenderer.MeasureText(text, BubbleFont,
            new Size(MaxBW - HPad * 2, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.Left);

        int bw = Math.Min(sz.Width + HPad * 2 + 4, MaxBW);
        int bh = sz.Height + VPad * 2 + 2;

        int scrollW = _msgScroll.ClientSize.Width > 0
            ? _msgScroll.ClientSize.Width
            : W - SystemInformation.VerticalScrollBarWidth - 2;

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

        _nextMsgY += bh + 8;
        _msgScroll.Controls.Add(bubble);
        _msgScroll.AutoScrollMinSize = new Size(1, _nextMsgY + 12);

        if (IsHandleCreated)
            _msgScroll.AutoScrollPosition = new Point(0, _nextMsgY);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void PositionOnFirstLoad(object? sender, EventArgs e)
    {
        Load -= PositionOnFirstLoad;
        var screen = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Location = new Point(
            Math.Max(screen.Left, screen.Right  - Width  - 32),
            Math.Max(screen.Top,  screen.Bottom - Height - 32));
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
}
