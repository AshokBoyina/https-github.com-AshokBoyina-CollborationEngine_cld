namespace NICE.Platform.Collaboration.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using NICE.Platform.Collaboration.Contracts.Constants;

public class RecordingHub(IIceServerProvider iceProvider) : Hub
{
    private readonly IIceServerProvider _iceProvider = iceProvider;

    /// <summary>Called by standalone recording clients — no user session required, only API key.</summary>
    public async Task StartRecordingSession(string recordingSessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId,
            SignalRGroups.Recording(Guid.Parse(recordingSessionId)));
        var config = await _iceProvider.GetConfigAsync();
        await Clients.Caller.SendAsync("IceServersReady", config);
    }

    public async Task SendOffer(string recordingSessionId, string sdp)
        => await Clients.OthersInGroup(SignalRGroups.Recording(Guid.Parse(recordingSessionId)))
               .SendAsync("Offer", sdp);

    public async Task SendAnswer(string recordingSessionId, string sdp)
        => await Clients.OthersInGroup(SignalRGroups.Recording(Guid.Parse(recordingSessionId)))
               .SendAsync("Answer", sdp);

    public async Task SendIceCandidate(string recordingSessionId, string candidate)
        => await Clients.OthersInGroup(SignalRGroups.Recording(Guid.Parse(recordingSessionId)))
               .SendAsync("IceCandidate", candidate);
}
