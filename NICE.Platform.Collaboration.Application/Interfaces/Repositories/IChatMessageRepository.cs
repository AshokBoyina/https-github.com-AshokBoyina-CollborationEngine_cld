namespace NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct);
    Task<IEnumerable<ChatMessage>> GetByCollaborationAsync(Guid collaborationId, CancellationToken ct);
    Task<int> GetNextSequenceNumberAsync(Guid collaborationId, CancellationToken ct);
}
