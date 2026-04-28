namespace NICE.Platform.Collaboration.Infrastructure.Webhooks;
using System.Net.Http.Json;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
public class HttpWebhookDispatcher(HttpClient http) : IWebhookDispatcher
{
    public async Task DispatchAsync(string webhookUrl, string eventName, object payload, CancellationToken ct)
    {
        // TODO: add retry policy (Polly), HMAC signature header
        var body = new { eventName, payload, timestamp = DateTime.UtcNow };
        await http.PostAsJsonAsync(webhookUrl, body, ct);
    }
}
