namespace NICE.Platform.Collaboration.ChatPlugin;

using System.Drawing.Drawing2D;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

/// <summary>
/// Standalone chat window — opens in the Windows taskbar as an independent window.
/// Clicking the FAB shows/activates it; clicking the OS close button hides it
/// (preserving chat state) rather than disposing it.
///
/// WebView2 is pre-warmed in OnShown so ConnectBtn_Click only needs to call
/// Navigate() — no EnsureCoreWebView2Async inside a button handler.
/// </summary>
public sealed class ChatFloatingForm : Form
{
    // ── Dimensions ─────────────────────────────────────────────────────────
    private const int DefaultW = 780;
    private const int DefaultH = 660;

    // ── NICE brand colours ─────────────────────────────────────────────────
    private static readonly Color NicePrimary       = Color.FromArgb(0,   91,  65);
    private static readonly Color NicePrimaryStrong = Color.FromArgb(0,   55,  39);
    private static readonly Color NicePrimarySoft   = Color.FromArgb(242, 247, 245);
    private static readonly Color NiceBorder        = Color.FromArgb(209, 213, 219);
    private static readonly Color NiceText          = Color.FromArgb(26,  32,  44);
    private static readonly Color NiceTextMuted     = Color.FromArgb(107, 124, 128);
    private static readonly Color NiceError         = Color.FromArgb(220,  53,  69);

    // ── Child controls ─────────────────────────────────────────────────────
    private readonly WebView2 _webView;
    private readonly Panel    _loginPanel;
    private readonly Panel    _loadingPanel;
    private readonly TextBox  _tokenInput;
    private readonly ComboBox _roleCombo;
    private readonly Button   _connectBtn;
    private readonly Label    _errorLabel;

    // ── WebView2 state ─────────────────────────────────────────────────────
    private bool       _webViewReady;
    private bool       _webViewInitStarted;
    private Exception? _webViewInitError;
    private bool       _navPending;

    // ── Plugin state ───────────────────────────────────────────────────────
    private ChatPluginConfig? _presetConfig;
    private bool              _connected;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    // ── Public events ──────────────────────────────────────────────────────
    public event EventHandler<int>? UnreadCountChanged;

    // ── Constructor ────────────────────────────────────────────────────────
    public ChatFloatingForm()
    {
        // ── Window chrome ───────────────────────────────────────────────────
        Text            = "NICE – Internal Chat";
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar   = true;
        Size            = new Size(DefaultW, DefaultH);
        MinimumSize     = new Size(500, 520);
        BackColor       = Color.White;
        StartPosition   = FormStartPosition.Manual;

        // Position bottom-right of the primary screen on first show.
        // The user can move/resize freely after that.
        Load += PositionOnFirstLoad;

        // ── WebView2 (fills the window body) ───────────────────────────────
        _webView = new WebView2 { Dock = DockStyle.Fill };

        // ── Loading panel ──────────────────────────────────────────────────
        _loadingPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = NicePrimarySoft,
            Visible   = false
        };

        _loadingPanel.Controls.Add(new Label
        {
            Text      = "⏳",
            Font      = new Font("Segoe UI Emoji", 26f),
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.None,
            Anchor    = AnchorStyles.None,
            Size      = new Size(DefaultW, 60)
        });
        _loadingPanel.Controls.Add(new Label
        {
            Text      = "Connecting to chat…",
            Font      = new Font("Segoe UI", 10f),
            ForeColor = NiceTextMuted,
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.None,
            Anchor    = AnchorStyles.None,
            Size      = new Size(DefaultW, 30)
        });
        _loadingPanel.Controls.Add(new Label
        {
            Text      = "Loading Blazor WASM — first load may take 10–15 seconds.",
            Font      = new Font("Segoe UI", 8f),
            ForeColor = NiceTextMuted,
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.None,
            Anchor    = AnchorStyles.None,
            Size      = new Size(DefaultW, 24)
        });
        _loadingPanel.Layout += CentreLoadingLabels;

