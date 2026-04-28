namespace NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
public interface ITransferRepository
{
    Task AddAsync(TransferRequest transfer, CancellationToken ct);
    Task UpdateAsync(TransferRequest transfer, CancellationToken ct);
    Task<TransferRequest?> GetPendingAsync(Guid collaborationId, CancellationToken ct);
}
