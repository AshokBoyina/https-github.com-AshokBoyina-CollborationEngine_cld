namespace NICE.Platform.Collaboration.ChatPlugin;

// ChatPanelControl is no longer used — WebView2 is now hosted directly inside
// ChatFloatingForm, which avoids all Panel-embedding and SynchronizationContext
// issues.  This stub is retained so any external references compile cleanly.

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

public sealed class ChatPanelControl : UserControl
{
    private readonly WebView2 _webView;

    public event EventHandler<int>?  UnreadCountChanged;
    public event EventHandler<bool>? PageNavigationCompleted;
    public event EventHandler?       ConnectionLost;
    public event EventHandler?       ConnectionRestored;

    public ChatPanelControl()
    {
        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);
        Dock = DockStyle.Fill;
    }

    public Task InitializeAsync(ChatPluginConfig config) => Task.CompletedTask;
    public void Reload() { }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _webView.Dispose();
        base.Dispose(disposing);
    }
}
