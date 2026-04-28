namespace NICE.Platform.Collaboration.Contracts.Requests;
public class StartRecordingRequest
{
    public Guid CollaborationId { get; set; }
    public string Type { get; set; } = "Screen";
}
