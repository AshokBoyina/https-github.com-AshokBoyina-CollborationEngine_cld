namespace NICE.Platform.Collaboration.Contracts.Responses;
public class RecordingResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string? SasUrl { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
