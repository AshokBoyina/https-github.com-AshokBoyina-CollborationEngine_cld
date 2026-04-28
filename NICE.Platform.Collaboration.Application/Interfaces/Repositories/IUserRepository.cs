namespace NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Enums;
public interface IUserRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UserProfile?> GetByExternalIdAsync(string externalId, Guid applicationId, CancellationToken ct);
    Task AddAsync(UserProfile user, CancellationToken ct);
    Task UpdateAsync(UserProfile user, CancellationToken ct);
    Task<IEnumerable<UserProfile>> GetByRoleAsync(Guid applicationId, UserRole role, CancellationToken ct);
}
