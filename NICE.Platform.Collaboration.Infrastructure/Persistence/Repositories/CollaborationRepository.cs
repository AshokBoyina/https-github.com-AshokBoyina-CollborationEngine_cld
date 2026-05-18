namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class CollaborationRepository(CollaborationDbContext db) : ICollaborationRepository
{
    public Task<Collaboration?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Collaborations.FindAsync([id], ct).AsTask();

    public Task<Collaboration?> GetByIdWithParticipantsAsync(Guid id, CancellationToken ct)
        => db.Collaborations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Collaboration>> GetActiveByApplicationAsync(
        Guid applicationId, CancellationToken ct)
        => await db.Collaborations
            .Where(c => c.ApplicationId == applicationId && c.EndedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Collaboration>> GetActiveByAgentAsync(
        Guid agentUserId, CancellationToken ct)
        => await db.Collaborations
            .Where(c => c.EndedAt == null &&
                        c.Participants.Any(p => p.UserId == agentUserId && p.LeftAt == null))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Collaboration collab, CancellationToken ct)
    {
        await db.Collaborations.AddAsync(collab, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Collaboration collab, CancellationToken ct)
    {
        db.Collaborations.Update(collab);
        await db.SaveChangesAsync(ct);
    }
}
