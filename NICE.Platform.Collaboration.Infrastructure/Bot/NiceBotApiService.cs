namespace NICE.Platform.Collaboration.Infrastructure.Bot;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NICE.Platform.Collaboration.Application.Interfaces.Services;

/// <summary>
/// Real bot service implementation — calls the external bot API.
/// Registered in DI when FeatureFlags:UseRealBot = true.
/// The existing mock (ExternalChat.razor keyword matching) remains untouched
/// and continues to work when the flag is false.
/// </summary>
public class NiceBotApiService(
    HttpClient               http,
    IOptions<BotApiOptions>  opts,
    ILogger<NiceBotApiService> logger)
    : IBotService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── IBotService ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sends <paramref name="userMessage"/> to the bot API and returns the reply text.
    /// Headers sent:
    ///   X-Api-Key        → BotApi:ApiKey
    ///   X-API-Access-Key → BotApi:ApiAccessKey
    /// Body:
    ///   { "sessionId": "...", "message": "..." }
    /// Expected response (flexible — any of the shapes below are accepted):
    ///   { "reply": "..." }            ← preferred
    ///   { "response": "..." }
    ///   { "answer": "..." }
    ///   { "message": "..." }
    ///   plain string                  ← fallback
    /// </summary>
    public async Task<string> SendMessageAsync(
        string sessionId, string userMessage, CancellationToken ct)
    {
        var o   = opts.Value;
        var url = $"{o.BaseUrl.TrimEnd('/')}{o.ChatPath}";

        var body = JsonSerializer.Serialize(new BotChatRequest
        {
            SessionId = sessionId,
            Message   = userMessage
        }, JsonOpts);

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(o.ApiKey))
            req.Headers.Add("X-Api-Key", o.ApiKey);

        if (!string.IsNullOrWhiteSpace(o.ApiAccessKey))
            req.Headers.Add("X-API-Access-Key", o.ApiAccessKey);

        logger.LogDebug("BotApi → POST {Url} session={SessionId}", url, sessionId);

        try
        {
            using var response = await http.SendAsync(req, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "BotApi returned {Status} for session={SessionId}. Body: {Body}",
                    (int)response.StatusCode, sessionId, raw);
                return "I'm sorry, I'm having trouble responding right now. Please try again.";
            }

            return ParseReply(raw);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "BotApi call failed for session={SessionId}", sessionId);
            return "I'm currently unavailable. Please try again in a moment.";
        }
    }

    /// <summary>
    /// Asks the bot API whether this session should escalate to a human agent.
    /// Sends: { "sessionId": "...", "checkEscalation": true }
    /// Expects: { "shouldEscalate": true/false }
    /// Falls back to false on any error so the existing mock logic in ExternalChat is unaffected.
    /// </summary>
    public async Task<bool> ShouldEscalateToAgentAsync(string sessionId, CancellationToken ct)
    {
        var o   = opts.Value;
        var url = $"{o.BaseUrl.TrimEnd('/')}{o.ChatPath}";

        var body = JsonSerializer.Serialize(new { sessionId, checkEscalation = true }, JsonOpts);

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(o.ApiKey))
            req.Headers.Add("X-Api-Key", o.ApiKey);

        if (!string.IsNullOrWhiteSpace(o.ApiAccessKey))
            req.Headers.Add("X-API-Access-Key", o.ApiAccessKey);

        try
        {
            using var response = await http.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode) return false;

            var raw = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("shouldEscalate", out var p) ||
                root.TryGetProperty("ShouldEscalate", out p))
                return p.ValueKind == JsonValueKind.True;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Notifies the bot API that the session has ended (best-effort).</summary>
    public async Task EndSessionAsync(string sessionId, CancellationToken ct)
    {
        var o   = opts.Value;
        var url = $"{o.BaseUrl.TrimEnd('/')}{o.ChatPath}";

        var body = JsonSerializer.Serialize(new { sessionId, action = "end" }, JsonOpts);

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(o.ApiKey))
            req.Headers.Add("X-Api-Key", o.ApiKey);

        if (!string.IsNullOrWhiteSpace(o.ApiAccessKey))
            req.Headers.Add("X-API-Access-Key", o.ApiAccessKey);

        try   { await http.SendAsync(req, ct); }
        catch { /* best-effort */ }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tolerant response parser — handles multiple response shapes and raw strings.
    /// </summary>
    private static string ParseReply(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "I didn't receive a response. Please try again.";

        // Try JSON first
        try
        {
            using var doc  = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Check common reply field names (camelCase and PascalCase)
            foreach (var key in new[] { "reply", "Reply", "response", "Response",
                                        "answer", "Answer", "message", "Message",
                                        "text", "Text", "content", "Content" })
            {
                if (root.TryGetProperty(key, out var prop)
                    && prop.ValueKind == JsonValueKind.String)
                {
                    var text = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
        }
        catch { /* not JSON — fall through */ }

        // If it's a plain string response
        var trimmed = raw.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(trimmed)
            ? "I didn't receive a response. Please try again."
            : trimmed;
    }

    // ── Request DTO ──────────────────────────────────────────────────────────

    private sealed record BotChatRequest
    {
        [JsonPropertyName("sessionId")] public string SessionId { get; init; } = string.Empty;
        [JsonPropertyName("message")]   public string Message   { get; init; } = string.Empty;
    }
}
