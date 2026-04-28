namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class ChatMessageRepository(CollaborationDbContext context) : IChatMessageRepository
{
    private readonly CollaborationDbContext _context = context;

    public async Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        await _context.ChatMessages.AddAsync(message, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<ChatMessage>> GetByCollaborationAsync(Guid collaborationId, CancellationToken ct)
        => await _context.ChatMessages
            .Where(m => m.CollaborationId == collaborationId)
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(ct);

    public async Task<int> GetNextSequenceNumberAsync(Guid collaborationId, CancellationToken ct)
    {
        var max = await _context.ChatMessages
            .Where(m => m.CollaborationId == collaborationId)
            .MaxAsync(m => (int?)m.SequenceNumber, ct);
        return (max ?? 0) + 1;
    }
}
