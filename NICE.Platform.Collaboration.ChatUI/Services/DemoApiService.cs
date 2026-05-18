namespace NICE.Platform.Collaboration.ChatUI.Services;

using System.Net.Http.Json;
using System.Text.Json;
using NICE.Platform.Collaboration.ChatUI.Models;

public class DemoApiService(HttpClient http, IAuthService auth) : IDemoApiService
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds an authenticated HTTP request with Bearer token + X-Api-Key header.
    /// Phase 1: any non-empty key is accepted by ApiKeyMiddleware (stub).
    /// Phase 2: store and replay the real API key from the login response.
    /// </summary>
    private HttpRequestMessage AuthedRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        var token = auth.Current.Token;
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // ApiKeyMiddleware requires X-Api-Key on all non-demo routes.
        // Phase 1 stub accepts any non-empty value; use a fixed demo placeholder
        // until the client stores the real key from the login flow.
        req.Headers.TryAddWithoutValidation("X-Api-Key", "demo-internal");
        return req;
    }

    // ── Ping — no DB, just checks if API is reachable ─────────────────────────
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync("api/v1/demo/ping", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Status — checks DB seeded state ──────────────────────────────────────
    public async Task<DemoStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync("api/v1/demo/status", ct);
            if (!resp.IsSuccessStatusCode)
                return new DemoStatusResult { DbOk = false, DbError = $"HTTP {(int)resp.StatusCode}" };

            return await resp.Content.ReadFromJsonAsync<DemoStatusResult>(JsonOpts, ct)
                   ?? new DemoStatusResult();
        }
        catch (Exception ex)
        {
            return new DemoStatusResult { DbOk = false, DbError = ex.Message };
        }
    }

    // ── Seed ─────────────────────────────────────────────────────────────────
    public async Task<(bool Ok, string Msg)> SeedAppsAsync(CancellationToken ct = default)
    {
        try
        {
            var resp     = await http.PostAsync("api/v1/demo/seed", null, ct);
            var bodyText = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                var errMsg = TryExtract(bodyText, "error") ?? TryExtract(bodyText, "detail") ?? bodyText;
                return (false, $"API error ({(int)resp.StatusCode}): {errMsg}");
            }

            var msg = TryExtract(bodyText, "message") ?? "Seeded successfully.";
            return (true, msg);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    // ── Get users for an app ──────────────────────────────────────────────────
    public async Task<List<DemoUser>> GetUsersAsync(Guid appId, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"api/v1/demo/users/{appId}", ct);
            if (!resp.IsSuccessStatusCode) return [];
            return await resp.Content.ReadFromJsonAsync<List<DemoUser>>(JsonOpts, ct) ?? [];
        }
        catch { return []; }
    }

    // ── Create a demo user ────────────────────────────────────────────────────
    public async Task<DemoUser?> CreateUserAsync(DemoCreateUserDto dto, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("api/v1/demo/users", dto, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<DemoUser>(JsonOpts, ct);
        }
        catch { return null; }
    }

    // ── Remove user from app ──────────────────────────────────────────────────
    public async Task<bool> RemoveUserAsync(Guid userId, Guid appId, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.DeleteAsync($"api/v1/demo/users/{userId}/{appId}", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Get active internal channels ──────────────────────────────────────
    public async Task<List<ActiveChannelInfo>> GetActiveInternalChannelsAsync(
        Guid appId, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"api/v1/demo/channels/{appId}", ct);
            if (!resp.IsSuccessStatusCode) return [];
            return await resp.Content.ReadFromJsonAsync<List<ActiveChannelInfo>>(JsonOpts, ct) ?? [];
        }
        catch { return []; }
    }

    // ── Get collaboration messages ─────────────────────────────────────────
    public async Task<List<CollaborationMessageDto>> GetCollaborationMessagesAsync(
        Guid collaborationId, CancellationToken ct = default)
    {
        try
        {
            var req  = AuthedRequest(HttpMethod.Get,
                $"api/v1/collaboration/messages/{collaborationId}");
            var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return [];
            return await resp.Content.ReadFromJsonAsync<List<CollaborationMessageDto>>(JsonOpts, ct) ?? [];
        }
        catch { return []; }
    }

    // ── Get currently online (hub-connected) non-External users ───────────
    public async Task<List<OnlineUserInfo>> GetOnlineUsersAsync(
        Guid appId, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"api/v1/demo/online-users/{appId}", ct);
            if (!resp.IsSuccessStatusCode) return [];
            return await resp.Content.ReadFromJsonAsync<List<OnlineUserInfo>>(JsonOpts, ct) ?? [];
        }
        catch { return []; }
    }

    // ── Get all non-ended (active) collaborations for an application ───────
    public async Task<List<ActiveCollaborationDto>> GetActiveCollaborationsAsync(
        Guid appId, CancellationToken ct = default)
    {
        try
        {
            var req  = AuthedRequest(HttpMethod.Get,
                $"api/v1/collaboration/collaborations/active/{appId}");
            var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return [];
            return await resp.Content.ReadFromJsonAsync<List<ActiveCollaborationDto>>(JsonOpts, ct) ?? [];
        }
        catch { return []; }
    }

    // ── JSON helper ────────────────────────────────────────────────────────
    private static string? TryExtract(string json, string key)
    {
        try
        {
            using var doc  = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var prop))
                return prop.GetString();
        }
        catch { }
        return null;
    }
}
