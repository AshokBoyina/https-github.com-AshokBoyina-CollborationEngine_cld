namespace NICE.Platform.Collaboration.Infrastructure.Bot;

using NICE.Platform.Collaboration.Application.Interfaces.Services;

/// <summary>
/// No-operation bot service used when <c>FeatureFlags:UseRealBot</c> is <c>false</c>.
///
/// In demo / development mode the Blazor ExternalChat widget handles bot-style
/// replies locally (keyword matching, escalation prompts). The server-side bot
/// pipeline is therefore inactive — this stub satisfies the DI registration
/// without throwing <see cref="NotImplementedException"/> at runtime.
///
/// To switch to the real bot, set <c>FeatureFlags:UseRealBot = true</c> in
/// appsettings and ensure <c>NiceBotApi:BaseUrl</c> + <c>NiceBotApi:ApiKey</c>
/// are configured. <see cref="NiceBotApiService"/> will be registered instead.
/// </summary>
public sealed class NoOpBotService : IBotService
{
    public Task<string> SendMessageAsync(
        string sessionId, string userMessage, CancellationToken ct)
        => Task.FromResult(string.Empty);   // no bot reply — client handles it

    public Task<bool> ShouldEscalateToAgentAsync(
        string sessionId, CancellationToken ct)
        => Task.FromResult(false);          // escalation decided client-side

    public Task EndSessionAsync(string sessionId, CancellationToken ct)
        => Task.CompletedTask;
}
