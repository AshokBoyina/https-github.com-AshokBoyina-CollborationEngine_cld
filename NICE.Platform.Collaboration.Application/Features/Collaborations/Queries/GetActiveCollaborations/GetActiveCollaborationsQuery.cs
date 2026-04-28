namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetActiveCollaborations;
using MediatR;
public record GetActiveCollaborationsQuery(Guid ApplicationId) : IRequest<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse>>;
