namespace NICE.Platform.Collaboration.Infrastructure.Features.Users.Queries.GetAvailableAgents;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableAgents;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class GetAvailableAgentsQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetAvailableAgentsQuery, IEnumerable<SessionResponse>>
{
    public async Task<IEnumerable<SessionResponse>> Handle(
        GetAvailableAgentsQuery request, CancellationToken cancellationToken)
    {
        // "Available" = agent has an active hub session and is not currently handling a collab
        var agents = await db.CurrentSessions
            .AsNoTracking()
            .Where(s => s.ApplicationId == request.ApplicationId
                     && s.UserType == "Agent"
                     && s.CurrentCollaborationId == null)
            .Select(s => new SessionResponse
            {
                SessionId     = s.Id,
                Token         = string.Empty,
                Role          = "Agent",
                UserType      = "Agent",
                ApplicationId = s.ApplicationId
            })
            .ToListAsync(cancellationToken);

        return agents;
    }
}