        // ── Login panel ────────────────────────────────────────────────────
        _loginPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = NicePrimarySoft
        };

        const int CardW = 420;
        var loginCard = new Panel
        {
            BackColor = Color.White,
            Size      = new Size(CardW, 330),
            Anchor    = AnchorStyles.None
        };

        void PositionCard() =>
            loginCard.Location = new Point(
                (_loginPanel.Width  - loginCard.Width)  / 2,
                (_loginPanel.Height - loginCard.Height) / 2);

        _loginPanel.Resize         += (_, _) => PositionCard();
        _loginPanel.VisibleChanged += (_, _) => PositionCard();

        loginCard.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(NiceBorder);
            using var path = RoundedRect(0, 0, loginCard.Width - 1, loginCard.Height - 1, 8);
            pe.Graphics.DrawPath(pen, path);
        };

        const int Lx = 24;
        const int Cw = CardW - 48;

        var cardTitle = new Label
        {
            Text      = "Sign in to Internal Chat",
            Font      = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = NiceText,
            Location  = new Point(Lx, 20),
            AutoSize  = true
        };

        var tokenLabel = new Label
        {
            Text      = "Access Token",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = NiceTextMuted,
            Location  = new Point(Lx, 58),
            AutoSize  = true
        };

        _tokenInput = new TextBox
        {
            Location        = new Point(Lx, 76),
            Size            = new Size(Cw, 28),
            Font            = new Font("Segoe UI", 9f),
            PlaceholderText = "Paste token, or leave blank to auto-connect"
        };

        var roleLabel = new Label
        {
            Text      = "Role",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = NiceTextMuted,
            Location  = new Point(Lx, 118),
            AutoSize  = true
        };

        _roleCombo = new ComboBox
        {
            Location      = new Point(Lx, 136),
            Size          = new Size(Cw, 28),
            Font          = new Font("Segoe UI", 9f),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _roleCombo.Items.AddRange(["Internal", "Agent", "Supervisor"]);
        _roleCombo.SelectedIndex = 0;

        _errorLabel = new Label
        {
            ForeColor = NiceError,
            Font      = new Font("Segoe UI", 8f),
            Location  = new Point(Lx, 176),
            Size      = new Size(Cw, 40),
            Visible   = false
        };

        _connectBtn = new Button
        {
            Text      = "Connect",
            Location  = new Point(Lx, 224),
            Size      = new Size(Cw, 38),
            Font      = new Font("Segoe UI", 9.5f),
            BackColor = NicePrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        _connectBtn.FlatAppearance.BorderSize         = 0;
        _connectBtn.FlatAppearance.MouseOverBackColor = NicePrimaryStrong;
        _connectBtn.Click += ConnectBtn_Click;

        var hintLabel = new Label
        {
            Text      = "💡 Leave token blank to auto-connect via the demo API.",
            Font      = new Font("Segoe UI", 7.5f),
            ForeColor = NiceTextMuted,
            Location  = new Point(Lx, 274),
            Size      = new Size(Cw, 36)
        };

        loginCard.Controls.AddRange([
            cardTitle, tokenLabel, _tokenInput,
            roleLabel, _roleCombo, _errorLabel,
            _connectBtn, hintLabel
        ]);
        _loginPanel.Controls.Add(loginCard);

        // ── Z-order: loginPanel front, loadingPanel behind, webView at back ─
        Controls.Add(_loginPanel);
        Controls.Add(_loadingPanel);
        Controls.Add(_webView);

        PositionCard();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void SetConfig(ChatPluginConfig config) => _presetConfig = config;

    // ── Window close → hide (preserve state) ───────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    // ── First-load positioning ─────────────────────────────────────────────

    private void PositionOnFirstLoad(object? sender, EventArgs e)
    {
        Load -= PositionOnFirstLoad;   // run once only

        var screen = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        var x = screen.Right  - Width  - 32;
        var y = screen.Bottom - Height - 32;
        Location = new Point(Math.Max(screen.Left, x), Math.Max(screen.Top, y));
    }

    // ── Form.Shown — pre-warm WebView2 ─────────────────────────────────────
    // Fires on the UI thread AFTER all HWNDs are created.  Pre-warming here
    // means ConnectBtn_Click never calls EnsureCoreWebView2Async.

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_webViewInitStarted) return;
        _webViewInitStarted = true;

        _ = PreWarmWebViewAsync();
    }

    private async Task PreWarmWebViewAsync()
    {
        try
        {
            var userDataDir = Path.Combine(Path.GetTempPath(), "NICE.Chat.WebView2");
            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder:          userDataDir);

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
            _webViewInitError = ex;
        }
    }

    // ── Connect button ─────────────────────────────────────────────────────

    private async void ConnectBtn_Click(object? sender, EventArgs e)
    {
        HideError();
        _connectBtn.Enabled = false;
        _connectBtn.Text    = "Connecting…";

        try
        {
            if (!_webViewReady)
            {
                ShowError(_webViewInitError is { } ex
                    ? $"WebView2 init failed [{ex.GetType().Name} 0x{ex.HResult:X8}]: {ex.Message}"
                    : "WebView2 is still initialising — please wait a moment and try again.");
                return;
            }

            var role  = _roleCombo.SelectedItem?.ToString() ?? "Internal";
            var token = _tokenInput.Text.Trim();

            // Auto-mint token if blank
            if (string.IsNullOrEmpty(token))
            {
                var apiBase = _presetConfig?.ApiBaseUrl ?? "http://localhost:65168";
                var mintUrl = $"{apiBase}/api/v1/demo/mint-token" +
                              $"?name=Alice&role={Uri.EscapeDataString(role)}";
                try
                {
                    var resp = await Http.PostAsync(mintUrl, null);
                    resp.EnsureSuccessStatusCode();
                    token = (await resp.Content.ReadAsStringAsync()).Trim().Trim('"');
                }
                catch (Exception mintEx)
                {
                    ShowError($"Auto-connect failed: {mintEx.Message}");
                    return;
                }
            }

            var config = new ChatPluginConfig
            {
                ChatUiBaseUrl   = _presetConfig?.ChatUiBaseUrl   ?? "http://localhost:5200",
                ApiBaseUrl      = _presetConfig?.ApiBaseUrl      ?? "http://localhost:65168",
                ApiKey          = _presetConfig?.ApiKey          ?? string.Empty,
                ApplicationName = _presetConfig?.ApplicationName ?? "SurveyPortal",
                AccessToken     = token,
                UserRole        = role
            };

            _loginPanel.Visible   = false;
            _loadingPanel.Visible = true;
            _navPending           = true;

            _webView.CoreWebView2.Navigate(BuildChatUrl(config));
            _connected = true;
        }
        catch (Exception ex)
        {
            _loadingPanel.Visible = false;
            _loginPanel.Visible   = true;
            _connected            = false;
            ShowError($"Error [{ex.GetType().Name}]: {ex.Message}");
        }
        finally
        {
            _connectBtn.Enabled = true;
            _connectBtn.Text    = "Connect";
        }
    }

    // ── NavigationCompleted ────────────────────────────────────────────────

    private void OnCoreNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!_navPending) return;
        _navPending = false;

        if (InvokeRequired)
            Invoke(() => OnNavigationCompleted(args.IsSuccess));
        else
            OnNavigationCompleted(args.IsSuccess);
    }

    private void OnNavigationCompleted(bool success)
    {
        _loadingPanel.Visible = false;

        if (!success)
        {
            _loginPanel.Visible = true;
            _connected          = false;
            ShowError("Could not reach the chat server — is ChatUI running on port 5200?");
        }
    }

    // ── Web message handler (Blazor → WinForms) ───────────────────────────

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson);
            if (!json.RootElement.TryGetProperty("type", out var typeProp)) return;

            switch (typeProp.GetString())
            {
                case "unreadCount":
                    if (json.RootElement.TryGetProperty("value", out var v))
                        UnreadCountChanged?.Invoke(this, v.GetInt32());
                    break;
            }
        }
        catch { }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string BuildChatUrl(ChatPluginConfig config)
    {
        var baseUrl = config.ChatUiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/login" +
               $"?token={Uri.EscapeDataString(config.AccessToken)}" +
               $"&role={Uri.EscapeDataString(config.UserRole)}" +
               $"&apiKey={Uri.EscapeDataString(config.ApiKey)}" +
               $"&app={Uri.EscapeDataString(config.ApplicationName)}";
    }

    private void ShowError(string msg)
    {
        _errorLabel.Text    = msg;
        _errorLabel.Visible = true;
    }

    private void HideError() => _errorLabel.Visible = false;

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

    // Centre loading labels vertically when the panel resizes
    private void CentreLoadingLabels(object? sender, LayoutEventArgs e)
    {
        var labels = _loadingPanel.Controls.OfType<Label>().ToArray();
        int totalH = labels.Sum(l => l.Height) + (labels.Length - 1) * 8;
        int y      = (_loadingPanel.Height - totalH) / 2;
        foreach (var lbl in labels)
        {
            lbl.Location = new Point(0, y);
            lbl.Width    = _loadingPanel.Width;
            y += lbl.Height + 8;
        }
    }

    // ── Disposal ───────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing) _webView.Dispose();
        base.Dispose(disposing);
    }
}
