namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class MessageRepository(CollaborationDbContext db) : IMessageRepository
{
    public Task<CollaborationMessage?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Messages.Include(m => m.Attachments).FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<CollaborationMessage>> GetByCollaborationAsync(
        Guid collaborationId, CancellationToken ct)
        => await db.Messages
            .Where(m => m.CollaborationId == collaborationId && !m.IsDeleted)
            .Include(m => m.Attachments)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

    public async Task AddAsync(CollaborationMessage message, CancellationToken ct)
    {
        await db.Messages.AddAsync(message, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CollaborationMessage message, CancellationToken ct)
    {
        db.Messages.Update(message);
        await db.SaveChangesAsync(ct);
    }
}
