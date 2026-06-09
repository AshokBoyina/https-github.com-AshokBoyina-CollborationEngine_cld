namespace NICE.Platform.Collaboration.ChatPlugin;

/// <summary>
/// Configuration passed by the host application when initialising the chat panel.
/// The plugin navigates its embedded WebView2 to:
///   {ChatUiBaseUrl}/chat/internal?token={AccessToken}&amp;role={UserRole}
/// Login.razor auto-submits those query params so the user lands directly on the
/// internal chat page with no manual login required.
/// </summary>
public sealed class ChatPluginConfig
{
    /// <summary>
    /// Base URL of the deployed ChatUI Blazor WASM app.
    /// Example: "https://chat.yourcompany.com" or "http://localhost:5200"
    /// </summary>
    public string ChatUiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the Collaboration Engine API server.
    /// Used for demo token minting.  Example: "http://localhost:65168"
    /// </summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:65168";

    /// <summary>
    /// The user's auth token (X-Access-Key / ExternalUserId).
    /// Passed as a query parameter so Login.razor auto-authenticates.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The user's role: "Agent", "Supervisor", or "Internal".
    /// </summary>
    public string UserRole { get; set; } = "Agent";

    /// <summary>
    /// Display name override shown in the panel title bar.
    /// If empty, the name resolved from the token is used.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// API key for the application (X-Api-Key header).
    /// Passed as a query parameter so Login.razor can forward it to the hub.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Application name registered in the Collaboration Engine (X-Access-Key header).
    /// Example: "SurveyPortal", "Readi", "NicePortal".
    /// Defaults to "SurveyPortal" when not set.
    /// </summary>
    public string ApplicationName { get; set; } = "SurveyPortal";

    /// <summary>
    /// Whether to show the panel as a floating overlay (true) or as an
    /// embedded inline control inside the host app's layout (false).
    /// </summary>
    public bool FloatingMode { get; set; } = false;

    /// <summary>
    /// Panel width when in FloatingMode. Ignored when embedded inline.
    /// </summary>
    public int FloatingWidth { get; set; } = 720;

    /// <summary>
    /// Panel height when in FloatingMode. Ignored when embedded inline.
    /// </summary>
    public int FloatingHeight { get; set; } = 560;
}
