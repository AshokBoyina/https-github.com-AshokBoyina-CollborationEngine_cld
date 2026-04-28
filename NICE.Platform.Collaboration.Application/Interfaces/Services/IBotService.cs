namespace NICE.Platform.Collaboration.Application.Interfaces.Services;
public interface IBotService
{
    Task<string> SendMessageAsync(string sessionId, string userMessage, CancellationToken ct);
    Task<bool> ShouldEscalateToAgentAsync(string sessionId, CancellationToken ct);
    Task EndSessionAsync(string sessionId, CancellationToken ct);
}
