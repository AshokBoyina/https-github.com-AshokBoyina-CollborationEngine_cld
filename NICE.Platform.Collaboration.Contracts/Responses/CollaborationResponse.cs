namespace NICE.Platform.Collaboration.Contracts.Responses;
public class CollaborationResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = default!;
    public string Type { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
