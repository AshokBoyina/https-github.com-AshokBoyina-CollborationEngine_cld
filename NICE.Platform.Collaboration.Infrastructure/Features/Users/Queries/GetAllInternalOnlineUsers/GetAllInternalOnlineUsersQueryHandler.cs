namespace NICE.Platform.Collaboration.Infrastructure.Features.Users.Queries.GetAllInternalOnlineUsers;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAllInternalOnlineUsers;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

/// <summary>
/// Returns all non-External users connected across every application.
/// Joins CurrentSessions → Users → Applications to resolve both display name
/// and the application name, so the caller can group staff by their application.
/// </summary>
public sealed class GetAllInternalOnlineUsersQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetAllInternalOnlineUsersQuery, IEnumerable<OnlineUserResponse>>
{
    public async Task<IEnumerable<OnlineUserResponse>> Handle(
        GetAllInternalOnlineUsersQuery request, CancellationToken cancellationToken)
    {
        var online = await db.CurrentSessions
            .AsNoTracking()
            .Include(s => s.Application)
            .Include(s => s.User)
            .Where(s => s.UserType != "External")
            .Select(s => new OnlineUserResponse
            {
                UserId          = s.User.Id,
                DisplayName     = (s.User.FirstName + " " + s.User.LastName).Trim(),
                UserType        = s.UserType,
                ConnectedAt     = s.ConnectedAt,
                ApplicationName = s.Application.Name
            })
            .OrderBy(x => x.ApplicationName)
            .ThenBy(x => x.UserType)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return online;
    }
}
