namespace NICE.Platform.Collaboration.Contracts.SignalR;
public class RecordingFailedPayload
{
    public Guid CollaborationId { get; set; }
    public string Reason { get; set; } = default!;
}
