namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class ApplicationRepository(CollaborationDbContext context) : IApplicationRepository
{
    private readonly CollaborationDbContext _context = context;

    public async Task<ApplicationRegistration?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Applications.FindAsync([id], ct);

    public async Task<ApplicationRegistration?> GetByApiKeyHashAsync(string hash, CancellationToken ct)
        => await _context.Applications
            .FirstOrDefaultAsync(a => a.HashedApiKey == hash, ct);

    public async Task AddAsync(ApplicationRegistration app, CancellationToken ct)
    {
        await _context.Applications.AddAsync(app, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ApplicationRegistration app, CancellationToken ct)
    {
        _context.Applications.Update(app);
        await _context.SaveChangesAsync(ct);
    }
}
