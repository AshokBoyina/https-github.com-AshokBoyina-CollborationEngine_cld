namespace NICE.Platform.Collaboration.Infrastructure.Bot;

/// <summary>
/// Configuration for the real bot API endpoint.
/// Bound from the "BotApi" section in appsettings.json.
/// Only active when FeatureFlags:UseRealBot = true.
///
/// Note: ApiKey and ApiAccessKey are NOT stored here — they are captured from the
/// user's login form and passed per-call via <see cref="IBotService"/> method parameters.
/// This keeps credentials out of appsettings and allows different callers to supply
/// their own keys without restarting the server.
/// </summary>
public class BotApiOptions
{
    public const string SectionName = "BotApi";

    /// <summary>Base URL of the bot API host, e.g. "https://your-api-host.example.com".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Path appended to BaseUrl for the chat endpoint, e.g. "/v1/AI/Bot/chat".</summary>
    public string ChatPath { get; set; } = "/v1/AI/Bot/chat";

    /// <summary>HTTP request timeout in seconds. Defaults to 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
