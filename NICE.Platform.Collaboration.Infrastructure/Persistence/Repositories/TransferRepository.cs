namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class TransferRepository(CollaborationDbContext db) : ITransferRepository
{
    public Task<CollaborationTransferRequest?> GetPendingAsync(Guid collaborationId, CancellationToken ct)
        => db.TransferRequests
            .FirstOrDefaultAsync(t => t.CollaborationId == collaborationId
                                   && t.Status == "Pending", ct);

    public async Task<IEnumerable<CollaborationTransferRequest>> GetByCollaborationAsync(
        Guid collaborationId, CancellationToken ct)
        => await db.TransferRequests
            .Where(t => t.CollaborationId == collaborationId)
            .OrderByDescending(t => t.RequestedAt)
            .ToListAsync(ct);

    public async Task AddAsync(CollaborationTransferRequest transfer, CancellationToken ct)
    {
        await db.TransferRequests.AddAsync(transfer, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CollaborationTransferRequest transfer, CancellationToken ct)
    {
        db.TransferRequests.Update(transfer);
        await db.SaveChangesAsync(ct);
    }
}
