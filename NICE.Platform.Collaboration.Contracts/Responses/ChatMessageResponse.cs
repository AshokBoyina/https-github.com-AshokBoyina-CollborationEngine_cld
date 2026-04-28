namespace NICE.Platform.Collaboration.Contracts.Responses;
public class ChatMessageResponse
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = default!;
    public int SequenceNumber { get; set; }
    public Guid? ReplyToId { get; set; }
    public bool IsSystemNotice { get; set; }
    public DateTime SentAt { get; set; }
}
