namespace NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableAgents;
using MediatR;
public record GetAvailableAgentsQuery(Guid ApplicationId) : IRequest<IEnumerable<NICE.Platform.Collaboration.Core.Responses.SessionResponse>>;
