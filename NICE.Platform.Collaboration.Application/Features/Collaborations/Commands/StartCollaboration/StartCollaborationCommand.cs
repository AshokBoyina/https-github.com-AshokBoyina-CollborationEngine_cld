namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;
using MediatR;
public record StartCollaborationCommand(Guid UserId, Guid AgentId, Guid ApplicationId) : IRequest<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse>;
