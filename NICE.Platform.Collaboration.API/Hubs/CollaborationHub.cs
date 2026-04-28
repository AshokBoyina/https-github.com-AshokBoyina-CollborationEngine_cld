namespace NICE.Platform.Collaboration.API.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NICE.Platform.Collaboration.Contracts.Constants;

public class CollaborationHub(ISender sender) : Hub
{
    private readonly ISender _sender = sender;

    public override async Task OnConnectedAsync()
    {
        // TODO: validate JWT from query string, extract claims, store connection in Redis
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // TODO: clean up session, decrement agent counters, notify group if agent
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinCollaborationGroup(string collaborationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId,
               SignalRGroups.Collaboration(Guid.Parse(collaborationId)));

    public async Task LeaveCollaborationGroup(string collaborationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId,
               SignalRGroups.Collaboration(Guid.Parse(collaborationId)));

    /// <summary>Supervisor joins silently — added to silent-monitor group only, no system notice posted.</summary>
    public async Task JoinSilently(string collaborationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId,
               SignalRGroups.SilentMonitor(Guid.Parse(collaborationId)));

    public async Task AcceptCollaboration(string collaborationId)
    {
        // TODO: dispatch AcceptCollaborationCommand via _sender
        throw new NotImplementedException();
    }

    public async Task TransferCollaboration(string collaborationId, string toAgentId, string? reason)
    {
        // TODO: dispatch TransferCollaborationCommand via _sender
        throw new NotImplementedException();
    }

    public async Task InviteSupervisor(string collaborationId, string supervisorId)
    {
        // TODO: dispatch InviteSupervisorCommand via _sender
        throw new NotImplementedException();
    }

    // ── WebRTC signalling pass-through ──────────────────────────────────────
    public async Task SendOffer(string collaborationId, string sdp)
        => await Clients.OthersInGroup(SignalRGroups.Collaboration(Guid.Parse(collaborationId)))
               .SendAsync("Offer", sdp);

    public async Task SendAnswer(string collaborationId, string sdp)
        => await Clients.OthersInGroup(SignalRGroups.Collaboration(Guid.Parse(collaborationId)))
               .SendAsync("Answer", sdp);

    public async Task SendIceCandidate(string collaborationId, string candidate)
        => await Clients.OthersInGroup(SignalRGroups.Collaboration(Guid.Parse(collaborationId)))
               .SendAsync("IceCandidate", candidate);
}
