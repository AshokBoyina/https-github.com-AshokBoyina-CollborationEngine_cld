namespace NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableSupervisors;
using MediatR;
public record GetAvailableSupervisorsQuery(Guid ApplicationId) : IRequest<IEnumerable<NICE.Platform.Collaboration.Core.Responses.SessionResponse>>;
