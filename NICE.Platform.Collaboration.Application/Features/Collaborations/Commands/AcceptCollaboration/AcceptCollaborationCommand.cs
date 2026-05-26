namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.AcceptCollaboration;
using MediatR;
public record AcceptCollaborationCommand(
    Guid CollaborationId,
    Guid AgentId)
    : IRequest<NICE.Platform.Collaboration.Core.Responses.CollaborationResponse>;
