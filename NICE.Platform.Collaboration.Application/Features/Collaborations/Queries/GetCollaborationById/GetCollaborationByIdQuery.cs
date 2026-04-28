namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetCollaborationById;
using MediatR;
public record GetCollaborationByIdQuery(Guid Id) : IRequest<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse?>;
