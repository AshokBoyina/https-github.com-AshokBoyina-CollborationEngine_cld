namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Enums;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class CollaborationRepository(CollaborationDbContext context) : ICollaborationRepository
{
    private readonly CollaborationDbContext _context = context;

    private static readonly CollaborationStatus[] ActiveStatuses =
    [
        CollaborationStatus.Waiting,
        CollaborationStatus.Active,
        CollaborationStatus.Paused,
        CollaborationStatus.Transferred,
    ];

    public async Task<Collaboration?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Collaborations.FindAsync([id], ct);

    public async Task AddAsync(Collaboration collab, CancellationToken ct)
    {
        await _context.Collaborations.AddAsync(collab, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Collaboration collab, CancellationToken ct)
    {
        _context.Collaborations.Update(collab);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Collaboration>> GetActiveByApplicationAsync(Guid applicationId, CancellationToken ct)
        => await _context.Collaborations
            .Where(c => c.ApplicationId == applicationId && ActiveStatuses.Contains(c.Status))
            .ToListAsync(ct);

    public async Task<IEnumerable<Collaboration>> GetActiveByAgentAsync(Guid agentId, CancellationToken ct)
        => await _context.Collaborations
            .Where(c => c.PrimaryAgentId == agentId && ActiveStatuses.Contains(c.Status))
            .ToListAsync(ct);
}
