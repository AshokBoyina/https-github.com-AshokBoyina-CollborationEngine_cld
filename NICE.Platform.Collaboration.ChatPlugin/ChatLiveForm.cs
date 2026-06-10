namespace NICE.Platform.Collaboration.ChatPlugin;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

/// <summary>
/// Full-size live-chat window that opens as a separate taskbar entry when the user
/// clicks "Talk with internal staff" in <see cref="ChatFloatingForm"/>.
///
/// Lifecycle:
///   1. Created (hidden) by ChatFloatingForm.OnShown — WebView2 begins pre-warming.
///   2. <see cref="NavigateAndShowAsync"/> is called when the user escalates — the
///      window becomes visible and Blazor WASM loads.
///   3. OS ✕ button hides the window (state preserved); ChatFloatingForm re-enables
///      the escalate button via the <see cref="LiveWindowHidden"/> event.
/// </summary>
public sealed class ChatLiveForm : Form
{
    // ── Dimensions ─────────────────────────────────────────────────────────
    private const int W = 820;
    private const int H = 680;

    // ── NICE brand colours ─────────────────────────────────────────────────
    private static readonly Color NicePrimarySoft = Color.FromArgb(242, 247, 245);
    private static readonly Color NiceTextMuted   = Color.FromArgb(107, 124, 128);

    // ── Controls ───────────────────────────────────────────────────────────
    private readonly WebView2 _webView;
    private readonly Panel    _loadingPanel;

    // ── WebView2 state ─────────────────────────────────────────────────────
    private bool       _webViewReady;
    private bool       _webViewInitStarted;
    private Exception? _webViewInitError;
    private bool       _navPending;

    // ── Plugin config ──────────────────────────────────────────────────────
    private ChatPluginConfig? _presetConfig;

    // ── Public events ──────────────────────────────────────────────────────
    /// <summary>Raised when the user closes (hides) this window.</summary>
    public event EventHandler? LiveWindowHidden;

    /// <summary>Raised when the Blazor app posts an unread-count message.</summary>
    public event EventHandler<int>? UnreadCountChanged;

    // ── Constructor ────────────────────────────────────────────────────────
    public ChatLiveForm()
    {
        Text            = "NICE – Internal Staff Chat";
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar   = true;
        Size            = new Size(W, H);
        MinimumSize     = new Size(540, 500);
        BackColor       = Color.White;
        StartPosition   = FormStartPosition.Manual;

        // WebView2 — hidden until navigation completes
        _webView = new WebView2 { Dock = DockStyle.Fill, Visible = false };

        // Loading panel — shown while connecting / navigating
        _loadingPanel = BuildLoadingPanel();

        // Z-order: loadingPanel in front, webView behind
        Controls.Add(_loadingPanel);
        Controls.Add(_webView);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void SetConfig(ChatPluginConfig config) => _presetConfig = config;

    /// <summary>
    /// Pre-warms the WebView2 environment without showing the window.
    /// Called by ChatFloatingForm.OnShown so WebView2 is ready before escalation.
    /// </summary>
    public async Task PreWarmAsync()
    {
        if (_webViewInitStarted) return;
        _webViewInitStarted = true;

        // CreateControl() ensures HWNDs exist without making the form visible.
        if (!IsHandleCreated)
            CreateControl();

        await InitWebViewAsync();
    }

    /// <summary>
    /// Mints a token (already done by caller), navigates to the Blazor chat URL,
    /// positions and shows this window as a separate taskbar entry.
    /// </summary>
    public async Task NavigateAndShowAsync(ChatPluginConfig config)
    {
        _loadingPanel.Visible = true;
        _webView.Visible      = false;

        // Position to the left of the screen (so it doesn't cover the bot window)
        PositionWindow();
        Show();
        Activate();

        // Wait for pre-warm to finish if it hasn't yet
        if (!_webViewReady)
        {
            if (!_webViewInitStarted)
            {
                _webViewInitStarted = true;
                await InitWebViewAsync();
            }
            else
            {
                for (int i = 0; i < 24 && !_webViewReady && _webViewInitError is null; i++)
                    await Task.Delay(500);
            }
        }

        if (_webViewInitError is { } err)
        {
            ShowError($"WebView2 unavailable [{err.GetType().Name} 0x{err.HResult:X8}]:\n{err.Message}");
            return;
        }

        if (!_webViewReady)
        {
            ShowError("WebView2 took too long to initialise. Please close and retry.");
            return;
        }

        _navPending = true;
        _webView.CoreWebView2.Navigate(BuildChatUrl(config));
    }

    // ── WebView2 initialisation ────────────────────────────────────────────

    private async Task InitWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: Path.Combine(Path.GetTempPath(), "NICE.Chat.WebView2"));

            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.WebMessageReceived  += OnWebMessageReceived;
            _webView.CoreWebView2.NavigationCompleted += OnCoreNavigationCompleted;
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled            = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled            = true;

