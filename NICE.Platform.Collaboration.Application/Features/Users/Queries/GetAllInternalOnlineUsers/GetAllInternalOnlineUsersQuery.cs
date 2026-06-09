namespace NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAllInternalOnlineUsers;

using MediatR;
using NICE.Platform.Collaboration.Core.Responses;

/// <summary>
/// Returns ALL non-External users (Agents, Supervisors, Internal) currently connected
/// to the SignalR hub across every application — used by the global Internal Chat
/// directory so staff from different applications can see and message each other.
/// Backed by CurrentSessions — rows exist only while the hub connection is live.
/// </summary>
public record GetAllInternalOnlineUsersQuery
    : IRequest<IEnumerable<OnlineUserResponse>>;
