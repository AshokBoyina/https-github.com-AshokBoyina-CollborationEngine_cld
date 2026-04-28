namespace NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Enums;
public class CollaborationRecording
{
    public Guid Id { get; private set; }
    public Guid CollaborationId { get; private set; }
    public RecordingType Type { get; private set; }
    public string BlobPath { get; private set; } = default!;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
}
