namespace NICE.Platform.Collaboration.Infrastructure.Services;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
// Implement ISignalRNotifier using IHubContext<T>
// Register each hub context in DI and delegate calls here
public class SignalRNotifier : ISignalRNotifier
{
    // TODO: inject IHubContext<CollaborationHub> and IHubContext<RecordingHub>
    public Task NotifyGroupAsync(string group, string eventName, object payload, CancellationToken ct)
        => throw new NotImplementedException();
    public Task NotifyUserAsync(string connectionId, string eventName, object payload, CancellationToken ct)
        => throw new NotImplementedException();
    public Task NotifyGroupExceptAsync(string group, string excludeConnectionId, string eventName, object payload, CancellationToken ct)
        => throw new NotImplementedException();
}
