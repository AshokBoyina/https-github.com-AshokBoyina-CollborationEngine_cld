namespace NICE.Platform.Collaboration.Application.Features.Users.Queries.GetOnlineUsers;

using MediatR;
using NICE.Platform.Collaboration.Core.Responses;

/// <summary>
/// Returns all non-External users (Agents, Supervisors, Internal) currently connected
/// to the SignalR hub for the given application.
/// Backed by CurrentSessions — rows exist only while the hub connection is live.
/// </summary>
public record GetOnlineUsersQuery(Guid ApplicationId)
    : IRequest<IEnumerable<OnlineUserResponse>>;
