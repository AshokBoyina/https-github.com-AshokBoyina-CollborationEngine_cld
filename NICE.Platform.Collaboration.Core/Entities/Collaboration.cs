namespace NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Enums;
public class Collaboration
{
    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PrimaryAgentId { get; private set; }
    public CollaborationStatus Status { get; private set; }
    public CollaborationType Type { get; private set; }
    public string StorageFolderPath { get; private set; } = default!;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();
    private readonly List<CollaborationRecording> _recordings = [];
    public IReadOnlyList<CollaborationRecording> Recordings => _recordings.AsReadOnly();
}
