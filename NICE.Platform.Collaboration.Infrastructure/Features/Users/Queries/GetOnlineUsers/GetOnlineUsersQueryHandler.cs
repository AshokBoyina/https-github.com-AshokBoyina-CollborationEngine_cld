namespace NICE.Platform.Collaboration.Infrastructure.Features.Users.Queries.GetOnlineUsers;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Features.Users.Queries.GetOnlineUsers;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

/// <summary>
/// Returns all non-External users currently connected to the SignalR hub for the given application.
/// Queries <c>CurrentSessions</c> (populated on hub connect, cleared on disconnect),
/// joined with <c>Users</c> to resolve display names.
/// Excludes External (customer) users — they are not staff-visible in the directory.
/// </summary>
public sealed class GetOnlineUsersQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetOnlineUsersQuery, IEnumerable<OnlineUserResponse>>
{
    public async Task<IEnumerable<OnlineUserResponse>> Handle(
        GetOnlineUsersQuery request, CancellationToken cancellationToken)
    {
        var online = await db.CurrentSessions
            .AsNoTracking()
            .Where(s => s.ApplicationId == request.ApplicationId
                     && s.UserType      != "External")
            .Join(db.Users,
                  s => s.UserId,
                  u => u.Id,
                  (s, u) => new OnlineUserResponse
                  {
                      UserId      = u.Id,
                      DisplayName = (u.FirstName + " " + u.LastName).Trim(),
                      UserType    = s.UserType,
                      ConnectedAt = s.ConnectedAt
                  })
            .OrderBy(x => x.UserType)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return online;
    }
}