            _webViewReady = true;
        }
        catch (Exception ex)
        {
            _webViewInitError   = ex;
            _webViewInitStarted = false; // allow retry
        }
    }

    // ── Navigation completed ───────────────────────────────────────────────

    private void OnCoreNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!_navPending) return;
        _navPending = false;

        if (InvokeRequired) Invoke(() => OnNavDone(args.IsSuccess));
        else OnNavDone(args.IsSuccess);
    }

    private void OnNavDone(bool success)
    {
        _loadingPanel.Visible = false;

        if (success)
        {
            _webView.Visible = true;
        }
        else
        {
            ShowError("Could not reach the chat server — is ChatUI running on port 5200?");
        }
    }

    // ── Web message handler ────────────────────────────────────────────────

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson);
            if (json.RootElement.TryGetProperty("type", out var t) &&
                t.GetString() == "unreadCount" &&
                json.RootElement.TryGetProperty("value", out var v))
                UnreadCountChanged?.Invoke(this, v.GetInt32());
        }
        catch { }
    }

    // ── Window lifecycle ───────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            LiveWindowHidden?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.OnFormClosing(e);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void PositionWindow()
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;

        // Place to the left of the bot window (which is at bottom-right).
        // Bot window: ~420 wide at screen.Right - 32.
        int botWindowLeft = screen.Right - 420 - 32;
        int x = Math.Max(screen.Left + 16, botWindowLeft - Width - 16);
        int y = Math.Max(screen.Top  + 16, screen.Bottom - Height - 32);
        Location = new Point(x, y);
    }

    private void ShowError(string message)
    {
        _loadingPanel.Controls.Clear();
        var lbl = new Label
        {
            Text      = message,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(220, 53, 69),
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Fill
        };
        _loadingPanel.Controls.Add(lbl);
        _loadingPanel.Visible = true;
        _webView.Visible      = false;
    }

    private static Panel BuildLoadingPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = NicePrimarySoft,
            Visible   = false
        };

        panel.Controls.Add(new Label
        {
            Text = "⏳", Font = new Font("Segoe UI Emoji", 28f),
            AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(W, 64)
        });
        panel.Controls.Add(new Label
        {
            Text = "Connecting you with internal staff…",
            Font = new Font("Segoe UI", 10.5f), ForeColor = NiceTextMuted,
            AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(W, 32)
        });
        panel.Controls.Add(new Label
        {
            Text = "Loading Blazor WASM — first load may take 10–15 seconds.",
            Font = new Font("Segoe UI", 8f), ForeColor = NiceTextMuted,
            AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(W, 24)
        });

        panel.Layout += (s, _) =>
        {
            if (s is not Panel p) return;
            var labels = p.Controls.OfType<Label>().ToArray();
            int total  = labels.Sum(l => l.Height) + (labels.Length - 1) * 10;
            int y      = Math.Max(0, (p.Height - total) / 2);
            foreach (var lbl in labels)
            {
                lbl.Location = new Point(0, y);
                lbl.Width    = p.Width;
                y += lbl.Height + 10;
            }
        };

        return panel;
    }

    private static string BuildChatUrl(ChatPluginConfig config)
    {
        var b = config.ChatUiBaseUrl.TrimEnd('/');
        return $"{b}/login" +
               $"?token={Uri.EscapeDataString(config.AccessToken)}" +
               $"&role={Uri.EscapeDataString(config.UserRole)}" +
               $"&apiKey={Uri.EscapeDataString(config.ApiKey)}" +
               $"&app={Uri.EscapeDataString(config.ApplicationName)}";
    }

    // ── Disposal ───────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing) _webView.Dispose();
        base.Dispose(disposing);
    }
}
