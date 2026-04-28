namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Enums;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class UserRepository(CollaborationDbContext context) : IUserRepository
{
    private readonly CollaborationDbContext _context = context;

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Users.FindAsync([id], ct);

    public async Task<UserProfile?> GetByExternalIdAsync(string externalId, Guid applicationId, CancellationToken ct)
        => await _context.Users
            .Join(
                _context.ApplicationUsers.Where(au => au.ApplicationId == applicationId),
                u  => u.Id,
                au => au.UserId,
                (u, _) => u)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);

    public async Task AddAsync(UserProfile user, CancellationToken ct)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserProfile user, CancellationToken ct)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<UserProfile>> GetByRoleAsync(Guid applicationId, UserRole role, CancellationToken ct)
        => await _context.Users
            .Join(
                _context.ApplicationUsers.Where(au => au.ApplicationId == applicationId && au.Role == role),
                u  => u.Id,
                au => au.UserId,
                (u, _) => u)
            .ToListAsync(ct);
}
