namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Queries.GetActiveCollaborations;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetActiveCollaborations;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Contracts.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class GetActiveCollaborationsQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetActiveCollaborationsQuery, IEnumerable<CollaborationResponse>>
{
    public async Task<IEnumerable<CollaborationResponse>> Handle(
        GetActiveCollaborationsQuery request, CancellationToken cancellationToken)
    {
        var collabs = await db.Collaborations
            .AsNoTracking()
            .Where(c => c.ApplicationId == request.ApplicationId && c.EndedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return collabs.Select(c => new CollaborationResponse
        {
            Id        = c.Id,
            Status    = c.Status,
            Type      = c.ChatMode,
            StartedAt = c.CreatedAt,
            EndedAt   = c.EndedAt
        });
    }
}
