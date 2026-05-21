namespace NICE.Platform.Collaboration.Infrastructure.Bot;
using NICE.Platform.Collaboration.Application.Interfaces.Services;

/// <summary>
/// Future Azure AI Foundry / Azure OpenAI bot integration.
/// apiKey and apiAccessKey are passed per-call (captured at login, not from appsettings).
/// </summary>
public class AzureAIFoundryBotService : IBotService
{
    // TODO: inject Azure AI Foundry / Azure OpenAI client from configuration
    public Task<string> SendMessageAsync(
        string sessionId, string userMessage,
        string apiKey, string apiAccessKey,
        CancellationToken ct)
        => throw new NotImplementedException();

    public Task<bool> ShouldEscalateToAgentAsync(
        string sessionId, string apiKey, string apiAccessKey, CancellationToken ct)
        => Task.FromResult(false); // TODO: implement escalation logic

    public Task EndSessionAsync(
        string sessionId, string apiKey, string apiAccessKey, CancellationToken ct)
        => Task.CompletedTask;
}
