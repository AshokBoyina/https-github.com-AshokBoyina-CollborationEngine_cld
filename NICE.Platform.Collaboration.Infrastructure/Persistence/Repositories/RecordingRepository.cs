namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public class RecordingRepository(CollaborationDbContext context) : IRecordingRepository
{
    private readonly CollaborationDbContext _context = context;

    public async Task<CollaborationRecording?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Recordings.FindAsync([id], ct);

    public async Task AddAsync(CollaborationRecording recording, CancellationToken ct)
    {
        await _context.Recordings.AddAsync(recording, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CollaborationRecording recording, CancellationToken ct)
    {
        _context.Recordings.Update(recording);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<CollaborationRecording>> GetByCollaborationAsync(Guid collaborationId, CancellationToken ct)
        => await _context.Recordings
            .Where(r => r.CollaborationId == collaborationId)
            .OrderBy(r => r.StartedAt)
            .ToListAsync(ct);
}
