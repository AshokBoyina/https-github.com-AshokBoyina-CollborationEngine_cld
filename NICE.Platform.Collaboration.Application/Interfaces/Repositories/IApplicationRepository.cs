namespace NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
public interface IApplicationRepository
{
    Task<ApplicationRegistration?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ApplicationRegistration?> GetByApiKeyHashAsync(string hash, CancellationToken ct);
    Task AddAsync(ApplicationRegistration app, CancellationToken ct);
    Task UpdateAsync(ApplicationRegistration app, CancellationToken ct);
}
