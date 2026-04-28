namespace NICE.Platform.Collaboration.Infrastructure.Bot;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
public class AzureAIFoundryBotService : IBotService
{
    // TODO: inject Azure AI Foundry / Azure OpenAI client from configuration
    public Task<string> SendMessageAsync(string sessionId, string userMessage, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<bool> ShouldEscalateToAgentAsync(string sessionId, CancellationToken ct)
        => Task.FromResult(false); // TODO: implement escalation logic
    public Task EndSessionAsync(string sessionId, CancellationToken ct)
        => Task.CompletedTask;
}
