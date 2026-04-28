namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Enums;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class TransferRepository(CollaborationDbContext context) : ITransferRepository
{
    private readonly CollaborationDbContext _context = context;

    public async Task AddAsync(TransferRequest transfer, CancellationToken ct)
    {
        await _context.TransferRequests.AddAsync(transfer, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TransferRequest transfer, CancellationToken ct)
    {
        _context.TransferRequests.Update(transfer);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<TransferRequest?> GetPendingAsync(Guid collaborationId, CancellationToken ct)
        => await _context.TransferRequests
            .FirstOrDefaultAsync(
                t => t.CollaborationId == collaborationId && t.Status == TransferStatus.Pending,
                ct);
}
