namespace NICE.Platform.Collaboration.Infrastructure.Features.Users.Queries.GetAvailableSupervisors;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableSupervisors;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Contracts.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class GetAvailableSupervisorsQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetAvailableSupervisorsQuery, IEnumerable<SessionResponse>>
{
    public async Task<IEnumerable<SessionResponse>> Handle(
        GetAvailableSupervisorsQuery request, CancellationToken cancellationToken)
    {
        var supervisors = await db.CurrentSessions
            .AsNoTracking()
            .Where(s => s.ApplicationId == request.ApplicationId
                     && s.UserType == "Supervisor")
            .Select(s => new SessionResponse
            {
                SessionId     = s.Id,
                Token         = string.Empty,
                Role          = "Supervisor",
                UserType      = "Supervisor",
                ApplicationId = s.ApplicationId
            })
            .ToListAsync(cancellationToken);

        return supervisors;
    }
}
