namespace NICE.Platform.Collaboration.Contracts.SignalR;
public class MessageReceivedPayload
{
    public Guid CollaborationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = default!;
    public int SequenceNumber { get; set; }
    public DateTime SentAt { get; set; }
}
