namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Queries.GetCollaborationById;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetCollaborationById;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class GetCollaborationByIdQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetCollaborationByIdQuery, CollaborationResponse?>
{
    public async Task<CollaborationResponse?> Handle(
        GetCollaborationByIdQuery request, CancellationToken cancellationToken)
    {
        var collab = await db.Collaborations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (collab is null) return null;

        return new CollaborationResponse
        {
            Id        = collab.Id,
            Status    = collab.Status,
            Type      = collab.ChatMode,
            StartedAt = collab.CreatedAt,
            EndedAt   = collab.EndedAt
        };
    }
}
